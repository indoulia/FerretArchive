using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Workspace;

namespace Ferret.Integration.Tests;

public sealed class WorkspaceE2ETests : IDisposable
{
    private readonly string _tempDir = Path.Join(
        Path.GetTempPath(),
        "ferret-e2e-" + Guid.NewGuid().ToString("N")[..8]);

    /// <summary>Initializes a new instance of the <see cref="WorkspaceE2ETests"/> class.</summary>
    public WorkspaceE2ETests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WorkspaceInit_CreatesDotFerretWithManifestAndState()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            using var output = new StringWriter();
            var exitCode = await RunAsync(["workspace", "init"], output);

            Assert.Equal(0, exitCode);
            Assert.True(
                File.Exists(Path.Join(_tempDir, ".ferret", "workspace.json")),
                "workspace.json must exist after init");
            Assert.True(
                File.Exists(Path.Join(_tempDir, ".ferret", "state.json")),
                "state.json must exist after init");
        }
        finally
        {
            Environment.CurrentDirectory = prev;
        }
    }

    [Fact]
    public async Task WorkspaceInit_CreatesExpectedContextOsArtifacts()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            await RunAsync(["workspace", "init"]);
            var ferret = Path.Join(_tempDir, ".ferret");

            Assert.True(Directory.Exists(Path.Join(ferret, "connectors", "git")));
            Assert.True(Directory.Exists(Path.Join(ferret, "indexes", "semantic")));
            Assert.True(Directory.Exists(Path.Join(ferret, "memory", "working")));
            Assert.True(Directory.Exists(Path.Join(ferret, "snapshots", "knowledge")));
            Assert.True(File.Exists(Path.Join(ferret, "config", "connectors.json")));
        }
        finally
        {
            Environment.CurrentDirectory = prev;
        }
    }

    [Fact]
    public async Task WorkspaceInit_ThenStatus_ShowsWorkspaceName()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            await RunAsync(["workspace", "init"]);

            using var output = new StringWriter();
            var exitCode = await RunAsync(["workspace", "status"], output);

            Assert.Equal(0, exitCode);
            Assert.Contains("Workspace:", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.CurrentDirectory = prev;
        }
    }

    [Fact]
    public async Task WorkspaceInit_WhenAlreadyInitialised_ReturnsNonZeroExitCode()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            await RunAsync(["workspace", "init"]);
            var exitCode = await RunAsync(["workspace", "init"]);
            Assert.NotEqual(0, exitCode);
        }
        finally
        {
            Environment.CurrentDirectory = prev;
        }
    }

    [Fact]
    public async Task WorkspaceStatus_WithCorruptManifest_ReturnsFailureExitCode()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            var ferretDir = Path.Join(_tempDir, ".ferret");
            Directory.CreateDirectory(ferretDir);
            await File.WriteAllTextAsync(
                Path.Join(ferretDir, "workspace.json"),
                "{ NOT VALID JSON !!!");

            using var output = new StringWriter();
            var exitCode = await RunAsync(["workspace", "status"], output);

            Assert.NotEqual(0, exitCode);
            Assert.Contains("corrupt", output.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.CurrentDirectory = prev;
        }
    }

    private static Task<int> RunAsync(string[] args, StringWriter? output = null)
    {
        var app = RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()], output);
        return app.InvokeAsync(args);
    }
}
