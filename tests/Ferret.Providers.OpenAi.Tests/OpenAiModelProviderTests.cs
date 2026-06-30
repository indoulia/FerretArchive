using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiModelProviderTests
{
    private static OpenAiModelProvider MakeProvider() =>
        new(
            new OpenAiOptions { Enabled = true, ApiKey = "sk-test" },
            NullLogger<OpenAiModelProvider>.Instance);

    [Fact]
    public void Descriptor_ProviderIdIsOpenAi()
    {
        var sut = MakeProvider();
        Assert.Equal("openai", sut.Descriptor.Id.Value);
    }

    [Fact]
    public void Descriptor_HasChatAndEmbeddingCapabilities()
    {
        var sut = MakeProvider();
        Assert.True(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
        Assert.True(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public async Task ListModelsAsync_ReturnsFourWellKnownModels()
    {
        var sut = MakeProvider();
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.Equal(4, models.Count);
    }

    [Fact]
    public async Task ListModelsAsync_IncludesGpt4o()
    {
        var sut = MakeProvider();
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.Contains(models, m => m.Id.Value == "openai/gpt-4o");
    }

    [Fact]
    public async Task ListModelsAsync_IncludesEmbeddingModels()
    {
        var sut = MakeProvider();
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.Contains(models, m => m.Id.Value == "openai/text-embedding-3-small");
        Assert.Contains(models, m => m.Id.Value == "openai/text-embedding-3-large");
    }

    [Fact]
    public async Task ListModelsAsync_DoesNotCallNetwork()
    {
        var sut = new OpenAiModelProvider(
            new OpenAiOptions { Enabled = true, ApiKey = "invalid-key-no-network" },
            NullLogger<OpenAiModelProvider>.Instance);
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.Equal(4, models.Count);
    }

    [Fact]
    public void GetChatModel_Gpt4o_ReturnsNonNull()
    {
        var sut = MakeProvider();
        var model = sut.GetChatModel(ModelId.Create("openai/gpt-4o"));
        Assert.NotNull(model);
    }

    [Fact]
    public void GetChatModel_Gpt4oMini_ReturnsNonNull()
    {
        var sut = MakeProvider();
        var model = sut.GetChatModel(ModelId.Create("openai/gpt-4o-mini"));
        Assert.NotNull(model);
    }

    [Fact]
    public void GetChatModel_UnknownModel_ReturnsNull()
    {
        var sut = MakeProvider();
        var model = sut.GetChatModel(ModelId.Create("ollama/llama3.2"));
        Assert.Null(model);
    }

    [Fact]
    public void GetChatModel_EmbeddingModelId_ReturnsNull()
    {
        var sut = MakeProvider();
        var model = sut.GetChatModel(ModelId.Create("openai/text-embedding-3-small"));
        Assert.Null(model);
    }

    [Fact]
    public void GetEmbeddingModel_TextEmbedding3Small_ReturnsNonNull()
    {
        var sut = MakeProvider();
        var model = sut.GetEmbeddingModel(ModelId.Create("openai/text-embedding-3-small"));
        Assert.NotNull(model);
    }

    [Fact]
    public void GetEmbeddingModel_TextEmbedding3Large_ReturnsNonNull()
    {
        var sut = MakeProvider();
        var model = sut.GetEmbeddingModel(ModelId.Create("openai/text-embedding-3-large"));
        Assert.NotNull(model);
    }

    [Fact]
    public void GetEmbeddingModel_ChatModelId_ReturnsNull()
    {
        var sut = MakeProvider();
        var model = sut.GetEmbeddingModel(ModelId.Create("openai/gpt-4o"));
        Assert.Null(model);
    }

    [Fact]
    public void GetEmbeddingModel_WrongProvider_ReturnsNull()
    {
        var sut = MakeProvider();
        var model = sut.GetEmbeddingModel(ModelId.Create("ollama/nomic-embed-text"));
        Assert.Null(model);
    }

    [Fact]
    public void GetReranker_AlwaysReturnsNull()
    {
        var sut = MakeProvider();
        Assert.Null(sut.GetReranker(ModelId.Create("openai/gpt-4o")));
    }

    [Fact]
    public void GetChatModel_ReturnedModel_HasChatCapability()
    {
        var sut = MakeProvider();
        var model = sut.GetChatModel(ModelId.Create("openai/gpt-4o"));
        Assert.NotNull(model);
        Assert.True(model.Descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
    }

    [Fact]
    public void GetEmbeddingModel_ReturnedModel_HasEmbeddingCapability()
    {
        var sut = MakeProvider();
        var model = sut.GetEmbeddingModel(ModelId.Create("openai/text-embedding-3-small"));
        Assert.NotNull(model);
        Assert.True(model.Descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }
}
