using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Indexing.Formatting;
using Ferret.Cli.Commands.Indexing.ViewModels;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Indexing;

/// <summary>Handles 'ferret index' — runs the full discover → parse → index pipeline.</summary>
internal sealed class IndexCommandHandler : ICommandHandler
{
    private readonly IIndexPipeline _pipeline;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly SwappableEventBus _eventBus;

    /// <summary>Initializes a new instance of the <see cref="IndexCommandHandler"/> class.</summary>
    /// <param name="pipeline">The index pipeline to run.</param>
    /// <param name="workspaceContext">The current workspace context.</param>
    /// <param name="eventBus">The swappable event bus; accepts a verbose sink when --verbose is set.</param>
    public IndexCommandHandler(
        IIndexPipeline pipeline,
        IWorkspaceContext workspaceContext,
        SwappableEventBus eventBus)
    {
        _pipeline = pipeline;
        _workspaceContext = workspaceContext;
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var forceRebuild = context.GetOption<bool>("rebuild");
        var verbose = context.GetOption<bool>("verbose");

        if (verbose)
        {
            _eventBus.Inner = new ConsoleIndexEventSink(context.Services.Output, NullEventBus.Instance);
        }

        try
        {
            var options = new IndexPipelineOptions { ForceRebuild = forceRebuild };

            context.Services.Output.WriteLine("Indexing workspace…");

            var result = await _pipeline.RunAsync(
                _workspaceContext.WorkspaceId,
                options,
                context.CancellationToken).ConfigureAwait(false);

            var dbPath = Path.Combine(
                _workspaceContext.WorkspaceRoot.FullPath,
                WorkspaceLayout.RootDirectoryName,
                IndexLayout.IndexDirectoryName,
                IndexLayout.KeywordDirectoryName,
                IndexLayout.KeywordDatabaseFileName);

            var vm = IndexSummaryViewModel.From(result, dbPath);
            var formatted = TextIndexSummaryFormatter.Format(vm);
            context.Services.Output.WriteLine(formatted);

            return result.Failures == 0 ? CommandResult.Success : CommandResult.Failure;
        }
        finally
        {
            if (verbose)
            {
                _eventBus.Inner = NullEventBus.Instance;
            }
        }
    }
}
