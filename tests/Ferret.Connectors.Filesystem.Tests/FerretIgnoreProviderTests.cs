using Ferret.Connectors.Filesystem.Ignore;
using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FerretIgnoreProviderTests
{
    [Fact]
    public void ShouldIgnore_Returns_False_When_No_FerretIgnore_File()
    {
        using var dir = new TempDirectory();
        var provider = new FerretIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///src/Program.cs"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_For_Non_Filesystem_Uri()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(Path.Join(dir.Path, ".ferretignore"), "*.log\n");
        var provider = new FerretIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("jira:///PROJ-1"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_True_For_Matching_Pattern()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(Path.Join(dir.Path, ".ferretignore"), "*.log\n");
        var provider = new FerretIgnoreProvider(dir.Path);

        Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///debug.log"))));
        Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///src/Program.cs"))));
    }

    [Fact]
    public void ShouldIgnore_Ignores_Comment_Lines()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(Path.Join(dir.Path, ".ferretignore"), "# comment\n*.tmp\n");
        var provider = new FerretIgnoreProvider(dir.Path);

        Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///readme.md"))));
        Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///temp.tmp"))));
    }

    [Fact]
    public void ShouldIgnore_DoubleGlob_Pattern_Matches_Nested_Path()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(Path.Join(dir.Path, ".ferretignore"), "**/bin\n");
        var provider = new FerretIgnoreProvider(dir.Path);

        Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///src/MyLib/bin"))));
        Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///src/MyLib/src"))));
    }

    private static AssetDescriptor MakeAsset(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = Path.GetFileName(uri.AbsolutePath),
        LastModified = DateTimeOffset.UtcNow,
    };
}
