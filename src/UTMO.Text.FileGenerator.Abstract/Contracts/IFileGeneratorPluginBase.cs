namespace UTMO.Text.FileGenerator.Abstract;

public interface IFileGeneratorPluginBase
{
    TimeSpan MaxRuntime { get; }
}