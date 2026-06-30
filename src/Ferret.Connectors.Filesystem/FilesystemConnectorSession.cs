using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem;

/// <summary>No-op session for the filesystem connector — the filesystem has no persistent connection.</summary>
internal sealed class FilesystemConnectorSession : IConnectorSession
{
    internal FilesystemConnectorSession(ConnectorInstanceId instanceId) =>
        InstanceId = instanceId;

    /// <inheritdoc/>
    public ConnectorInstanceId InstanceId { get; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
