namespace UTMO.Text.FileGenerator.Models;

/// <summary>
/// A lightweight singleton holder that captures the generation exit code.
/// Resolved before the host runs so the value remains readable after the host disposes.
/// </summary>
public class GenerationExitCodeHolder
{
    /// <summary>
    /// The exit code produced by the generation run. Defaults to success (0).
    /// </summary>
    public int ExitCode { get; set; }
}

