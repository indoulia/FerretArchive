namespace Ferret.Workspace.Graph;

/// <summary>
/// A reference from one workspace to another (<c>03-Cross-Workspace-References.md</c> §2), added
/// via the v1.1 <see cref="WorkspaceRegistryEntry.SchemaVersion"/> bump. References are always
/// live/federated, never materialized copies (ADR-0027) — this record only carries the edge
/// itself, not any content.
/// </summary>
public sealed record WorkspaceReference
{
    /// <summary>Gets the identity of the referenced workspace.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Gets the access mode. <c>"read-only"</c> is the only value in v1 (ADR-0029 defers write-back modes).</summary>
    public string Mode { get; init; } = "read-only";

    /// <summary>Gets the pinned knowledge state hash, or null for a floating (always-current) reference.
    /// Pinning is out of scope for the vertical slice (WIP-SLICE-1/2, see backlog) — always null until WIP-022.</summary>
    public string? PinnedStateHash { get; init; }
}
