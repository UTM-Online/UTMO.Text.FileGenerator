namespace UTMO.Text.FileGenerator.Abstract.Contracts;

public interface IGeneratorLogger
{
    Task Debug(string message, params object[]? args);
    
    Task Verbose(string message, params object[]? args);
    
    Task Information(string message, params object[]? args);
    
    Task Warning(string message, params object[]? args);
    
    Task Error(string message, params object[]? args);
    
    Task Fatal(string message, bool shouldExit = true, int exitCode = 1, params object[]? args);
}