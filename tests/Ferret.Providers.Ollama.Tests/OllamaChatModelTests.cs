#pragma warning disable CA2000 // Test handlers have no real resources; lifetime is bounded to test process

using System.Net;
using System.Text;

using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.Ollama.Tests;

public sealed class OllamaChatModelTests
{
    private static OllamaChatModel MakeModel(HttpMessageHandler handler, string modelName = "llama3.2")
    {
        var options = new OllamaOptions { BaseUrl = "http://localhost:11434", TimeoutSeconds = 30, Enabled = true };
        return new OllamaChatModel(
            modelName,
            options,
            NullLogger<OllamaChatModel>.Instance,
            new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) });
    }

    [Fact]
    public void Descriptor_HasCorrectModelIdAndCapabilities()
    {
        var sut = MakeModel(new NotCalledHandler());
        Assert.Equal("ollama/llama3.2", sut.Descriptor.Id.Value);
        Assert.True(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
    }

    [Fact]
    public async Task ChatAsync_FakeStreamingResponse_ReturnsAssembledChatResponse()
    {
        var ndjson = new StringBuilder();
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"Hello"},"done":false}""");
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":" world"},"done":false}""");
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":""},"done":true}""");

        var sut = MakeModel(new FakeStreamingHandler("/api/chat", ndjson.ToString()));
        var request = new ChatRequest { Messages = [ChatMessage.User("hi")] };

        var response = await sut.ChatAsync(request, CancellationToken.None);

        Assert.Contains("Hello", response.Content, StringComparison.Ordinal);
        Assert.Contains("world", response.Content, StringComparison.Ordinal);
        Assert.Equal(FinishReason.Stop, response.FinishReason);
    }

    [Fact]
    public async Task ChatStreamAsync_FakeStreamingResponse_YieldsChunks()
    {
        var ndjson = new StringBuilder();
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"chunk1"},"done":false}""");
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"chunk2"},"done":true}""");

        var sut = MakeModel(new FakeStreamingHandler("/api/chat", ndjson.ToString()));
        var request = new ChatRequest { Messages = [ChatMessage.User("hi")] };
        var chunks = new List<ChatResponseChunk>();

        await foreach (var chunk in sut.ChatStreamAsync(request, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(2, chunks.Count);
        Assert.Equal("chunk1", chunks[0].Delta);
        Assert.Equal("chunk2", chunks[1].Delta);
        Assert.Equal(FinishReason.Stop, chunks[1].FinishReason);
    }

    [Fact]
    public async Task ChatAsync_EmptyMessages_StillCallsApi()
    {
        var ndjson = """{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"ok"},"done":true}""";
        var sut = MakeModel(new FakeStreamingHandler("/api/chat", ndjson));
        var request = new ChatRequest { Messages = [] };

        var response = await sut.ChatAsync(request, CancellationToken.None);

        Assert.NotNull(response);
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class FakeStreamingHandler(string expectedPath, string ndjsonBody)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.RequestUri?.AbsolutePath == expectedPath
                ? new HttpResponseMessage(HttpStatusCode.OK)
                  {
                      Content = new StringContent(ndjsonBody, Encoding.UTF8, "application/x-ndjson"),
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
