using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces show'.</summary>
internal sealed class WorkspacesShowCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;
    private readonly IWorkspacesShowFormatter _formatter;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesShowCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="formatter">The show formatter.</param>
    public WorkspacesShowCommandHandler(IWorkspaceRegistry registry, IWorkspacesShowFormatter formatter)
    {
        _registry = registry;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        if (string.IsNullOrWhiteSpace(workspaceArg))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces show <id-or-name>.");
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

        _formatter.Format(entry, context.Services.Output);
        return CommandResult.Success;
    }
}
