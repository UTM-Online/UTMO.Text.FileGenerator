using Microsoft.FeatureManagement;

namespace UTMO.Text.FileGenerator.ResourceManifestGeneration;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Constants;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using Formatting = Newtonsoft.Json.Formatting;

/// <summary>
/// A <see cref="IPipelinePlugin"/> that emits a portable **Manifest Package** describing every
/// subject-bearing manifest produced for an environment (Manifest v2 phase P3, gap G4): a
/// <c>manifest-package.json</c> index plus a generated <c>Subjects.g.cs</c> enum-like constants
/// class, so a consumer in another repository can build an <c>ArtifactManifestProvider</c>
/// against these files without a source dependency on the producing project.
/// </summary>
/// <remarks>
/// Gated by the <c>ManifestPackaging</c> feature flag, which is additive to (and requires)
/// <c>ManifestReferenceResolution</c>. Each package entry embeds its manifest payload directly
/// (mirroring ConfigGen's "a manifest is a dictionary" design tenet), so the package is
/// self-contained and can be consumed by <c>ArtifactManifestProvider</c> without depending on the
/// separate per-type <c>&lt;ResourceTypeName&gt;.Manifest.json</c> files emitted by
/// <see cref="ManifestPipelineProcessor"/> (which remain unchanged for existing consumers).
/// </remarks>
[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public sealed class ManifestPackageEmissionPlugin : IPipelinePlugin
{
    private const int SchemaVersion = 1;

    public ManifestPackageEmissionPlugin(
        IGeneralFileWriter writer,
        ILogger<ManifestPackageEmissionPlugin> logger,
        ILogger<ManifestPipelineProcessor> resourceWalkLogger,
        IFeatureManager featureManager)
    {
        this.Writer = writer;
        this.Logger = logger;
        this.ResourceWalkLogger = resourceWalkLogger;
        this.FeatureManager = featureManager;
    }

    public IGeneralFileWriter? Writer { get; init; }

    public ITemplateGenerationEnvironment? Environment { get; init; }

    public PluginPosition Position => PluginPosition.Before;

    public TimeSpan MaxRuntime => TimeSpan.FromMinutes(10);

    public bool RequiresGeneration => false;

    private ILogger<ManifestPackageEmissionPlugin> Logger { get; }

    private ILogger<ManifestPipelineProcessor> ResourceWalkLogger { get; }

    private IFeatureManager FeatureManager { get; }

    public async Task<bool> ProcessPlugin(ITemplateGenerationEnvironment environment)
    {
        try
        {
            if (!await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution) ||
                !await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableManifestPackaging))
            {
                this.Logger.LogDebug(
                    "Manifest packaging is disabled ({FeatureFlag}). Skipping package emission for environment '{EnvironmentName}'.",
                    FeatureFlags.EnableManifestPackaging,
                    environment.EnvironmentName);
                return true;
            }

            if (environment.GeneratorOptions is { GenerateManifest: false, GenerateManifestsOnly: false })
            {
                this.Logger.LogInformation("Skipping Manifest Package emission for environment '{EnvironmentName}': manifest generation is disabled.", environment.EnvironmentName);
                return true;
            }

            var resourceManifests = new List<(string ResourceTypeName, string ResourceName, IManifestProducer Producer)>();

            foreach (var resource in environment.Resources)
            {
                await resource.GenerateResourceManifest(resourceManifests, this.ResourceWalkLogger, this.FeatureManager);
            }

            var distinctProducers = resourceManifests
                                   .Where(r => !string.IsNullOrWhiteSpace(r.Producer.ManifestSubject))
                                   .DistinctBy(r => MakeSubjectDedupeKey(r.Producer.ManifestSubject!, r.Producer.ParentManifestSubject))
                                   .OrderBy(r => r.Producer.ManifestSubject, StringComparer.OrdinalIgnoreCase)
                                   .ToList();

            var entries = new List<ManifestPackageEntry>(distinctProducers.Count);

            foreach (var r in distinctProducers)
            {
                var manifestData = await r.Producer.ToManifest<IManifest>();
                entries.Add(new ManifestPackageEntry(
                    r.Producer.ManifestSubject!,
                    r.Producer.ParentManifestSubject,
                    r.ResourceTypeName,
                    r.ResourceName,
                    $"{ToManifestResourceTypeSafeName(r.ResourceTypeName)}.Manifest.json",
                    manifestData));
            }

            var manifestOutputPath = Path.Join(environment.GeneratorOptions.OutputPath, "Manifests");

            var package = new ManifestPackageDescriptor(
                SchemaVersion,
                "local",
                environment.EnvironmentName,
                DateTime.UtcNow,
                entries);

            var packageJson = JsonConvert.SerializeObject(package, Formatting.Indented);

            if (this.Writer is null)
            {
                this.Logger.LogError("No writer found for Manifest Package output");
                return false;
            }

            await this.Writer.WriteFile(Path.Join(manifestOutputPath, "manifest-package.json"), packageJson, environment.GeneratorOptions.AllowOverwrite);

            var subjectsSource = GenerateSubjectsSource(entries);
            await this.Writer.WriteFile(Path.Join(manifestOutputPath, "Subjects.g.cs"), subjectsSource, environment.GeneratorOptions.AllowOverwrite);

            this.Logger.LogInformation(
                "Manifest Package emitted for environment '{EnvironmentName}': {EntryCount} subject(s).",
                environment.EnvironmentName,
                entries.Count);

            return true;
        }
        catch (Exception e) when (IsRecoverableException(e))
        {
            this.Logger.LogError(e, "Error during Manifest Package emission");
            return false;
        }
    }

    /// <summary>
    /// Excludes exceptions that indicate a fatal, non-recoverable process state (e.g. out of
    /// memory, stack overflow) from the "log and return false" handling above, so those
    /// conditions bubble up instead of being silently swallowed.
    /// </summary>
    private static bool IsRecoverableException(Exception exception) =>
        exception is not (OutOfMemoryException or StackOverflowException
            or System.Threading.ThreadAbortException or AccessViolationException
            or AppDomainUnloadedException);

    private static string ToManifestResourceTypeSafeName(string resourceTypeName) => resourceTypeName.Split('/').Last();

    /// <summary>
    /// Builds a case-insensitive de-duplication key for a producer's (subject, parent subject)
    /// pair, treating a <see langword="null"/> or whitespace-only parent subject as equivalent
    /// to "no parent" (root scope). This mirrors <c>ManifestReferenceIndex.MakeSubjectKey</c>'s
    /// case-insensitive, root-scope-normalizing behavior so the emitted package cannot contain
    /// duplicate/ambiguous entries for what the in-memory index treats as the same manifest.
    /// </summary>
    private static string MakeSubjectDedupeKey(string subject, string? parentSubject)
    {
        var normalizedParent = string.IsNullOrWhiteSpace(parentSubject) ? string.Empty : parentSubject.Trim();
        return $"{normalizedParent}|{subject.Trim()}".ToUpperInvariant();
    }

    private static string GenerateSubjectsSource(IReadOnlyList<ManifestPackageEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// Generated by UTMO.Text.FileGenerator Manifest Package emission (Manifest v2 phase P3).");
        builder.AppendLine("// Do not edit this file directly - regenerate it instead.");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine();
        builder.AppendLine("namespace ManifestPackage.Generated;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>Typo-proof constants for every manifest subject known to this Manifest Package.</summary>");
        builder.AppendLine("public static class Subjects");
        builder.AppendLine("{");

        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            var identifier = ToIdentifier(entry.Subject, seenNames);
            var escapedIdentifier = CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;
            builder.AppendLine($"    public const string {escapedIdentifier} = \"{EscapeStringLiteral(entry.Subject)}\";");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string ToIdentifier(string subject, HashSet<string> seenNames)
    {
        var sb = new StringBuilder();

        foreach (var c in subject)
        {
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        var candidate = sb.ToString();

        if (candidate.Length == 0 || char.IsDigit(candidate[0]))
        {
            candidate = "_" + candidate;
        }

        var unique = candidate;
        var suffix = 1;

        while (!seenNames.Add(unique))
        {
            unique = $"{candidate}_{suffix++}";
        }

        return unique;
    }

    private static string EscapeStringLiteral(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>
    /// C# reserved keywords that would fail to compile as a bare identifier in the generated
    /// <c>Subjects.g.cs</c> class (e.g. a subject that normalizes to <c>class</c> or
    /// <c>namespace</c>). Escaped with a leading <c>@</c> in <see cref="GenerateSubjectsSource"/>
    /// so consumers can still reference the constant by its original (unescaped) name.
    /// </summary>
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while",
    };

    private sealed record ManifestPackageEntry(string Subject, string? ParentSubject, string ResourceTypeName, string ResourceName, string ManifestFile, IManifest? Manifest);

    private sealed record ManifestPackageDescriptor(int SchemaVersion, string ProviderKind, string Environment, DateTime GeneratedAtUtc, IReadOnlyList<ManifestPackageEntry> Entries);
}
