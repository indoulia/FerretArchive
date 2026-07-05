using Ferret.Cli.Cli;
using Ferret.Core.Workspace;
using Ferret.Workspace;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Contributes workspace subcommands to the Ferret CLI.</summary>
internal sealed class WorkspaceCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.workspace";

    /// <inheritdoc/>
    public override string Description => "ContextOS workspace management.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(new CommandMetadata("workspace", "Manage Ferret workspaces."), HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("init", "Initialise a new Ferret workspace in the current directory."),
            typeof(WorkspaceInitCommandHandler),
            Group: "workspace");

        yield return new CommandDefinition(
            new CommandMetadata("status", "Show the status of the current workspace."),
            typeof(WorkspaceStatusCommandHandler),
            Group: "workspace");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWorkspaceEngine, WorkspaceEngine>();
        services.AddSingleton<IWorkspaceLocator, WorkspaceLocator>();
        services.AddSingleton<IWorkspaceInitFormatter, TextWorkspaceInitFormatter>();
        services.AddSingleton<IWorkspaceStatusFormatter, TextWorkspaceStatusFormatter>();
        services.TryAddSingleton<IWorkspaceRegistry>(_ =>
        {
            var root = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ferret", "workspaces");
            return new CachingWorkspaceRegistry(new FileWorkspaceRegistry(root));
        });
        services.AddSingleton<IWorkspaceRegistryAutoMigrator, WorkspaceRegistryAutoMigrator>();
        services.AddTransient<WorkspaceInitCommandHandler>();
        services.AddTransient<WorkspaceStatusCommandHandler>();
    }
}
