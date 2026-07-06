namespace Ferret.Workspace.Graph;

/// <summary>
/// A workspace registry entry (ADR-0026), identified by <see cref="WorkspaceId"/> rather than by
/// any local path. Carries the v1.0 manifest fields from <c>02-Workspace-Model.md</c> §3
/// (<see cref="Kind"/>, <see cref="Members"/>) as of WIP-011, plus the v1.1 <see cref="References"/>
/// field added by WIP-SLICE-2 (<c>03-Cross-Workspace-References.md</c>). <c>sharing</c> (v1.2, Phase 5)
/// is deliberately not carried here — adding it is additive via the <see cref="SchemaVersion"/>
/// upgrade mechanism (ARCH-001 §12.4), not a breaking change to this type or to <see cref="IWorkspaceRegistry"/>.
/// Equality is overridden because the record-generated equality for an <see cref="IReadOnlyList{T}"/>
/// property falls back to reference equality — see <see cref="WorkspaceMembers"/> for the same fix.
/// </summary>
public sealed record WorkspaceRegistryEntry
{
    /// <summary>Gets the durable identity of the workspace (ADR-0026: a UUIDv4, generated client-side, never reused).</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Gets the human-readable workspace name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the schema version of this entry, for the upgrade mechanism in ARCH-001 §12.4.
    /// Stays "1.0" until <see cref="References"/> is non-empty, at which point a writer bumps it to "1.1" —
    /// an existing entry with no references is byte-identical to pre-WIP-SLICE-2 output.</summary>
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

    /// <summary>Gets the workspaces this one references (v1.1, <c>03-Cross-Workspace-References.md</c>). Empty until WIP-SLICE-2's <c>add-reference</c> is used.</summary>
    public IReadOnlyList<WorkspaceReference> References { get; init; } = [];

    /// <summary>Determines whether this instance and another have the same fields, comparing <see cref="References"/> element-wise.</summary>
    /// <param name="other">The instance to compare against.</param>
    /// <returns><see langword="true"/> if every field is equal, including <see cref="References"/> element-wise; otherwise <see langword="false"/>.</returns>
    public bool Equals(WorkspaceRegistryEntry? other) =>
        other is not null
        && WorkspaceId == other.WorkspaceId
        && Name == other.Name
        && SchemaVersion == other.SchemaVersion
        && Kind == other.Kind
        && Members == other.Members
        && References.SequenceEqual(other.References);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(WorkspaceId);
        hash.Add(Name);
        hash.Add(SchemaVersion);
        hash.Add(Kind);
        hash.Add(Members);
        foreach (var reference in References)
        {
            hash.Add(reference);
        }

        return hash.ToHashCode();
    }
}
