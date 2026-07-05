namespace Ferret.Workspace.Graph;

/// <summary>
/// A member repository of a workspace (<c>02-Workspace-Model.md</c> §3). <see cref="Remote"/> is
/// the repo's identity per ADR-0026's Identity Rules — canonicalization and fallback resolution
/// (no-remote, multi-remote) are a WIP-012 (CLI) concern; this type stores whatever identity
/// string it is given, unmodified.
/// </summary>
public sealed record RepoMember
{
    /// <summary>Gets the repo's canonicalized remote identity (ADR-0026), or a locally-generated fallback identity for a repo with no remote.</summary>
    public required string Remote { get; init; }

    /// <summary>Gets the cached local checkout path, or null if not yet resolved on this machine. Never used as the identity itself (ADR-0026).</summary>
    public string? LocalPath { get; init; }
}
