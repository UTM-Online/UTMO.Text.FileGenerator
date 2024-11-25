namespace UTMO.Text.FileGenerator.Abstract;

public interface IPluginResolver
{
    T Resolve<T>();
    
    IGeneratorLogger ResolveLogger();
}