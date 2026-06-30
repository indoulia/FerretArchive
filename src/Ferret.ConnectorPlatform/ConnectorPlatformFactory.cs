using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform;

/// <summary>
/// Factory helpers for <see cref="ConnectorPlatform"/> types that are <see langword="internal"/>.
/// Allows external callers (e.g. Ferret.Cli) to create instances without exposing implementation details.
/// </summary>
public static class ConnectorPlatformFactory
{
    /// <summary>
    /// Creates a new <see cref="IConnectorManager"/> backed by <see cref="ConnectorManager"/>.
    /// </summary>
    /// <param name="store">The persistent instance store.</param>
    /// <param name="factories">All registered connector factories.</param>
    /// <param name="rootPath">The workspace root path.</param>
    /// <returns>A new <see cref="IConnectorManager"/> instance.</returns>
    public static IConnectorManager CreateConnectorManager(
        IConnectorInstanceStore store,
        IEnumerable<IConnectorFactory> factories,
        WorkspacePath rootPath) =>
        new ConnectorManager(store, factories, rootPath);
}
