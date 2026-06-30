namespace Ferret.Core.Indexing;

#pragma warning disable CA1040 // Reserved stub interface — members added in Sprint 10+
/// <summary>Reports live progress during pipeline operations. Reserved for Sprint 10+.
/// Implementations will be injected into <c>IIndexPipeline</c> to surface per-document
/// progress events to CLI spinners, log sinks, or IPC streams.</summary>
public interface IProgressReporter
{
    // Reserved: void Report(IndexProgress progress);
    // Reserved: IAsyncEnumerable<IndexProgress> WatchAsync(CancellationToken ct);
}
#pragma warning restore CA1040
