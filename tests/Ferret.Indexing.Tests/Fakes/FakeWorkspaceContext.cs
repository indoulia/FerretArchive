using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for <see cref="IWorkspaceContext"/>. Uses a temp directory as workspace root.</summary>
internal sealed class FakeWorkspaceContext : IWorkspaceContext
{
    /// <summary>Initializes a new instance with a temp-directory workspace root.</summary>
    internal FakeWorkspaceContext()
    {
        WorkspaceId = WorkspaceId.Create("test-workspace");
        WorkspaceRoot = WorkspacePath.Create(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
    }

    /// <inheritdoc/>
    public WorkspaceId WorkspaceId { get; }

    /// <inheritdoc/>
    public WorkspacePath WorkspaceRoot { get; }
}
