#pragma warning disable CA2000 // Test handler has no real resources; lifetime is bounded to test process

using System.Net;
using System.Text;

using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Providers.Compliance;

using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Providers.Ollama.Tests;

/// <summary>Runs the shared <see cref="ProviderComplianceTests"/> contract suite against <see cref="OllamaModelProvider"/>.</summary>
public sealed class OllamaProviderComplianceTests : ProviderComplianceTests
{
    protected override IModelProvider CreateProvider()
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
        var options = new OllamaOptions { BaseUrl = "http://localhost:11434", TimeoutSeconds = 30, Enabled = true };
        return new OllamaModelProvider(
            options,
            NullLogger<OllamaModelProvider>.Instance,
            new HttpClient(new FakeTagsHandler(json)));
    }

    private sealed class FakeTagsHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.RequestUri?.AbsolutePath == "/api/tags"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }
}
