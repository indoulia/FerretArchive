using Ferret.Cli.Cli;
using Ferret.Core.Events;
using Ferret.Core.Events.Indexing;

namespace Ferret.Cli.Commands.Indexing;

/// <summary>
/// IEventBus decorator that writes per-document indexing events to the console output formatter.
/// Constructed inline in IndexCommandHandler when --verbose is set; not registered in DI.
/// </summary>
internal sealed class ConsoleIndexEventSink : IEventBus
{
    private readonly IOutputFormatter _output;
    private readonly IEventBus _inner;

    internal ConsoleIndexEventSink(IOutputFormatter output, IEventBus inner)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(inner);
        _output = output;
        _inner = inner;
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent
    {
        WriteToConsole(domainEvent);
        await _inner.PublishAsync(domainEvent, ct).ConfigureAwait(false);
    }

    private void WriteToConsole(DomainEvent evt)
    {
        switch (evt)
        {
            case IndexingStartedEvent started:
                _output.WriteVerbose($"  [index] Starting{(started.IsRebuild ? " (rebuild)" : string.Empty)}…");
                break;
            case DocumentDiscoveredEvent discovered:
                _output.WriteVerbose($"  [discover] {discovered.AssetId.Value}");
                break;
            case DocumentIndexedEvent indexed:
                _output.WriteVerbose($"  [indexed]  {indexed.DocumentId.Value}  ({indexed.CharCount} chars, {indexed.MediaType})");
                break;
            case DocumentSkippedEvent skipped:
                _output.WriteVerbose($"  [skipped]  {skipped.AssetId.Value}  — {skipped.Reason}");
                break;
            case DocumentParsingFailedEvent failed:
                _output.WriteVerbose($"  [failed]   {failed.AssetId.Value}  — {failed.ErrorMessage}");
                break;
            case IndexingCompletedEvent:
                _output.WriteVerbose("  [index] Complete.");
                break;
        }
    }
}
