namespace UTMO.Text.FileGenerator.Logging;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using UTMO.Text.FileGenerator.Abstract;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public class FileGeneratorLogger : IGeneratorLogger
{
    public FileGeneratorLogger()
    {
        this.Logger = new LoggerConfiguration()
                            .WriteTo.Console()
                     .MinimumLevel.Verbose()
                            .CreateLogger();
    }

    private Logger Logger { get; }

    public void Write(LogEvent logEvent)
    {
        this.Logger.Write(logEvent);
    }

    public void Debug(string message, params object[]? args)
    {
        this.Logger.Debug(message, args);
    }

    public void Verbose(string message, params object[]? args)
    {
        this.Logger.Verbose(message, args);
    }

    public void Information(string message, params object[]? args)
    {
        this.Logger.Information(message, args);
    }

    public void Warning(string message, params object[]? args)
    {
        this.Logger.Warning(message, args);
    }

    public void Error(string message, params object[]? args)
    {
        this.Logger.Error(message, args);
    }

    public void Fatal(string message, bool shouldExit = true, int exitCode = 1, params object[]? args)
    {
        this.Logger.Fatal(message, args);
        
        if (shouldExit)
        {
            Environment.Exit(exitCode);
        }
    }
}