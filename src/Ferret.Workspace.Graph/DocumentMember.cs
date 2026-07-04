namespace Ferret.Workspace.Graph;

/// <summary>A member document of a workspace not tied to a repo (<c>02-Workspace-Model.md</c> §3) — notes, specs, or other non-repo content.</summary>
public sealed record DocumentMember
{
    /// <summary>Gets the document's location.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the document's free-form type label (e.g. "notes"). Not a closed enum — no consumer validates this value as of this schema version.</summary>
    public required string Type { get; init; }
}
