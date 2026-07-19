namespace UTMO.Text.FileGenerator.Models;

using System.Collections.ObjectModel;
using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <inheritdoc cref="IGenerationScope"/>
public sealed class GenerationScope : IGenerationScope
{
    private static readonly IReadOnlyDictionary<string, string> EmptyCoordinates =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Initializes a new <see cref="GenerationScope"/>.
    /// </summary>
    /// <param name="environment">The mandatory environment dimension. Cannot be null/whitespace.</param>
    /// <param name="coordinates">
    /// Optional additional provider-declared coordinates (e.g. <c>{ "DataCenter", "EUS" }</c>).
    /// Coordinate names are matched case-insensitively.
    /// </param>
    public GenerationScope(string environment, IReadOnlyDictionary<string, string>? coordinates = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        this.Environment = environment;
        this.Coordinates = coordinates is { Count: > 0 }
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(coordinates, StringComparer.OrdinalIgnoreCase))
            : EmptyCoordinates;
    }

    /// <inheritdoc/>
    public string Environment { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Coordinates { get; }

    /// <summary>
    /// Creates a v1-parity scope containing only the <see cref="Environment"/> dimension.
    /// Resolution against this scope reproduces v1's environment-only behavior exactly.
    /// </summary>
    public static GenerationScope ForEnvironment(string environment) => new(environment);

    /// <inheritdoc/>
    public bool TryGetCoordinate(string dimension, out string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dimension);

        if (string.Equals(dimension, nameof(this.Environment), StringComparison.OrdinalIgnoreCase))
        {
            value = this.Environment;
            return true;
        }

        return this.Coordinates.TryGetValue(dimension, out value!);
    }

    /// <inheritdoc/>
    public string GetIdentifier()
    {
        if (this.Coordinates.Count == 0)
        {
            return this.Environment;
        }

        var orderedCoordinates = this.Coordinates
                                      .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                                      .Select(kvp => $"{kvp.Key}={kvp.Value}");

        return $"{this.Environment}/{string.Join('/', orderedCoordinates)}";
    }

    /// <inheritdoc/>
    public override string ToString() => this.GetIdentifier();
}
