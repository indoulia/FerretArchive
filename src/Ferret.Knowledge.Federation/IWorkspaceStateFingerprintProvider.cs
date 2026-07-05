using Ferret.Workspace.Graph;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// Computes the Workspace State Fingerprint (ADR-0027 Amendment, <c>13-Storage.md</c> §4) used by
/// pinned references (<c>03-Cross-Workspace-References.md</c> §3). The fingerprint is always
/// derived on demand — never persisted by this abstraction — and reflects only the given
/// workspace's own member repos, not any workspace it references.
/// </summary>
public interface IWorkspaceStateFingerprintProvider
{
    /// <summary>Computes the current fingerprint for a workspace's own member repos.</summary>
    /// <param name="entry">The workspace whose state to fingerprint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A deterministic, portable fingerprint string, or <see langword="null"/> if it cannot
    /// be computed (e.g. a member repo's local checkout is unreachable) — callers must treat a null
    /// result as fail-closed, the same as a mismatch.</returns>
    Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default);
}
