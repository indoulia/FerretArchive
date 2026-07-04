namespace Ferret.Workspace.Graph;

/// <summary>
/// A workspace registry entry (ADR-0026), identified by <see cref="WorkspaceId"/> rather than by
/// any local path. This is intentionally minimal — WIP-010 (registry storage mechanics) does not
/// carry the full manifest schema (<c>kind</c>, member repos/documents) from
/// <c>02-Workspace-Model.md</c> §3, which is WIP-011's scope. Adding those fields later is additive
/// via the <see cref="SchemaVersion"/> upgrade mechanism (ARCH-001 §12.4), not a breaking change to
/// this type or to <see cref="IWorkspaceRegistry"/>.
/// </summary>
public sealed record WorkspaceRegistryEntry
{
    /// <summary>Gets the durable identity of the workspace (ADR-0026: a UUIDv4, generated client-side, never reused).</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Gets the human-readable workspace name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the schema version of this entry, for the upgrade mechanism in ARCH-001 §12.4.</summary>
    public string SchemaVersion { get; init; } = "1.0";
}
