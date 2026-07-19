namespace UTMO.Text.FileGenerator.Models;

using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Base class for a manifest that is nested beneath a parent manifest (Manifest v2 phase P4,
/// gap G7), mirroring ConfigGen's typed subjects-chain nesting. Provider manifest base classes
/// (e.g. a future <c>DscConfigurationManifestBase</c>) can derive from this to expose their
/// parent manifest in a strongly typed way instead of only a raw <c>ParentSubject</c> string.
/// </summary>
/// <typeparam name="TParent">The parent manifest's identity type.</typeparam>
public abstract class NestedManifestBase<TParent> : ManifestBase, INestedManifest<TParent>
    where TParent : class, IManifest
{
    private readonly Func<TParent> _parentAccessor;

    /// <param name="parentAccessor">
    /// A callback that resolves the parent manifest, typically by calling back into an
    /// <see cref="IManifestProvider"/> with the parent's subject. Invoked lazily by
    /// <see cref="GetParentManifest"/> rather than eagerly at construction time so a large
    /// object graph is only walked when actually needed.
    /// </param>
    protected NestedManifestBase(Func<TParent> parentAccessor)
    {
        ArgumentNullException.ThrowIfNull(parentAccessor);
        this._parentAccessor = parentAccessor;
    }

    /// <inheritdoc/>
    public TParent GetParentManifest() => this._parentAccessor();
}
