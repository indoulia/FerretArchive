using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Tests;

/// <summary>Verifies <see cref="FilesystemConnector.TryGetAsync"/> — the single-asset lookup used by
/// watch-mode incremental reindexing (issue #17) to avoid a full <see cref="FilesystemConnector.DiscoverAsync"/>
/// walk per changed file.</summary>
public sealed class FilesystemConnectorTryGetTests
{
    [Fact]
    public async Task TryGetAsync_ExistingFile_ReturnsMatchingDescriptor()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "a.cs"), "class A {}");
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///a.cs"));

        var result = await connector.TryGetAsync(assetId);

        Assert.NotNull(result);
        Assert.Equal("a.cs", result.DisplayName);
        Assert.Equal(AssetKind.File, result.Kind);
        Assert.Equal(assetId, result.Id);
    }

    [Fact]
    public async Task TryGetAsync_NestedExistingFile_ReturnsMatchingDescriptor()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, "src"));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "src", "A.cs"), string.Empty);
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///src/A.cs"));

        var result = await connector.TryGetAsync(assetId);

        Assert.NotNull(result);
        Assert.Equal("A.cs", result.DisplayName);
    }

    [Fact]
    public async Task TryGetAsync_NonExistentPath_ReturnsNull()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///does-not-exist.cs"));

        var result = await connector.TryGetAsync(assetId);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("node_modules")]
    [InlineData(".superpowers")]
    public async Task TryGetAsync_FileUnderHardcodedSkipDir_ReturnsNull(string skipDir)
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, skipDir));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, skipDir, "output.dll"), string.Empty);
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri($"filesystem:///{skipDir}/output.dll"));

        var result = await connector.TryGetAsync(assetId);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetAsync_HardcodedSkipDirItself_ReturnsNull()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, "bin"));
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///bin"));

        var result = await connector.TryGetAsync(assetId);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetAsync_PathIgnoredByFerretIgnore_ReturnsNull()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, ".ferretignore"), "*.log");
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "debug.log"), string.Empty);
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///debug.log"));

        var result = await connector.TryGetAsync(assetId);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetAsync_FileUnderAncestorDirectoryIgnoredByFerretIgnore_ReturnsNull()
    {
        // WalkDirectoryAsync checks every ancestor directory it walks through and never descends
        // into one that's ignored, so a file whose *ancestor* (not the file itself) matches a
        // .ferretignore pattern must never be reachable via a targeted single-asset lookup either.
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, "src", "vendor"));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, ".ferretignore"), "vendor");
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "src", "vendor", "lib.js"), string.Empty);
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///src/vendor/lib.js"));

        var result = await connector.TryGetAsync(assetId);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetAsync_ExistingDirectory_ReturnsDirectoryDescriptor()
    {
        // Directories are returned, not filtered here -- callers (IndexPipeline) decide
        // whether a non-File asset is processable, mirroring DiscoverAsync's own behaviour.
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, "src"));
        var connector = MakeConnector(dir.Path);
        var assetId = AssetId.From(new Uri("filesystem:///src"));

        var result = await connector.TryGetAsync(assetId);

        Assert.NotNull(result);
        Assert.Equal(AssetKind.Directory, result.Kind);
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath }, new Ferret.ParserPlatform.MimeTypeResolver());
}
