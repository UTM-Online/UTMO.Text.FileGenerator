namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// A scope-bound, resolvable pointer to a manifest (Manifest v2 phase P1, gap G6). Splits the
/// scope-agnostic <c>ManifestReference</c> (the question — "which subject") from the concrete
/// <see cref="IGenerationScope"/> the question is being asked against (the answer's coordinates),
/// enabling cross-provider translation (<see cref="ITranslatableScope"/>) and BCDR-style targets
/// (<see cref="ManifestReferenceTarget"/>) without changing the reference's declaration.
/// </summary>
public abstract class ManifestReferenceInfo
{
    private static readonly IReadOnlyList<Exception> NoErrors = Array.Empty<Exception>();

    /// <summary>Optional observation sink notified whenever <see cref="GetManifest"/> resolves successfully.</summary>
    public static IManifestObservationSink? ObservationSink { get; set; }

    protected ManifestReferenceInfo(IManifestProvider provider, IGenerationScope scope, string subject, string? parentSubject)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        this.Provider = provider;
        this.Scope = scope;
        this.Subject = subject;
        this.ParentSubject = parentSubject;
    }

    /// <summary>The provider this reference resolves against.</summary>
    public IManifestProvider Provider { get; }

    /// <summary>The Generation Scope this reference resolves within.</summary>
    public IGenerationScope Scope { get; }

    /// <summary>The referenced manifest's subject identity.</summary>
    public string Subject { get; }

    /// <summary>The optional parent manifest subject that scopes <see cref="Subject"/>.</summary>
    public string? ParentSubject { get; }

    /// <summary>The expected manifest CLR type for this reference.</summary>
    public abstract Type GetManifestType();

    /// <summary>
    /// Fetches the referenced manifest (untyped), notifying <see cref="ObservationSink"/> when
    /// resolution succeeds. Returns <see langword="null"/> when the subject cannot be found or
    /// the stored value does not match <see cref="GetManifestType"/>.
    /// </summary>
    public abstract object? GetManifest();

    /// <summary>
    /// Validates that this reference resolves without throwing (Manifest v2 phase P2, gap G10).
    /// Returns an empty sequence when valid; otherwise one or more descriptive exceptions
    /// (dangling subject, type mismatch) suitable for aggregation into a pre-render validation
    /// failure.
    /// </summary>
    public virtual IEnumerable<Exception> ValidateNoThrow()
    {
        if (!this.Provider.HasManifestBySubject(this.Scope, this.Subject, this.ParentSubject))
        {
            return new Exception[]
            {
                new InvalidOperationException(
                    $"Dangling manifest reference: subject '{this.Subject}' " +
                    (this.ParentSubject is { } parent ? $"(parent '{parent}') " : string.Empty) +
                    $"was not found in scope '{this.Scope.GetIdentifier()}' via provider '{this.Provider.ProviderKind}'."),
            };
        }

        return NoErrors;
    }

    /// <summary>Returns a stable identifier combining provider/scope/type/subject, suitable for logging and ordering edges.</summary>
    public string GetIdentifier() =>
        this.ParentSubject is { } parent
            ? $"{this.Provider.ProviderKind}/{this.Scope.GetIdentifier()}/{this.GetManifestType().Name}/{parent}/{this.Subject}"
            : $"{this.Provider.ProviderKind}/{this.Scope.GetIdentifier()}/{this.GetManifestType().Name}/{this.Subject}";
}

/// <inheritdoc cref="ManifestReferenceInfo"/>
/// <typeparam name="TManifest">The expected manifest payload type.</typeparam>
public sealed class ManifestReferenceInfo<TManifest> : ManifestReferenceInfo
    where TManifest : class, IManifest
{
    private readonly string? _referrerIdentifier;

    public ManifestReferenceInfo(
        IManifestProvider provider,
        IGenerationScope scope,
        string subject,
        string? parentSubject = null,
        string? referrerIdentifier = null)
        : base(provider, scope, subject, parentSubject)
    {
        _referrerIdentifier = referrerIdentifier;
    }

    /// <inheritdoc/>
    public override Type GetManifestType() => typeof(TManifest);

    /// <summary>Fetches and casts the referenced manifest to <typeparamref name="TManifest"/>.</summary>
    public TManifest? GetTypedManifest()
    {
        if (!this.Provider.TryGetManifest(this.Scope, this.Subject, this.ParentSubject, out var raw))
        {
            return null;
        }

        if (raw is not TManifest typed)
        {
            return null;
        }

        if (_referrerIdentifier is { } referrer && ObservationSink is { } sink)
        {
            sink.OnResolved(new ManifestOrderingEdge(referrer, this.GetIdentifier()));
        }

        return typed;
    }

    /// <inheritdoc/>
    public override object? GetManifest() => this.GetTypedManifest();

    /// <inheritdoc/>
    public override IEnumerable<Exception> ValidateNoThrow()
    {
        if (!this.Provider.TryGetManifest(this.Scope, this.Subject, this.ParentSubject, out var raw))
        {
            return new Exception[]
            {
                new InvalidOperationException(
                    $"Dangling manifest reference: subject '{this.Subject}' " +
                    (this.ParentSubject is { } parent ? $"(parent '{parent}') " : string.Empty) +
                    $"of type '{typeof(TManifest).Name}' was not found in scope '{this.Scope.GetIdentifier()}' " +
                    $"via provider '{this.Provider.ProviderKind}'."),
            };
        }

        if (raw is not TManifest)
        {
            return new Exception[]
            {
                new InvalidOperationException(
                    $"Manifest reference type mismatch: subject '{this.Subject}' " +
                    (this.ParentSubject is { } parent ? $"(parent '{parent}') " : string.Empty) +
                    $"resolved to '{raw?.GetType().Name ?? "null"}' but reference expects '{typeof(TManifest).Name}'."),
            };
        }

        return Array.Empty<Exception>();
    }
}
