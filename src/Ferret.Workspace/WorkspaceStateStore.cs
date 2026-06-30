using Ferret.Core.Workspace;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace;

/// <summary>Reads and writes workspace statistics from the statistics sub-object in state.json.</summary>
public sealed class WorkspaceStateStore : IWorkspaceStateStore
{
    /// <inheritdoc/>
    public async Task<WorkspaceStatistics> ReadStatisticsAsync(WorkspacePath rootPath, CancellationToken ct = default)
    {
        var dto = await JsonWorkspaceStore.ReadStateAsync(rootPath, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return WorkspaceStatistics.Create(0, 0, DateTimeOffset.MinValue, "1.0");
        }

        var s = dto.Statistics;
        return WorkspaceStatistics.Create(
            s.TotalFiles,
            s.IndexedFiles,
            s.LastIndexedAt ?? DateTimeOffset.MinValue,
            s.SchemaVersion);
    }

    /// <inheritdoc/>
    public async Task WriteStatisticsAsync(WorkspacePath rootPath, WorkspaceStatistics statistics, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        var dto = await JsonWorkspaceStore.ReadStateAsync(rootPath, ct).ConfigureAwait(false) ?? new WorkspaceStateDto();
        dto.Statistics = new StatisticsDto
        {
            TotalFiles = statistics.TotalFiles,
            IndexedFiles = statistics.IndexedFiles,
            LastIndexedAt = statistics.LastIndexed == DateTimeOffset.MinValue ? null : statistics.LastIndexed,
            SchemaVersion = statistics.SchemaVersion,
        };
        await JsonWorkspaceStore.WriteStateAsync(rootPath, dto, ct).ConfigureAwait(false);
    }
}
