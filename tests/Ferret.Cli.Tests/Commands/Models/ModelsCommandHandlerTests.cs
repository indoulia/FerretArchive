using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Models;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Models;

// ── Fakes ──────────────────────────────────────────────────────────────────

internal sealed class FakeModelsOutput : IOutputFormatter
{
    private readonly List<string> _lines = [];

    internal IReadOnlyList<string> Lines => _lines;

    internal string AllText => string.Join(Environment.NewLine, _lines);

    public void WriteLine(string text = "") => _lines.Add(text);

    public void WriteSuccess(string message) => _lines.Add($"✓ {message}");

    public void WriteError(string message) => _lines.Add($"✗ {message}");

    public void WriteVerbose(string message) => _lines.Add($"  {message}");
}

internal sealed class FakeModelsServices : IFerretServices
{
    internal FakeModelsServices(FakeModelsOutput output) => Output = output;

    public IOutputFormatter Output { get; }

    public IConfiguration Configuration => new ConfigurationBuilder().Build();

    public ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;

    public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();

    public Ferret.Core.Runtime.IRuntimeHost? Runtime => null;
}

internal sealed class FakeModelsContext : IFerretContext
{
    private readonly Dictionary<string, object?> _options;

    internal FakeModelsContext(IFerretServices services, Dictionary<string, object?>? options = null)
    {
        Services = services;
        _options = options ?? [];
    }

    public CancellationToken CancellationToken => CancellationToken.None;

    public VerbosityLevel Verbosity => VerbosityLevel.Normal;

    public OutputFormat OutputFormat => OutputFormat.Text;

    public IFerretServices Services { get; }

    public string WorkingDirectory => @"C:\fake\cwd";

    public T? GetOption<T>(string name) =>
        _options.TryGetValue(name, out var v) ? (T?)v : default;
}

internal sealed class FakeModelRegistry(IReadOnlyList<ModelDescriptor> models) : IModelRegistry
{
    public IReadOnlyList<ProviderDescriptor> GetProviders() => [];

    public IModelProvider? GetProvider(ProviderId id) => null;

    public IReadOnlyList<ModelDescriptor> GetModels() => models;

    public ModelDescriptor? GetModel(ModelId id) => models.FirstOrDefault(m => m.Id == id);
}

// ── ModelsListCommandHandler tests ─────────────────────────────────────────

public sealed class ModelsListCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_NoModels_WritesEmptyStateMessage()
    {
        var registry = new FakeModelRegistry([]);
        var output = new FakeModelsOutput();
        var sut = new ModelsListCommandHandler(registry);
        var context = new FakeModelsContext(new FakeModelsServices(output));

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("No models", output.AllText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithModels_WritesTabularOutput()
    {
        var descriptor = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "llama3.2",
            Capabilities = ModelCapabilities.Chat,
            ContextWindow = 131072,
        };
        var registry = new FakeModelRegistry([descriptor]);
        var output = new FakeModelsOutput();
        var sut = new ModelsListCommandHandler(registry);
        var context = new FakeModelsContext(new FakeModelsServices(output));

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("ollama", output.AllText, StringComparison.Ordinal);
        Assert.Contains("ollama/llama3.2", output.AllText, StringComparison.Ordinal);
        Assert.Contains("Chat", output.AllText, StringComparison.Ordinal);
    }
}

// ── ModelsInfoCommandHandler tests ─────────────────────────────────────────

public sealed class ModelsInfoCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_KnownModel_WritesDetailOutput()
    {
        var modelId = ModelId.Create("ollama/llama3.2");
        var descriptor = new ModelDescriptor
        {
            Id = modelId,
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "llama3.2",
            Capabilities = ModelCapabilities.Chat,
            ContextWindow = 128000,
        };
        var registry = new FakeModelRegistry([descriptor]);
        var output = new FakeModelsOutput();
        var sut = new ModelsInfoCommandHandler(registry);
        var context = new FakeModelsContext(
            new FakeModelsServices(output),
            new Dictionary<string, object?> { ["model-id"] = "ollama/llama3.2" });

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("ollama/llama3.2", output.AllText, StringComparison.Ordinal);
        Assert.Contains("Chat", output.AllText, StringComparison.Ordinal);
        Assert.Contains("128,000", output.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownModel_WritesErrorAndReturnsFailure()
    {
        var registry = new FakeModelRegistry([]);
        var output = new FakeModelsOutput();
        var sut = new ModelsInfoCommandHandler(registry);
        var context = new FakeModelsContext(
            new FakeModelsServices(output),
            new Dictionary<string, object?> { ["model-id"] = "unknown/model" });

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains("not found", output.AllText, StringComparison.OrdinalIgnoreCase);
    }
}
