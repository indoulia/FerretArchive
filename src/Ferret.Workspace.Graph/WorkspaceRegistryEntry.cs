namespace Ferret.Workspace.Graph;

/// <summary>
/// A workspace registry entry (ADR-0026), identified by <see cref="WorkspaceId"/> rather than by
/// any local path. Carries the v1.0 manifest fields from <c>02-Workspace-Model.md</c> §3
/// (<see cref="Kind"/>, <see cref="Members"/>) as of WIP-011. <c>references</c> (v1.1, Phase 2) and
/// <c>sharing</c> (v1.2, Phase 5) are deliberately not carried here — adding them is additive via
/// the <see cref="SchemaVersion"/> upgrade mechanism (ARCH-001 §12.4), not a breaking change to
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

    /// <summary>
    /// Gets the workspace kind (<c>02-Workspace-Model.md</c> §3). A plain string, not a closed C#
    /// enum: v1.0 only writes <c>"personal"</c> or <c>"team"</c> (the two values with an actual
    /// Phase 1–4 consumer), but a string lets an older reader tolerate a value introduced by a
    /// later schema version instead of throwing — the same forward-compatibility rationale as an
    /// unrecognized JSON field. Enum-level validation of what a v1.0 *writer* is allowed to produce
    /// is a WIP-012 (CLI) concern, not this persistence layer's.
    /// </summary>
    public string Kind { get; init; } = "personal";

    /// <summary>Gets the workspace's member repos and documents.</summary>
    public WorkspaceMembers Members { get; init; } = WorkspaceMembers.Empty;
}
