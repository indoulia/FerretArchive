using Ferret.Core.Connectors;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for <see cref="IConnectorManager"/>. Returns a pre-configured list of runtimes.</summary>
internal sealed class FakeConnectorManager : IConnectorManager
{
    private readonly List<ConnectorRuntime> _runtimes;

    /// <summary>Initializes a new instance of the <see cref="FakeConnectorManager"/> class.</summary>
    /// <param name="runtimes">The runtimes to return from <see cref="GetActiveConnectorsAsync"/>.</param>
    internal FakeConnectorManager(IEnumerable<ConnectorRuntime> runtimes)
    {
        _runtimes = runtimes.ToList();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ConnectorRuntime>>(_runtimes);

    /// <inheritdoc/>
    public Task<ConnectorInstance?> GetInstanceAsync(
        ConnectorInstanceId id,
        CancellationToken ct = default) =>
        Task.FromResult(_runtimes.FirstOrDefault(r => r.Instance.Id == id)?.Instance);
}
