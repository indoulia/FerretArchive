using System.ComponentModel.DataAnnotations;

namespace Ferret.Configuration.Ai;

/// <summary>Base configuration for any AI model provider.</summary>
public class ProviderOptions
{
    /// <summary>Gets or sets a value indicating whether this provider is active. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the provider API base URL.</summary>
#pragma warning disable CA1056 // POCO options class — config binder requires string; Uri conversion is caller responsibility
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>Gets or sets the optional API key. Null means no key required (e.g. local Ollama).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the request timeout in seconds. Default: 60.</summary>
    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 60;
}
