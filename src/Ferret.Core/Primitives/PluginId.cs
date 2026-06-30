namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a plugin.</summary>
public sealed class PluginId : IEquatable<PluginId>
{
    private PluginId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="PluginId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="PluginId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static PluginId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new PluginId(value);
    }

    /// <inheritdoc/>
    public bool Equals(PluginId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PluginId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
