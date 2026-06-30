namespace Ferret.Core.Ai.Models;

/// <summary>Strongly-typed model identifier in the format "provider/model-name" (e.g. "ollama/llama3.2").</summary>
public readonly record struct ModelId
{
    private ModelId(string value) => Value = value;

    /// <summary>Gets the raw string value.</summary>
    public string Value { get; }

    /// <summary>Gets the provider prefix — the segment before the first '/'.</summary>
    public string ProviderPrefix
    {
        get
        {
            var slash = Value.IndexOf('/', StringComparison.Ordinal);
            return slash < 0 ? Value : Value[..slash];
        }
    }

    /// <summary>Gets the local model name — the segment after the first '/'.</summary>
    public string LocalName
    {
        get
        {
            var slash = Value.IndexOf('/', StringComparison.Ordinal);
            return slash < 0 ? Value : Value[(slash + 1)..];
        }
    }

    /// <summary>Creates a <see cref="ModelId"/> from a raw string value.</summary>
    /// <param name="value">The raw identifier value in "provider/model" format.</param>
    /// <returns>A new <see cref="ModelId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ModelId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ModelId(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
