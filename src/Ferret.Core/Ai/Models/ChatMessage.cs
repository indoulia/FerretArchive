namespace Ferret.Core.Ai.Models;

/// <summary>A single message in a chat conversation.</summary>
public sealed record ChatMessage
{
    /// <summary>Gets the role of the participant who authored this message.</summary>
    public required ChatRole Role { get; init; }

    /// <summary>Gets the text content of this message.</summary>
    public required string Content { get; init; }

    /// <summary>Creates a system-role message with the given content.</summary>
    /// <param name="content">The system instruction text.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.System"/>.</returns>
    public static ChatMessage System(string content) =>
        new() { Role = ChatRole.System, Content = content };

    /// <summary>Creates a user-role message with the given content.</summary>
    /// <param name="content">The user input text.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.User"/>.</returns>
    public static ChatMessage User(string content) =>
        new() { Role = ChatRole.User, Content = content };

    /// <summary>Creates an assistant-role message with the given content.</summary>
    /// <param name="content">The assistant response text.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.Assistant"/>.</returns>
    public static ChatMessage Assistant(string content) =>
        new() { Role = ChatRole.Assistant, Content = content };
}
