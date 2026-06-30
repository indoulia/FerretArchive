using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform;

/// <summary>
/// Process-scoped lifecycle owner for connector runtimes.
/// Loads enabled instances from <see cref="ConnectorInstanceStore"/>, creates connectors
/// via registered <see cref="IConnectorFactory"/> instances, and caches the resulting
/// <see cref="ConnectorRuntime"/> objects for the lifetime of the process.
/// <para>Instances with an unregistered <see cref="ConnectorInstance.ConnectorType"/> are silently skipped.</para>
/// <para>Thread safety: protected by a <see cref="SemaphoreSlim"/>.</para>
/// </summary>
internal sealed class ConnectorManager : IConnectorManager, IDisposable
{
    private readonly IConnectorInstanceStore _store;
    private readonly Dictionary<ConnectorId, IConnectorFactory> _factories;
    private readonly WorkspacePath _rootPath;
    private readonly Dictionary<ConnectorInstanceId, ConnectorRuntime> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="ConnectorManager"/> class.</summary>
    /// <param name="store">The persistent instance store.</param>
    /// <param name="factories">All registered connector factories.</param>
    /// <param name="rootPath">The workspace root path.</param>
    public ConnectorManager(
        IConnectorInstanceStore store,
        IEnumerable<IConnectorFactory> factories,
        WorkspacePath rootPath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(factories);
        ArgumentNullException.ThrowIfNull(rootPath);

        _store = store;
        _factories = factories.ToDictionary(f => f.ConnectorId);
        _rootPath = rootPath;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(
        CancellationToken ct = default)
    {
        var instances = await _store.LoadAllAsync(_rootPath, ct).ConfigureAwait(false);

        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var result = new List<ConnectorRuntime>();
            foreach (var instance in instances.Where(i => i.IsEnabled))
            {
                if (!_factories.TryGetValue(instance.ConnectorType, out var factory))
                {
                    continue;
                }

                if (!_cache.TryGetValue(instance.Id, out var runtime))
                {
                    var connector = factory.Create(instance);
                    runtime = new ConnectorRuntime
                    {
                        Instance = instance,
                        Connector = connector,
                        Status = new ConnectorStatus
                        {
                            ConnectorId = instance.ConnectorType,
                            InstanceId = instance.Id,
                            IsActive = true,
                            Health = ConnectorHealth.Connected(DateTimeOffset.UtcNow),
                        },
                    };
                    _cache[instance.Id] = runtime;
                }

                result.Add(runtime);
            }

            return result;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ConnectorInstance?> GetInstanceAsync(
        ConnectorInstanceId id,
        CancellationToken ct = default)
    {
        var instances = await _store.LoadAllAsync(_rootPath, ct).ConfigureAwait(false);
        return instances.FirstOrDefault(i => i.Id == id);
    }

    /// <inheritdoc/>
    public void Dispose() => _cacheLock.Dispose();
}
