using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Workspace;

using Xunit;

namespace Ferret.Core.Tests.Workspace;

/// <summary>Tests for <see cref="IWorkspaceContext"/>, <see cref="IndexLayout"/>, and <see cref="DefaultWorkspaceContext"/>.</summary>
public sealed class WorkspaceContextTests
{
    /// <summary>IWorkspaceContext is an interface.</summary>
    [Fact]
    public void IWorkspaceContext_Is_An_Interface()
    {
        Assert.True(typeof(IWorkspaceContext).IsInterface);
    }

    /// <summary>IWorkspaceContext has a WorkspaceId property of the correct type.</summary>
    [Fact]
    public void IWorkspaceContext_Has_WorkspaceId_Property()
    {
        var prop = typeof(IWorkspaceContext).GetProperty("WorkspaceId");

        Assert.NotNull(prop);
        Assert.Equal(typeof(WorkspaceId), prop.PropertyType);
    }

    /// <summary>IWorkspaceContext has a WorkspaceRoot property of the correct type.</summary>
    [Fact]
    public void IWorkspaceContext_Has_WorkspaceRoot_Property()
    {
        var prop = typeof(IWorkspaceContext).GetProperty("WorkspaceRoot");

        Assert.NotNull(prop);
        Assert.Equal(typeof(WorkspacePath), prop.PropertyType);
    }

    /// <summary>DefaultWorkspaceContext exposes the WorkspaceId passed to the constructor.</summary>
    [Fact]
    public void DefaultWorkspaceContext_Exposes_WorkspaceId_Correctly()
    {
        var id = WorkspaceId.Create("test-id");
        var path = WorkspacePath.Create(Environment.CurrentDirectory);
        var ctx = new DefaultWorkspaceContext(id, path);

        Assert.Equal(id, ctx.WorkspaceId);
    }

    /// <summary>DefaultWorkspaceContext exposes the WorkspaceRoot passed to the constructor.</summary>
    [Fact]
    public void DefaultWorkspaceContext_Exposes_WorkspaceRoot_Correctly()
    {
        var id = WorkspaceId.Create("test-id");
        var path = WorkspacePath.Create(Environment.CurrentDirectory);
        var ctx = new DefaultWorkspaceContext(id, path);

        Assert.Equal(path, ctx.WorkspaceRoot);
    }

    /// <summary>IndexLayout.IndexDirectoryName is "indexes".</summary>
    [Fact]
    public void IndexLayout_IndexDirectoryName_Is_Indexes()
    {
        Assert.Equal("indexes", IndexLayout.IndexDirectoryName);
    }

    /// <summary>IndexLayout.KeywordDirectoryName is "keyword".</summary>
    [Fact]
    public void IndexLayout_KeywordDirectoryName_Is_Keyword()
    {
        Assert.Equal("keyword", IndexLayout.KeywordDirectoryName);
    }

    /// <summary>IndexLayout.KeywordDatabaseFileName is "keyword-index.db".</summary>
    [Fact]
    public void IndexLayout_KeywordDatabaseFileName_Is_Keyword_Index_Db()
    {
        Assert.Equal("keyword-index.db", IndexLayout.KeywordDatabaseFileName);
    }

    /// <summary>IndexLayout constants combine to the expected relative path.</summary>
    [Fact]
    public void IndexLayout_Constants_Combine_To_Correct_Relative_Path()
    {
        var relative = Path.Join(
            WorkspaceLayout.RootDirectoryName,
            IndexLayout.IndexDirectoryName,
            IndexLayout.KeywordDirectoryName,
            IndexLayout.KeywordDatabaseFileName);

        Assert.Equal(
            Path.Join(".ferret", "indexes", "keyword", "keyword-index.db"),
            relative);
    }

    /// <summary>IProgressReporter is an interface.</summary>
    [Fact]
    public void IProgressReporter_Is_An_Interface()
    {
        Assert.True(typeof(Ferret.Core.Indexing.IProgressReporter).IsInterface);
    }
}
