﻿using UTMO.Text.FileGenerator.Abstract.Attributes;

 namespace UTMO.Text.FileGenerator.Models;

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Attributes;
using UTMO.Text.FileGenerator.Constants;

public abstract class TemplateResourceBase : ITemplateModel, IManifestProducer
{
    // ReSharper disable once CollectionNeverUpdated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly Dictionary<string, object> TemplateConstants = new();

    protected internal IFeatureManager? FeatureManager { get; internal set; }
    
    protected internal ILogger? Logger { get; internal set; }

    public virtual bool GenerateManifest { get; } = false;

    public virtual Task<object?> ToManifest()
    {
        return Task.FromResult(null as object);
    }

    public abstract string ResourceTypeName { get; }

    public abstract string TemplatePath { get; }

    public abstract string OutputExtension { get; }

    public abstract string ResourceName { get; }

    public virtual bool EnableGeneration { get; } = true;

    public virtual bool UseAlternateName { get; } = false;

    public virtual Task<List<ValidationFailedException>> Validate()
    {
        return Task.FromResult(new List<ValidationFailedException>());
    }

    public virtual async Task<Dictionary<string, object>> ToTemplateContext()
    {
        var properties = new Dictionary<string, object>();

        foreach (var prop in this.GetProperties())
        {
            var propertyName  = prop.GetCustomAttribute<MemberNameAttribute>(true)?.Name ?? prop.Name;
            var propertyValue = prop.GetValue(this);

            switch (propertyValue)
            {
                case null:
                    continue;

                case TemplateResourceBase templateResource:
                    properties.Add(propertyName, await templateResource.ToTemplateContext());
                    break;

                case IEnumerable<TemplateResourceBase> resources:
                {
                    if (this.FeatureManager is not null && await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableParallelPropertyRendering))
                    {
                        var resourceList = new ConcurrentBag<Dictionary<string, object>>();
                        await Parallel.ForEachAsync(resources, async (resource, token) => { resourceList.Add(await resource.ToTemplateContext().WaitAsync(token)); });
                        properties.Add(propertyName, resourceList);
                    }
                    else
                    {
                        var resourceList = new List<Dictionary<string, object>>();

                        foreach (var resource in resources)
                        {
                            resourceList.Add(await resource.ToTemplateContext());
                        }

                        properties.Add(propertyName, resourceList);
                    }

                    break;
                }

                default:
                    properties.Add(propertyName, propertyValue);
                    break;
            }
        }

        // ReSharper disable once InvertIf
        if (this.TemplateConstants.Count != 0)
        {
            foreach (var prop in this.TemplateConstants)
            {
                properties.Add(prop.Key, prop.Value);
            }
        }

        return properties;
    }

    public virtual string ProduceOutputPath(string basePath)
    {
        var path = this.UseAlternateName
                       ? $"{this.ResourceName}.{this.ResourceTypeName}.{this.OutputExtension.TrimStart('.')}"
                       : $"{this.ResourceName}.{this.OutputExtension.TrimStart('.')}";
        return Path.Join(basePath, this.ResourceTypeName, path);
    }

    public virtual ITemplateModel AddAdditionalProperty<T>(string key, T value)
    {
        this.TemplateConstants.Add(key, value!);
        return this;
    }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        var allProperties = this.GetType()
                                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var prop in allProperties)
        {
            // Skip properties with IgnoreMemberAttribute
            if (prop.GetCustomAttributes<IgnoreMemberAttribute>(true).Any())
            {
                continue;
            }

            // Check if property has TemplatePropertyAttribute
            var hasTemplateProperty = prop.GetCustomAttribute<TemplatePropertyAttribute>(true) != null;
            var isPublic = prop.GetMethod?.IsPublic == true;

            // New secure behavior: Only expose properties with [TemplateProperty] attribute
            if (hasTemplateProperty && isPublic)
            {
                yield return prop;
                continue;
            }

            // Legacy/Deprecated behavior: Log warnings for non-public properties or public properties without attribute
            if (!isPublic && hasTemplateProperty)
            {
                // Non-public property with TemplateProperty attribute - this is not allowed
                this.Logger?.LogWarning(
                    "Property '{PropertyName}' on type '{TypeName}' is marked with [TemplateProperty] but is not public. " +
                    "Only public properties can be exposed to templates. This property will be ignored.",
                    prop.Name,
                    this.GetType().Name);
                continue;
            }

            if (!hasTemplateProperty && !isPublic)
            {
                // Non-public property without TemplateProperty attribute - log warning about deprecated behavior
                this.Logger?.LogWarning(
                    "Non-public property '{PropertyName}' on type '{TypeName}' is being exposed to templates without [TemplateProperty] attribute. " +
                    "This behavior is DEPRECATED and will be removed in a future version. " +
                    "To continue exposing this property, make it public and add the [TemplateProperty] attribute. " +
                    "This is a security risk as it may expose sensitive data.",
                    prop.Name,
                    this.GetType().Name);

                // For now, still yield the property to maintain backward compatibility
                yield return prop;
                continue;
            }

            if (!hasTemplateProperty && isPublic)
            {
                // Public property without TemplateProperty attribute
                // This is now opt-in, so we don't expose it anymore
                // Optionally log an info message for migration purposes
                this.Logger?.LogInformation(
                    "Public property '{PropertyName}' on type '{TypeName}' is not marked with [TemplateProperty] and will not be exposed to templates. " +
                    "Add [TemplateProperty] attribute if this property should be accessible in templates.",
                    prop.Name,
                    this.GetType().Name);
            }
        }
    }
}