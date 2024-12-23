namespace UTMO.Text.FileGenerator.Models;

public abstract class SubTemplateResourceBase : TemplateResourceBase
{
    /// <summary>
    ///     Gets the template path.
    /// </summary>
    /// <value>The template path.</value>
    public override string TemplatePath => "NaN";

    /// <summary>
    ///     Gets the name of the resource type.
    /// </summary>
    /// <value>The name of the resource type.</value>
    public override string ResourceTypeName => "NaN";

    /// <summary>
    ///     Gets the output extension.
    /// </summary>
    /// <value>The output extension.</value>
    public sealed override string OutputExtension => "NaN";

    /// <summary>
    ///     Gets the name of the resource.
    /// </summary>
    /// <value>The name of the resource.</value>
    public override string ResourceName => "NaN";
    
    /// <summary>
    ///     Adds the additional property.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>TemplateResourceBase.</returns>
    /// <exception cref="System.NotSupportedException">
    ///     This method is not supported for types derived from
    ///     {nameof(RelatedTemplateResourceBase)}.
    /// </exception>
    public override TemplateResourceBase AddAdditionalProperty<T>(string key, T value)
    {
        throw new NotSupportedException($"This method is not supported for types derived from {nameof(SubTemplateResourceBase)}.");
    }
}