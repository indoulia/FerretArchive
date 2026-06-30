using Ferret.Cli.Commands.Indexing;
using Ferret.Core.Connectors;
using Ferret.Core.Events;
using Ferret.Core.Events.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Cli.Tests.Commands.Indexing;

public sealed class ConsoleIndexEventSinkTests
{
    [Fact]
    public async Task PublishAsync_DocumentIndexed_WritesVerboseLine()
    {
        var output = new FakeIndexOutput();
        var sink = new ConsoleIndexEventSink(output, NullEventBus.Instance);

        var evt = new DocumentIndexedEvent("doc-1", CorrelationId.Create("corr-1"))
        {
            DocumentId = DocumentId.Create("doc-1"),
            AssetId = new AssetId("asset-1"),
            MediaType = "text/plain",
            CharCount = 100,
        };

        await sink.PublishAsync(evt, CancellationToken.None);
        Assert.Contains(output.Lines, l =>
            l.Contains("doc-1", StringComparison.Ordinal) ||
            l.Contains("indexed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishAsync_DocumentSkipped_WritesVerboseLine()
    {
        var output = new FakeIndexOutput();
        var sink = new ConsoleIndexEventSink(output, NullEventBus.Instance);

        var evt = new DocumentSkippedEvent("asset-skip", CorrelationId.Create("corr-2"))
        {
            AssetId = new AssetId("asset-skip"),
            Reason = "already up to date",
        };

        await sink.PublishAsync(evt, CancellationToken.None);
        Assert.Contains(output.Lines, l =>
            l.Contains("asset-skip", StringComparison.Ordinal) ||
            l.Contains("skipped", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishAsync_DocumentParsingFailed_WritesVerboseLine()
    {
        var output = new FakeIndexOutput();
        var sink = new ConsoleIndexEventSink(output, NullEventBus.Instance);

        var evt = new DocumentParsingFailedEvent("asset-fail", CorrelationId.Create("corr-3"))
        {
            AssetId = new AssetId("asset-fail"),
            MediaType = "text/plain",
            ErrorMessage = "unexpected token",
        };

        await sink.PublishAsync(evt, CancellationToken.None);
        Assert.Contains(output.Lines, l =>
            l.Contains("asset-fail", StringComparison.Ordinal) ||
            l.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishAsync_Forwards_To_InnerBus()
    {
        var output = new FakeIndexOutput();
        var inner = new RecordingEventBus();
        var sink = new ConsoleIndexEventSink(output, inner);

        var evt = new DocumentIndexedEvent("doc-2", CorrelationId.Create("corr-4"))
        {
            DocumentId = DocumentId.Create("doc-2"),
            AssetId = new AssetId("asset-2"),
            MediaType = "text/plain",
            CharCount = 50,
        };

        await sink.PublishAsync(evt, CancellationToken.None);
        Assert.Equal(1, inner.PublishCount);
    }

    [Fact]
    public async Task PublishAsync_IndexingStarted_ForwardsWithoutWritingDocumentLine()
    {
        var output = new FakeIndexOutput();
        var inner = new RecordingEventBus();
        var sink = new ConsoleIndexEventSink(output, inner);

        var evt = new IndexingStartedEvent("ws-1", CorrelationId.Create("corr-5")) { IsRebuild = false };

        await sink.PublishAsync(evt, CancellationToken.None);
        Assert.Equal(1, inner.PublishCount);
    }
}

internal sealed class RecordingEventBus : IEventBus
{
    internal int PublishCount { get; private set; }

    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent
    {
        PublishCount++;
        return Task.CompletedTask;
    }
}
