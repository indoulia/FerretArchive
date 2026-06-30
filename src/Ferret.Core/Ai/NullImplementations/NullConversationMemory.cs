using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.NullImplementations;

/// <summary>
/// No-op conversation memory. Registered by default until Sprint 15 provides persistent storage.
/// All operations complete successfully without storing or returning any data.
/// </summary>
public sealed class NullConversationMemory : IConversationMemory
{
    /// <inheritdoc/>
    public Task AddAsync(ConversationTurn turn, CancellationToken ct) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConversationTurn>> GetRecentAsync(int count, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ConversationTurn>>([]);

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
}
