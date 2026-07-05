using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>
/// Resolves a user-typed workspace identifier (a UUID or a name) to its registry entry. A CLI-layer
/// concern, not a registry one — <see cref="IWorkspaceRegistry"/> only resolves by ID; name lookup
/// is safe here specifically because <see cref="WorkspacesCreateCommandHandler"/> rejects a
/// duplicate name at creation time (see its own validation, and <c>12-API.md</c> §2).
/// </summary>
internal static class WorkspaceLookup
{
    /// <summary>Resolves <paramref name="idOrName"/> against the registry.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="idOrName">A workspace UUID or name, as typed by the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching entry, or null if none matches.</returns>
    public static async Task<WorkspaceRegistryEntry?> ResolveAsync(IWorkspaceRegistry registry, string idOrName, CancellationToken ct)
    {
        if (Guid.TryParse(idOrName, out var id))
        {
            var byId = await registry.ResolveAsync(id, ct).ConfigureAwait(false);
            if (byId is not null)
            {
                return byId;
            }
        }

        var all = await registry.ListAsync(ct).ConfigureAwait(false);
        return all.FirstOrDefault(e => string.Equals(e.Name, idOrName, StringComparison.Ordinal));
    }
}
