using Ferret.ConnectorPlatform;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

/// <summary>
/// Tests for <see cref="ConnectorInstanceStore"/>.
/// </summary>
public sealed class ConnectorInstanceStoreTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorInstanceStoreTests"/> class.
    /// </summary>
    public ConnectorInstanceStoreTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
        {
            Directory.Delete(_tmpDir, recursive: true);
        }
    }

    /// <summary>
    /// LoadAllAsync returns an empty list when the file does not exist.
    /// </summary>
    [Fact]
    public async Task LoadAllAsync_Returns_Empty_When_File_Does_Not_Exist()
    {
        var store = new ConnectorInstanceStore();

        var result = await store.LoadAllAsync(_root);

        Assert.Empty(result);
    }

    /// <summary>
    /// SaveAsync creates the parent directory and file.
    /// </summary>
    [Fact]
    public async Task SaveAsync_Creates_Parent_Directory_And_File()
    {
        var store = new ConnectorInstanceStore();
        var instances = new[]
        {
            new ConnectorInstance
            {
                Id = new ConnectorInstanceId("default"),
                ConnectorType = new ConnectorId("filesystem"),
                DisplayName = "Workspace",
            },
        };

        await store.SaveAsync(_root, instances);

        var filePath = Path.Combine(_tmpDir, ".ferret", "connectors.json");
        Assert.True(File.Exists(filePath));
    }

    /// <summary>
    /// SaveAsync then LoadAllAsync round-trips all fields correctly.
    /// </summary>
    [Fact]
    public async Task SaveAsync_Then_LoadAllAsync_Round_Trips_All_Fields()
    {
        var store = new ConnectorInstanceStore();
        var config = new ConnectorConfiguration(new Dictionary<string, string>
        {
            ["rootPath"] = "./src",
            ["excludeExtensions"] = ".dll,.exe",
        });
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("my-conn"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "My Connector",
            IsEnabled = false,
            SchemaVersion = "1.0",
            Tags = ["tag-a", "tag-b"],
            Configuration = config,
        };

        await store.SaveAsync(_root, [instance]);
        var loaded = await store.LoadAllAsync(_root);

        Assert.Single(loaded);
        var l = loaded[0];
        Assert.Equal("my-conn", l.Id.Value);
        Assert.Equal("filesystem", l.ConnectorType.Value);
        Assert.Equal("My Connector", l.DisplayName);
        Assert.False(l.IsEnabled);
        Assert.Equal(["tag-a", "tag-b"], l.Tags);
        Assert.Equal("./src", l.Configuration.GetValue("rootPath"));
        Assert.Equal(".dll,.exe", l.Configuration.GetValue("excludeExtensions"));
    }

    /// <summary>
    /// LoadAllAsync throws InvalidOperationException for malformed JSON.
    /// </summary>
    [Fact]
    public async Task LoadAllAsync_Throws_InvalidOperationException_For_Malformed_Json()
    {
        var ferretDir = Path.Combine(_tmpDir, ".ferret");
        Directory.CreateDirectory(ferretDir);
        await File.WriteAllTextAsync(Path.Combine(ferretDir, "connectors.json"), "{ not valid json }}}");
        var store = new ConnectorInstanceStore();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.LoadAllAsync(_root));

        Assert.Contains("connectors.json", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Configuration keys are case-insensitive after load.
    /// </summary>
    [Fact]
    public async Task Configuration_Keys_Are_Case_Insensitive_After_Load()
    {
        var store = new ConnectorInstanceStore();
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("ci"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "CI",
            Configuration = new ConnectorConfiguration(new Dictionary<string, string> { ["RootPath"] = "/ci" }),
        };

        await store.SaveAsync(_root, [instance]);
        var loaded = await store.LoadAllAsync(_root);

        Assert.Equal("/ci", loaded[0].Configuration.GetValue("rootpath"));
        Assert.Equal("/ci", loaded[0].Configuration.GetValue("ROOTPATH"));
    }

    /// <summary>
    /// SaveAsync is atomic: no temp files are left after save completes.
    /// </summary>
    [Fact]
    public async Task SaveAsync_Is_Atomic_Temp_Then_Rename()
    {
        var store = new ConnectorInstanceStore();
        await store.SaveAsync(_root, []);

        var ferretDir = Path.Combine(_tmpDir, ".ferret");
        var tmpFiles = Directory.GetFiles(ferretDir, "*.tmp");
        Assert.Empty(tmpFiles);
    }
}
