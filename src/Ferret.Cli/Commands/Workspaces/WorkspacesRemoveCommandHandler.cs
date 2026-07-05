using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces remove' (WIP-037) — deletes a workspace's own registry entry entirely, not a member repo within it (see <see cref="WorkspacesRemoveRepoCommandHandler"/> for that).</summary>
internal sealed class WorkspacesRemoveCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesRemoveCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesRemoveCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        if (string.IsNullOrWhiteSpace(workspaceArg))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces remove <id-or-name>.");
            return CommandResult.Failure;
        }

        WorkspaceRegistryEntry? entry;
        try
        {
            entry = await WorkspaceLookup.ResolveAsync(_registry, workspaceArg, context.CancellationToken).ConfigureAwait(false);
        }
        catch (WorkspaceRegistryCorruptException ex)
        {
            context.Services.Output.WriteError(ex.Message);
            return CommandResult.Failure;
        }

        if (entry is null)
        {
            context.Services.Output.WriteError($"Workspace '{workspaceArg}' not found. Run 'ferret workspaces list' to see available workspaces.");
            return CommandResult.Failure;
        }

        await _registry.RemoveAsync(entry.WorkspaceId, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess($"Removed workspace '{entry.Name}' ({entry.WorkspaceId}).");
        return CommandResult.Success;
    }
}
