namespace UTMO.Text.FileGenerator.Abstract.Contracts;

public interface IPluginResolver
{
    T Resolve<T>();
    
    IGeneratorLogger ResolveLogger();
}