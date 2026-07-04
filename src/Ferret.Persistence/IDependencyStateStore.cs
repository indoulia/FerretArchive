namespace Ferret.Persistence;

/// <summary>Persists and retrieves <see cref="DependencyRecord"/> instances, per ARCH-032's
/// persistence mechanism. Names no storage technology, file format, or key structure —
/// those are left to the implementation (ARCH-032 §9).</summary>
public interface IDependencyStateStore
{
    /// <summary>Returns the stored record for the given request identity, or null if none has been recorded.</summary>
    /// <param name="engineResponsibility">The engine responsibility the request invoked (ARCH-028 §2, property 1).</param>
    /// <param name="requestPath">The request's explicit parameter — the file path (ARCH-028 §2, property 2).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored <see cref="DependencyRecord"/>, or null if no entry exists.</returns>
    ValueTask<DependencyRecord?> GetRecordAsync(string engineResponsibility, string requestPath, CancellationToken ct = default);

    /// <summary>Records or updates the dependency record for the request identity it carries.</summary>
    /// <param name="record">The record to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the record has been stored.</returns>
    Task SetRecordAsync(DependencyRecord record, CancellationToken ct = default);
}
