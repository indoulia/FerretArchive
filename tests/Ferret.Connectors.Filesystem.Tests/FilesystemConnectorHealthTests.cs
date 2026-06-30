using Ferret.Connectors.Filesystem;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorHealthTests
{
    [Fact]
    public async Task GetHealthAsync_Returns_Connected_When_Path_Exists()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);

        var health = await connector.GetHealthAsync();

        Assert.True(health.IsConnected);
        Assert.Null(health.ErrorMessage);
    }

    [Fact]
    public async Task GetHealthAsync_Returns_Disconnected_When_Path_Missing()
    {
        var missingPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        var connector = MakeConnector(missingPath);

        var health = await connector.GetHealthAsync();

        Assert.False(health.IsConnected);
        Assert.NotNull(health.ErrorMessage);
    }

    [Fact]
    public async Task ConnectAsync_Returns_Session_When_Path_Exists()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);

        await using var session = await connector.ConnectAsync();

        Assert.NotNull(session);
    }

    [Fact]
    public async Task ConnectAsync_Session_DisposeAsync_Does_Not_Throw()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);
        var session = await connector.ConnectAsync();

        var ex = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task DisconnectAsync_Does_Not_Throw()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);

        var ex = await Record.ExceptionAsync(() => connector.DisconnectAsync());

        Assert.Null(ex);
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath }, new Ferret.ParserPlatform.MimeTypeResolver());
}
