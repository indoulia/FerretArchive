using Ferret.Core.Errors;
using Ferret.Core.Workspace.Errors;

namespace Ferret.Core.Tests.Errors;

public sealed class WorkspaceExceptionTests
{
    [Fact]
    public void WorkspaceException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceNotFoundException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspaceNotFoundException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspaceAlreadyExistsException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspaceAlreadyExistsException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspaceConfigurationException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspaceConfigurationException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspaceSchemaVersionException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspaceSchemaVersionException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspaceUpgradeRequiredException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspaceUpgradeRequiredException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspaceUpgradeFailedException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspaceUpgradeFailedException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspacePathTraversalException_Is_WorkspaceException() =>
        Assert.True(typeof(WorkspacePathTraversalException).IsSubclassOf(typeof(WorkspaceException)));

    [Fact]
    public void WorkspaceNotFoundException_Stores_WorkspaceId()
    {
        var ex = new WorkspaceNotFoundException("ws-123");
        Assert.Equal("ws-123", ex.WorkspaceId);
        Assert.Contains("ws-123", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspacePathTraversalException_Stores_Path()
    {
        var ex = new WorkspacePathTraversalException("../../../etc/passwd");
        Assert.Equal("../../../etc/passwd", ex.AttemptedPath);
    }
}
