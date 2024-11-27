// // ***********************************************************************
// // Assembly         : UTMO.Text.FileGenerator
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 2:10 PM
// // ***********************************************************************
// // <copyright file="ManifestHelpers.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Manifests;

using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Messages;

internal static class ManifestHelpers
{
    private static string ToManifestResourceTypeSafeName(this ITemplateModel resource)
    {
        return resource.ResourceTypeName.Split('/').Last();
    }

    internal static void GenerateResourceManifest(this ITemplateModel resource, Dictionary<string, List<ITemplateModel>> manifestDict, IGeneratorLogger logger)
    {
        // Using reflection find all properties that inherit from RelatedTemplateResourceBase class and call GenerateResourceManifest on them
        var props = resource.GetType().GetProperties().Where(p => p.PropertyType.IsSubclassOf(typeof(RelatedTemplateResourceBase)));
        
        foreach (var prop in props)
        {
            var propValue = prop.GetValue(resource) as RelatedTemplateResourceBase;
            propValue?.GenerateResourceManifest(manifestDict, logger);
        }
        
        if (resource is {ResourceName: "NaN", ResourceTypeName: "NaN"} || resource.GenerateManifest == false)
        {
            return;
        }

        logger.Verbose(LogMessage.ProcessingResource, resource.ResourceName, resource.ResourceTypeName);

        foreach (var propertyInfo in resource.GetType().GetProperties())
        {
            var propertyValue = propertyInfo.GetValue(resource);
            if (propertyValue is ITemplateModel innerResource)
            {
                innerResource.GenerateResourceManifest(manifestDict, logger);
            }
            else if (propertyValue is IEnumerable<ITemplateModel> nestedResources)
            {
                foreach (var item in nestedResources)
                {
                    item.GenerateResourceManifest(manifestDict, logger);
                }
            }
        }

        AddManifest(manifestDict, resource, logger);
        var manifestCount = manifestDict.Sum(x => x.Value.Count);
        logger.Information(LogMessage.ManifestGenerationCompleate, manifestCount);
    }

    private static void AddManifest(Dictionary<string, List<ITemplateModel>> manifestDict, ITemplateModel resource, IGeneratorLogger logger)
    {
        if (manifestDict.TryGetValue(resource.ToManifestResourceTypeSafeName(), out var value))
        {
            if (value.Any(x => x.ResourceName == resource.ResourceName))
            {
                logger.Warning(LogMessage.SkippingDuplicateResourceDefinition, resource.ResourceName, resource.ResourceTypeName);
                return;
            }

            manifestDict[resource.ToManifestResourceTypeSafeName()].Add(resource);
        }
        else
        {
            manifestDict.Add(resource.ToManifestResourceTypeSafeName(), new List<ITemplateModel> {resource});
        }
    }
}