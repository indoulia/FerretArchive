using Ferret.Connectors.Filesystem.Ignore;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;

namespace Ferret.Connectors.Filesystem;

/// <summary>Discovers files and directories from the local filesystem.</summary>
public sealed class FilesystemConnector : IConnector, IAssetSource, IAssetReader
{
    private static readonly ConnectorInstanceId DefaultInstanceId = new("filesystem-default");
    private static readonly ConnectorId FilesystemConnectorId = new("filesystem");
    private static readonly HashSet<string> HardcodedSkipDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".ferret", ".svn", ".hg",
        "node_modules", "bin", "obj",
    };

    private readonly FilesystemConnectorConfiguration _config;
    private readonly IMimeTypeResolver _mimeTypeResolver;

    /// <summary>Initializes a new instance of the <see cref="FilesystemConnector"/> class.</summary>
    /// <param name="config">The filesystem connector configuration.</param>
    /// <param name="mimeTypeResolver">The MIME type resolver for populating AssetDescriptor.MediaType.</param>
    public FilesystemConnector(FilesystemConnectorConfiguration config, IMimeTypeResolver mimeTypeResolver)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(mimeTypeResolver);
        _config = config;
        _mimeTypeResolver = mimeTypeResolver;
    }

    /// <inheritdoc/>
    public ConnectorType ConnectorType => ConnectorType.Filesystem;

    /// <inheritdoc/>
    public ConnectorMetadata Metadata { get; } = ConnectorMetadata.Create("filesystem", "Filesystem Connector", "Discovers files and directories from the local filesystem.", ConnectorType.Filesystem, "1.0");

    /// <inheritdoc/>
    public ConnectorIoCapabilities Capabilities { get; } = ConnectorIoCapabilities.ReadOnly();

    /// <inheritdoc/>
    public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_config.RootPath))
        {
            return Task.FromResult(
                ConnectorHealth.Disconnected($"Root path does not exist: {_config.RootPath}", DateTimeOffset.UtcNow));
        }

        try
        {
            _ = Directory.GetFileSystemEntries(_config.RootPath);
            return Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));
        }
        catch (IOException ex)
        {
            return Task.FromResult(ConnectorHealth.Disconnected(ex.Message, DateTimeOffset.UtcNow));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult(ConnectorHealth.Disconnected(ex.Message, DateTimeOffset.UtcNow));
        }
    }

    /// <inheritdoc/>
    public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default)
    {
        IConnectorSession session = new FilesystemConnectorSession(DefaultInstanceId);
        return Task.FromResult(session);
    }

    /// <inheritdoc/>
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ct.ThrowIfCancellationRequested();

        var relativePath = asset.CanonicalUri.AbsolutePath.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar);
        var fullPath = System.IO.Path.Join(_config.RootPath, relativePath);
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        AssetDiscoveryOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var root = new DirectoryInfo(_config.RootPath);
        if (!root.Exists)
        {
            yield break;
        }

        // Default ignore provider: .ferretignore in workspace root (if not already supplied).
        var effectiveOptions = options.IgnoreProvider is null
            ? new AssetDiscoveryOptions { IgnoreProvider = new FerretIgnoreProvider(_config.RootPath) }
            : options;

        await foreach (var descriptor in WalkDirectoryAsync(root, root, effectiveOptions, _mimeTypeResolver, ct).ConfigureAwait(false))
        {
            yield return descriptor;
        }
    }

    private static async IAsyncEnumerable<AssetDescriptor> WalkDirectoryAsync(
        DirectoryInfo dir,
        DirectoryInfo root,
        AssetDiscoveryOptions options,
        IMimeTypeResolver mimeTypeResolver,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        FileSystemInfo[] entries;
        try
        {
            entries = dir.GetFileSystemInfos();
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            if (entry is DirectoryInfo subDir)
            {
                if (HardcodedSkipDirs.Contains(subDir.Name))
                {
                    continue;
                }

                var dirDescriptor = BuildDescriptor(subDir, root, AssetKind.Directory, mimeTypeResolver);
                if (options.IgnoreProvider?.ShouldIgnore(dirDescriptor) == true)
                {
                    continue;
                }

                yield return dirDescriptor;

                await foreach (var child in WalkDirectoryAsync(subDir, root, options, mimeTypeResolver, ct).ConfigureAwait(false))
                {
                    yield return child;
                }
            }
            else if (entry is FileInfo file)
            {
                var descriptor = BuildDescriptor(file, root, AssetKind.File, mimeTypeResolver);
                if (options.IgnoreProvider?.ShouldIgnore(descriptor) == true)
                {
                    continue;
                }

                yield return descriptor;
            }
        }
    }

    private static AssetDescriptor BuildDescriptor(
        FileSystemInfo entry,
        DirectoryInfo root,
        AssetKind kind,
        IMimeTypeResolver mimeTypeResolver)
    {
        var relative = System.IO.Path.GetRelativePath(root.FullName, entry.FullName)
            .Replace('\\', '/');
        var uri = new Uri($"filesystem:///{relative}");

        long? size = kind == AssetKind.File ? ((FileInfo)entry).Length : null;
        AssetFingerprint? fingerprint = kind == AssetKind.File
            ? AssetFingerprint.CreateLightweight(entry.LastWriteTimeUtc, ((FileInfo)entry).Length)
            : null;

        string? mediaType = kind == AssetKind.File
            ? mimeTypeResolver.Resolve(entry.Name).MediaType
            : null;

        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = FilesystemConnectorId,
            InstanceId = DefaultInstanceId,
            Kind = kind,
            CanonicalUri = uri,
            DisplayName = entry.Name,
            LastModified = entry.LastWriteTimeUtc,
            Fingerprint = fingerprint,
            SizeBytes = size,
            MediaType = mediaType,
        };
    }
}
