using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiChatModelTests
{
    private static OpenAiChatModel MakeModel(string modelName = "gpt-4o") =>
        new(
            modelName,
            new OpenAiOptions { Enabled = true, ApiKey = "sk-test" },
            NullLogger<OpenAiChatModel>.Instance);

    [Fact]
    public void Descriptor_ModelIdMatchesConstructorArg()
    {
        var sut = MakeModel("gpt-4o");
        Assert.Equal("openai/gpt-4o", sut.Descriptor.Id.Value);
    }

    [Fact]
    public void Descriptor_HasChatCapability()
    {
        var sut = MakeModel("gpt-4o");
        Assert.True(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
    }

    [Fact]
    public void Descriptor_ProviderIdIsOpenAi()
    {
        var sut = MakeModel("gpt-4o");
        Assert.Equal("openai", sut.Descriptor.ProviderId.Value);
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiChatModel("gpt-4o", null!, NullLogger<OpenAiChatModel>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiChatModel("gpt-4o", new OpenAiOptions { ApiKey = "sk-test" }, null!));
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ChatAsync_SimpleRequest_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiChatModel(
            "gpt-4o-mini",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiChatModel>.Instance);
        var request = new ChatRequest
        {
            Messages = [ChatMessage.User("Say hello in one word.")],
            MaxTokens = 10,
            Temperature = 0.0,
        };
        var response = await model.ChatAsync(request, CancellationToken.None);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
        Assert.Equal(FinishReason.Stop, response.FinishReason);
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ChatStreamAsync_SimpleRequest_YieldsChunks()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiChatModel(
            "gpt-4o-mini",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiChatModel>.Instance);
        var request = new ChatRequest
        {
            Messages = [ChatMessage.User("Count to 3.")],
            MaxTokens = 20,
            Temperature = 0.0,
        };
        var chunks = new List<ChatResponseChunk>();
        await foreach (var chunk in model.ChatStreamAsync(request, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.NotEmpty(chunks);
    }
}
