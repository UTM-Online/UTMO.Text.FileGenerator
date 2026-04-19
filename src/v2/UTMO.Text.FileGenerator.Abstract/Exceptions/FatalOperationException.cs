namespace UTMO.Text.FileGenerator.Abstract.Exceptions;

/// <summary>
/// Represents a fatal error that should stop the generation pipeline,
/// carrying the intended process exit code instead of calling Environment.Exit().
/// </summary>
public class FatalOperationException : Exception
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

    /// <summary>
    /// Formats the message template with the supplied args.
    /// Only attempts <see cref="string.Format"/> when the template contains composite-format
    /// positional placeholders (e.g. <c>{0}</c>). Named MEL-style placeholders (e.g. <c>{Name}</c>)
    /// are not passed to <see cref="string.Format"/>; instead the raw template is preserved and
    /// the args are appended for readability.
    /// </summary>
    private static string FormatMessage(string template, object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return template;
        }

        if (ContainsCompositeFormatPlaceholders(template))
        {
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                // Fall back when positional-placeholder count doesn't match args.
            }
        }

        return $"{template} [{FormatArguments(args)}]";
    }

    /// <summary>
    /// Returns <see langword="true"/> only when the template contains at least one positional
    /// composite-format placeholder (<c>{0}</c>, <c>{1:N2}</c>, etc.).
    /// Named placeholders such as <c>{ResourceName}</c> used by MEL are intentionally excluded.
    /// </summary>
    private static bool ContainsCompositeFormatPlaceholders(string template)
    {
        for (var index = 0; index < template.Length; index++)
        {
            if (template[index] != '{')
            {
                continue;
            }

            // Skip escaped braces "{{".
            if (index + 1 < template.Length && template[index + 1] == '{')
            {
                index++;
                continue;
            }

            var placeholderIndex = index + 1;
            if (placeholderIndex >= template.Length || !char.IsDigit(template[placeholderIndex]))
            {
                continue;
            }

            while (placeholderIndex < template.Length && char.IsDigit(template[placeholderIndex]))
            {
                placeholderIndex++;
            }

            if (placeholderIndex < template.Length &&
                (template[placeholderIndex] == '}' ||
                 template[placeholderIndex] == ':' ||
                 template[placeholderIndex] == ','))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatArguments(object[] args)
        => string.Join(", ", args.Select(a => a?.ToString() ?? "<null>"));
}
