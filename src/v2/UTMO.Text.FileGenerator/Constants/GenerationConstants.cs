namespace UTMO.Text.FileGenerator.Constants;

/// <summary>
/// Defines constants used throughout the file generation process.
/// </summary>
public static class GenerationConstants
{
    /// <summary>
    /// The delay in milliseconds before retrying validation when null resources are detected.
    /// This allows time for asynchronously loaded resources to be populated.
    /// </summary>
    public const int ValidationRetryDelayMs = 100;
    
    /// <summary>
    /// The maximum number of additional validation retry attempts (0 = try once with no retries, 1 = try once then retry once).
    /// </summary>
    public const int MaxValidationRetryAttempts = 1;
    
    /// <summary>
    /// The file extension for Liquid template files.
    /// </summary>
    public const string LiquidTemplateExtension = ".liquid";
}
