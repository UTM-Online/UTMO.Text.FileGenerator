namespace UTMO.Text.FileGenerator.Models;

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.FeatureManagement;
using Serilog;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Attributes;
using UTMO.Text.FileGenerator.Constants;

public abstract class TemplateResourceBase : ITemplateModel, IManifestProducer
{
    public abstract string ResourceTypeName { get; }
    
    public abstract string TemplatePath { get; }

    public abstract string OutputExtension { get; }

    public abstract string ResourceName { get; }

    public bool EnableGeneration { get; set; }

    public bool UseAlternateName { get; set; }
    
    protected internal IFeatureManager? FeatureManager { get; internal set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly Dictionary<string, object> TemplateConstants = new();

    private IEnumerable<PropertyInfo> GetProperties()
    {
        var propertyBag = this.GetType()
                              .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                              .Where(x => !x.GetCustomAttributes<IgnoreMemberAttribute>(true).Any());

        foreach (var prop in propertyBag)
        {
            yield return prop;
        }
    }

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
                    if(this.FeatureManager is not null && await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableParallelPropertyRendering))
                    {
                        var resourceList = new ConcurrentBag<Dictionary<string, object>>();
                        await Parallel.ForEachAsync(resources, async (resource, token) =>
                        {
                            resourceList.Add(await resource.ToTemplateContext().WaitAsync(token));
                        });
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

    public abstract bool GenerateManifest { get; }

    public virtual Task<object?> ToManifest()
    {
        return Task.FromResult(null as object);
    }
}