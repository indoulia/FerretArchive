using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces add-reference' (WIP-SLICE-2, <c>03-Cross-Workspace-References.md</c> §5).
/// Adds a read-only <see cref="WorkspaceReference"/> edge; a self-reference or any edge that would
/// close a cycle in the reference graph is rejected outright, never resolved.</summary>
internal sealed class WorkspacesAddReferenceCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesAddReferenceCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesAddReferenceCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var targetArg = context.GetOption<string>("target");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(targetArg))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces add-reference <id-or-name> <target-id-or-name>.");
            return CommandResult.Failure;
        }

        WorkspaceRegistryEntry? source;
        WorkspaceRegistryEntry? target;
        IReadOnlyList<WorkspaceRegistryEntry> all;
        try
        {
            source = await WorkspaceLookup.ResolveAsync(_registry, workspaceArg, context.CancellationToken).ConfigureAwait(false);
            target = await WorkspaceLookup.ResolveAsync(_registry, targetArg, context.CancellationToken).ConfigureAwait(false);
            all = await _registry.ListAsync(context.CancellationToken).ConfigureAwait(false);
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

        if (source.WorkspaceId == target.WorkspaceId)
        {
            context.Services.Output.WriteError($"Workspace '{source.Name}' cannot reference itself.");
            return CommandResult.Failure;
        }

        if (source.References.Any(r => r.WorkspaceId == target.WorkspaceId))
        {
            context.Services.Output.WriteError($"Workspace '{source.Name}' already references '{target.Name}'.");
            return CommandResult.Failure;
        }

        if (ReferenceGraph.WouldCreateCycle(all, source.WorkspaceId, target.WorkspaceId))
        {
            context.Services.Output.WriteError(
                $"Adding a reference from '{source.Name}' to '{target.Name}' would create a cycle. Reference graphs must be a DAG (03-Cross-Workspace-References.md §5).");
            return CommandResult.Failure;
        }

        var updated = source with
        {
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [.. source.References, new WorkspaceReference { WorkspaceId = target.WorkspaceId }],
        };
        await _registry.SaveAsync(updated, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess(
            $"Workspace '{source.Name}' now references '{target.Name}'. Next: ferret workspaces query {source.Name} <text>.");
        return CommandResult.Success;
    }
}
