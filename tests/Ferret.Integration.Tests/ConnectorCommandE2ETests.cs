using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Connector;
using Ferret.Cli.Commands.Workspace;
using Ferret.Connectors.Filesystem;
using Ferret.ParserPlatform;

namespace Ferret.Integration.Tests;

/// <summary>E2E tests for ferret connector commands.</summary>
public sealed class ConnectorCommandE2ETests
{
    /// <summary>connector list returns the filesystem connector.</summary>
    [Fact]
    public async Task ConnectorList_Returns_Filesystem_Connector()
    {
        using var sw = new StringWriter();
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration { RootPath = Directory.GetCurrentDirectory() },
            new MimeTypeResolver());

        var exitCode = await RunAsync(["connector", "list"], factory, sw);

        Assert.Equal(0, exitCode);
        Assert.Contains("filesystem", sw.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>connector info filesystem returns connector detail.</summary>
    [Fact]
    public async Task ConnectorInfo_Returns_Filesystem_Detail()
    {
        using var sw = new StringWriter();
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration { RootPath = Directory.GetCurrentDirectory() },
            new MimeTypeResolver());

        var exitCode = await RunAsync(["connector", "info", "filesystem"], factory, sw);

        Assert.Equal(0, exitCode);
        Assert.Contains("Filesystem Connector", sw.ToString(), StringComparison.Ordinal);
        Assert.Contains("Asset Discovery", sw.ToString(), StringComparison.Ordinal);
    }

    /// <summary>connector info with unknown id returns non-zero exit code.</summary>
    [Fact]
    public async Task ConnectorInfo_Unknown_Id_Returns_NonZero_ExitCode()
    {
        using var sw = new StringWriter();
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration { RootPath = Directory.GetCurrentDirectory() },
            new MimeTypeResolver());

        var exitCode = await RunAsync(["connector", "info", "nonexistent"], factory, sw);

        Assert.NotEqual(0, exitCode);
    }

    private static Task<int> RunAsync(
        string[] args,
        FilesystemConnectorFactory factory,
        StringWriter? output = null)
    {
        var app = RootCommandFactory.Build(
            [new CoreCliModule(), new WorkspaceCliModule(), new ConnectorCliModule([factory])],
            output);
        return app.InvokeAsync(args);
    }
}
