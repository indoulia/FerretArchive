using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces unpin-reference' (WIP-022, <c>03-Cross-Workspace-References.md</c> §3).
/// The reverse of <c>pin-reference</c> — clears <see cref="WorkspaceReference.PinnedStateHash"/> so the
/// reference floats (always queries the referenced workspace's current state) again.</summary>
internal sealed class WorkspacesUnpinReferenceCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesUnpinReferenceCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesUnpinReferenceCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var targetArg = context.GetOption<string>("target");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(targetArg))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces unpin-reference <id-or-name> <target-id-or-name>.");
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

        if (source.References.All(r => r.WorkspaceId != target.WorkspaceId))
        {
            context.Services.Output.WriteError($"Workspace '{source.Name}' does not reference '{target.Name}'.");
            return CommandResult.Failure;
        }

        var updatedReferences = source.References
            .Select(r => r.WorkspaceId == target.WorkspaceId ? r with { PinnedStateHash = null } : r)
            .ToList();
        await _registry.SaveAsync(source with { References = updatedReferences }, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess($"Workspace '{source.Name}' reference to '{target.Name}' now floats (always queries its current state).");
        return CommandResult.Success;
    }
}
