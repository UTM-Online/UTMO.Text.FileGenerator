namespace UTMO.Text.FileGenerator.Constants;

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
}