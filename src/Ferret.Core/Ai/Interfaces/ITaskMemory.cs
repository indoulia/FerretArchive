using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Short-lived key-value memory scoped to a single task execution.
/// The default implementation is <see cref="NullImplementations.NullTaskMemory"/>;
/// persistent storage is introduced in Sprint 15.
/// </summary>
public interface ITaskMemory
{
    /// <summary>Saves or replaces a memory entry by key.</summary>
    /// <param name="entry">The entry to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveAsync(MemoryEntry entry, CancellationToken ct);

    /// <summary>Retrieves a memory entry by key, or <see langword="null"/> if not found.</summary>
    /// <param name="key">The entry key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="MemoryEntry"/>, or <see langword="null"/>.</returns>
    Task<MemoryEntry?> GetAsync(string key, CancellationToken ct);

    /// <summary>Returns all entries whose tag set contains at least one of the given tags.</summary>
    /// <param name="tags">The tags to match against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All matching <see cref="MemoryEntry"/> values.</returns>
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(IReadOnlyList<string> tags, CancellationToken ct);
}
