using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Indexing;

/// <summary>Orchestrates a complete discover → parse → index pipeline run.
/// This is the primary entry point for the CLI and any automation layer.</summary>
public interface IIndexPipeline
{
    /// <summary>Runs the full index pipeline and returns the aggregated result.</summary>
    /// <param name="workspaceId">The workspace identifier scoping this pipeline run.</param>
    /// <param name="options">Options controlling this run (instance filter, force rebuild).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to an <see cref="IndexResult"/> summary of the run.</returns>
    Task<IndexResult> RunAsync(WorkspaceId workspaceId, IndexPipelineOptions options, CancellationToken ct = default);

    /// <summary>Reindexes a single already-known-changed asset without a full corpus discovery walk.
    /// Used by incremental callers (e.g. watch-mode reindexing, issue #17) that already know which
    /// asset changed. Removes it from the index and state store if it no longer resolves (deleted,
    /// moved out of scope, or newly ignored). Does not run the global stale-asset sweep — that stays
    /// O(corpus) and is <see cref="RunAsync"/>'s responsibility.
    /// The default implementation delegates to <see cref="RunAsync"/> with <see cref="IndexPipelineOptions.Default"/>,
    /// so adding this member is a non-breaking addition per ADR-0012 rule 2 — existing implementors
    /// do not need to change (they simply forgo the single-asset optimisation).</summary>
    /// <param name="workspaceId">The workspace identifier scoping this run.</param>
    /// <param name="assetId">The canonical Id of the changed asset.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to an <see cref="IndexResult"/> summary scoped to this one asset.</returns>
    Task<IndexResult> RunSingleAssetAsync(WorkspaceId workspaceId, AssetId assetId, CancellationToken ct = default) =>
        RunAsync(workspaceId, IndexPipelineOptions.Default, ct);
}
