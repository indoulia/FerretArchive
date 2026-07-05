using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces remove-reference' (WIP-021, `03-Cross-Workspace-References.md` §5,
/// `12-API.md` §2). The removal half of `add-reference` — also the repair step for a moved/renamed
/// repo's stale reference (Founder Dogfooding Sprint 1, Friction #4): remove the stale reference,
/// then `add-repo`/`add-reference` again at the corrected path.</summary>
internal sealed class WorkspacesRemoveReferenceCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesRemoveReferenceCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesRemoveReferenceCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var targetArg = context.GetOption<string>("target");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(targetArg))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces remove-reference <id-or-name> <target-id-or-name>.");
            return CommandResult.Failure;
        }

        WorkspaceRegistryEntry? source;
        WorkspaceRegistryEntry? target;
        try
        {
            source = await WorkspaceLookup.ResolveAsync(_registry, workspaceArg, context.CancellationToken).ConfigureAwait(false);
            target = await WorkspaceLookup.ResolveAsync(_registry, targetArg, context.CancellationToken).ConfigureAwait(false);
        }
        catch (WorkspaceRegistryCorruptException ex)
        {
            context.Services.Output.WriteError(ex.Message);
            return CommandResult.Failure;
        }

        if (source is null)
        {
            context.Services.Output.WriteError($"Workspace '{workspaceArg}' not found. Run 'ferret workspaces list' to see available workspaces.");
            return CommandResult.Failure;
        }

        if (target is null)
        {
            context.Services.Output.WriteError($"Workspace '{targetArg}' not found. Run 'ferret workspaces list' to see available workspaces.");
            return CommandResult.Failure;
        }

        if (!source.References.Any(r => r.WorkspaceId == target.WorkspaceId))
        {
            context.Services.Output.WriteError($"Workspace '{source.Name}' does not reference '{target.Name}'.");
            return CommandResult.Failure;
        }

        var updated = source with
        {
            References = [.. source.References.Where(r => r.WorkspaceId != target.WorkspaceId)],
        };
        await _registry.SaveAsync(updated, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess($"Workspace '{source.Name}' no longer references '{target.Name}'.");
        return CommandResult.Success;
    }
}
