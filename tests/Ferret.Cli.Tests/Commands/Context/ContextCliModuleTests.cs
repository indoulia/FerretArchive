using System.Text;

using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Context;
using Ferret.Core.Context;
using Ferret.Core.Runtime;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Ferret.Cli.Tests.Commands.Context;

public sealed class ContextCliModuleTests
{
    [Fact]
    public void ContextCliModule_HasContextCommand()
    {
        var module = new ContextCliModule();
        var commands = module.GetCommands().ToList();
        Assert.Contains(commands, c => c.Metadata.Name == "context");
    }

    [Fact]
    public void ContextCliModule_ContextCommand_HasQueryArgument()
    {
        var module = new ContextCliModule();
        var contextCmd = module.GetCommands().First(c => c.Metadata.Name == "context");
        Assert.NotNull(contextCmd.Arguments);
        Assert.Contains(contextCmd.Arguments!, a => a.Name == "query");
    }

    [Fact]
    public void ContextCliModule_ContextCommand_HasHandlerType()
    {
        var module = new ContextCliModule();
        var contextCmd = module.GetCommands().First(c => c.Metadata.Name == "context");
        Assert.Equal(typeof(ContextAssembleCommandHandler), contextCmd.HandlerType);
    }

    [Fact]
    public void ContextCliModule_ContextCommand_OptionDefaults_MatchDocumentedDefaults()
    {
        // --help displayed "[default: 0]" for both options because OptionDefinition had no
        // DefaultValue set, even though the description text (and actual handler fallback)
        // promise 8000/10. Regression test: the CLI-visible default must match the description.
        var module = new ContextCliModule();
        var contextCmd = module.GetCommands().First(c => c.Metadata.Name == "context");

        var maxTokens = contextCmd.Options!.First(o => o.LongName == "--max-tokens");
        var maxDocuments = contextCmd.Options!.First(o => o.LongName == "--max-documents");

        Assert.Equal(8000, maxTokens.DefaultValue);
        Assert.Equal(10, maxDocuments.DefaultValue);
    }

    [Fact]
    public async Task ExecuteAsync_OptionsPassedThrough_UsesCorrectRequestValues()
    {
        ContextRequest? captured = null;
        var stub = new CapturingAssembler(onAssemble: req => captured = req);
        var handler = new ContextAssembleCommandHandler(stub);

        var ctx = new StubFerretContext(options: new Dictionary<string, object?>
        {
            ["query"] = "authentication",
            ["max-tokens"] = (int?)4000,
            ["max-documents"] = (int?)5,
        });

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Success, result);
        Assert.NotNull(captured);
        Assert.Equal("authentication", captured!.Query);
        Assert.Equal(4000, captured.MaxTokens);
        Assert.Equal(5, captured.MaxDocuments);
    }

    [Fact]
    public async Task ExecuteAsync_AssemblerThrows_WritesError()
    {
        var stub = new ThrowingAssembler(new InvalidOperationException("index unavailable"));
        var handler = new ContextAssembleCommandHandler(stub);

        var ctx = new StubFerretContext(options: new Dictionary<string, object?>
        {
            ["query"] = "anything",
        });

        var result = await handler.ExecuteAsync(ctx);

        Assert.Equal(CommandResult.Failure, result);
        Assert.True(ctx.ErrorOutput.Length > 0, "Expected error output when assembler throws.");
    }

    // ── Stubs ──────────────────────────────────────────────────────────────────

    private sealed class CapturingAssembler : IContextAssembler
    {
        private readonly Action<ContextRequest> _onAssemble;

        public CapturingAssembler(Action<ContextRequest> onAssemble) =>
            _onAssemble = onAssemble;

        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct)
        {
            _onAssemble(request);
            return Task.FromResult(new ContextPackage
            {
                Query = request.Query,
                Documents = [],
                TotalTokenEstimate = 0,
                DocumentsConsidered = 0,
                DocumentsIncluded = 0,
                AssembledAt = DateTimeOffset.UtcNow,
            });
        }
    }

    private sealed class ThrowingAssembler : IContextAssembler
    {
        private readonly Exception _exception;

        public ThrowingAssembler(Exception exception) => _exception = exception;

        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            throw _exception;
    }

    private sealed class StubFerretContext : IFerretContext
    {
        private readonly IReadOnlyDictionary<string, object?> _options;
        private readonly StubOutputFormatter _formatter = new();

        public StubFerretContext(IReadOnlyDictionary<string, object?>? options = null)
        {
            _options = options ?? new Dictionary<string, object?>();
            Services = new StubFerretServices(_formatter);
        }

        public string ErrorOutput => _formatter.ErrorOutput;

        public CancellationToken CancellationToken => CancellationToken.None;

        public VerbosityLevel Verbosity => VerbosityLevel.Normal;

        public OutputFormat OutputFormat => OutputFormat.Text;

        public IFerretServices Services { get; }

        public string WorkingDirectory => string.Empty;

        public T? GetOption<T>(string name)
        {
            if (_options.TryGetValue(name, out var value) && value is T typed)
            {
                return typed;
            }

            return default;
        }
    }

    private sealed class StubFerretServices : IFerretServices
    {
        public StubFerretServices(IOutputFormatter output) => Output = output;

        public IServiceProvider Services => throw new NotSupportedException();

        public IConfiguration Configuration => throw new NotSupportedException();

        public ILoggerFactory LoggerFactory => throw new NotSupportedException();

        public IOutputFormatter Output { get; }

        public IRuntimeHost? Runtime => null;
    }

    private sealed class StubOutputFormatter : IOutputFormatter
    {
        private readonly StringBuilder _out = new();
        private readonly StringBuilder _err = new();

        public string Output => _out.ToString();

        public string ErrorOutput => _err.ToString();

        public void WriteLine(string text = "") => _out.AppendLine(text);

        public void WriteSuccess(string message) => _out.AppendLine(message);

        public void WriteError(string message) => _err.AppendLine(message);

        public void WriteVerbose(string message)
        {
        }
    }
}
