namespace Ferret.Core.Connectors;

/// <summary>Configuration values for a connector instance. Internally a case-insensitive string dictionary.</summary>
public sealed class ConnectorConfiguration
{
    private readonly IReadOnlyDictionary<string, string> _values;

    /// <summary>Initializes a new instance of the <see cref="ConnectorConfiguration"/> class with no values.</summary>
    public ConnectorConfiguration()
        => _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new instance of the <see cref="ConnectorConfiguration"/> class.</summary>
    /// <param name="values">The initial key-value pairs.</param>
    public ConnectorConfiguration(IDictionary<string, string> values)
        => _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a shared empty configuration instance.</summary>
    public static ConnectorConfiguration Empty { get; } = new();

    /// <summary>Creates a <see cref="ConnectorConfiguration"/> from a dictionary.</summary>
    /// <param name="values">The key-value pairs to initialise from.</param>
    /// <returns>A new <see cref="ConnectorConfiguration"/> instance.</returns>
    public static ConnectorConfiguration FromDictionary(IDictionary<string, string> values)
        => new(values);

    /// <summary>Gets the value for the given key, or null if not present.</summary>
    /// <param name="key">The configuration key (case-insensitive).</param>
    /// <returns>The value for the key, or null if not found.</returns>
    public string? GetValue(string key) => _values.GetValueOrDefault(key);

    /// <summary>Gets the value for the given key, or the default value if not present.</summary>
    /// <param name="key">The configuration key (case-insensitive).</param>
    /// <param name="defaultValue">The default value to return when the key is absent.</param>
    /// <returns>The value for the key, or the default value if not found.</returns>
    public string GetValueOrDefault(string key, string defaultValue = "")
        => _values.GetValueOrDefault(key, defaultValue);

    /// <summary>Returns a new configuration with the given key set to the given value.</summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A new <see cref="ConnectorConfiguration"/> instance with the updated key.</returns>
    public ConnectorConfiguration With(string key, string value)
    {
        var dict = new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase)
            { [key] = value };
        return new ConnectorConfiguration(dict);
    }

    /// <summary>Returns the underlying dictionary for serialization purposes.</summary>
    /// <returns>A read-only dictionary of all configuration values.</returns>
    public IReadOnlyDictionary<string, string> AsReadOnlyDictionary() => _values;
}
