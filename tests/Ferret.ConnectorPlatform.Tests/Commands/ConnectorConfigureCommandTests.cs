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

/// <summary>Tests for <see cref="ConnectorConfigureCommandHandler"/>.</summary>
public sealed class ConnectorConfigureCommandTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorConfigureCommandTests"/> class.</summary>
    public ConnectorConfigureCommandTests()
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

    /// <summary>Configuring only path changes rootPath and leaves exclude unchanged.</summary>
    [Fact]
    public async Task Configure_Path_Only_Changes_RootPath_Leaves_Exclude_Unchanged()
    {
        var initial = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            Configuration = new ConnectorConfiguration(new Dictionary<string, string>
            {
                ["rootPath"] = "old-path",
                ["exclude"] = "node_modules",
            }),
        };
        await _store.SaveAsync(_root, [initial]);

        var handler = new ConnectorConfigureCommandHandler(_store);
        var context = MakeContext(new Dictionary<string, object?>
        {
            ["name"] = "default",
            ["path"] = "new-path",
        });

        var result = await handler.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var instances = await _store.LoadAllAsync(_root);
        var updated = instances[0];
        Assert.Equal("new-path", updated.Configuration.GetValue("rootPath"));
        Assert.Equal("node_modules", updated.Configuration.GetValue("exclude"));
    }

    /// <summary>Configuring only exclude changes exclude and leaves rootPath unchanged.</summary>
    [Fact]
    public async Task Configure_Exclude_Only_Changes_Exclude_Leaves_RootPath_Unchanged()
    {
        var initial = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            Configuration = new ConnectorConfiguration(new Dictionary<string, string>
            {
                ["rootPath"] = "src",
                ["exclude"] = "bin",
            }),
        };
        await _store.SaveAsync(_root, [initial]);

        var handler = new ConnectorConfigureCommandHandler(_store);
        var context = MakeContext(new Dictionary<string, object?>
        {
            ["name"] = "default",
            ["exclude"] = "obj,bin",
        });

        var result = await handler.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var instances = await _store.LoadAllAsync(_root);
        var updated = instances[0];
        Assert.Equal("src", updated.Configuration.GetValue("rootPath"));
        Assert.Equal("obj,bin", updated.Configuration.GetValue("exclude"));
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
