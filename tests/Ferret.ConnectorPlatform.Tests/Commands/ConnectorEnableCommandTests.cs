using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Connector;
using Ferret.ConnectorPlatform;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Ferret.ConnectorPlatform.Tests.Commands;

/// <summary>Tests for <see cref="ConnectorEnableCommandHandler"/>.</summary>
public sealed class ConnectorEnableCommandTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorEnableCommandTests"/> class.</summary>
    public ConnectorEnableCommandTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
        {
            Directory.Delete(_tmpDir, recursive: true);
        }
    }

    /// <summary>Enabling a new connector creates an instance in the store.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enable_New_Connector_Creates_Instance_In_Store()
    {
        var handler = new ConnectorEnableCommandHandler(_store);
        var context = MakeContext(new Dictionary<string, object?>
        {
            ["name"] = "default",
            ["type"] = "filesystem",
            ["path"] = ".",
        });

        var result = await handler.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var instances = await _store.LoadAllAsync(_root);
        Assert.Single(instances);
        Assert.Equal("default", instances[0].Id.Value);
        Assert.Equal("filesystem", instances[0].ConnectorType.Value);
        Assert.True(instances[0].IsEnabled);
    }

    /// <summary>Enabling an already-enabled connector returns Success without adding a duplicate.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enable_Already_Enabled_Returns_Success_No_Write()
    {
        var existing = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            IsEnabled = true,
        };
        await _store.SaveAsync(_root, [existing]);

        var handler = new ConnectorEnableCommandHandler(_store);
        var context = MakeContext(new Dictionary<string, object?> { ["name"] = "default" });

        var result = await handler.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var instances = await _store.LoadAllAsync(_root);
        Assert.Single(instances);
        Assert.True(instances[0].IsEnabled);
    }

    /// <summary>Enabling a disabled connector sets IsEnabled to true.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Enable_Disabled_Connector_Sets_IsEnabled_True()
    {
        var existing = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            IsEnabled = false,
        };
        await _store.SaveAsync(_root, [existing]);

        var handler = new ConnectorEnableCommandHandler(_store);
        var context = MakeContext(new Dictionary<string, object?> { ["name"] = "default" });

        var result = await handler.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var instances = await _store.LoadAllAsync(_root);
        Assert.Single(instances);
        Assert.True(instances[0].IsEnabled);
    }

    private FakeCommandContext MakeContext(Dictionary<string, object?> options) =>
        new(_root.FullPath, options);

    private sealed class FakeOutput : IOutputFormatter
    {
        public void WriteLine(string text = "")
        {
        }

        public void WriteSuccess(string message)
        {
        }

        public void WriteError(string message)
        {
        }

        public void WriteVerbose(string message)
        {
        }
    }

    private sealed class FakeServices : IFerretServices
    {
        internal FakeServices()
        {
            Output = new FakeOutput();
        }

        public IOutputFormatter Output { get; }

        public IConfiguration Configuration =>
            new ConfigurationBuilder().Build();

        public ILoggerFactory LoggerFactory =>
            NullLoggerFactory.Instance;

        public IServiceProvider Services =>
            new ServiceCollection().BuildServiceProvider();

        public Ferret.Core.Runtime.IRuntimeHost? Runtime =>
            null;
    }

    private sealed class FakeCommandContext : IFerretContext
    {
        private readonly string _cwd;
        private readonly Dictionary<string, object?> _options;

        internal FakeCommandContext(string cwd, Dictionary<string, object?> options)
        {
            _cwd = cwd;
            _options = options;
            Services = new FakeServices();
        }

        public CancellationToken CancellationToken =>
            CancellationToken.None;

        public VerbosityLevel Verbosity =>
            VerbosityLevel.Normal;

        public OutputFormat OutputFormat =>
            OutputFormat.Text;

        public IFerretServices Services { get; }

        public string WorkingDirectory =>
            _cwd;

        public T? GetOption<T>(string name) =>
            _options.TryGetValue(name, out var v) ? (T?)v : default;
    }
}
