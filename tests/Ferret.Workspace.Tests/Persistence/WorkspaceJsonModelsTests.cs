using System.Text.Json;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests.Persistence;

public sealed class WorkspaceJsonModelsTests
{
    [Fact]
    public void WorkspaceManifest_SerializesContextOsFields()
    {
        var manifest = new WorkspaceManifest
        {
            Id = "ws-001",
            Name = "my-project",
            SchemaVersion = "1.0",
            FerretVersion = "0.7.0",
            ContextOsVersion = "1.0",
            CreatedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
            WorkspaceType = "repository",
        };

        var json = JsonSerializer.Serialize(manifest);
        var restored = JsonSerializer.Deserialize<WorkspaceManifest>(json)!;

        Assert.Equal("ws-001", restored.Id);
        Assert.Equal("1.0", restored.ContextOsVersion);
        Assert.Equal("repository", restored.WorkspaceType);
        Assert.Contains("contextOsVersion", json, StringComparison.Ordinal);
        Assert.Contains("workspaceType", json, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceStateDto_NestedStatistics_RoundTrips()
    {
        var dto = new WorkspaceStateDto
        {
            KnowledgeVersion = 1,
            GraphVersion = 2,
            Statistics = new StatisticsDto { TotalFiles = 50, IndexedFiles = 40, SchemaVersion = "1.0" },
        };

        var json = JsonSerializer.Serialize(dto);
        var restored = JsonSerializer.Deserialize<WorkspaceStateDto>(json)!;

        Assert.Equal(1, restored.KnowledgeVersion);
        Assert.Equal(2, restored.GraphVersion);
        Assert.Equal(50, restored.Statistics.TotalFiles);
        Assert.Equal(40, restored.Statistics.IndexedFiles);
    }

    [Fact]
    public void WorkspaceStateDto_LastIndex_NullableRoundTrips()
    {
        var dto = new WorkspaceStateDto { LastIndex = null };
        var json = JsonSerializer.Serialize(dto);
        var restored = JsonSerializer.Deserialize<WorkspaceStateDto>(json)!;
        Assert.Null(restored.LastIndex);
    }

    [Fact]
    public void ConnectorStateDto_RoundTrips()
    {
        var state = new ConnectorStateDto { Enabled = true, LastSyncAt = DateTimeOffset.UtcNow };
        var json = JsonSerializer.Serialize(state);
        var restored = JsonSerializer.Deserialize<ConnectorStateDto>(json)!;
        Assert.True(restored.Enabled);
        Assert.NotNull(restored.LastSyncAt);
    }
}
