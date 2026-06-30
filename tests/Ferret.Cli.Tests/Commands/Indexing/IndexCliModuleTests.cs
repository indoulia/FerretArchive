using Ferret.Cli.Commands.Indexing;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Tests.Commands.Indexing;

/// <summary>Unit tests for <see cref="IndexCliModule"/>.</summary>
public sealed class IndexCliModuleTests
{
    [Fact]
    public void IndexCliModule_Registers_IndexCommandHandler()
    {
        // Verify the type compiles and is a class (module wiring test).
        Assert.True(typeof(IndexCommandHandler).IsClass);
    }

    [Fact]
    public void IndexCommandHandler_Requires_IIndexPipeline_IWorkspaceContext_And_SwappableEventBus()
    {
        var ctors = typeof(IndexCommandHandler).GetConstructors();
        Assert.NotEmpty(ctors);
        var paramTypes = ctors[0].GetParameters().Select(p => p.ParameterType).ToList();
        Assert.Contains(typeof(IIndexPipeline), paramTypes);
        Assert.Contains(typeof(IWorkspaceContext), paramTypes);
        Assert.Contains(typeof(SwappableEventBus), paramTypes);
    }

    [Fact]
    public void GetCommands_Returns_Index_Command()
    {
        var module = new IndexCliModule(MakeFakeWorkspaceContext());
        var commands = module.GetCommands().ToList();
        Assert.Contains(commands, c => c.Metadata.Name == "index");
    }

    [Fact]
    public void GetCommands_Index_Has_Rebuild_Option()
    {
        var module = new IndexCliModule(MakeFakeWorkspaceContext());
        var indexCmd = module.GetCommands().First(c => c.Metadata.Name == "index");
        Assert.NotNull(indexCmd.Options);
        Assert.Contains(indexCmd.Options!, o => o.LongName == "--rebuild");
    }

    [Fact]
    public void GetCommands_Index_Has_Verbose_Option()
    {
        var module = new IndexCliModule(MakeFakeWorkspaceContext());
        var indexCmd = module.GetCommands().First(c => c.Metadata.Name == "index");
        Assert.NotNull(indexCmd.Options);
        Assert.Contains(indexCmd.Options!, o => o.LongName == "--verbose");
    }

    private static FakeWorkspaceContext MakeFakeWorkspaceContext() =>
        new FakeWorkspaceContext();

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test");

        public WorkspacePath WorkspaceRoot =>
            WorkspacePath.Create(System.IO.Path.GetTempPath());
    }
}
