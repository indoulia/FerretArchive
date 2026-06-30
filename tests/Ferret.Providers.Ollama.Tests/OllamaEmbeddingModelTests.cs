#pragma warning disable CA2000 // Test handlers have no real resources; lifetime is bounded to test process

using System.Net;
using System.Text;

using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.Ollama.Tests;

public sealed class OllamaEmbeddingModelTests
{
    private static OllamaEmbeddingModel MakeModel(HttpMessageHandler handler, string modelName = "nomic-embed-text")
    {
        var options = new OllamaOptions { BaseUrl = "http://localhost:11434", TimeoutSeconds = 30, Enabled = true };
        return new OllamaEmbeddingModel(
            modelName,
            options,
            NullLogger<OllamaEmbeddingModel>.Instance,
            new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) });
    }

    [Fact]
    public void Descriptor_HasCorrectModelIdAndCapabilities()
    {
        var sut = MakeModel(new NotCalledHandler());
        Assert.Equal("ollama/nomic-embed-text", sut.Descriptor.Id.Value);
        Assert.True(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public async Task EmbedAsync_FakeResponse_ReturnsVectorWithCorrectLength()
    {
        var json = """
            {
              "model": "nomic-embed-text",
              "embeddings": [[0.1, 0.2, 0.3, 0.4]],
              "prompt_eval_count": 5
            }
            """;
        var sut = MakeModel(new FakeHandler("/api/embed", HttpStatusCode.OK, json));
        var request = new EmbeddingRequest { Text = "hello world" };

        var result = await sut.EmbedAsync(request, CancellationToken.None);

        Assert.Equal(4, result.Vector.Length);
        Assert.Equal(0.1f, result.Vector.Span[0], precision: 5);
        Assert.Equal(5, result.TokenCount);
        Assert.Equal("ollama/nomic-embed-text", result.ModelId.Value);
    }

    [Fact]
    public async Task EmbedAsync_EmptyEmbeddings_ReturnsEmptyVector()
    {
        var json = """{"model":"nomic-embed-text","embeddings":[],"prompt_eval_count":0}""";
        var sut = MakeModel(new FakeHandler("/api/embed", HttpStatusCode.OK, json));
        var request = new EmbeddingRequest { Text = "test" };

        var result = await sut.EmbedAsync(request, CancellationToken.None);

        Assert.Equal(0, result.Vector.Length);
    }

    [Fact]
    public async Task EmbedBatchAsync_MultipleRequests_ReturnsAllResults()
    {
        var json = """{"model":"nomic-embed-text","embeddings":[[1.0,2.0]],"prompt_eval_count":3}""";
        var sut = MakeModel(new FakeHandler("/api/embed", HttpStatusCode.OK, json));
        var requests = new List<EmbeddingRequest>
        {
            new() { Text = "first" },
            new() { Text = "second" },
        };

        var results = await sut.EmbedBatchAsync(requests, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(2, r.Vector.Length));
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class FakeHandler(string expectedPath, HttpStatusCode status, string responseBody)
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
