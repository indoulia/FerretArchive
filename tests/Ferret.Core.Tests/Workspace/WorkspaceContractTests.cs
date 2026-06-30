using Ferret.Core.Abstractions;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspaceContractTests
{
    private static readonly string[] StepOne = { "step-001" };

    [Fact]
    public void HealthCheckDepth_HasExpectedValues()
    {
        Assert.Equal(0, (int)HealthCheckDepth.Quick);
        Assert.Equal(1, (int)HealthCheckDepth.Deep);
    }

    [Fact]
    public void WorkspaceOptions_DefaultIsReadOnly_False()
    {
        var options = new WorkspaceOptions();
        Assert.False(options.ReadOnly);
    }

    [Fact]
    public void WorkspaceMetadata_Create_StoresValues()
    {
        var meta = WorkspaceMetadata.Create("My Project", "A test project", "1.0", DateTimeOffset.UtcNow);
        Assert.Equal("My Project", meta.Name);
        Assert.Equal("A test project", meta.Description);
        Assert.Equal("1.0", meta.SchemaVersion);
    }

    [Fact]
    public void WorkspaceCapabilities_Create_StoresValues()
    {
        var caps = WorkspaceCapabilities.Create(readOnly: false, pluginCount: 2, indexedFileCount: 150);
        Assert.False(caps.ReadOnly);
        Assert.Equal(2, caps.PluginCount);
        Assert.Equal(150, caps.IndexedFileCount);
    }

    [Fact]
    public void WorkspaceStatistics_Create_StoresValues()
    {
        var stats = WorkspaceStatistics.Create(totalFiles: 500, indexedFiles: 450, lastIndexed: DateTimeOffset.UtcNow, schemaVersion: "1.0");
        Assert.Equal(500, stats.TotalFiles);
        Assert.Equal(450, stats.IndexedFiles);
        Assert.Equal("1.0", stats.SchemaVersion);
    }

    [Fact]
    public void WorkspaceContext_Create_StoresPath()
    {
        var path = WorkspacePath.Create(@"C:\repos\project");
        var id = WorkspaceId.Create("ws-001");
        var meta = WorkspaceMetadata.Create("Project", string.Empty, "1.0", DateTimeOffset.UtcNow);
        var caps = WorkspaceCapabilities.Create(false, 0, 0);

        var ctx = WorkspaceContext.Create(path, id, meta, caps);

        Assert.Equal(path, ctx.RootPath);
        Assert.Equal(id, ctx.Id);
    }

    [Fact]
    public void Changeset_Create_StoreCounts()
    {
        var added = new[] { "file1.cs" };
        var modified = new[] { "file2.cs" };
        var deleted = new[] { "file3.cs" };

        var changeset = Changeset.Create(added, modified, deleted, DateTimeOffset.UtcNow);

        Assert.Single(changeset.Added);
        Assert.Single(changeset.Modified);
        Assert.Single(changeset.Deleted);
    }

    [Fact]
    public void WorkspaceInitResult_Succeeded_HasContext()
    {
        var path = WorkspacePath.Create(@"C:\repos\project");
        var id = WorkspaceId.Create("ws-001");
        var meta = WorkspaceMetadata.Create("Project", string.Empty, "1.0", DateTimeOffset.UtcNow);
        var caps = WorkspaceCapabilities.Create(false, 0, 0);
        var ctx = WorkspaceContext.Create(path, id, meta, caps);

        var result = WorkspaceInitResult.Success(ctx);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Context);
    }

    [Fact]
    public void WorkspaceInitResult_Failed_HasErrorMessage()
    {
        var result = WorkspaceInitResult.Failure("Workspace already exists.");
        Assert.False(result.Succeeded);
        Assert.Equal("Workspace already exists.", result.ErrorMessage);
        Assert.Null(result.Context);
    }

    [Fact]
    public void WorkspaceUpgradeResult_Succeeded_HasVersions()
    {
        var result = WorkspaceUpgradeResult.Success("1.0", "2.0", StepOne);
        Assert.True(result.Succeeded);
        Assert.Equal("1.0", result.FromVersion);
        Assert.Equal("2.0", result.ToVersion);
    }
}
