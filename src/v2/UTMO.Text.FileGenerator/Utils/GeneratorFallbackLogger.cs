namespace UTMO.Text.FileGenerator.Utils;

public static class GeneratorFallbackLogger
{
    [Obsolete("Use Serilog's \"Log\" static class or ILogger instead")]
    public static void Log(string message)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        Serilog.Log.Information(message);
    }
}