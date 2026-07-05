using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>
/// Contributes the <c>workspaces</c> (plural) subcommands to the Ferret CLI — the multi-repo
/// Workspace Registry from ADR-0026/WIP-010–012. Named distinctly from the existing
/// <c>workspace</c> (singular) group (<see cref="Ferret.Cli.Commands.Workspace.WorkspaceCliModule"/>),
/// which manages the unrelated, unchanged per-repo <c>.ferret/</c> workspace (ARCH-001 §12) —
/// see <c>12-API.md</c> §2's correction note for why these are two different command groups.
/// </summary>
internal sealed class WorkspacesCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.workspaces";

    /// <inheritdoc/>
    public override string Description => "Multi-repository workspace registry management.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(new CommandMetadata("workspaces", "Manage multi-repository workspaces."), HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("create", "Create a new workspace."),
            typeof(WorkspacesCreateCommandHandler),
            Group: "workspaces",
            Options:
            [
                new OptionDefinition("--name", "Workspace name (must be unique).", typeof(string)),
                new OptionDefinition("--kind", "Workspace kind: personal or team (default: personal).", typeof(string)),
            ]);

        yield return new CommandDefinition(
            new CommandMetadata("list", "List all workspaces."),
            typeof(WorkspacesListCommandHandler),
            Group: "workspaces");

        yield return new CommandDefinition(
            new CommandMetadata("show", "Show full detail for one workspace."),
            typeof(WorkspacesShowCommandHandler),
            Group: "workspaces")
            .WithArgument("workspace", "Workspace ID or name.");

        yield return new CommandDefinition(
            new CommandMetadata("add-repo", "Add a member repo to a workspace."),
            typeof(WorkspacesAddRepoCommandHandler),
            Group: "workspaces")
            .WithArgument("workspace", "Workspace ID or name.")
            .WithArgument("path", "Local path to the repo to add.");

        yield return new CommandDefinition(
            new CommandMetadata("remove-repo", "Remove a member repo from a workspace."),
            typeof(WorkspacesRemoveRepoCommandHandler),
            Group: "workspaces")
            .WithArgument("workspace", "Workspace ID or name.")
            .WithArgument("path", "Local path to the repo to remove (matched by resolved identity).");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWorkspaceRegistry>(_ =>
        {
            var root = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ferret", "workspaces");
            return new FileWorkspaceRegistry(root);
        });

        services.AddSingleton<IWorkspacesListFormatter, TextWorkspacesListFormatter>();
        services.AddSingleton<IWorkspacesShowFormatter, TextWorkspacesShowFormatter>();
        services.AddTransient<WorkspacesCreateCommandHandler>();
        services.AddTransient<WorkspacesListCommandHandler>();
        services.AddTransient<WorkspacesShowCommandHandler>();
        services.AddTransient<WorkspacesAddRepoCommandHandler>();
        services.AddTransient<WorkspacesRemoveRepoCommandHandler>();
    }
}
