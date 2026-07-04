using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.ParserPlatform;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorReaderTests
{
    [Fact]
    public void FilesystemConnector_Implements_IAssetReader()
    {
        Assert.True(typeof(IAssetReader).IsAssignableFrom(typeof(FilesystemConnector)));
    }

    [Fact]
    public void FilesystemConnector_Implements_Both_IAssetSource_And_IAssetReader()
    {
        Assert.True(typeof(IAssetSource).IsAssignableFrom(typeof(FilesystemConnector)));
        Assert.True(typeof(IAssetReader).IsAssignableFrom(typeof(FilesystemConnector)));
    }

    [Fact]
    public async Task OpenAsync_Returns_Stream_With_Correct_Content()
    {
        using var tmp = new TempDirectory();
        var expected = "Hello from the filesystem connector reader.";
        var fileName = "test.txt";
        var filePath = Path.Join(tmp.Path, fileName);
        await File.WriteAllTextAsync(filePath, expected);

        var connector = MakeConnector(tmp.Path);

        // Discover the asset to get the proper CanonicalUri
        var assets = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();
        var asset = assets.First(a => a.DisplayName == fileName);

        await using var stream = await ((IAssetReader)connector).OpenAsync(asset);
        using var reader = new StreamReader(stream);
        var actual = await reader.ReadToEndAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task OpenAsync_Returns_Stream_For_FileName_With_Space()
    {
        using var tmp = new TempDirectory();
        var expected = "Content of a file whose name needs URI escaping.";
        var fileName = "file with space.txt";
        var filePath = Path.Join(tmp.Path, fileName);
        await File.WriteAllTextAsync(filePath, expected);

        var connector = MakeConnector(tmp.Path);

        var assets = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();
        var asset = assets.First(a => a.DisplayName == fileName);

        await using var stream = await ((IAssetReader)connector).OpenAsync(asset);
        using var reader = new StreamReader(stream);
        var actual = await reader.ReadToEndAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task OpenAsync_Throws_For_Missing_File()
    {
        using var tmp = new TempDirectory();
        var connector = MakeConnector(tmp.Path);

        // Construct a descriptor that refers to a non-existent file
        var missingUri = new Uri("filesystem:///nonexistent.txt");
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(missingUri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("filesystem-default"),
            Kind = AssetKind.File,
            CanonicalUri = missingUri,
            DisplayName = "nonexistent.txt",
            LastModified = DateTimeOffset.UtcNow,
        };

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await using var stream = await ((IAssetReader)connector).OpenAsync(asset);
        });
    }

    [Fact]
    public async Task OpenAsync_Respects_Cancellation()
    {
        using var tmp = new TempDirectory();
        var fileName = "file.txt";
        await File.WriteAllTextAsync(Path.Join(tmp.Path, fileName), "content");

        var connector = MakeConnector(tmp.Path);
        var assets = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();
        var asset = assets.First(a => a.DisplayName == fileName);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await using var stream = await ((IAssetReader)connector).OpenAsync(asset, cts.Token);
        });
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath }, new MimeTypeResolver());
}
