using Ferret.Core.Workspace.Errors;

using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspaceExceptionNamespaceTests
{
    [Fact]
    public void WorkspaceException_IsInWorkspaceErrorsNamespace()
    {
        Assert.Equal("Ferret.Core.Workspace.Errors", typeof(WorkspaceException).Namespace);
    }

    [Fact]
    public void WorkspaceNotFoundException_IsInWorkspaceErrorsNamespace()
    {
        Assert.Equal("Ferret.Core.Workspace.Errors", typeof(WorkspaceNotFoundException).Namespace);
    }

    [Fact]
    public void WorkspaceAlreadyExistsException_DerivesFromWorkspaceException()
    {
        Assert.True(typeof(WorkspaceException).IsAssignableFrom(typeof(WorkspaceAlreadyExistsException)));
    }

    [Fact]
    public void WorkspacePathTraversalException_IsInWorkspaceErrorsNamespace()
    {
        Assert.Equal("Ferret.Core.Workspace.Errors", typeof(WorkspacePathTraversalException).Namespace);
    }
}
