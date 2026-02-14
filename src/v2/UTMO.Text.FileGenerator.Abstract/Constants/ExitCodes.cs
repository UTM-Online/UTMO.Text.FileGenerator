namespace UTMO.Text.FileGenerator.Abstract.Constants;

/// <summary>
/// Defines standard exit codes for the file generator application.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Exit code indicating successful execution.
    /// </summary>
    public const int Success = 0;
    
    /// <summary>
    /// Exit code indicating an unhandled exception occurred.
    /// </summary>
    public const int UnhandledException = 1;
    
    /// <summary>
    /// Exit code indicating file generation completed with errors.
    /// </summary>
    public const int GenerationErrors = 3;
    
    /// <summary>
    /// Exit code indicating the operation was cancelled.
    /// </summary>
    public const int Cancelled = 5;
    
    /// <summary>
    /// Exit code indicating validation failures were encountered.
    /// </summary>
    public const int ValidationFailure = 45;
    
    /// <summary>
    /// Exit code indicating exceptions were tracked during execution.
    /// </summary>
    public const int ExceptionsTracked = -315;
    
    /// <summary>
    /// Exit code indicating path normalization failed.
    /// </summary>
    public const int PathNormalizationError = -3828;
}
