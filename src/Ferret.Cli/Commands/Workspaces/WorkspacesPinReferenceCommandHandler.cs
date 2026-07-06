using Ferret.Cli.Cli;
using Ferret.Knowledge.Federation;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces pin-reference' (WIP-022, <c>03-Cross-Workspace-References.md</c> §3).
/// Pins an existing reference to the referenced workspace's current Workspace State Fingerprint
/// (ADR-0027 Amendment) — federated queries then fail closed if that workspace's state later changes.</summary>
internal sealed class WorkspacesPinReferenceCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;
    private readonly IWorkspaceStateFingerprintProvider _fingerprintProvider;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesPinReferenceCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="fingerprintProvider">Computes the target workspace's current Workspace State Fingerprint.</param>
    public WorkspacesPinReferenceCommandHandler(IWorkspaceRegistry registry, IWorkspaceStateFingerprintProvider fingerprintProvider)
    {
        _registry = registry;
        _fingerprintProvider = fingerprintProvider;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var targetArg = context.GetOption<string>("target");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(targetArg))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces pin-reference <id-or-name> <target-id-or-name>.");
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

        var reference = source.References.FirstOrDefault(r => r.WorkspaceId == target.WorkspaceId);
        if (reference is null)
        {
            context.Services.Output.WriteError(
                $"Workspace '{source.Name}' does not reference '{target.Name}'. Run 'ferret workspaces add-reference' first.");
            return CommandResult.Failure;
        }

        var fingerprint = await _fingerprintProvider.ComputeFingerprintAsync(target, context.CancellationToken).ConfigureAwait(false);
        if (fingerprint is null)
        {
            context.Services.Output.WriteError(
                $"Cannot pin '{target.Name}': its current state could not be verified (a member repo may be unreachable).");
            return CommandResult.Failure;
        }

        var updatedReferences = source.References
            .Select(r => r.WorkspaceId == target.WorkspaceId ? r with { PinnedStateHash = fingerprint } : r)
            .ToList();
        await _registry.SaveAsync(source with { References = updatedReferences }, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess(
            $"Workspace '{source.Name}' pinned its reference to '{target.Name}' at its current state. Run 'ferret workspaces unpin-reference' to let it float again.");
        return CommandResult.Success;
    }
}
