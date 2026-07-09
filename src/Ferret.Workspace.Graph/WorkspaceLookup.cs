namespace Ferret.Workspace.Graph;

/// <summary>
/// Resolves a user-typed workspace identifier (a UUID or a name) to its registry entry.
/// Name lookup is safe here specifically because workspace creation rejects a duplicate name
/// at creation time (see <c>12-API.md</c> §2). Shared by the CLI (<c>Ferret.Cli</c>) and the MCP
/// tool surface (<c>Ferret.Mcp</c>) — both already reference this project, so this lives here
/// rather than in either presentation layer to avoid a Cli/Mcp cross-dependency.
/// </summary>
public static class WorkspaceLookup
{
    /// <summary>Resolves <paramref name="idOrName"/> against the registry.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="idOrName">A workspace UUID or name, as typed by the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching entry, or null if none matches.</returns>
    public static async Task<WorkspaceRegistryEntry?> ResolveAsync(IWorkspaceRegistry registry, string idOrName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(idOrName);

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
