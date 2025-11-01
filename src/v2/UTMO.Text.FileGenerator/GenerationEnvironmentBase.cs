namespace UTMO.Text.FileGenerator;

using System.Diagnostics.CodeAnalysis;
using Abstract;
using Abstract.Contracts;
using Abstract.Exceptions;
using Common.Guards;
using Microsoft.Extensions.Configuration;

[SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract class GenerationEnvironmentBase : ITemplateGenerationEnvironment
{
    protected internal GenerationEnvironmentBase(IConfiguration configuration, IGeneratorCliOptions generatorOptions)
    {
        this.Configuration = configuration;
        this.GeneratorOptions = generatorOptions;
        this.InternalResources = new();
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    protected IConfiguration Configuration { get; }

    protected virtual List<ITemplateModel> InternalResources { get; }

    public IGeneratorCliOptions GeneratorOptions { get; }

    public virtual Dictionary<string, object> EnvironmentConstants { get; } = new();

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public async Task<List<ValidationFailedException>> Validate()
    {
        return await this.ValidateWithRetry(retryCount: 0);
    }

    private async Task<List<ValidationFailedException>> ValidateWithRetry(int retryCount)
    {
        var failures = new List<ValidationFailedException>();
        var hasNullResources = false;

        foreach (var resource in this.Resources)
        {
            if (resource == null)
            {
                hasNullResources = true;
                failures.Add(new("Resource", "ITemplateModel", ValidationFailureType.InvalidResource, "Resource is null"));
                continue;
            }

            var resourceFailures = await resource.Validate();

            if (resourceFailures != null)
            {
                failures.AddRange(resourceFailures);
            }
        }

        // If we found null resources and haven't retried yet, wait briefly and retry once
        if (hasNullResources && retryCount == 0)
        {
            await Task.Delay(100); // Brief delay to allow resources to be populated
            return await this.ValidateWithRetry(retryCount: 1);
        }

        return failures;
    }

    public virtual void Initialize()
    {
    }

    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public ITemplateGenerationEnvironment AddResource<T>(T resource) where T : ITemplateModel
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Guard.Requires<ArgumentNullException>(resource is not null, nameof(resource));

        this.InternalResources.Add(resource!);

        return this;
    }

    public ITemplateGenerationEnvironment AddResource<T>() where T : ITemplateModel, new()
    {
        var resource = new T();

        this.InternalResources.Add(resource);

        return this;
    }

    public ITemplateGenerationEnvironment AddResources<T>(IEnumerable<T> resources) where T : ITemplateModel
    {
        foreach (var resource in resources)
        {
            this.InternalResources.Add(resource);
        }

        return this;
    }

    public ITemplateGenerationEnvironment AddEnvironmentContext(string key, object value)
    {
        this.EnvironmentConstants.Add(key, value);

        return this;
    }

    public abstract string EnvironmentName { get; }

    public IReadOnlyList<ITemplateModel> Resources => this.InternalResources;
}