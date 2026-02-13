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
    /// The maximum number of validation retry attempts.
    /// </summary>
    public const int MaxValidationRetries = 1;
    
    /// <summary>
    /// The file extension for Liquid template files.
    /// </summary>
    public const string LiquidTemplateExtension = ".liquid";
}
