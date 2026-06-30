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
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ILogger<WatchCommandHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="WatchCommandHandler"/> class.</summary>
    /// <param name="pipeline">The index pipeline used for incremental re-indexing.</param>
    /// <param name="engine">The index engine used to delete removed documents.</param>
    /// <param name="workspaceContext">Provides workspace root and ID.</param>
    /// <param name="logger">Logger for watch activity.</param>
    public WatchCommandHandler(
        IIndexPipeline pipeline,
        IIndexEngine engine,
        IWorkspaceContext workspaceContext,
        ILogger<WatchCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(logger);
        _pipeline = pipeline;
        _engine = engine;
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

            foreach (var (path, _) in deletions)
            {
                var docId = BuildDocumentId(path);
                LogRemovingDocument(_logger, path);
                await _engine.DeleteAsync(docId, ct).ConfigureAwait(false);
            }

            if (modifications.Count > 0)
            {
                LogReIndexing(_logger, modifications.Count);
                var options = new IndexPipelineOptions { ForceRebuild = false };
                var result = await _pipeline.RunAsync(_workspaceContext.WorkspaceId, options, ct).ConfigureAwait(false);
                LogReIndexComplete(_logger, result.DocumentsIndexed, result.DocumentsSkipped);
            }
        }
        catch (Exception ex)
        {
            LogProcessChangesError(_logger, ex);
        }
    }

    private DocumentId BuildDocumentId(string absolutePath)
    {
        // Match the canonical URI format used by FilesystemConnector: filesystem:///relative/path
        var workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath;
        var relative = Path.GetRelativePath(workspaceRoot, absolutePath).Replace('\\', '/');
        var canonicalUri = AssetId.From(new Uri($"filesystem:///{relative}"));
        return DocumentId.From(canonicalUri);
    }
}
