using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Config;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Ferret.Cli.Tests.Commands.Config;

public sealed class ConfigValidateCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ValidConfig_ReturnsSuccess()
    {
        using var dir = new TempDirectory();
        var json = """
            {
              "Ferret": {
                "Workspace": { "Name": "test-ws", "Root": "." }
              }
            }
            """;
        var configPath = System.IO.Path.Join(dir.Path, "ferret.config.json");
        await File.WriteAllTextAsync(configPath, json);

        var handler = new ConfigValidateCommandHandler();
        var ctx = MakeCtx(dir.Path, configPath);

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task ExecuteAsync_MissingWorkspaceName_ReturnsFailure()
    {
        using var dir = new TempDirectory();
        var json = """{ "Ferret": { "Workspace": { "Root": "." } } }""";
        var configPath = System.IO.Path.Join(dir.Path, "ferret.config.json");
        await File.WriteAllTextAsync(configPath, json);

        var handler = new ConfigValidateCommandHandler();
        var ctx = MakeCtx(dir.Path, configPath);

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task ExecuteAsync_MalformedJson_ReturnsFailure()
    {
        using var dir = new TempDirectory();
        var configPath = System.IO.Path.Join(dir.Path, "ferret.config.json");
        await File.WriteAllTextAsync(configPath, "{ not valid json");

        var handler = new ConfigValidateCommandHandler();
        var ctx = MakeCtx(dir.Path, configPath);

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task ExecuteAsync_MissingConfigFile_ReturnsFailure()
    {
        using var dir = new TempDirectory();
        var handler = new ConfigValidateCommandHandler();
        var ctx = MakeCtx(dir.Path, System.IO.Path.Join(dir.Path, "ferret.config.json"));

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Failure, result);
    }

    private static ConfigFakeContext MakeCtx(string workingDirectory, string? configPath = null)
    {
        var output = new ConfigFakeOutput();
        var services = new ConfigFakeServices(output);
        return new ConfigFakeContext(services, workingDirectory, configPath);
    }
}

internal sealed class ConfigFakeOutput : IOutputFormatter
{
    private readonly List<string> _lines = [];
    internal IReadOnlyList<string> Lines => _lines;
    public void WriteLine(string text = "") => _lines.Add(text);
    public void WriteSuccess(string message) => _lines.Add($"✓ {message}");
    public void WriteError(string message) => _lines.Add($"✗ {message}");
    public void WriteVerbose(string message) => _lines.Add($"  {message}");
}

internal sealed class ConfigFakeServices : IFerretServices
{
    internal ConfigFakeServices(ConfigFakeOutput output) => Output = output;
    public IOutputFormatter Output { get; }
    public IConfiguration Configuration => new ConfigurationBuilder().Build();
    public Microsoft.Extensions.Logging.ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;
    public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();
    public Ferret.Core.Runtime.IRuntimeHost? Runtime => null;
}

internal sealed class ConfigFakeContext : IFerretContext
{
    private readonly string? _configPath;

    internal ConfigFakeContext(IFerretServices services, string workingDirectory, string? configPath = null)
    {
        Services = services;
        WorkingDirectory = workingDirectory;
        _configPath = configPath;
    }

    public CancellationToken CancellationToken => CancellationToken.None;
    public VerbosityLevel Verbosity => VerbosityLevel.Normal;
    public OutputFormat OutputFormat => OutputFormat.Text;
    public IFerretServices Services { get; }
    public string WorkingDirectory { get; }

    public T? GetOption<T>(string name)
    {
        if (name == "--config" && _configPath is T val)
        {
            return val;
        }

        return default;
    }
}
