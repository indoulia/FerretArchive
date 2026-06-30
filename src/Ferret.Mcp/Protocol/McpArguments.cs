#pragma warning disable SA1202 // internal factory placed after public API by design
namespace Ferret.Mcp.Protocol;

/// <summary>Ferret-owned container for MCP tool invocation arguments.</summary>
public sealed class McpArguments
{
    private readonly IReadOnlyDictionary<string, string> _values;

    internal McpArguments(IReadOnlyDictionary<string, string> values) => _values = values;

    /// <summary>Gets an empty argument set.</summary>
    public static McpArguments Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>Returns the string value for <paramref name="name"/>, or <see langword="null"/> if absent.</summary>
    /// <param name="name">Argument name.</param>
    /// <returns>The string value, or <see langword="null"/>.</returns>
    public string? GetString(string name) =>
        _values.TryGetValue(name, out var v) ? v : null;

    /// <summary>Returns the string value for <paramref name="name"/>, or throws if absent.</summary>
    /// <param name="name">Argument name.</param>
    /// <returns>The string value.</returns>
    public string GetRequiredString(string name) =>
        GetString(name) ?? throw new InvalidOperationException($"Required MCP argument '{name}' is missing.");

    /// <summary>Tries to parse an integer value for <paramref name="name"/>.</summary>
    /// <param name="name">Argument name.</param>
    /// <param name="value">Parsed value on success.</param>
    /// <returns><see langword="true"/> if the argument was found and parsed; otherwise <see langword="false"/>.</returns>
    public bool TryGetInt32(string name, out int value)
    {
        var s = GetString(name);
        return int.TryParse(s, out value);
    }

    internal static McpArguments From(params (string Key, string Value)[] pairs) =>
        new(pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));

    /// <summary>Creates an <see cref="McpArguments"/> from a dictionary of mixed-type values, converting each to its string representation.</summary>
    /// <param name="values">Key/value pairs where values are converted via <see cref="object.ToString"/>.</param>
    /// <returns>A new <see cref="McpArguments"/> instance.</returns>
    public static McpArguments FromDictionary(IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var dict = new Dictionary<string, string>(values.Count, StringComparer.Ordinal);
        foreach (var (k, v) in values)
        {
            if (v is not null)
            {
                dict[k] = v.ToString()!;
            }
        }

        return new McpArguments(dict);
    }
}
