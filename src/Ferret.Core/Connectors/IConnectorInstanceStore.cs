using Ferret.Core.Workspace;

namespace Ferret.Core.Connectors;

/// <summary>
/// Loads and persists <see cref="ConnectorInstance"/> records to a workspace-local store.
/// The concrete implementation (<c>ConnectorInstanceStore</c>) uses <c>.ferret/connectors.json</c>.
/// </summary>
public interface IConnectorInstanceStore
{
    /// <summary>Loads all connector instances from the workspace store.
    /// Returns an empty list when the store file does not yet exist.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that yields the list of loaded connector instances.</returns>
    Task<IReadOnlyList<ConnectorInstance>> LoadAllAsync(WorkspacePath rootPath, CancellationToken ct = default);

    /// <summary>Saves the given instances to the workspace store, replacing all previous content.
    /// Uses an atomic write (temp file → rename) to prevent partial writes.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="instances">The complete list of instances to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync(WorkspacePath rootPath, IReadOnlyList<ConnectorInstance> instances, CancellationToken ct = default);
}
