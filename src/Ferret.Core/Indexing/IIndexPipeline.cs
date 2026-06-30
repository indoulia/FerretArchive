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
}
