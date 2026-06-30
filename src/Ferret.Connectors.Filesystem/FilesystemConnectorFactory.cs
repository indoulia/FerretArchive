using Ferret.Core.Connectors;
using Ferret.Core.Documents;

namespace Ferret.Connectors.Filesystem;

/// <summary>Factory that creates FilesystemConnector instances from configuration.</summary>
public sealed class FilesystemConnectorFactory : IConnectorFactory
{
    private readonly FilesystemConnectorConfiguration _defaultConfig;
    private readonly IMimeTypeResolver _mimeTypeResolver;

    /// <summary>Initializes a new instance of the <see cref="FilesystemConnectorFactory"/> class.</summary>
    /// <param name="defaultConfig">The default configuration for connectors created by this factory.</param>
    /// <param name="mimeTypeResolver">The MIME type resolver to pass to created connectors.</param>
    public FilesystemConnectorFactory(
        FilesystemConnectorConfiguration defaultConfig,
        IMimeTypeResolver mimeTypeResolver)
    {
        ArgumentNullException.ThrowIfNull(defaultConfig);
        ArgumentNullException.ThrowIfNull(mimeTypeResolver);
        _defaultConfig = defaultConfig;
        _mimeTypeResolver = mimeTypeResolver;
    }

    /// <inheritdoc/>
    public ConnectorId ConnectorId { get; } = new("filesystem");

    /// <inheritdoc/>
    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Id = new ConnectorId("filesystem"),
        Metadata = ConnectorMetadata.Create(
            "filesystem",
            "Filesystem Connector",
            "Discovers files and directories from the local filesystem.",
            ConnectorType.Filesystem,
            "1.0"),
        Capabilities = [ConnectorCapabilities.AssetDiscovery],
        SupportedPlatforms = ["Linux", "macOS", "Windows"],
    };

    /// <inheritdoc/>
    public IConnector Create(ConnectorInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var config = new FilesystemConnectorConfiguration
        {
            RootPath = instance.Configuration.GetValueOrDefault("rootPath", "."),
            IncludeExtensions = ParseExtensions(
                instance.Configuration.GetValue("includeExtensions")),
            ExcludeExtensions = ParseExtensions(
                instance.Configuration.GetValue("excludeExtensions")),
        };
        return new FilesystemConnector(config, _mimeTypeResolver);
    }

    /// <summary>Parses comma-separated extension string into list, ensuring dot prefix.</summary>
    /// <param name="value">The extension string (e.g. "dll,exe" or ".dll,.exe").</param>
    /// <returns>List of extensions with dot prefix.</returns>
    private static List<string> ParseExtensions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToList();
    }
}
