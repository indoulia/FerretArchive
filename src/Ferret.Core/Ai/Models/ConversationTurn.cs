namespace Ferret.Core.Ai.Models;

/// <summary>A single turn in a tracked conversation.</summary>
public sealed record ConversationTurn
{
    /// <summary>Gets the unique identifier for this turn.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the role of the participant who authored this turn.</summary>
    public required ChatRole Role { get; init; }

    /// <summary>Gets the text content of this turn.</summary>
    public required string Content { get; init; }

    /// <summary>Gets the UTC time at which this turn was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Creates a new turn with a generated ID and the current UTC timestamp.</summary>
    /// <param name="role">The role of the participant authoring this turn.</param>
    /// <param name="content">The text content of this turn.</param>
    /// <returns>A new <see cref="ConversationTurn"/> with a unique <see cref="Id"/> and <see cref="CreatedAt"/> set to <see cref="DateTimeOffset.UtcNow"/>.</returns>
    public static ConversationTurn Create(ChatRole role, string content) => new()
    {
        Id = Guid.NewGuid(),
        Role = role,
        Content = content,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
