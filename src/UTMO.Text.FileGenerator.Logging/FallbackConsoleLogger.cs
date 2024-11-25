namespace UTMO.Text.FileGenerator.Logging;

using Serilog.Events;
using UTMO.Text.FileGenerator.Abstract;
using static System.Console;

public class FallbackConsoleLogger : IGeneratorLogger
{
    private static void WriteColorConsoleMessage(LogLevel? level, string message, object[]? args = null)
    {
        var init = ForegroundColor;
        Console.Write("[");
        ForegroundColor = MapLogLevelToColor(level);
        Console.Write(level);
        ForegroundColor = init;
        Console.Write("] ");
        WriteLine(message, args);
    }
    
    private static ConsoleColor MapLogLevelToColor(LogLevel? level) =>
        level switch
        {
            LogLevel.Debug       => ConsoleColor.DarkGray,
            LogLevel.Verbose     => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning     => ConsoleColor.Yellow,
            LogLevel.Error       => ConsoleColor.Red,
            LogLevel.Fatal       => ConsoleColor.DarkRed,
            var _                => ConsoleColor.White,
        };

    public void Write(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Verbose:
                this.Verbose(logEvent.RenderMessage());
                break;
            case LogEventLevel.Debug:
                this.Debug(logEvent.RenderMessage());
                break;
            case LogEventLevel.Information:
                this.Information(logEvent.RenderMessage());
                break;
            case LogEventLevel.Warning:
                this.Warning(logEvent.RenderMessage());
                break;
            case LogEventLevel.Error:
                this.Error(logEvent.RenderMessage());
                break;
            case LogEventLevel.Fatal:
                this.Fatal(logEvent.RenderMessage());
                break;
            default:
                WriteColorConsoleMessage(null, logEvent.RenderMessage());
                break;
        }
    }

    public void Debug(string message, params object[]? args)
    {
        WriteColorConsoleMessage(LogLevel.Debug, message, args);
    }

    public void Verbose(string message, params object[]? args)
    {
        WriteColorConsoleMessage(LogLevel.Verbose, message, args);
    }

    public void Information(string message, params object[]? args)
    {
        WriteColorConsoleMessage(LogLevel.Information, message, args);
    }

    public void Warning(string message, params object[]? args)
    {
        WriteColorConsoleMessage(LogLevel.Warning, message, args);
    }

    public void Error(string message, params object[]? args)
    {
        WriteColorConsoleMessage(LogLevel.Error, message, args);
    }

    public void Fatal(string message, bool shouldExit = true, int exitCode = 1, params object[]? args)
    {
        WriteColorConsoleMessage(LogLevel.Fatal, message, args);
        
        if (shouldExit)
        {
            Environment.Exit(exitCode);
        }
    }
}