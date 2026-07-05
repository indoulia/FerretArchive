using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Connector;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;
using Ferret.Core.Runtime;
using Ferret.Core.Workspace;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Ferret.Cli.Tests.Commands.Connector;

public sealed class ConnectorListCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_TypeWithNoInstances_IsConfigured_False()
    {
        var registry = new FakeConnectorRegistry([MakeDescriptor("filesystem")]);
        var store = new FakeConnectorInstanceStore([]);
        var formatter = new TextConnectorListFormatter();
        var handler = new ConnectorListCommandHandler(registry, store, formatter);
        var ctx = new StubFerretContext();

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("no", ctx.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_TypeWithEnabledInstance_IsConfigured_True()
    {
        // ConnectorListCommandHandler previously hardcoded IsConfigured: false for every
        // connector type, regardless of whether an instance had actually been enabled
        // (ferret connector enable). This test locks in the real, instance-aware value.
        var registry = new FakeConnectorRegistry([MakeDescriptor("filesystem")]);
        var store = new FakeConnectorInstanceStore(
        [
            new ConnectorInstance
            {
                Id = new ConnectorInstanceId("test-instance"),
                ConnectorType = new ConnectorId("filesystem"),
                DisplayName = "test-instance",
            },
        ]);
        var formatter = new TextConnectorListFormatter();
        var handler = new ConnectorListCommandHandler(registry, store, formatter);
        var ctx = new StubFerretContext();

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("yes", ctx.Output, StringComparison.OrdinalIgnoreCase);
    }

    private static ConnectorDescriptor MakeDescriptor(string id) => new()
    {
        Id = new ConnectorId(id),
        Metadata = ConnectorMetadata.Create(id, "Filesystem Connector", "desc", ConnectorType.Filesystem, "1.0"),
        Capabilities = [],
    };

    // ── Fakes ──────────────────────────────────────────────────────────────────

    private sealed class FakeConnectorRegistry(IReadOnlyList<ConnectorDescriptor> descriptors) : IConnectorRegistry
    {
        public IReadOnlyList<ConnectorDescriptor> GetAll() => descriptors;

        public ConnectorDescriptor? GetById(ConnectorId id) => descriptors.FirstOrDefault(d => d.Id == id);

        public bool IsRegistered(ConnectorId id) => descriptors.Any(d => d.Id == id);

        public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) => [];
    }

    private sealed class FakeConnectorInstanceStore(IReadOnlyList<ConnectorInstance> instances) : IConnectorInstanceStore
    {
        public Task<IReadOnlyList<ConnectorInstance>> LoadAllAsync(WorkspacePath rootPath, CancellationToken ct = default) =>
            Task.FromResult(instances);

        public Task SaveAsync(WorkspacePath rootPath, IReadOnlyList<ConnectorInstance> instances2, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class StubFerretContext : IFerretContext
    {
        private readonly StubOutputFormatter _formatter = new();

        public StubFerretContext() => Services = new StubFerretServices(_formatter);

        public string Output => _formatter.Output;

        public CancellationToken CancellationToken => CancellationToken.None;

        public VerbosityLevel Verbosity => VerbosityLevel.Normal;

        public OutputFormat OutputFormat => OutputFormat.Text;

        public IFerretServices Services { get; }

        public string WorkingDirectory => Directory.GetCurrentDirectory();

        public T? GetOption<T>(string name) => default;

        private sealed class StubFerretServices : IFerretServices
        {
            public StubFerretServices(IOutputFormatter output) => Output = output;

            public IServiceProvider Services => throw new NotSupportedException();

            public IConfiguration Configuration => throw new NotSupportedException();

            public ILoggerFactory LoggerFactory => throw new NotSupportedException();

            public IOutputFormatter Output { get; }

            public IRuntimeHost? Runtime => null;
        }
    }

    private sealed class StubOutputFormatter : IOutputFormatter
    {
        private readonly System.Text.StringBuilder _out = new();

        public string Output => _out.ToString();

        public void WriteLine(string text = "") => _out.AppendLine(text);

        public void WriteSuccess(string message) => _out.AppendLine(message);

        public void WriteError(string message) => _out.AppendLine(message);

        public void WriteVerbose(string message)
        {
        }
    }
}
