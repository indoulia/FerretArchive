using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.NullImplementations;

/// <summary>
/// No-op workspace memory. Registered by default until Sprint 15 provides persistent storage.
/// All operations complete successfully without storing or returning any data.
/// </summary>
public sealed class NullWorkspaceMemory : IWorkspaceMemory
{
    /// <inheritdoc/>
    public Task SaveAsync(MemoryEntry entry, CancellationToken ct) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<MemoryEntry?> GetAsync(string key, CancellationToken ct) =>
        Task.FromResult<MemoryEntry?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<MemoryEntry>> SearchAsync(IReadOnlyList<string> tags, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MemoryEntry>>([]);
}
