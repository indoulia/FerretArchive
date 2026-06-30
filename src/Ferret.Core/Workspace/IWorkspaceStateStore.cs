namespace Ferret.Core.Workspace;

/// <summary>Persists and retrieves workspace state statistics between platform invocations.</summary>
public interface IWorkspaceStateStore
{
    /// <summary>Reads the stored statistics for the workspace at <paramref name="rootPath"/>.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="WorkspaceStatistics"/>.</returns>
    Task<WorkspaceStatistics> ReadStatisticsAsync(WorkspacePath rootPath, CancellationToken ct = default);

    /// <summary>Persists updated statistics for the workspace at <paramref name="rootPath"/>.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="statistics">The statistics to write.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteStatisticsAsync(WorkspacePath rootPath, WorkspaceStatistics statistics, CancellationToken ct = default);
}
