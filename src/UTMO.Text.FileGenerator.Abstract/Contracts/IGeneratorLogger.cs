namespace UTMO.Text.FileGenerator.Abstract;

using Serilog.Events;

public interface IGeneratorLogger
{
    void Write(LogEvent logEvent);
    
    void Debug(string message, params object[]? args);
    
    void Verbose(string message, params object[]? args);
    
    void Information(string message, params object[]? args);
    
    void Warning(string message, params object[]? args);
    
    void Error(string message, params object[]? args);
    
    void Fatal(string message, bool shouldExit = true, int exitCode = 1, params object[]? args);
}