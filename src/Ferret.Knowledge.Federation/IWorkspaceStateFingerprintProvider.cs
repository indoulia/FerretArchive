using Ferret.Workspace.Graph;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// Computes the Workspace State Fingerprint (ADR-0027 Amendment, <c>13-Storage.md</c> §4) used by
/// pinned references (<c>03-Cross-Workspace-References.md</c> §3). The fingerprint is always
/// derived on demand — never persisted by this abstraction — and reflects only the given
/// workspace's own member repos, not any workspace it references.
/// </summary>
/// <remarks>
/// This interface deliberately exposes two different change-detection mechanisms, and they are
/// <b>not interchangeable</b> — do not "simplify" a caller to use only one of them:
/// <list type="bullet">
/// <item><description><see cref="ComputeFingerprintAsync"/> tracks the workspace's own file content,
/// independently of indexing, because a <i>pinned</i> reference's drift-check (ADR-0027) must detect
/// any content change, indexed or not — that is the whole point of pinning.</description></item>
/// <item><description><see cref="ComputeIndexChangeSignalAsync"/> tracks the workspace's search index
/// instead, because a <i>floating</i> reference's cache validity (P3-002) only needs to know whether a
/// federated query's result could differ — and that is derived exclusively from indexed content, so
/// tracking anything more (or less) than the index is either wasted work or a correctness gap.</description></item>
/// </list>
/// See <c>26-P3-002-Query-Cache-Regression.md</c> for the measurements that made this split necessary.
/// </remarks>
public interface IWorkspaceStateFingerprintProvider
{
    /// <summary>Computes the current fingerprint for a workspace's own member repos.</summary>
    /// <param name="entry">The workspace whose state to fingerprint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A deterministic, portable fingerprint string, or <see langword="null"/> if it cannot
    /// be computed (e.g. a member repo's local checkout is unreachable) — callers must treat a null
    /// result as fail-closed, the same as a mismatch.</returns>
    Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Computes a cheap, best-effort signal for whether a workspace's currently-indexed (searchable)
    /// content may have changed since it was last observed (P3-002). Unlike
    /// <see cref="ComputeFingerprintAsync"/>, this reflects only the on-disk keyword index artifact's
    /// own state (existence, size, last-write time) — never per-file content — and is <b>not</b> a
    /// portable, content-based fingerprint (two checkouts of identical content are not guaranteed to
    /// produce the same signal). It exists solely to gate cache validity for a <i>floating</i>
    /// reference, where all that matters is "would a fresh query see different results" — which is
    /// entirely determined by what the index currently contains, never by content that was never
    /// indexed. A pinned reference's drift-check must keep using <see cref="ComputeFingerprintAsync"/>.
    /// </summary>
    /// <param name="entry">The workspace whose indexed state to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A signal string that changes whenever the workspace's keyword index changes, or
    /// <see langword="null"/> if it cannot be determined (e.g. no index built yet) — callers must
    /// treat a null result as fail-closed, the same as a mismatch.</returns>
    Task<string?> ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default);
}
