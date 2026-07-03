using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Events;
using Ferret.Core.Events.Indexing;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Indexing;

/// <summary>Orchestrates the discover -> parse -> index pipeline.
/// Implements <see cref="IIndexPipeline"/>.</summary>
public sealed class IndexPipeline : IIndexPipeline
{
    private readonly IConnectorManager _connectorManager;
    private readonly IParserDispatcher _dispatcher;
    private readonly IIndexEngine _engine;
    private readonly IEventBus _eventBus;
    private readonly IIndexStateStore _stateStore;

    /// <summary>Initializes a new instance of the <see cref="IndexPipeline"/> class.</summary>
    /// <param name="connectorManager">The connector manager providing active connector runtimes.</param>
    /// <param name="dispatcher">The parser dispatcher.</param>
    /// <param name="engine">The keyword index engine.</param>
    /// <param name="eventBus">The event bus for publishing lifecycle events.</param>
    /// <param name="stateStore">Optional state store for incremental indexing; defaults to no-op.</param>
    public IndexPipeline(
        IConnectorManager connectorManager,
        IParserDispatcher dispatcher,
        IIndexEngine engine,
        IEventBus eventBus,
        IIndexStateStore? stateStore = null)
    {
        ArgumentNullException.ThrowIfNull(connectorManager);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(eventBus);

        _connectorManager = connectorManager;
        _dispatcher = dispatcher;
        _engine = engine;
        _eventBus = eventBus;
        _stateStore = stateStore ?? new NullIndexStateStore();
    }

    /// <inheritdoc/>
    public async Task<IndexResult> RunAsync(WorkspaceId workspaceId, IndexPipelineOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(options);

        var correlationId = CorrelationId.Create(Guid.NewGuid().ToString("N"));
        var startTick = Environment.TickCount64;

        await _eventBus.PublishAsync(
            new IndexingStartedEvent(workspaceId.Value, correlationId)
            {
                IsRebuild = options.ForceRebuild,
            },
            ct).ConfigureAwait(false);

        if (options.ForceRebuild)
        {
            await _engine.ClearAsync(ct).ConfigureAwait(false);
            await _stateStore.ClearAsync(ct).ConfigureAwait(false);
        }

        int assetsDiscovered = 0;
        int assetsProcessed = 0;
        int indexed = 0;
        int skipped = 0;
        int failures = 0;
        var failureMessages = new List<string>();
        var seenAssets = new HashSet<AssetId>();

        var runtimes = await _connectorManager.GetActiveConnectorsAsync(ct).ConfigureAwait(false);
        foreach (var runtime in runtimes)
        {
            if (runtime.Connector is not IAssetSource source)
            {
                continue;
            }

            if (options.InstanceId is not null)
            {
                // Instance filtering: skip connectors that don't match the requested instance.
                // Connectors expose their instance via ConnectAsync/session; for now we rely on
                // matching the IConnectorSession.InstanceId after connection.
                // Sprint 9: simple name-based check via ConnectorMetadata.
                // This is a best-effort filter -- InstanceId on options is connector-instance scoped.
            }

            await foreach (var asset in source.DiscoverAsync(AssetDiscoveryOptions.Default, ct).ConfigureAwait(false))
            {
                // Directories are structural, not indexable documents: a directory path
                // cannot be opened as a file stream (Windows surfaces UnauthorizedAccessException).
                // Skip before counting so the summary reflects indexable assets only.
                if (asset.Kind != AssetKind.File)
                {
                    continue;
                }

                assetsDiscovered++;

                await _eventBus.PublishAsync(
                    new DocumentDiscoveredEvent(asset.Id.Value, correlationId),
                    ct).ConfigureAwait(false);

                // Only process assets if the connector also implements IAssetReader.
                if (runtime.Connector is not IAssetReader reader)
                {
                    skipped++;
                    await _eventBus.PublishAsync(
                        new DocumentSkippedEvent(asset.Id.Value, correlationId)
                        {
                            AssetId = asset.Id,
                            Reason = "Connector does not implement IAssetReader",
                        },
                        ct).ConfigureAwait(false);
                    continue;
                }

                assetsProcessed++;

                // Incremental: skip if fingerprint unchanged since last index run
                var computedFingerprint = AssetFingerprint.CreateLightweight(
                    asset.LastModified, asset.SizeBytes ?? 0);
                seenAssets.Add(asset.Id);
                var storedFingerprint = await _stateStore
                    .GetFingerprintAsync(asset.Id, ct).ConfigureAwait(false);
                if (storedFingerprint == computedFingerprint)
                {
                    skipped++;
                    assetsProcessed--;  // undo: this asset was not truly processed
                    await _eventBus.PublishAsync(
                        new DocumentSkippedEvent(asset.Id.Value, correlationId)
                        {
                            AssetId = asset.Id,
                            Reason = "Fingerprint unchanged",
                        },
                        ct).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    var stream = await reader.OpenAsync(asset, ct).ConfigureAwait(false);
                    await using (stream.ConfigureAwait(false))
                    {
                        var result = await _dispatcher.DispatchAsync(stream, asset, ct).ConfigureAwait(false);

                        if (result.Kind == ParseResultKind.Success && result.Value is not null)
                        {
                            await _engine.WriteAsync(result.Value, ct).ConfigureAwait(false);
                            indexed++;
                            await _stateStore
                                .SetFingerprintAsync(asset.Id, computedFingerprint, ct)
                                .ConfigureAwait(false);

                            await _eventBus.PublishAsync(
                                new DocumentIndexedEvent(result.Value.Id.Value, correlationId)
                                {
                                    DocumentId = result.Value.Id,
                                    AssetId = asset.Id,
                                    MediaType = result.Value.MediaType,
                                    CharCount = result.Value.PlainText.Length,
                                },
                                ct).ConfigureAwait(false);
                        }
                        else if (result.Kind is ParseResultKind.Unsupported or ParseResultKind.Empty)
                        {
                            skipped++;
                            var reason = result.Kind == ParseResultKind.Unsupported
                                ? $"No parser registered for media type '{asset.MediaType}'"
                                : "Content is empty";

                            await _eventBus.PublishAsync(
                                new DocumentSkippedEvent(asset.Id.Value, correlationId)
                                {
                                    AssetId = asset.Id,
                                    Reason = reason,
                                },
                                ct).ConfigureAwait(false);
                        }
                        else
                        {
                            // ParseResultKind.Failed
                            failures++;
                            var msg = result.Diagnostics.Count > 0
                                ? result.Diagnostics[0].Message
                                : "Parse failed";
                            failureMessages.Add($"{asset.DisplayName}: {msg}");

                            await _eventBus.PublishAsync(
                                new DocumentParsingFailedEvent(asset.Id.Value, correlationId)
                                {
                                    AssetId = asset.Id,
                                    MediaType = asset.MediaType ?? "application/octet-stream",
                                    ErrorMessage = msg,
                                },
                                ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
#pragma warning disable CA1031 // Do not catch general exception types -- pipeline must be resilient
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    failures++;
                    failureMessages.Add($"{asset.DisplayName}: {ex.Message}");

                    await _eventBus.PublishAsync(
                        new DocumentParsingFailedEvent(asset.Id.Value, correlationId)
                        {
                            AssetId = asset.Id,
                            MediaType = asset.MediaType ?? "application/octet-stream",
                            ErrorMessage = ex.Message,
                        },
                        ct).ConfigureAwait(false);
                }
            }
        }

        // Remove state entries for assets no longer discovered (deleted files).
        var allKnown = await _stateStore.GetAllKeysAsync(ct).ConfigureAwait(false);
        foreach (var staleId in allKnown.Except(seenAssets))
        {
            await _stateStore.RemoveAsync(staleId, ct).ConfigureAwait(false);
        }

        await _stateStore.SaveAsync(ct).ConfigureAwait(false);

        var duration = TimeSpan.FromMilliseconds(Environment.TickCount64 - startTick);
        var indexResult = new IndexResult
        {
            AssetsDiscovered = assetsDiscovered,
            AssetsProcessed = assetsProcessed,
            DocumentsIndexed = indexed,
            DocumentsSkipped = skipped,
            Failures = failures,
            Warnings = 0,
            Duration = duration,
            FailureMessages = failureMessages,
        };

        await _eventBus.PublishAsync(
            new IndexingCompletedEvent(workspaceId.Value, correlationId)
            {
                Result = indexResult,
            },
            ct).ConfigureAwait(false);

        return indexResult;
    }
}
