namespace Ferret.Core.Ai.Models;

/// <summary>Strongly-typed provider identifier (e.g. "ollama", "openai").</summary>
public readonly record struct ProviderId
{
    private ProviderId(string value) => Value = value;

    /// <summary>Gets the raw string value.</summary>
    public string Value { get; }

    /// <summary>Creates a <see cref="ProviderId"/> from a raw string value.</summary>
    /// <param name="value">The raw provider identifier.</param>
    /// <returns>A new <see cref="ProviderId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ProviderId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ProviderId(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
