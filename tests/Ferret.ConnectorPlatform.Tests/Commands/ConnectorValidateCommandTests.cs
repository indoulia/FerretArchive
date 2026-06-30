using Ferret.Cli.Commands.Connector;
using Ferret.ConnectorPlatform;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

using Xunit;

namespace Ferret.ConnectorPlatform.Tests.Commands;

/// <summary>Tests for <see cref="ConnectorValidateCommandHandler"/>.</summary>
public sealed class ConnectorValidateCommandTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorValidateCommandTests"/> class.</summary>
    public ConnectorValidateCommandTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
        {
            Directory.Delete(_tmpDir, recursive: true);
        }
    }

    /// <summary>A registered connector type returns a valid result.</summary>
    [Fact]
    public async Task Validate_Known_Type_Returns_IsValid_True()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
        };
        await _store.SaveAsync(_root, [instance]);

        var registry = new FakeRegistry(["filesystem"]);
        var handler = new ConnectorValidateCommandHandler(_store, registry);

        var result = await handler.ValidateAsync(_root);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    /// <summary>An unregistered connector type returns a validation error.</summary>
    [Fact]
    public async Task Validate_Unknown_Type_Returns_IsValid_False()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("broken"),
            ConnectorType = new ConnectorId("unknown-type"),
            DisplayName = "broken",
        };
        await _store.SaveAsync(_root, [instance]);

        var registry = new FakeRegistry([]);
        var handler = new ConnectorValidateCommandHandler(_store, registry);

        var result = await handler.ValidateAsync(_root);

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal("broken", result.Issues[0].InstanceId);
    }

    /// <summary>No connectors.json file returns a valid (empty) result.</summary>
    [Fact]
    public async Task Validate_No_File_Returns_Valid()
    {
        var registry = new FakeRegistry([]);
        var handler = new ConnectorValidateCommandHandler(_store, registry);

        var result = await handler.ValidateAsync(_root);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    private sealed class FakeRegistry : IConnectorRegistry
    {
        private readonly HashSet<string> _ids;

        internal FakeRegistry(IEnumerable<string> registeredIds) =>
            _ids = new HashSet<string>(registeredIds, StringComparer.Ordinal);

        public IReadOnlyList<ConnectorDescriptor> GetAll() =>
            [];

        public ConnectorDescriptor? GetById(ConnectorId id) =>
            null;

        public bool IsRegistered(ConnectorId id) =>
            _ids.Contains(id.Value);

        public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) =>
            [];
    }
}
