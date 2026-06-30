using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Tracks conversation turns for an ongoing session.
/// The default implementation is <see cref="NullImplementations.NullConversationMemory"/>;
/// persistent storage is introduced in Sprint 15.
/// </summary>
public interface IConversationMemory
{
    /// <summary>Records a conversation turn.</summary>
    /// <param name="turn">The turn to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(ConversationTurn turn, CancellationToken ct);

    /// <summary>Returns the most recent turns, newest first.</summary>
    /// <param name="count">The maximum number of turns to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of the most recent <see cref="ConversationTurn"/> values, up to <paramref name="count"/>.</returns>
    Task<IReadOnlyList<ConversationTurn>> GetRecentAsync(int count, CancellationToken ct);

    /// <summary>Clears all stored turns.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ClearAsync(CancellationToken ct);
}
