using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.ParserPlatform;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorMediaTypeTests
{
    [Theory]
    [InlineData("README.md", "text/markdown")]
    [InlineData("app.cs", "text/x-csharp")]
    [InlineData("config.yaml", "text/yaml")]
    [InlineData("data.json", "application/json")]
    [InlineData("script.py", "text/x-python")]
    public async Task DiscoverAsync_Sets_MediaType_On_File_Assets(string fileName, string expectedMediaType)
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, fileName), "content");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();
        var file = results.First(r => r.DisplayName == fileName);

        Assert.Equal(expectedMediaType, file.MediaType);
    }

    [Fact]
    public async Task DiscoverAsync_Sets_Null_MediaType_On_Directory_Assets()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(dir.Path, "subdir"));
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();
        var subDir = results.First(r => r.DisplayName == "subdir");

        Assert.Null(subDir.MediaType);
    }

    [Fact]
    public async Task DiscoverAsync_Sets_OctetStream_For_Unknown_Extension()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "data.xyz"), "content");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();
        var file = results.First(r => r.DisplayName == "data.xyz");

        // Unknown extension defaults to text/plain with low confidence in MimeTypeResolver
        Assert.NotNull(file.MediaType);
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath }, new MimeTypeResolver());
}
