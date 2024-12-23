namespace UTMO.Text.FileGenerator.Logging;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class GeneratorLoggerBuilder
{
    public static ILoggingBuilder AddGeneratorLogger(this ILoggingBuilder builder)
    {
        
        return builder;
    }
}