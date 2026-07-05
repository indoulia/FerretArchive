namespace Ferret.Workspace.Graph;

/// <summary>
/// The member repos and documents of a workspace (<c>02-Workspace-Model.md</c> §3, the "members"
/// object). Both collections are optional — a freshly created workspace has neither yet.
/// Equality is overridden to compare <see cref="Repos"/> and <see cref="Documents"/> element-wise:
/// the record-generated equality for an <see cref="IReadOnlyList{T}"/> property falls back to
/// reference equality (neither <see cref="List{T}"/> nor the interface overrides <c>Equals</c>),
/// which would make two workspaces with identical members compare as unequal purely because
/// deserialization produced a different list instance. This is the minimal fix for that, not a
/// new abstraction — no interface or extensibility point is introduced.
/// </summary>
public sealed record WorkspaceMembers
{
    /// <summary>Gets the empty member set — the default for a workspace with no repos or documents added yet.</summary>
    public static WorkspaceMembers Empty { get; } = new();

    /// <summary>Gets the member repositories.</summary>
    public IReadOnlyList<RepoMember> Repos { get; init; } = [];

    /// <summary>Gets the member documents not tied to a repo.</summary>
    public IReadOnlyList<DocumentMember> Documents { get; init; } = [];

    /// <summary>Determines whether this instance and another have the same members, in the same order.</summary>
    /// <param name="other">The instance to compare against.</param>
    /// <returns><c>true</c> if <see cref="Repos"/> and <see cref="Documents"/> are element-wise equal; otherwise <c>false</c>.</returns>
    public bool Equals(WorkspaceMembers? other) =>
        other is not null
        && Repos.SequenceEqual(other.Repos)
        && Documents.SequenceEqual(other.Documents);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var repo in Repos)
        {
            hash.Add(repo);
        }

        foreach (var document in Documents)
        {
            hash.Add(document);
        }

        return hash.ToHashCode();
    }
}
