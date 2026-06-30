using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Indexing.Formatting;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Ferret.ParserPlatform;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Indexing;

/// <summary>CLI module for the <c>ferret index</c> command.
/// Registers <see cref="IEventBus"/>, the parser platform,
/// <see cref="IIndexEngine"/>, <see cref="IIndexPipeline"/>, and
/// <see cref="IndexCommandHandler"/> into the DI container.</summary>
internal sealed class IndexCliModule : CliModuleBase
{
    private readonly IWorkspaceContext _workspaceContext;

    /// <summary>Initializes a new instance of the <see cref="IndexCliModule"/> class.</summary>
    /// <param name="workspaceContext">Provides workspace root for the database path.</param>
    public IndexCliModule(IWorkspaceContext workspaceContext)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        _workspaceContext = workspaceContext;
    }

    /// <inheritdoc/>
    public override string Name => "ferret.index";

    /// <inheritdoc/>
    public override string Description => "Content indexing.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("index", "Index workspace assets into the search database."),
            typeof(IndexCommandHandler),
            Options:
            [
                new OptionDefinition("--rebuild", "Rebuild index from scratch, discarding existing data.", typeof(bool)),
                new OptionDefinition("--verbose", "Stream per-document indexing events to console.", typeof(bool)),
            ]);
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var dbPath = System.IO.Path.Combine(
            _workspaceContext.WorkspaceRoot.FullPath,
            WorkspaceLayout.RootDirectoryName,
            IndexLayout.IndexDirectoryName,
            IndexLayout.KeywordDirectoryName,
            IndexLayout.KeywordDatabaseFileName);

        // Register IWorkspaceContext so ConnectorCliModule and IndexCommandHandler can resolve it.
        services.AddSingleton<IWorkspaceContext>(_workspaceContext);

        // IEventBus — SwappableEventBus allows IndexCommandHandler to inject a verbose sink at runtime.
        services.AddSingleton<SwappableEventBus>(_ => new SwappableEventBus(NullEventBus.Instance));
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<SwappableEventBus>());

        // Parser platform — resolves IParserDispatcher required by IIndexPipeline.
        ParserPlatformModule.ConfigureServices(services);

        // IIndexEngine — SQLite FTS5 database at workspace-resolved path.
        services.AddSingleton<IIndexEngine>(_ => new SqliteKeywordIndexEngine(dbPath));

        // IIndexPipeline — depends on IConnectorManager, IParserDispatcher, IIndexEngine, IEventBus.
        IndexingModule.ConfigureServices(services);

        services.AddSingleton<TextIndexSummaryFormatter>();
        services.AddSingleton<IndexCommandHandler>();
    }
}
