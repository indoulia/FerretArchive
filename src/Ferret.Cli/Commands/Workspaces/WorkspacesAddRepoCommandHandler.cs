using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces add-repo'.</summary>
internal sealed class WorkspacesAddRepoCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesAddRepoCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesAddRepoCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var path = context.GetOption<string>("path");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(path))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces add-repo <id-or-name> <path>.");
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
            context.Services.Output.WriteError(ex.Message);
            return CommandResult.Failure;
        }

        if (entry.Members.Repos.Any(r => string.Equals(r.Remote, identity, StringComparison.Ordinal)))
        {
            context.Services.Output.WriteError($"'{fullPath}' (identity: {identity}) is already a member of workspace '{entry.Name}'.");
            return CommandResult.Failure;
        }

        var updated = entry with
        {
            Members = entry.Members with
            {
                Repos = [.. entry.Members.Repos, new RepoMember { Remote = identity, LocalPath = fullPath }],
            },
        };
        await _registry.SaveAsync(updated, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess($"Added '{fullPath}' (identity: {identity}) to workspace '{entry.Name}'. Now {updated.Members.Repos.Count} repo(s). Next: ferret workspaces show {entry.Name}.");
        return CommandResult.Success;
    }
}
