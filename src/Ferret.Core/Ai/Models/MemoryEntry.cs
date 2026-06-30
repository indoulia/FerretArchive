namespace Ferret.Core.Ai.Models;

/// <summary>A tagged key-value memory entry used by workspace and task memory.</summary>
public sealed record MemoryEntry
{
    /// <summary>Gets the unique key identifying this entry within its memory scope.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the tags used for categorisation and retrieval.</summary>
    public required IReadOnlyList<string> Tags { get; init; }

    /// <summary>Gets the stored content.</summary>
    public required string Content { get; init; }

    /// <summary>Gets the UTC time at which this entry was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
