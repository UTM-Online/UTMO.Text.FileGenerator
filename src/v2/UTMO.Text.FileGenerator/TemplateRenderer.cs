namespace UTMO.Text.FileGenerator;

using Abstract.Exceptions;
using Constants;
using DotLiquid;
using Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

/// <summary>
/// Renders Liquid templates to generate text files with support for global context injection.
/// </summary>
public class TemplateRenderer : ITemplateRenderer
{
    /// <summary>Default render timeout in seconds when not specified in configuration.</summary>
    private const int DefaultRenderTimeoutSeconds = 30;

    /// <summary>Default maximum output size in bytes (10 MB) when not specified in configuration.</summary>
    private const int DefaultMaxOutputSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateRenderer"/> with default (empty) configuration.
    /// Use this overload when constructing outside of a DI container.
    /// </summary>
    public TemplateRenderer(IGeneratorCliOptions options, IGeneralFileWriter fileWriter, ILogger<TemplateRenderer> logger)
        : this(options, fileWriter, logger, new ConfigurationBuilder().Build())
    {
    }

    public TemplateRenderer(IGeneratorCliOptions options, IGeneralFileWriter fileWriter, ILogger<TemplateRenderer> logger, IConfiguration configuration)
    {
        this.FileWriter = fileWriter;
        this.GlobalContext = new Dictionary<string, object>();
        this.TemplatePath = options.TemplatePath;
        this.Logger = logger;
        this.Configuration = configuration;
    }
    
    /// <summary>
    /// Generates a file from a Liquid template using a dictionary context.
    /// </summary>
    /// <param name="templateName">The name of the template file (will auto-append .liquid if not present).</param>
    /// <param name="outputFileName">The full path where the generated file should be written.</param>
    /// <param name="dict">The data context for template rendering.</param>
    /// <exception cref="TemplateNotFoundException">Thrown when the template file cannot be found.</exception>
    /// <exception cref="TemplateRenderingException">Thrown when template rendering fails.</exception>
    /// <exception cref="NoGeneratedTextException">Thrown when the template produces no output.</exception>
    public async Task GenerateFile(string templateName, string outputFileName, Dictionary<string, object> dict)
    {
        if (!templateName.EndsWith(GenerationConstants.LiquidTemplateExtension))
        {
            templateName = string.Concat(templateName, GenerationConstants.LiquidTemplateExtension);
        }
        
        if (this.GlobalContext.Count > 0)
        {
            dict = dict.Merge(this.GlobalContext);
        }
        
        var templatePath = Path.Combine(this.TemplatePath, templateName);
        
        if (!File.Exists(templatePath))
        {
            this.Logger.LogError("Template {TemplateName} not found in {TemplateSearchPath}", templateName, this.TemplatePath);
            throw new TemplateNotFoundException(templateName, this.TemplatePath);
        }
        
        var templateText   = await File.ReadAllTextAsync(templatePath);
        var parsedTemplate = Template.Parse(templateText);

        var timeoutSeconds = this.Configuration.GetValue<int?>("TemplateRendering:TimeoutSeconds") ?? DefaultRenderTimeoutSeconds;
        var maxOutputBytes = this.Configuration.GetValue<int?>("TemplateRendering:MaxOutputSizeBytes") ?? DefaultMaxOutputSizeBytes;

        // Validate configuration values up front so any misconfiguration is surfaced immediately.
        if (timeoutSeconds < 1)
        {
            throw new TemplateRenderingException(
                $"Invalid TemplateRendering:TimeoutSeconds value: {timeoutSeconds}. Must be >= 1.",
                dict, outputFileName, templateName);
        }

        if (maxOutputBytes < 1)
        {
            throw new TemplateRenderingException(
                $"Invalid TemplateRendering:MaxOutputSizeBytes value: {maxOutputBytes}. Must be >= 1.",
                dict, outputFileName, templateName);
        }

        var timeoutMs = timeoutSeconds * 1000;

        // Use DotLiquid's native cooperative timeout (fires slightly before the outer CTS safety net)
        // so that the rendering thread is actually stopped, not merely abandoned on the ThreadPool.
        var dotLiquidTimeoutMs = Math.Max(100, timeoutMs - 500);

        // Outer CTS provides a belt-and-suspenders fallback in case DotLiquid's timeout is not triggered
        // (e.g., a tight busy loop with no DotLiquid tag boundaries).
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs + 1000));

        // SizeLimitedTextWriter enforces the output cap during rendering, aborting before the full
        // string is materialised in memory — rather than measuring it only after rendering completes.
        // Declared outside the try block so we can call ToString() after the await completes.
        // Lifetime is safe: WaitAsync guarantees the render task has produced its final value (or faulted)
        // before we proceed past the await, so the writer is read only after all writes are done.
        var sizeLimitedWriter = new SizeLimitedTextWriter(maxOutputBytes, dict, outputFileName, templateName);

        string results;

        try
        {
            var renderParams = new RenderParameters(CultureInfo.CurrentCulture)
            {
                LocalVariables  = Hash.FromDictionary(dict),
                ErrorsOutputMode = ErrorsOutputMode.Rethrow,
                Timeout         = dotLiquidTimeoutMs,
            };

            var renderTask = Task.Run(() => parsedTemplate.Render(sizeLimitedWriter, renderParams));
            await renderTask.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Safety-net outer timeout fired (DotLiquid's cooperative timeout did not interrupt in time).
            this.Logger.LogError(
                "Template rendering for {TemplateName} exceeded timeout of {TimeoutSeconds}s (outer safety-net CTS)",
                templateName, timeoutSeconds);
            throw new TemplateRenderingException(
                $"Template rendering timeout after {timeoutSeconds}s", dict, outputFileName, templateName);
        }
        catch (TimeoutException tex)
        {
            // DotLiquid's native cooperative timeout fired — the rendering thread was actually stopped.
            this.Logger.LogError(tex,
                "Template rendering for {TemplateName} timed out after {TimeoutSeconds}s (DotLiquid native)",
                templateName, timeoutSeconds);
            throw new TemplateRenderingException(
                $"Template rendering timeout after {timeoutSeconds}s", dict, outputFileName, templateName, tex);
        }
        catch (TemplateRenderingException)
        {
            // Re-throw size-limit exceptions originating from SizeLimitedTextWriter without wrapping.
            throw;
        }
        catch (Exception ex)
        {
            if (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                this.Logger.LogError("Template {TemplateName} not found in {TemplateSearchPath}", templateName, this.TemplatePath);
                throw new TemplateNotFoundException(templateName, this.TemplatePath);
            }
            
            this.Logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
            throw new TemplateRenderingException($"Failed to render template {templateName}", dict, outputFileName, templateName, ex);
        }

        results = sizeLimitedWriter.ToString();
        sizeLimitedWriter.Dispose();

        if (string.IsNullOrWhiteSpace(results))
        {
            var noGeneratedTextException = new NoGeneratedTextException(templateName, outputFileName);
            this.Logger.LogError(noGeneratedTextException, "No text generated for template {TemplateName} to {OutPutFileName}", templateName, outputFileName);
            throw noGeneratedTextException;
        }
        
        // Check for DotLiquid error messages in the output
        if (results.StartsWith("Liquid error: Error - Illegal template path"))
        {
            var invalidTemplatePathException = new InvalidTemplateDirectoryException(templateName, this.TemplatePath);
            this.Logger.LogError(invalidTemplatePathException, "Invalid template path for template {TemplateName} in {TemplateSearchPath}", templateName, this.TemplatePath);
            throw invalidTemplatePathException;
        }
        
        ValidateTemplateOutput(results, dict, outputFileName, templateName);
        
        await this.FileWriter.WriteFile(outputFileName, results);
    }

    /// <summary>
    /// Generates a file from a Liquid template using a strongly-typed model.
    /// </summary>
    /// <typeparam name="T">The model type implementing ITemplateModel.</typeparam>
    /// <param name="templateName">The name of the template file.</param>
    /// <param name="outputFileName">The full path where the generated file should be written.</param>
    /// <param name="model">The model instance to use for template rendering.</param>
    public async Task GenerateFile<T>(string templateName, string outputFileName, T model) where T : ITemplateModel
    {
        await this.GenerateFile(templateName, outputFileName, await model.ToTemplateContext());
    }

    /// <summary>
    /// Adds a key-value pair to the global template context that will be available in all template renderings.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    public void AddToGlobalContext(string key, object value)
    {
        this.GlobalContext.TryAdd(key, value);
    }

    public void AddToGlobalContext(Dictionary<string, object> dict)
    {
        foreach (var (key, value) in dict)
        {
            this.AddToGlobalContext(key, value);
        }
    }

    private static void ValidateTemplateOutput(string templateOutput, Dictionary<string,object> model, string outputPath, string templateName)
    {
        if (templateOutput == "Liquid error: Error - This liquid context does not allow includes")
        {
            throw new TemplateRenderingException("This liquid context does not allow includes", model, outputPath, templateName);
        }
    }

    // ReSharper disable once InconsistentNaming
    private readonly Dictionary<string, object> GlobalContext;
    
    private IGeneralFileWriter FileWriter { get; }
    
    private string TemplatePath { get; }
    
    private ILogger<TemplateRenderer> Logger { get; }

    private IConfiguration Configuration { get; }

    /// <summary>
    /// A <see cref="StringWriter"/> that tracks UTF-8 byte output and throws
    /// <see cref="TemplateRenderingException"/> as soon as the configured limit is exceeded,
    /// aborting the render before the full output is materialised in memory.
    /// </summary>
    private sealed class SizeLimitedTextWriter : StringWriter
    {
        private readonly int _maxBytes;
        private int _bytesWritten;
        private readonly Dictionary<string, object> _model;
        private readonly string _outputFileName;
        private readonly string _templateName;

        public SizeLimitedTextWriter(int maxBytes, Dictionary<string, object> model, string outputFileName, string templateName)
        {
            _maxBytes      = maxBytes;
            _model         = model;
            _outputFileName = outputFileName;
            _templateName  = templateName;
        }

        public override void Write(string? value)
        {
            if (value is null) return;
            CheckSizeLimit(Encoding.UTF8.GetByteCount(value));
            base.Write(value);
        }

        public override void Write(char value)
        {
            Span<char> chars = stackalloc char[1];
            chars[0] = value;
            CheckSizeLimit(Encoding.UTF8.GetByteCount(chars));
            base.Write(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (count == 0) return;
            CheckSizeLimit(Encoding.UTF8.GetByteCount(buffer, index, count));
            base.Write(buffer, index, count);
        }

        private void CheckSizeLimit(int additionalBytes)
        {
            _bytesWritten += additionalBytes;
            if (_bytesWritten > _maxBytes)
                throw new TemplateRenderingException(
                    $"Template output size {_bytesWritten} bytes exceeds maximum allowed size of {_maxBytes} bytes",
                    _model, _outputFileName, _templateName);
        }
    }
}