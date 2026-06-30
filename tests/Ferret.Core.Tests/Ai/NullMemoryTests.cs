#pragma warning disable CA1859 // Interface return types are intentional — tests verify the contract, not the concrete type
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Core.Ai.NullImplementations;

using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class NullMemoryTests
{
    // Factory methods returning the interface type: the concrete type is unknown
    // at each call site, which keeps tests honest about the interface contract.
    private static IConversationMemory CreateConversationMemory() => new NullConversationMemory();

    private static IWorkspaceMemory CreateWorkspaceMemory() => new NullWorkspaceMemory();

    private static ITaskMemory CreateTaskMemory() => new NullTaskMemory();

    // --- IConversationMemory ---

    [Fact]
    public async Task NullConversationMemory_AddAsync_DoesNotThrow()
    {
        var sut = CreateConversationMemory();
        var turn = ConversationTurn.Create(ChatRole.User, "hello");
        await sut.AddAsync(turn, CancellationToken.None);
    }

    [Fact]
    public async Task NullConversationMemory_GetRecentAsync_ReturnsEmpty()
    {
        var sut = CreateConversationMemory();
        var result = await sut.GetRecentAsync(10, CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullConversationMemory_GetRecentAsync_ZeroCount_ReturnsEmpty()
    {
        var sut = CreateConversationMemory();
        var result = await sut.GetRecentAsync(0, CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullConversationMemory_ClearAsync_DoesNotThrow()
    {
        var sut = CreateConversationMemory();
        await sut.ClearAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NullConversationMemory_AddThenGetRecent_StillReturnsEmpty()
    {
        var sut = CreateConversationMemory();
        await sut.AddAsync(ConversationTurn.Create(ChatRole.User, "hello"), CancellationToken.None);
        var result = await sut.GetRecentAsync(10, CancellationToken.None);
        Assert.Empty(result);
    }

    // --- IWorkspaceMemory ---

    [Fact]
    public async Task NullWorkspaceMemory_SaveAsync_DoesNotThrow()
    {
        var sut = CreateWorkspaceMemory();
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await sut.SaveAsync(entry, CancellationToken.None);
    }

    [Fact]
    public async Task NullWorkspaceMemory_GetAsync_ReturnsNull()
    {
        var sut = CreateWorkspaceMemory();
        var result = await sut.GetAsync("any-key", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task NullWorkspaceMemory_SearchAsync_ReturnsEmpty()
    {
        var sut = CreateWorkspaceMemory();
        var result = await sut.SearchAsync(["tag1"], CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullWorkspaceMemory_SaveThenGet_StillReturnsNull()
    {
        var sut = CreateWorkspaceMemory();
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await sut.SaveAsync(entry, CancellationToken.None);
        var result = await sut.GetAsync("k", CancellationToken.None);
        Assert.Null(result);
    }

    // --- ITaskMemory ---

    [Fact]
    public async Task NullTaskMemory_SaveAsync_DoesNotThrow()
    {
        var sut = CreateTaskMemory();
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await sut.SaveAsync(entry, CancellationToken.None);
    }

    [Fact]
    public async Task NullTaskMemory_GetAsync_ReturnsNull()
    {
        var sut = CreateTaskMemory();
        var result = await sut.GetAsync("any-key", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task NullTaskMemory_SearchAsync_ReturnsEmpty()
    {
        var sut = CreateTaskMemory();
        var result = await sut.SearchAsync(["tag1"], CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullTaskMemory_SaveThenGet_StillReturnsNull()
    {
        var sut = CreateTaskMemory();
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await sut.SaveAsync(entry, CancellationToken.None);
        var result = await sut.GetAsync("k", CancellationToken.None);
        Assert.Null(result);
    }

    // --- Interface compliance: each null type is accessed solely via its interface ---

    [Fact]
    public void NullConversationMemory_ImplementsInterface()
    {
        Assert.IsAssignableFrom<IConversationMemory>(new NullConversationMemory());
    }

    [Fact]
    public void NullWorkspaceMemory_ImplementsInterface()
    {
        Assert.IsAssignableFrom<IWorkspaceMemory>(new NullWorkspaceMemory());
    }

    [Fact]
    public void NullTaskMemory_ImplementsInterface()
    {
        Assert.IsAssignableFrom<ITaskMemory>(new NullTaskMemory());
    }
}
