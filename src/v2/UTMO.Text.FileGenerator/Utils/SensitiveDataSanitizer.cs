namespace UTMO.Text.FileGenerator.Utils;

using System.Text.RegularExpressions;

/// <summary>
/// Utility class for sanitizing sensitive data from template context dictionaries.
/// Detects and redacts common sensitive patterns like passwords, API keys, tokens, etc.
/// SECURITY: This is a basic pattern-based sanitizer. For comprehensive PII protection,
/// consider implementing more sophisticated scanning or external services.
/// </summary>
public static class SensitiveDataSanitizer
{
    /// <summary>
    /// Default placeholder for redacted sensitive values.
    /// </summary>
    public const string RedactedPlaceholder = "***REDACTED***";

    /// <summary>
    /// Compiled regex pattern for detecting credentials in URLs/connection strings (user:pass@host).
    /// Uses a timeout to prevent ReDoS attacks on untrusted input.
    /// </summary>
    private static readonly Regex CredentialPatternRegex = new(
        @"([a-zA-Z0-9_-]+):(.+)@",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Sensitive keywords that indicate a property contains sensitive data.
    /// </summary>
    private static readonly string[] SensitiveKeywords = new[]
    {
        "password",
        "secret",
        "key",
        "token",
        "credential",
        "connectionstring",
        "connection_string",
        "apikey",
        "api_key",
        "authorization",
        "auth",
        "bearer",
        "apiSecret",
        "api_secret",
        "privatekey",
        "private_key",
        "accesskey",
        "access_key",
        "secretkey",
        "secret_key",
        "clientsecret",
        "client_secret",
        "username",
        "user_name",
        "passwd",
        "pwd",
        "pass",
        "sql",
        "database",
        "db_",
        "oauth",
        "jwt",
        "kek",
        "dek",
        "encryption",
        "cipher"
    };


    /// <summary>
    /// Sanitizes a template context dictionary by redacting values for keys that match sensitive patterns.
    /// Creates a new dictionary without modifying the original.
    /// </summary>
    /// <param name="dict">The context dictionary to sanitize. Can be null.</param>
    /// <returns>A new dictionary with sensitive values replaced, or null if input is null.</returns>
    /// <remarks>
    /// This method is safe to call multiple times and does not modify the input dictionary.
    /// It returns a shallow copy, so nested objects maintain their original references.
    /// </remarks>
    public static Dictionary<string, object>? Sanitize(Dictionary<string, object>? dict)
    {
        if (dict == null)
        {
            return null;
        }

        var sanitized = new Dictionary<string, object>(dict.Count);

        foreach (var kvp in dict)
        {
            sanitized[kvp.Key] = IsSensitive(kvp.Key, kvp.Value) ? RedactedPlaceholder : kvp.Value;
        }

        return sanitized;
    }

    /// <summary>
    /// Gets only the keys from a context dictionary, useful for logging structure without values.
    /// </summary>
    /// <param name="dict">The context dictionary. Can be null.</param>
    /// <returns>List of keys, or empty list if dict is null.</returns>
    public static List<string> GetContextKeys(Dictionary<string, object>? dict)
    {
        return dict?.Keys.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Determines if a key-value pair should be considered sensitive.
    /// </summary>
    /// <param name="key">The property name to check.</param>
    /// <param name="value">The value (used for pattern matching if string).</param>
    /// <returns>True if the property is likely to contain sensitive data.</returns>
    private static bool IsSensitive(string key, object? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        // Check if key contains any sensitive keywords
        if (SensitiveKeywords.Any(keyword =>
            key.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Additional pattern matching for string values
        if (value is string stringValue)
        {
            // Check for common sensitive patterns if the value looks like it might contain secrets
            // (e.g., starts with common prefixes)
            if (stringValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                stringValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase) ||
                stringValue.Contains("=", StringComparison.Ordinal) && stringValue.Contains(";", StringComparison.Ordinal))
            {
                // Possibly a connection string or authorization header
                return true;
            }

            // Check for common credential patterns (user:pass@host format) with timeout protection
            try
            {
                if (CredentialPatternRegex.IsMatch(stringValue))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // On timeout, assume it might be sensitive (fail-secure)
                return true;
            }
        }

        return false;
    }
}

