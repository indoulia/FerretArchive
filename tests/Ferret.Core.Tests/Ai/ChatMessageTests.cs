using Ferret.Core.Ai.Models;

using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ChatMessageTests
{
    [Fact]
    public void System_SetsRoleAndContent()
    {
        var msg = ChatMessage.System("you are a helpful assistant");
        Assert.Equal(ChatRole.System, msg.Role);
        Assert.Equal("you are a helpful assistant", msg.Content);
    }

    [Fact]
    public void User_SetsRoleAndContent()
    {
        var msg = ChatMessage.User("hello");
        Assert.Equal(ChatRole.User, msg.Role);
        Assert.Equal("hello", msg.Content);
    }

    [Fact]
    public void Assistant_SetsRoleAndContent()
    {
        var msg = ChatMessage.Assistant("hi there");
        Assert.Equal(ChatRole.Assistant, msg.Role);
        Assert.Equal("hi there", msg.Content);
    }

    [Fact]
    public void ChatRequest_DefaultTemperature_IsPointSeven()
    {
        var request = new ChatRequest { Messages = [ChatMessage.User("hello")] };
        Assert.Equal(0.7, request.Temperature);
    }

    [Fact]
    public void ChatRequest_MaxTokens_DefaultsToNull()
    {
        var request = new ChatRequest { Messages = [ChatMessage.User("hello")] };
        Assert.Null(request.MaxTokens);
    }

    [Fact]
    public void ChatResponseChunk_NullableFinishReason()
    {
        var chunk = new ChatResponseChunk { Delta = "hello" };
        Assert.Null(chunk.FinishReason);
    }
}
