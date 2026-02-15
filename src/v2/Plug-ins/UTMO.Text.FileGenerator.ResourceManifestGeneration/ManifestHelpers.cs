﻿// // ***********************************************************************
// // Assembly         : UTMO.Text.FileGenerator
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 2:10 PM
// // ***********************************************************************
// // <copyright file="ManifestHelpers.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.ResourceManifestGeneration;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using static LogMessage;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
internal static class ManifestHelpers
{
    private static string ToManifestResourceTypeSafeName(this ITemplateModel resource)
    {
        return resource.ResourceTypeName.Split('/').Last();
    }

    internal static async Task GenerateResourceManifest(this ITemplateModel resource, List<(string ResourceTypeName, string ResourceName, IManifestProducer producer)> resourcesOut, ILogger<ManifestPipelineProcessor> logger)
    {
        var generateManifest = false;
        logger.LogTrace(GeneratingManifestForResource, resource.ResourceName, resource.ResourceTypeName);

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (resource is IManifestProducer {GenerateManifest: false} producer)
        {
            logger.LogDebug(SkippingManifestGenerationFiltered, resource.ResourceName, resource.ResourceTypeName, nameof(producer.GenerateManifest), producer.GenerateManifest);
        }
        else if (resource.ResourceName.Equals("NaN", StringComparison.OrdinalIgnoreCase) && resource.ResourceTypeName.Equals("NaN", StringComparison.OrdinalIgnoreCase) &&
                 resource is IManifestProducer {GenerateManifest: true} producer2)
        {
            logger.LogDebug(SkippingManifestGenerationFiltered, resource.ResourceName, resource.ResourceTypeName, nameof(producer2.GenerateManifest), producer2.GenerateManifest);
        }
        else
        {
            generateManifest = true;
        }

        logger.LogTrace(ProcessingResource, resource.ResourceName, resource.ResourceTypeName);

        foreach (var propertyInfo in resource.GetType().GetProperties())
        {
            var propertyValue = propertyInfo.GetValue(resource);
            if (propertyValue is ITemplateModel innerResource)
            {
                if (innerResource.ResourceName != resource.ResourceName)
                {
                    await innerResource.GenerateResourceManifest(resourcesOut, logger);
                }
            }
            else if (propertyValue is IEnumerable<ITemplateModel> nestedResources)
            {
                foreach (var item in nestedResources)
                {
                    if (item.ResourceName != resource.ResourceName)
                    {
                        await item.GenerateResourceManifest(resourcesOut, logger);
                    }
                }
            }
        }

        if (generateManifest && resource is IManifestProducer manifestProducer)
        {
            resourcesOut.Add((resource.ResourceTypeName, resource.ResourceName, manifestProducer));
        }
    }
}