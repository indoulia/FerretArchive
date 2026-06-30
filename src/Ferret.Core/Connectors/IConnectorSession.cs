namespace Ferret.Core.Connectors;

/// <summary>Represents an active connection to a data source. Dispose to release runtime resources.</summary>
public interface IConnectorSession : IAsyncDisposable
{
    /// <summary>Gets the workspace-scoped instance identifier this session belongs to.</summary>
    ConnectorInstanceId InstanceId { get; }
}
