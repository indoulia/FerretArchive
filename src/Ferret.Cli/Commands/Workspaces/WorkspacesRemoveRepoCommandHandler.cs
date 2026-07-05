using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces remove-repo'.</summary>
internal sealed class WorkspacesRemoveRepoCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesRemoveRepoCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesRemoveRepoCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var path = context.GetOption<string>("path");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(path))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces remove-repo <id-or-name> <path>.");
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

        var fullPath = Path.GetFullPath(path);
        string identity;
        try
        {
            identity = await RepoIdentityResolver.ResolveAsync(fullPath, context.CancellationToken).ConfigureAwait(false);
        }
        catch (RepoIdentityResolutionException ex)
        {
            // Documented friction (WIP-012 dogfooding): if the path was deleted after being added,
            // this fails here rather than letting the user remove it by identity alone. Not fixed —
            // see WIP-012's "what did implementation teach us" note.
            context.Services.Output.WriteError(ex.Message);
            return CommandResult.Failure;
        }

        var remaining = entry.Members.Repos.Where(r => !string.Equals(r.Remote, identity, StringComparison.Ordinal)).ToList();
        if (remaining.Count == entry.Members.Repos.Count)
        {
            context.Services.Output.WriteError($"'{fullPath}' (identity: {identity}) is not a member of workspace '{entry.Name}'.");
            return CommandResult.Failure;
        }

        var updated = entry with { Members = entry.Members with { Repos = remaining } };
        await _registry.SaveAsync(updated, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess($"Removed '{fullPath}' (identity: {identity}) from workspace '{entry.Name}'. Now {updated.Members.Repos.Count} repo(s).");
        return CommandResult.Success;
    }
}
