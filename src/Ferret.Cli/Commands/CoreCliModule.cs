using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Handlers;
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Diagnostics.Checks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: The built-in ICliModule — contributes all Sprint 6 working commands plus 13 reserved group stubs.
///      Sprint 7 modules add their own ICliModule without touching RootCommandFactory.
/// Thread Safety: Thread Safe — called once during startup.
/// </summary>
internal sealed class CoreCliModule : CliModuleBase
{
    private IReadOnlyList<IDiagnosticCheck>? _checks;

    /// <inheritdoc/>
    public override string Name => "ferret.core";

    /// <inheritdoc/>
    public override string Description => "Core Ferret CLI commands.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return Cmd("version", "Print the Ferret platform version.", typeof(VersionCommandHandler));
        yield return Cmd("about", "About Ferret and ContextOS.", typeof(AboutCommandHandler));
        yield return CmdWithOptions(
            "start",
            "Start the Ferret runtime host.",
            typeof(StartCommandHandler),
            new OptionDefinition("--config", "Path to ferret.json.", typeof(string)));
        yield return Cmd("doctor", "Validate the local Ferret installation.", typeof(DoctorCommandHandler));
        yield return Cmd("status", "Report the current Ferret runtime status.", typeof(StatusCommandHandler));

        // Reserved command groups — show Sprint roadmap when invoked
        yield return CommandDefinition.EmptyGroup(
            "memory",
            "Semantic memory management.",
            "Sprint 9",
            ["memory store", "memory recall"]);
        yield return CommandDefinition.EmptyGroup(
            "review",
            "AI-assisted code review.",
            "Sprint 10",
            []);
        yield return CommandDefinition.EmptyGroup(
            "git",
            "Git integration.",
            "Sprint 10",
            ["git sync", "git status"]);
        yield return CommandDefinition.EmptyGroup(
            "jira",
            "JIRA integration.",
            "Sprint 10",
            ["jira search", "jira create"]);
        yield return CommandDefinition.EmptyGroup(
            "docs",
            "Documentation management.",
            "Sprint 11",
            []);
        yield return CommandDefinition.EmptyGroup(
            "plugin",
            "Plugin management.",
            "Sprint 11",
            ["plugin install", "plugin list"]);
        yield return CommandDefinition.EmptyGroup(
            "model",
            "AI model management.",
            "Sprint 12",
            []);
        yield return CommandDefinition.EmptyGroup(
            "logs",
            "Runtime log access.",
            "Sprint 7",
            ["logs tail", "logs clear"]);
        yield return CommandDefinition.EmptyGroup(
            "telemetry",
            "Usage telemetry.",
            "Sprint 12",
            []);
    }

    /// <inheritdoc/>
    public override IEnumerable<IDiagnosticCheck> GetDiagnosticChecks() =>
        _checks ?? BuildChecks(null, Environment.CurrentDirectory, ComposeParsers());

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<VersionCommandHandler>();
        services.AddTransient<AboutCommandHandler>();
        services.AddTransient<StartCommandHandler>();
        services.AddTransient<StatusCommandHandler>();

        var tempProvider = services.BuildServiceProvider();
        var config = tempProvider.GetRequiredService<IConfiguration>();
        var workspaceRoot = config["Ferret:Workspace:Root"] ?? Environment.CurrentDirectory;

        var parsers = ComposeParsers();
        _checks = BuildChecks(config, workspaceRoot, parsers).ToList();
        var parserReport = new ParserPlatformReport(parsers);
        services.AddTransient<DoctorCommandHandler>(_ => new DoctorCommandHandler(_checks, parserReport));
    }

    // Composes the full parser pack and returns the parser instances (plain objects that remain valid
    // after the temporary provider is disposed). Shared by the health check and the doctor report.
    private static List<Ferret.Core.Documents.IContentParser> ComposeParsers()
    {
        var parserServices = new ServiceCollection();
        Ferret.Parsers.ParserPackModule.ConfigureServices(parserServices);
        using var provider = parserServices.BuildServiceProvider();
        return provider.GetServices<Ferret.Core.Documents.IContentParser>().ToList();
    }

    private static IEnumerable<IDiagnosticCheck> BuildChecks(
        IConfiguration? config, string workspaceRoot, List<Ferret.Core.Documents.IContentParser> parsers)
    {
        yield return new ConfigurationCheck();
        yield return new RuntimeLifecycleCheck();
        yield return new WorkspaceRootCheck(workspaceRoot);
        yield return new FerretConfigDirCheck(workspaceRoot);

        // Introspect the composed parser pack so `doctor` reports installed parsers + supported extensions.
        yield return new InstalledParsersCheck(
            parsers, parsers.Count, Ferret.ParserPlatform.MimeTypeResolver.KnownExtensionCount);

        var dbPath = Path.Join(
            workspaceRoot,
            Ferret.Core.Workspace.WorkspaceLayout.RootDirectoryName,
            Ferret.Core.Indexing.IndexLayout.IndexDirectoryName,
            Ferret.Core.Indexing.IndexLayout.KeywordDirectoryName,
            Ferret.Core.Indexing.IndexLayout.KeywordDatabaseFileName);
        var statePath = Path.Join(
            workspaceRoot,
            Ferret.Core.Workspace.WorkspaceLayout.RootDirectoryName,
            Ferret.Core.Indexing.IndexLayout.StateFileName);
        yield return new IndexFreshnessCheck(dbPath, workspaceRoot, new Ferret.Indexing.JsonIndexStateStore(statePath));

        if (config is not null)
        {
            yield return new AiProviderConfigCheck(config);
        }
    }

    private static CommandDefinition Cmd(string name, string description, Type handlerType) =>
        new(new CommandMetadata(name, description), handlerType);

    private static CommandDefinition CmdWithOptions(
        string name,
        string description,
        Type handlerType,
        params OptionDefinition[] options) =>
        new(
            new CommandMetadata(name, description),
            handlerType,
            Options: options.Length > 0 ? options : null);
}
