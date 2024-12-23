using System.Reflection;

namespace UTMO.Text.FileGenerator.Utils;

public static class InterfaceImplementationsFinder
{
    public static IEnumerable<Type> FindImplementations<TInterface>(IEnumerable<Assembly> assemblies)
    {
        var interfaceType   = typeof(TInterface);
        var implementations = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            implementations.AddRange(types);
        }

        return implementations;
    }
}