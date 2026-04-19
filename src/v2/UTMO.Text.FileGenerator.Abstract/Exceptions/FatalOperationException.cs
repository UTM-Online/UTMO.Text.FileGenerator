namespace UTMO.Text.FileGenerator.Abstract.Exceptions;

/// <summary>
/// Represents a fatal error that should stop the generation pipeline,
/// carrying the intended process exit code instead of calling Environment.Exit().
/// </summary>
public class FatalOperationException : ApplicationException
{
    /// <summary>
    /// The exit code that would have been passed to Environment.Exit().
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="FatalOperationException"/>.
    /// </summary>
    public FatalOperationException(int exitCode, string messageTemplate, params object[] args)
        : base(FormatMessage(messageTemplate, args))
    {
        this.ExitCode = exitCode;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FatalOperationException"/> with an inner exception.
    /// </summary>
    public FatalOperationException(int exitCode, Exception innerException, string messageTemplate, params object[] args)
        : base(FormatMessage(messageTemplate, args), innerException)
    {
        this.ExitCode = exitCode;
    }

    private static string FormatMessage(string template, object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return $"{template} [{string.Join(", ", args)}]";
        }
    }
}

