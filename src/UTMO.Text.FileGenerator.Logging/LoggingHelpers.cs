namespace UTMO.Text.FileGenerator.Logging;

using Serilog;
using Serilog.Formatting.Json;
using UTMO.Text.FileGenerator.Abstract;

public static class LoggingHelpers
{
    public static LoggerConfiguration GetDefaultConfiguration(this ITemplateGenerationEnvironment _)
    {
        return new LoggerConfiguration()
           .WriteTo.Console();
    }
    
    public static LoggerConfiguration ConfigureJsonFileLogging(this LoggerConfiguration cfg, string filePath)
    {
        return cfg
            .WriteTo.File(new JsonFormatter(), filePath, rollingInterval: RollingInterval.Minute);
    }
}