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
        var generateManifest = false;
        logger.Information(LogMessage.GeneratingManifestForResource, resource.ResourceName, resource.ResourceTypeName);

        if (resource is {ResourceName: "NaN", ResourceTypeName: "NaN"} || resource.GenerateManifest == false)
        {
            if (resource.ResourceName == "NaN")
            {
                logger.Warning(LogMessage.SkippingManifestGenerationFiltered, nameof(resource.ResourceName), resource.ResourceName);
            }
            else if (resource.ResourceTypeName == "NaN")
            {
                logger.Warning(LogMessage.SkippingManifestGenerationFiltered, nameof(resource.ResourceTypeName), resource.ResourceTypeName);
            }
            else
            {
                logger.Warning(LogMessage.SkippingManifestGenerationFiltered, nameof(resource.GenerateManifest), resource.GenerateManifest);
            }
        }
        else
        {
            generateManifest = true;
        }

        logger.Verbose(LogMessage.ProcessingResource, resource.ResourceName, resource.ResourceTypeName);

        foreach (var propertyInfo in resource.GetType().GetProperties())
        {
            var propertyValue = propertyInfo.GetValue(resource);
            if (propertyValue is ITemplateModel innerResource)
            {
                if (innerResource.ResourceName != resource.ResourceName)
                {
                    innerResource.GenerateResourceManifest(manifestDict, logger);
                }
            }
            else if (propertyValue is IEnumerable<ITemplateModel> nestedResources)
            {
                foreach (var item in nestedResources)
                {
                    if (item.ResourceName != resource.ResourceName)
                    {
                        item.GenerateResourceManifest(manifestDict, logger);
                    }
                }
            }
        }

        if (generateManifest)
        {
            AddManifest(manifestDict, resource, logger);
        }

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