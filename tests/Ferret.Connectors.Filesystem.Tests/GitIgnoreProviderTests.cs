using Ferret.Connectors.Filesystem.Ignore;
using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class GitIgnoreProviderTests
{
    [Fact]
    public void ShouldIgnore_Returns_False_For_Non_Filesystem_Uri()
    {
        using var dir = new TempDirectory();
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("jira:///PROJ-1"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_When_No_Gitignore_File()
    {
        using var dir = new TempDirectory();
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///src/Program.cs"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_True_For_File_Matching_Pattern()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(System.IO.Path.Join(dir.Path, ".gitignore"), "*.log\n");
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///debug.log"));

        Assert.True(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_For_File_Not_Matching_Pattern()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(System.IO.Path.Join(dir.Path, ".gitignore"), "*.log\n");
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///src/Program.cs"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Ignores_Comment_Lines()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(System.IO.Path.Join(dir.Path, ".gitignore"), "# this is a comment\n*.log\n");
        var provider = new GitIgnoreProvider(dir.Path);

        Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///readme.md"))));
        Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///output.log"))));
    }

    private static AssetDescriptor MakeAsset(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = System.IO.Path.GetFileName(uri.AbsolutePath),
        LastModified = DateTimeOffset.UtcNow,
    };
}
