using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorDiscoveryTests
{
    [Fact]
    public async Task DiscoverAsync_Yields_Files_In_Root()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "a.cs"), "class A {}");
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "b.cs"), "class B {}");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "a.cs");
        Assert.Contains(results, r => r.DisplayName == "b.cs");
    }

    [Fact]
    public async Task DiscoverAsync_Yields_Files_Recursively()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, "sub"));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "sub", "nested.cs"), string.Empty);
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "nested.cs");
    }

    [Fact]
    public async Task DiscoverAsync_Skips_DotGit_Directory()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, ".git"));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, ".git", "HEAD"), "ref: refs/heads/main");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.DoesNotContain(results, r => r.CanonicalUri.ToString().Contains("/.git/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiscoverAsync_Skips_DotFerret_Directory()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, ".ferret"));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, ".ferret", "state.json"), "{}");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.DoesNotContain(results, r => r.CanonicalUri.ToString().Contains("/.ferret/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiscoverAsync_CanonicalUri_Is_Workspace_Relative()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "Program.cs"), string.Empty);
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        var file = Assert.Single(results, r => r.Kind == AssetKind.File);
        Assert.Equal("filesystem:///Program.cs", file.CanonicalUri.ToString());
    }

    [Fact]
    public async Task DiscoverAsync_CanonicalUri_Uses_Forward_Slashes()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, "src"));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "src", "A.cs"), string.Empty);
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        var file = results.Single(r => r.Kind == AssetKind.File);
        Assert.DoesNotContain('\\', file.CanonicalUri.ToString());
    }

    [Fact]
    public async Task DiscoverAsync_AssetId_Is_Deterministic()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "X.cs"), string.Empty);
        var connector = MakeConnector(dir.Path);

        var r1 = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).FirstAsync();
        var r2 = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).FirstAsync();

        Assert.Equal(r1.Id, r2.Id);
    }

    [Theory]
    [InlineData("node_modules")]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("packages")]
    public async Task DiscoverAsync_Skips_BuildAndDependency_Directories(string skipDir)
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Join(dir.Path, skipDir));
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, skipDir, "ignored.cs"), "class Ignored {}");
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "keep.cs"), "class Keep {}");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "keep.cs");
        Assert.DoesNotContain(results, r => r.DisplayName == skipDir);
        Assert.DoesNotContain(
            results,
            r => r.CanonicalUri.ToString().Contains($"/{skipDir}/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiscoverAsync_Skips_Assets_Ignored_By_Provider()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "keep.cs"), string.Empty);
        await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, "skip.log"), string.Empty);
        var connector = MakeConnector(dir.Path);
        var options = new AssetDiscoveryOptions { IgnoreProvider = new SkipLogsIgnoreProvider() };

        var results = await connector.DiscoverAsync(options).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "keep.cs");
        Assert.DoesNotContain(results, r => r.DisplayName == "skip.log");
    }

    [Fact]
    public async Task DiscoverAsync_Respects_CancellationToken()
    {
        using var dir = new TempDirectory();
        for (var i = 0; i < 20; i++)
        {
            await File.WriteAllTextAsync(System.IO.Path.Join(dir.Path, $"file{i}.cs"), string.Empty);
        }

        var connector = MakeConnector(dir.Path);
        using var cts = new CancellationTokenSource();

        var count = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in connector.DiscoverAsync(AssetDiscoveryOptions.Default, cts.Token))
            {
                _ = item;
                count++;
                if (count == 1)
                {
                    await cts.CancelAsync();
                }
            }
        });
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath }, new Ferret.ParserPlatform.MimeTypeResolver());

    private sealed class SkipLogsIgnoreProvider : IIgnoreProvider
    {
        public bool ShouldIgnore(AssetDescriptor asset) =>
            asset.CanonicalUri.ToString().EndsWith(".log", StringComparison.OrdinalIgnoreCase);
    }
}
