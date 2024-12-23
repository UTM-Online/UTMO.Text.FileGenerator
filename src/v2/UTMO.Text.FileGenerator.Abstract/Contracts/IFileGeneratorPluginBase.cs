namespace UTMO.Text.FileGenerator.Abstract.Contracts;

public interface IFileGeneratorPluginBase
{
    TimeSpan MaxRuntime { get; }
}