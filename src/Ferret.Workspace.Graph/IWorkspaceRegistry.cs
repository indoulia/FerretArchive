namespace Ferret.Workspace.Graph;

/// <summary>
/// Stores and retrieves <see cref="WorkspaceRegistryEntry"/> instances, per ADR-0026. Names no
/// storage technology — the default is a local file-based backend (<see cref="FileWorkspaceRegistry"/>),
/// following the same pluggable-backend pattern ARCH-001 §19.3 established for <c>IKnowledgeStore</c>.
/// </summary>
public interface IWorkspaceRegistry
{
    /// <summary>Returns the entry for the given workspace ID, or null if no such workspace has been created.</summary>
    /// <param name="workspaceId">The workspace's durable identity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored <see cref="WorkspaceRegistryEntry"/>, or null if none exists for this ID.</returns>
    /// <exception cref="WorkspaceRegistryCorruptException">
    /// Thrown when a registry entry exists on disk for this ID but cannot be read as a valid entry
    /// (ADR-0026: fail closed, never silently discard or auto-repair a broken manifest).
    /// </exception>
    Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>Returns every workspace entry currently in the registry.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All stored entries. Empty if none exist.</returns>
    /// <exception cref="WorkspaceRegistryCorruptException">
    /// Thrown if any entry encountered while enumerating the registry is corrupt (ADR-0026).
    /// </exception>
    Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default);

    /// <summary>Creates or overwrites the entry for its own <see cref="WorkspaceRegistryEntry.WorkspaceId"/>.</summary>
    /// <param name="entry">The entry to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the entry has been durably stored.</returns>
    Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default);

    /// <summary>Removes the entry for the given workspace ID, if one exists (WIP-037). A no-op if none does.
    /// Default no-op implementation so existing implementers (test doubles) compile unmodified; a real
    /// backend overrides this to actually delete the entry.</summary>
    /// <param name="workspaceId">The workspace's durable identity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when removal has been attempted.</returns>
    Task RemoveAsync(Guid workspaceId, CancellationToken ct = default) => Task.CompletedTask;
}
