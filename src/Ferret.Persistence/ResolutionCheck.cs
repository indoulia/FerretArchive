using Ferret.Core.Connectors;

namespace Ferret.Persistence;

/// <summary>
/// Performs ARCH-033 §5's comparison procedure. <see cref="Compare"/> covers Sprint 1's single
/// dependency shape (source content, ARCH-025 §3 shape 1) exactly as before — unchanged by S2-7,
/// still a pure, synchronous, single-shape comparison (protected by the S2-0 architecture
/// regression test). S2-7 adds three siblings, not a replacement: <see cref="CompareConfiguration"/>
/// (shape 4), <see cref="CompareChainAsync"/> (shape 2, ARCH-029 §6's transitive-chain rule), and
/// <see cref="Combine"/> (the shared combination rule both of them, and any caller composing
/// several shapes' outcomes, use).
/// </summary>
public static class ResolutionCheck
{
    /// <summary>Compares a candidate's recorded source-content fingerprint against the current one.</summary>
    /// <param name="recordReadable">Whether the persisted record could be read at all (ARCH-032 §5's integrity signal).</param>
    /// <param name="recordedFingerprint">The fingerprint recorded for the candidate, or null if none is available.</param>
    /// <param name="currentFingerprint">The current, freshly computed fingerprint of the source content.</param>
    /// <returns>Satisfied if the fingerprints match; Not-satisfied if they differ; Indeterminate if the record could not be read.</returns>
    public static ResolutionOutcome Compare(bool recordReadable, AssetFingerprint? recordedFingerprint, AssetFingerprint currentFingerprint)
    {
        ArgumentNullException.ThrowIfNull(currentFingerprint);

        if (!recordReadable)
        {
            return ResolutionOutcome.Indeterminate;
        }

        return recordedFingerprint == currentFingerprint ? ResolutionOutcome.Satisfied : ResolutionOutcome.NotSatisfied;
    }

    /// <summary>
    /// Compares a candidate's recorded dependency-shape-4 identity (S2-5, ARCH-032 §2.1) against
    /// the current one. A record that never captured this shape (<paramref name="recorded"/> is
    /// null) asserts nothing about it, so it cannot be violated — Satisfied. A record that did
    /// capture it, evaluated where no current value is available to compare against, cannot be
    /// determined — Indeterminate, the same fail-closed answer <see cref="Compare"/> gives for an
    /// unreadable record.
    /// </summary>
    /// <param name="recorded">The configuration/registration identity recorded at production time, or null if this artifact never tracked one.</param>
    /// <param name="current">The current configuration/registration identity, or null if it cannot be determined.</param>
    /// <returns>Satisfied, Not-satisfied, or Indeterminate per the rule above.</returns>
    public static ResolutionOutcome CompareConfiguration(ConfigurationDependency? recorded, ConfigurationDependency? current)
    {
        if (recorded is null)
        {
            return ResolutionOutcome.Satisfied;
        }

        if (current is null)
        {
            return ResolutionOutcome.Indeterminate;
        }

        return IdentitiesMatch(recorded.Parser, current.Parser) && IdentitiesMatch(recorded.Connector, current.Connector)
            ? ResolutionOutcome.Satisfied
            : ResolutionOutcome.NotSatisfied;
    }

    /// <summary>
    /// Combines several independently-computed outcomes into one, per ARCH-029 §6's rule:
    /// Not-satisfied outranks Indeterminate, which outranks Satisfied. An empty input combines to
    /// Satisfied — there is nothing present to be unsatisfied about, the same vacuous-truth answer
    /// <see cref="DependencyChain.Empty"/> represents for a chain with no links.
    /// </summary>
    /// <param name="outcomes">The outcomes to combine.</param>
    /// <returns>The combined outcome.</returns>
    public static ResolutionOutcome Combine(IEnumerable<ResolutionOutcome> outcomes)
    {
        ArgumentNullException.ThrowIfNull(outcomes);

        var sawIndeterminate = false;
        foreach (var outcome in outcomes)
        {
            if (outcome == ResolutionOutcome.NotSatisfied)
            {
                return ResolutionOutcome.NotSatisfied;
            }

            if (outcome == ResolutionOutcome.Indeterminate)
            {
                sawIndeterminate = true;
            }
        }

        return sawIndeterminate ? ResolutionOutcome.Indeterminate : ResolutionOutcome.Satisfied;
    }

    /// <summary>
    /// Evaluates a dependency-shape-2 chain (S2-6, ARCH-025 §3) per ARCH-029 §6: a chain is
    /// Satisfied only if every reference in it, followed transitively, independently resolves to
    /// Satisfied. Each reference is followed by fetching the referenced artifact's own persisted
    /// record through <paramref name="store"/> — never by trusting an embedded copy, since none
    /// exists (<see cref="DependencyReference"/> carries no such copy by design). A missing or
    /// unreadable referenced record is fail-closed Indeterminate for that reference, the same
    /// answer <see cref="Compare"/> gives for an unreadable top-level record — this mechanism
    /// checks only whether each edge and its transitive closure are intact; it does not, and
    /// cannot, recompute a referenced artifact's own shape-1/shape-4 currency, since doing so
    /// would require the referenced artifact's owning engine's own knowledge of "current" state,
    /// which this generic, ownership-respecting traversal does not have and must not assume
    /// (ARCH-023 §9 — V2 never performs an owning engine's work).
    /// </summary>
    /// <param name="chain">The chain to evaluate.</param>
    /// <param name="store">The store to fetch each referenced record from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The combined outcome across the chain's full transitive closure.</returns>
    public static async Task<ResolutionOutcome> CompareChainAsync(DependencyChain chain, IDependencyStateStore store, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(store);

        return await CompareLinksAsync(chain, store, [], ct).ConfigureAwait(false);
    }

    private static async Task<ResolutionOutcome> CompareLinksAsync(
        DependencyChain chain,
        IDependencyStateStore store,
        HashSet<(string EngineResponsibility, string RequestPath)> visited,
        CancellationToken ct)
    {
        var outcomes = new List<ResolutionOutcome>(chain.References.Count);
        foreach (var reference in chain.References)
        {
            outcomes.Add(await CompareLinkAsync(reference, store, visited, ct).ConfigureAwait(false));
        }

        return Combine(outcomes);
    }

    private static async Task<ResolutionOutcome> CompareLinkAsync(
        DependencyReference reference,
        IDependencyStateStore store,
        HashSet<(string EngineResponsibility, string RequestPath)> visited,
        CancellationToken ct)
    {
        var key = (reference.EngineResponsibility, reference.RequestPath);
        if (!visited.Add(key))
        {
            // A reference cycle is a structurally unexpected input this mechanism cannot safely
            // resolve by recursing further — fail closed rather than recurse forever. This is a
            // correctness guard against non-termination, not a cache or a graph-optimization.
            return ResolutionOutcome.Indeterminate;
        }

        // S2-8: FileDependencyStateStore itself classifies and fail-closes on an unreadable
        // referenced record (malformed content, I/O failure) — it never throws, only returns
        // null, so this caller stays purely in terms of the IDependencyStateStore abstraction.
        var referenced = await store.GetRecordAsync(reference.EngineResponsibility, reference.RequestPath, ct).ConfigureAwait(false);

        if (referenced is null)
        {
            return ResolutionOutcome.Indeterminate;
        }

        return await CompareLinksAsync(referenced.DependencyChain, store, visited, ct).ConfigureAwait(false);
    }

    private static bool IdentitiesMatch(ComponentRegistrationIdentity? recorded, ComponentRegistrationIdentity? current) =>
        recorded is null ? current is null : recorded == current;
}
