using Ferret.Cli.Commands.Workspace;
using Ferret.Core.Workspace;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Tests.Commands.Workspace;

public sealed class WorkspaceCliModuleTests
{
    private readonly WorkspaceCliModule _module = new();

    [Fact]
    public void GetCommands_ContainsWorkspaceParentCommand()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "workspace" && c.Group is null);
    }

    [Fact]
    public void GetCommands_ContainsInitSubcommandInWorkspaceGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "init" && c.Group == "workspace");
    }

    [Fact]
    public void GetCommands_ContainsStatusSubcommandInWorkspaceGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "status" && c.Group == "workspace");
    }

    [Fact]
    public void ConfigureServices_RegistersIWorkspaceEngine()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IWorkspaceEngine>());
    }

    [Fact]
    public void ConfigureServices_RegistersIWorkspaceLocator()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IWorkspaceLocator>());
    }

    [Fact]
    public void ConfigureServices_DoesNotRegisterConcreteImplementationsDirectly()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);

        // All registrations must be against an interface (not the concrete type as the service type)
        var concreteRegistrations = services
            .Where(d => d.ServiceType == d.ImplementationType)
            .Select(d => d.ServiceType.Name)
            .ToList();

        // Only handler registrations (Transient, concrete type) are permitted — they have no public interface
        var nonHandlerConcretes = concreteRegistrations
            .Where(name => !name.EndsWith("CommandHandler", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(nonHandlerConcretes);
    }
}
