namespace UTMO.Text.FileGenerator.Utils;

using Microsoft.Extensions.Logging;

public static class LoggingHelpers
{
    public static void Fatal<T>(this ILogger<T> logger, string message, bool terminate, int eventId, params object[] args) where T : class
    {
        logger.LogCritical(message, args);
        
        if (terminate)
        {
            throw new ApplicationException($"Fatal error (event {eventId}): {message}");
        }
    }
    
    public static void Fatal<T>(this ILogger<T> logger, Exception exception, string message, bool terminate, int eventId, params object[] args) where T : class
    {
        logger.LogCritical(exception, message, args);
        
        if (terminate)
        {
            throw new ApplicationException($"Fatal error (event {eventId}): {message}", exception);
        }
    }
    
    public static void Fatal(this ILogger logger, string message, bool terminate, int eventId, params object[] args)
    {
        logger.LogCritical(message, args);
        
        if (terminate)
        {
            throw new ApplicationException($"Fatal error (event {eventId}): {message}");
        }
    }
    
    public static void Fatal(this ILogger logger, Exception exception, string message, bool terminate, int eventId, params object[] args)
    {
        logger.LogCritical(exception, message, args);
        
        if (terminate)
        {
            throw new ApplicationException($"Fatal error (event {eventId}): {message}", exception);
        }
    }
}