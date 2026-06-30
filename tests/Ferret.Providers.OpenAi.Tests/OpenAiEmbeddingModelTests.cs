using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiEmbeddingModelTests
{
    private static OpenAiEmbeddingModel MakeModel(string modelName = "text-embedding-3-small") =>
        new(
            modelName,
            new OpenAiOptions { Enabled = true, ApiKey = "sk-test" },
            NullLogger<OpenAiEmbeddingModel>.Instance);

    [Fact]
    public void Descriptor_ModelIdMatchesConstructorArg()
    {
        var sut = MakeModel("text-embedding-3-small");
        Assert.Equal("openai/text-embedding-3-small", sut.Descriptor.Id.Value);
    }

    [Fact]
    public void Descriptor_HasEmbeddingCapability()
    {
        var sut = MakeModel("text-embedding-3-small");
        Assert.True(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public void Descriptor_ProviderIdIsOpenAi()
    {
        var sut = MakeModel("text-embedding-3-small");
        Assert.Equal("openai", sut.Descriptor.ProviderId.Value);
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel("text-embedding-3-small", null!, NullLogger<OpenAiEmbeddingModel>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiEmbeddingModel("text-embedding-3-small", new OpenAiOptions { ApiKey = "sk-test" }, null!));
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task EmbedAsync_ReturnsNonEmptyVector()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiEmbeddingModel(
            "text-embedding-3-small",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiEmbeddingModel>.Instance);
        var request = new EmbeddingRequest { Text = "hello world" };
        var result = await model.EmbedAsync(request, CancellationToken.None);
        Assert.Equal(1536, result.Vector.Length);
        Assert.True(result.TokenCount > 0);
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task EmbedBatchAsync_ReturnsOneResultPerRequest()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiEmbeddingModel(
            "text-embedding-3-small",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiEmbeddingModel>.Instance);
        var requests = new List<EmbeddingRequest>
        {
            new() { Text = "first document" },
            new() { Text = "second document" },
        };
        var results = await model.EmbedBatchAsync(requests, CancellationToken.None);
        Assert.Equal(2, results.Count);
    }
}
