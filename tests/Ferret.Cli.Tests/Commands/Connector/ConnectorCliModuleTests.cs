using Ferret.Cli.Commands.Connector;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Workspace;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Tests.Commands.Connector;

/// <summary>Unit tests for <see cref="ConnectorCliModule"/>.</summary>
public sealed class ConnectorCliModuleTests
{
    private readonly IConnectorFactory _fakeFactory = new FakeConnectorFactory();
    private readonly ConnectorCliModule _module;

    /// <summary>Initializes a new instance of the <see cref="ConnectorCliModuleTests"/> class.</summary>
    public ConnectorCliModuleTests() => _module = new ConnectorCliModule([_fakeFactory]);

    [Fact]
    public void GetCommands_ContainsConnectorParentCommand()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "connector" && c.Group is null);
    }

    [Fact]
    public void GetCommands_ContainsListSubcommandInConnectorGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "list" && c.Group == "connector");
    }

    [Fact]
    public void GetCommands_ContainsEnableSubcommandInConnectorGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "enable" && c.Group == "connector");
    }

    [Fact]
    public void GetCommands_ContainsDisableSubcommandInConnectorGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "disable" && c.Group == "connector");
    }

    [Fact]
    public void GetCommands_ContainsConfigureSubcommandInConnectorGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "configure" && c.Group == "connector");
    }

    [Fact]
    public void GetCommands_ContainsInspectSubcommandInConnectorGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "inspect" && c.Group == "connector");
    }

    [Fact]
    public void GetCommands_ContainsValidateSubcommandInConnectorGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "validate" && c.Group == "connector");
    }

    [Fact]
    public void ConfigureServices_RegistersIConnectorRegistry()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IConnectorRegistry>());
    }

    [Fact]
    public void ConfigureServices_RegistersIConnectorInstanceStore()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IConnectorInstanceStore>());
    }

    [Fact]
    public void ConfigureServices_RegistersIConnectorManager()
    {
        var services = new ServiceCollection();

        // IWorkspaceContext is required by the ConnectorManager factory lambda.
        services.AddSingleton<IWorkspaceContext>(
            new DefaultWorkspaceContext(
                WorkspaceId.Create("test"),
                WorkspacePath.Create(System.IO.Path.GetTempPath())));

        _module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IConnectorManager>());
    }

    [Fact]
    public void ConfigureServices_RegistersIConnectorFactory()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        var factories = provider.GetServices<IConnectorFactory>().ToList();
        Assert.NotEmpty(factories);
    }

    /// <summary>Minimal in-test fake connector factory for module wiring tests.</summary>
    private sealed class FakeConnectorFactory : IConnectorFactory
    {
        public ConnectorId ConnectorId => new("fake");

        public ConnectorDescriptor Descriptor => new()
        {
            Id = ConnectorId,
            Metadata = ConnectorMetadata.Create("fake", "Fake", "Fake connector", ConnectorType.Custom, "1.0"),
            Capabilities = [],
            SupportedPlatforms = [],
        };

        public IConnector Create(ConnectorInstance instance) =>
            throw new NotImplementedException();
    }
}
