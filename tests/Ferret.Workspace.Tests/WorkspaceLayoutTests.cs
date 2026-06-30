namespace Ferret.Workspace.Tests;

public sealed class WorkspaceLayoutTests
{
    [Fact]
    public void RootDirectoryName_IsDotFerret() =>
        Assert.Equal(".ferret", WorkspaceLayout.RootDirectoryName);

    [Fact]
    public void ManifestFileName_IsWorkspaceJson() =>
        Assert.Equal("workspace.json", WorkspaceLayout.ManifestFileName);

    [Fact]
    public void StateFileName_IsStateJson() =>
        Assert.Equal("state.json", WorkspaceLayout.StateFileName);

    [Fact]
    public void AllDirectories_ContainsTopLevelContextOsDirectories()
    {
        Assert.Contains("config", WorkspaceLayout.AllDirectories);
        Assert.Contains("connectors", WorkspaceLayout.AllDirectories);
        Assert.Contains("indexes", WorkspaceLayout.AllDirectories);
        Assert.Contains("knowledge", WorkspaceLayout.AllDirectories);
        Assert.Contains("memory", WorkspaceLayout.AllDirectories);
        Assert.Contains("models", WorkspaceLayout.AllDirectories);
        Assert.Contains("snapshots", WorkspaceLayout.AllDirectories);
        Assert.Contains("telemetry", WorkspaceLayout.AllDirectories);
        Assert.Contains("temp", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedConnectorDirectories()
    {
        Assert.Contains("connectors/git", WorkspaceLayout.AllDirectories);
        Assert.Contains("connectors/jira", WorkspaceLayout.AllDirectories);
        Assert.Contains("connectors/filesystem", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedIndexDirectories()
    {
        Assert.Contains("indexes/semantic", WorkspaceLayout.AllDirectories);
        Assert.Contains("indexes/keyword", WorkspaceLayout.AllDirectories);
        Assert.Contains("indexes/graph", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedMemoryDirectories()
    {
        Assert.Contains("memory/working", WorkspaceLayout.AllDirectories);
        Assert.Contains("memory/episodic", WorkspaceLayout.AllDirectories);
        Assert.Contains("memory/longterm", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedSnapshotDirectories()
    {
        Assert.Contains("snapshots/workspace", WorkspaceLayout.AllDirectories);
        Assert.Contains("snapshots/indexes", WorkspaceLayout.AllDirectories);
        Assert.Contains("snapshots/knowledge", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void ConfigFileNames_ContainsFourFiles()
    {
        Assert.Contains("runtime.json", WorkspaceLayout.ConfigFileNames);
        Assert.Contains("plugins.json", WorkspaceLayout.ConfigFileNames);
        Assert.Contains("models.json", WorkspaceLayout.ConfigFileNames);
        Assert.Contains("connectors.json", WorkspaceLayout.ConfigFileNames);
        Assert.Equal(4, WorkspaceLayout.ConfigFileNames.Count);
    }
}
