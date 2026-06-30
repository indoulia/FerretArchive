#pragma warning disable CA2000 // Test handlers have no real resources; lifetime is bounded to test process

using System.Net;
using System.Text;

using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Ferret.Providers.Ollama.Tests;

public sealed class OllamaModelProviderTests
{
    private static OllamaModelProvider MakeProvider(HttpMessageHandler handler)
    {
        var options = new OllamaOptions { BaseUrl = "http://localhost:11434", TimeoutSeconds = 30, Enabled = true };
        return new OllamaModelProvider(options, NullLogger<OllamaModelProvider>.Instance, new HttpClient(handler));
    }

    [Fact]
    public void Descriptor_HasCorrectProviderIdAndDisplayName()
    {
        using var sut = MakeProvider(new NotCalledHandler());
        Assert.Equal("ollama", sut.Descriptor.Id.Value);
        Assert.Equal("Ollama", sut.Descriptor.DisplayName);
    }

    [Fact]
    public async Task ListModelsAsync_FakeHttpResponse_ReturnsModelDescriptors()
    {
        var json = """
            {
              "models": [
                { "name": "llama3.2", "model": "llama3.2", "modified_at": "2026-01-01T00:00:00Z",
                  "size": 2048000000, "digest": "abc123",
                  "details": { "format": "gguf", "family": "llama", "families": null,
                               "parameter_size": "3.2B", "quantization_level": "Q4_0",
                               "parent_model": null } },
                { "name": "nomic-embed-text", "model": "nomic-embed-text", "modified_at": "2026-01-01T00:00:00Z",
                  "size": 274000000, "digest": "def456",
                  "details": { "format": "gguf", "family": "nomic-bert", "families": null,
                               "parameter_size": "137M", "quantization_level": "F16",
                               "parent_model": null } }
              ]
            }
            """;
        using var sut = MakeProvider(new FakeHttpHandler("/api/tags", HttpStatusCode.OK, json));

        var models = await sut.ListModelsAsync(CancellationToken.None);

        Assert.Equal(2, models.Count);
        Assert.Contains(models, m => m.Id.Value == "ollama/llama3.2");
        Assert.Contains(models, m => m.Id.Value == "ollama/nomic-embed-text");
        Assert.All(models, m => Assert.True(m.Capabilities.HasFlag(ModelCapabilities.Chat)));
        Assert.All(models, m => Assert.True(m.Capabilities.HasFlag(ModelCapabilities.Embedding)));
    }

    [Fact]
    public async Task ListModelsAsync_HttpFailure_ReturnsEmpty()
    {
        using var sut = MakeProvider(new FakeHttpHandler("/api/tags", HttpStatusCode.ServiceUnavailable, string.Empty));

        var models = await sut.ListModelsAsync(CancellationToken.None);

        Assert.Empty(models);
    }

    [Fact]
    public void GetChatModel_CorrectPrefix_ReturnsOllamaChatModel()
    {
        using var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("ollama/llama3.2");

        var model = sut.GetChatModel(modelId);

        Assert.NotNull(model);
    }

    [Fact]
    public void GetChatModel_WrongPrefix_ReturnsNull()
    {
        using var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("openai/gpt-4o");

        var model = sut.GetChatModel(modelId);

        Assert.Null(model);
    }

    [Fact]
    public void GetEmbeddingModel_CorrectPrefix_ReturnsOllamaEmbeddingModel()
    {
        using var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("ollama/nomic-embed-text");

        var model = sut.GetEmbeddingModel(modelId);

        Assert.NotNull(model);
    }

    [Fact]
    public void GetEmbeddingModel_WrongPrefix_ReturnsNull()
    {
        using var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("openai/text-embedding-3-small");

        var model = sut.GetEmbeddingModel(modelId);

        Assert.Null(model);
    }

    [Fact]
    public void GetReranker_AlwaysReturnsNull()
    {
        using var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("ollama/llama3.2");

        Assert.Null(sut.GetReranker(modelId));
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class FakeHttpHandler(string expectedPath, HttpStatusCode status, string responseBody)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.RequestUri?.AbsolutePath == expectedPath
                ? new HttpResponseMessage(status)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }

    private sealed class NotCalledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("HTTP should not be called in this test.");
    }
}
