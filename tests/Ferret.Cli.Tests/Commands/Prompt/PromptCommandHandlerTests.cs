using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Prompt;
using Ferret.Core.Ai.Prompts;
using Ferret.Prompts;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Prompt;

// ── Fakes ──────────────────────────────────────────────────────────────────

internal sealed class FakePromptOutput : IOutputFormatter
{
    private readonly List<string> _lines = [];

    internal IReadOnlyList<string> Lines => _lines;

    internal string AllText => string.Join(Environment.NewLine, _lines);

    public void WriteLine(string text = "") => _lines.Add(text);

    public void WriteSuccess(string message) => _lines.Add($"✓ {message}");

    public void WriteError(string message) => _lines.Add($"✗ {message}");

    public void WriteVerbose(string message) => _lines.Add($"  {message}");
}

internal sealed class FakePromptServices : IFerretServices
{
    internal FakePromptServices(FakePromptOutput output) => Output = output;

    public IOutputFormatter Output { get; }

    public IConfiguration Configuration => new ConfigurationBuilder().Build();

    public ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;

    public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();

    public Ferret.Core.Runtime.IRuntimeHost? Runtime => null;
}

internal sealed class FakePromptContext : IFerretContext
{
    internal FakePromptContext(IFerretServices services) => Services = services;

    public CancellationToken CancellationToken => CancellationToken.None;

    public VerbosityLevel Verbosity => VerbosityLevel.Normal;

    public OutputFormat OutputFormat => OutputFormat.Text;

    public IFerretServices Services { get; }

    public string WorkingDirectory => @"C:\fake\cwd";

    public T? GetOption<T>(string name) => default;
}

// ── PromptListCommandHandler tests ─────────────────────────────────────────

public sealed class PromptListCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_NoTemplates_WritesEmptyStateMessage()
    {
        var registry = new PromptRegistry([]);
        var output = new FakePromptOutput();
        var sut = new PromptListCommandHandler(registry);
        var context = new FakePromptContext(new FakePromptServices(output));

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("No prompt templates", output.AllText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplates_WritesTabularOutput()
    {
        var template = new PromptTemplate
        {
            Name = "summarize",
            Version = "1.0.0",
            Template = "Summarize: {{text}}",
            RequiredVariables = ["text"],
        };
        var registry = new PromptRegistry([template]);
        var output = new FakePromptOutput();
        var sut = new PromptListCommandHandler(registry);
        var context = new FakePromptContext(new FakePromptServices(output));

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("summarize", output.AllText, StringComparison.Ordinal);
        Assert.Contains("1.0.0", output.AllText, StringComparison.Ordinal);
        Assert.Contains("text", output.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TemplateWithNoRequiredVars_WritesNone()
    {
        var template = new PromptTemplate
        {
            Name = "greeting",
            Version = "1.0.0",
            Template = "Hello world!",
            RequiredVariables = [],
        };
        var registry = new PromptRegistry([template]);
        var output = new FakePromptOutput();
        var sut = new PromptListCommandHandler(registry);
        var context = new FakePromptContext(new FakePromptServices(output));

        await sut.ExecuteAsync(context);

        Assert.Contains("(none)", output.AllText, StringComparison.Ordinal);
    }
}
