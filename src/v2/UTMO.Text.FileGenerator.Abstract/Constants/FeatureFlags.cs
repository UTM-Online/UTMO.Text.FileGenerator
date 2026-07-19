namespace UTMO.Text.FileGenerator.Abstract.Constants;

public static class FeatureFlags
{
    public const string EnableParallelPropertyRendering = "ParallelPropertyRendering";

    /// <summary>
    /// The feature flag key for enabling parallel template rendering.
    /// <para>
    /// <b>Migration note:</b> The previous value of this constant was <c>"ParallelResourceRendering"</c>, which
    /// did not match the key in <c>FeatureFlights.manifest.json</c> (<c>"ParallelTemplateRendering"</c>) and
    /// therefore never functioned when used through this constant. The value has been corrected to
    /// <c>"ParallelTemplateRendering"</c>. If you have existing configuration files that explicitly set
    /// <c>ParallelResourceRendering</c>, update those entries to <c>ParallelTemplateRendering</c>.
    /// </para>
    /// </summary>
    public const string EnableParallelResourceRendering = "ParallelTemplateRendering";

    public const string EnableLegacyNonPublicTemplateProperties = "LegacyNonPublicTemplateProperties";

    /// <summary>
    /// The feature flag key for suppressing warning messages about non-public properties that are not marked
    /// with <c>[TemplateProperty]</c>. When enabled, the per-property warnings that are emitted by default
    /// (when <see cref="EnableLegacyNonPublicTemplateProperties"/> is disabled) are silenced. Enable this flag
    /// when non-public properties are intentional and the migration warnings are no longer needed.
    /// </summary>
    public const string SuppressNonPublicPropertyWarnings = "SuppressNonPublicPropertyWarnings";

    /// <summary>
    /// The feature flag key for enabling manifest-reference resolution.
    /// When enabled, resources that declare manifest references via
    /// <c>TemplateResourceBase.AddManifestReference</c> will have those references resolved
    /// from the in-memory manifest index before each template render.  Required references
    /// that cannot be resolved will cause generation to fail; optional references fall back
    /// to the declared default value.
    /// </summary>
    /// <remarks>
    /// This flag is <c>false</c> by default.  Enable it once the referenced resources
    /// produce manifests (i.e. have <c>GenerateManifest = true</c>) and you have validated
    /// the reference declarations in at least one generation run.
    /// </remarks>
    public const string EnableManifestReferenceResolution = "ManifestReferenceResolution";

    /// <summary>
    /// The feature flag key for enabling the pre-render manifest reference validation pass
    /// (Manifest v2 phase P2, gaps G3/G10). Requires <see cref="EnableManifestReferenceResolution"/>
    /// to also be enabled. When enabled, all declared manifest references are checked for
    /// dangling subjects and type mismatches before any template is rendered; failures stop
    /// generation with a descriptive error instead of silently falling back to
    /// <c>ManifestReference.DefaultValue</c> or surfacing mid-render.
    /// </summary>
    public const string EnableManifestReferenceValidation = "ManifestReferenceValidation";

    /// <summary>
    /// The feature flag key for enabling portable Manifest Package emission (Manifest v2 phase
    /// P3, gap G4). When enabled, in addition to the existing per-type manifest JSON files, the
    /// framework emits a <c>manifest-package.json</c> index and a generated <c>Subjects.g.cs</c>
    /// enum of known subjects, suitable for consumption by other projects via
    /// <c>ArtifactManifestProvider</c>.
    /// </summary>
    public const string EnableManifestPackaging = "ManifestPackaging";

    /// <summary>
    /// The feature flag key for enabling multi-coordinate Generation Scopes (Manifest v2 phase
    /// P4, gaps G2/G7/G8/G9). When disabled, scopes are Environment-only and reproduce v1 output
    /// byte-for-byte. When enabled, provider-declared coordinates (e.g. DataCenter), nested
    /// manifest resolution, dependency-ordering observation, and scope translation are active.
    /// </summary>
    public const string EnableGenerationScopes = "GenerationScopes";
}