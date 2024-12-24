namespace UTMO.Text.FileGenerator.Extensions;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using UTMO.Text.FileGenerator.Constants;

[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "API Surface, must remain public for consumers")]
[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "API Surface, must remain public for consumers")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "API Surface, must remain public for consumers")]
public static class FeatureManagement
{
    public static IConfigurationBuilder EnableFeature(this IConfigurationBuilder configBuilder, string featureName)
    {
        configBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>($"FeatureManagement:{featureName}", "true")]);
        return configBuilder;
    }
    
    public static IConfigurationBuilder DisableFeature(this IConfigurationBuilder configBuilder, string featureName)
    {
        configBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>($"FeatureManagement:{featureName}", "false")]);
        return configBuilder;
    }
    
    public static IConfiguration EnableFeature(this IConfiguration config, string featureName)
    {
        config[$"FeatureManagement:{featureName}"] = "true";
        return config;
    }
    
    public static IConfiguration DisableFeature(this IConfiguration config, string featureName)
    {
        config[$"FeatureManagement:{featureName}"] = "false";
        return config;
    }
    
    public static IConfigurationBuilder EnableParallelPropertyRendering(this IConfigurationBuilder configBuilder) => configBuilder.EnableFeature(FeatureFlags.EnableParallelPropertyRendering);
    
    public static IConfigurationBuilder DisableParallelPropertyRendering(this IConfigurationBuilder configBuilder) => configBuilder.DisableFeature(FeatureFlags.EnableParallelPropertyRendering);
    
    public static IConfiguration EnableParallelPropertyRendering(this IConfiguration config) => config.EnableFeature(FeatureFlags.EnableParallelPropertyRendering);
    
    public static IConfiguration DisableParallelPropertyRendering(this IConfiguration config) => config.DisableFeature(FeatureFlags.EnableParallelPropertyRendering);
    
    public static IConfigurationBuilder EnableParallelResourceRendering(this IConfigurationBuilder configBuilder) => configBuilder.EnableFeature(FeatureFlags.EnableParallelResourceRendering);
    
    public static IConfigurationBuilder DisableParallelResourceRendering(this IConfigurationBuilder configBuilder) => configBuilder.DisableFeature(FeatureFlags.EnableParallelResourceRendering);
    
    public static IConfiguration EnableParallelResourceRendering(this IConfiguration config) => config.EnableFeature(FeatureFlags.EnableParallelResourceRendering);
    
    public static IConfiguration DisableParallelResourceRendering(this IConfiguration config) => config.DisableFeature(FeatureFlags.EnableParallelResourceRendering);
}