using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ConversationTurnTests
{
    [Fact]
    public void Create_GeneratesNewGuid()
    {
        var a = ConversationTurn.Create(ChatRole.User, "hello");
        var b = ConversationTurn.Create(ChatRole.User, "hello");
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Create_SetsRoleAndContent()
    {
        var turn = ConversationTurn.Create(ChatRole.Assistant, "hi");
        Assert.Equal(ChatRole.Assistant, turn.Role);
        Assert.Equal("hi", turn.Content);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var turn = ConversationTurn.Create(ChatRole.User, "test");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.InRange(turn.CreatedAt, before, after);
    }

    [Fact]
    public void Create_IdIsNonEmpty()
    {
        var turn = ConversationTurn.Create(ChatRole.User, "hello");
        Assert.NotEqual(Guid.Empty, turn.Id);
    }

    [Fact]
    public void MemoryEntry_PreservesTagsAndContent()
    {
        var entry = new MemoryEntry
        {
            Key = "sprint-context",
            Tags = ["sprint", "context"],
            Content = "Sprint 12 is AI platform",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        Assert.Equal(2, entry.Tags.Count);
        Assert.Equal("sprint-context", entry.Key);
    }

    [Fact]
    public void MemoryEntry_PreservesContent()
    {
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "stored value",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        Assert.Equal("stored value", entry.Content);
    }

    [Fact]
    public void MemoryEntry_EmptyTags_IsValid()
    {
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        Assert.Empty(entry.Tags);
    }
}
