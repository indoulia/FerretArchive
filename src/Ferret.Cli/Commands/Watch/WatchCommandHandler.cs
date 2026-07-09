using System.Diagnostics.CodeAnalysis;
using System.IO;

using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Commands.Watch;

/// <summary>Handles 'ferret watch' — watches the workspace for file changes and automatically re-indexes.</summary>
internal sealed partial class WatchCommandHandler : ICommandHandler
{
    private readonly IIndexPipeline _pipeline;
    private readonly IIndexEngine _engine;
    private readonly IIndexStateStore _stateStore;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ILogger<WatchCommandHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="WatchCommandHandler"/> class.</summary>
    /// <param name="pipeline">The index pipeline used for incremental re-indexing.</param>
    /// <param name="engine">The index engine used to delete removed documents.</param>
    /// <param name="stateStore">The incremental state store, kept in sync with deletions so it does not
    /// depend on a future full <see cref="IIndexPipeline.RunAsync"/> stale-sweep to self-correct.</param>
    /// <param name="workspaceContext">Provides workspace root and ID.</param>
    /// <param name="logger">Logger for watch activity.</param>
    public WatchCommandHandler(
        IIndexPipeline pipeline,
        IIndexEngine engine,
        IIndexStateStore stateStore,
        IWorkspaceContext workspaceContext,
        ILogger<WatchCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(logger);
        _pipeline = pipeline;
        _engine = engine;
        _stateStore = stateStore;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath;
        if (!Directory.Exists(workspaceRoot))
        {
            LogWorkspaceNotFound(_logger, workspaceRoot);
            context.Services.Output.WriteError($"Workspace root does not exist: {workspaceRoot}");
            return CommandResult.Failure;
        }

        var ct = context.CancellationToken;

        using var debouncer = new FileChangeDebouncer(TimeSpan.FromMilliseconds(500));
        debouncer.ChangesReady += (_, e) =>
        {
            // Fire-and-forget: process changes without blocking the event callback.
            _ = ProcessChangesAsync(e.Changes, ct);
        };

        using var watcher = new FileSystemWatcher(workspaceRoot)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
        };

        watcher.Changed += (_, e) => debouncer.Track(e.FullPath, WatcherChangeTypes.Changed);
        watcher.Created += (_, e) => debouncer.Track(e.FullPath, WatcherChangeTypes.Created);
        watcher.Deleted += (_, e) => debouncer.Track(e.FullPath, WatcherChangeTypes.Deleted);
        watcher.Renamed += (_, e) =>
        {
            debouncer.Track(e.OldFullPath, WatcherChangeTypes.Deleted);
            debouncer.Track(e.FullPath, WatcherChangeTypes.Created);
        };

        LogWatching(_logger, workspaceRoot);
        context.Services.Output.WriteLine($"Watching {workspaceRoot} for changes. Press Ctrl+C to stop.");

        try
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown on Ctrl+C
        }

        return CommandResult.Success;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Workspace root does not exist: {Path}")]
    private static partial void LogWorkspaceNotFound(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Watching {Path} for changes. Press Ctrl+C to stop.")]
    private static partial void LogWatching(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removing deleted document: {Path}")]
    private static partial void LogRemovingDocument(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Re-indexing {Count} changed file(s)...")]
    private static partial void LogReIndexing(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Re-index complete: {Indexed} indexed, {Skipped} skipped.")]
    private static partial void LogReIndexComplete(ILogger logger, int indexed, int skipped);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing file changes")]
    private static partial void LogProcessChangesError(ILogger logger, Exception exception);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Fire-and-forget event handler; broad catch ensures exceptions are logged")]
    private async Task ProcessChangesAsync(
        IReadOnlyList<(string Path, WatcherChangeTypes ChangeType)> changes,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            var deletions = changes.Where(c => c.ChangeType == WatcherChangeTypes.Deleted).ToList();
            var modifications = changes.Where(c => c.ChangeType != WatcherChangeTypes.Deleted).ToList();

            if (deletions.Count > 0)
            {
                foreach (var (path, _) in deletions)
                {
                    var assetId = BuildAssetId(path);
                    LogRemovingDocument(_logger, path);
                    await _engine.DeleteAsync(DocumentId.From(assetId), ct).ConfigureAwait(false);
                    await _stateStore.RemoveAsync(assetId, ct).ConfigureAwait(false);
                }

                // RunSingleAssetAsync (below, for modifications) persists its own state-store
                // changes; a deletion-only batch has no such call, so it must flush explicitly --
                // otherwise the removal exists only in memory and is lost if the process exits
                // before any later modification happens to save it.
                await _stateStore.SaveAsync(ct).ConfigureAwait(false);
            }

            if (modifications.Count > 0)
            {
                LogReIndexing(_logger, modifications.Count);
                var indexed = 0;
                var skipped = 0;
                foreach (var (path, _) in modifications)
                {
                    var result = await _pipeline
                        .RunSingleAssetAsync(_workspaceContext.WorkspaceId, BuildAssetId(path), ct)
                        .ConfigureAwait(false);
                    indexed += result.DocumentsIndexed;
                    skipped += result.DocumentsSkipped;
                }

                LogReIndexComplete(_logger, indexed, skipped);
            }
        }
        catch (Exception ex)
        {
            LogProcessChangesError(_logger, ex);
        }
    }

    private AssetId BuildAssetId(string absolutePath)
    {
        // Match the canonical URI format used by FilesystemConnector: filesystem:///relative/path
        var workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath;
        var relative = Path.GetRelativePath(workspaceRoot, absolutePath).Replace('\\', '/');
        return AssetId.From(new Uri($"filesystem:///{relative}"));
    }
}
