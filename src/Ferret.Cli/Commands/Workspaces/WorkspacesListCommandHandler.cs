using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces list'.</summary>
internal sealed class WorkspacesListCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;
    private readonly IWorkspacesListFormatter _formatter;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesListCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="formatter">The list formatter.</param>
    public WorkspacesListCommandHandler(IWorkspaceRegistry registry, IWorkspacesListFormatter formatter)
    {
        _registry = registry;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        IReadOnlyList<WorkspaceRegistryEntry> entries;
        try
        {
            entries = await _registry.ListAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (WorkspaceRegistryCorruptException ex)
        {
            // WIP-010 scope decision: ListAsync propagates on the first corrupt entry it finds
            // rather than skipping it, so a single corrupt manifest currently blocks listing every
            // workspace, not just the broken one. Real friction, not fixed here — see WIP-012's
            // "what did implementation teach us" note.
            context.Services.Output.WriteError($"{ex.Message} This blocks listing every workspace, not just this one — fix or remove the file, then try again.");
            return CommandResult.Failure;
        }

        _formatter.Format(entries, context.Services.Output);
        return CommandResult.Success;
    }
}
