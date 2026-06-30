# Sprint 12 s3 — Ollama Provider (`Ferret.Providers.Ollama`) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `Ferret.Providers.Ollama` — the OllamaSharp-backed provider package that exposes Ollama models as `IModelProvider`, `IChatModel`, and `IEmbeddingModel`. No OllamaSharp types appear outside this package. Tests use a fake `HttpMessageHandler` — no running Ollama instance required.

**Architecture:** `OllamaModelProvider` owns the `OllamaApiClient` instance and vends `OllamaChatModel` / `OllamaEmbeddingModel` per model name. All OllamaSharp types (`ChatRequest`, `EmbedRequest`, `Message`, `ChatRole`, `Model`, `EmbedResponse`, `ChatResponseStream`) are confined to `src/Ferret.Providers.Ollama/`. `Ferret.Core.Ai` contracts (`IChatModel`, `IEmbeddingModel`, `IModelProvider`) cross the boundary — SDK types do not.

**Tech Stack:** .NET 9, C# 13, xUnit, OllamaSharp 5.x, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`

**OllamaSharp API reference (verified against 5.4.x docs):**
- `OllamaApiClient(Uri baseAddress)` — constructor
- `client.ListLocalModelsAsync(ct)` → `Task<IEnumerable<Model>>`; `Model.Name`, `Model.Details.ParameterSize`
- `client.ChatAsync(ChatRequest, ct)` → `IAsyncEnumerable<ChatResponseStream?>`; each chunk has `.Message.Content (string?)` and `.Done (bool)`
- `client.EmbedAsync(EmbedRequest, ct)` → `Task<EmbedResponse>`; `EmbedResponse.Embeddings (List<float[]>)`, `EmbedResponse.PromptEvalCount (int?)`
- `OllamaSharp.Models.Chat.ChatRequest` — properties: `Model (string)`, `Messages (IEnumerable<Message>?)`, `Stream (bool)`, `Options (RequestOptions?)`
- `OllamaSharp.Models.Chat.Message` — properties: `Role (ChatRole?)`, `Content (string?)`
- `OllamaSharp.Models.Chat.ChatRole` — readonly struct with static properties: `ChatRole.System`, `ChatRole.User`, `ChatRole.Assistant`
- `OllamaSharp.Models.EmbedRequest` — properties: `Model (string)`, `Input (List<string>)`

## Global Constraints

- s1 (AI Core Contracts) and s2 (Model Platform) must be fully implemented before s3.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`, `test(sprint-12):`, `chore(sprint-12):`, `docs(sprint-12):`.
- No OllamaSharp types (`OllamaSharp.*`) outside `src/Ferret.Providers.Ollama/`. This is enforced by architecture tests.
- Tests must NOT require a running Ollama instance — use fake `HttpMessageHandler` to intercept HTTP.
- Version Gate Rule: no actual LLM calls during `dotnet test`.
- `CancellationToken` must propagate to all async calls.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.

---

## File Structure Map

```
src/Ferret.Providers.Ollama/
  Ferret.Providers.Ollama.csproj      [NEW — Task 1]
  OllamaModelProvider.cs              [NEW — Task 2]
  OllamaChatModel.cs                  [NEW — Task 3]
  OllamaEmbeddingModel.cs             [NEW — Task 3]
  OllamaProviderModule.cs             [NEW — Task 4]

tests/Ferret.Providers.Ollama.Tests/
  Ferret.Providers.Ollama.Tests.csproj [NEW — Task 1]
  OllamaModelProviderTests.cs          [NEW — Task 2]
  OllamaChatModelTests.cs              [NEW — Task 3]
  OllamaEmbeddingModelTests.cs         [NEW — Task 3]

src/Ferret.sln                         [MODIFY — Task 4]
Directory.Packages.props               [MODIFY — Task 1]
```

---

### Task 1: Project Setup

Creates the csproj pair, adds `OllamaSharp` to Central Package Management, and wires `InternalsVisibleTo` so test internals can access package-internal types.

**Files:**
- Create: `src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj`
- Create: `tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj`
- Modify: `Directory.Packages.props`

**Interfaces:**
- Consumes: `Ferret.Core` (for `Ferret.Core.Ai` contracts), `Ferret.Configuration.AI` (for `OllamaOptions`), `OllamaSharp` NuGet
- Produces: compilable project pair; `InternalsVisibleTo` declaration

- [ ] **Step 1: Add OllamaSharp to Central Package Management**

Open `Directory.Packages.props` and add a new `<ItemGroup>` after the MCP SDK entry:

```xml
<ItemGroup Label="Ollama SDK">
  <PackageVersion Include="OllamaSharp" Version="5.4.25" />
</ItemGroup>
```

> **Note:** 5.4.25 is the latest stable as of 2026-06-29. Check NuGet at `https://www.nuget.org/packages/OllamaSharp` for a newer patch if needed — stay on the 5.x major.

- [ ] **Step 2: Create `src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Providers.Ollama</AssemblyName>
    <RootNamespace>Ferret.Providers.Ollama</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OllamaSharp" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Ferret.Providers.Ollama.Tests" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Configuration.AI\Ferret.Configuration.AI.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Providers.Ollama.Tests</AssemblyName>
    <RootNamespace>Ferret.Providers.Ollama.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Providers.Ollama\Ferret.Providers.Ollama.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Restore and verify projects compile empty**

```
dotnet restore src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj
dotnet build src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj -v n
dotnet restore tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj
dotnet build tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj -v n
```

Expected: both build successfully (no source files yet — that is expected).

- [ ] **Step 5: Commit**

```
git add Directory.Packages.props src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj
git commit -m "chore(sprint-12): Ferret.Providers.Ollama project setup — csproj pair + OllamaSharp NuGet ref"
```

---

### Task 2: OllamaModelProvider + ListModelsAsync

`OllamaModelProvider` is the entry point: it constructs an `OllamaApiClient`, calls `ListLocalModelsAsync`, and exposes the catalogue as `IReadOnlyList<ModelDescriptor>`. It also vends `OllamaChatModel` and `OllamaEmbeddingModel` for a given `ModelId`. Tests intercept HTTP so no Ollama process is needed.

**Files:**
- Create: `src/Ferret.Providers.Ollama/OllamaModelProvider.cs`
- Create: `tests/Ferret.Providers.Ollama.Tests/OllamaModelProviderTests.cs`

**Interfaces:**
- Consumes (from s1): `IModelProvider`, `ModelDescriptor`, `ProviderDescriptor`, `ModelId`, `ProviderId`, `ModelCapabilities`
- Consumes (from s2): `OllamaOptions` (has `BaseUrl` string, `TimeoutSeconds` int, `Enabled` bool)
- Produces: `OllamaModelProvider : IModelProvider`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Providers.Ollama.Tests/OllamaModelProviderTests.cs
using System.Net;
using System.Text;
using System.Text.Json;
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.Ollama;
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
        var sut = MakeProvider(new NotCalledHandler());
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
        var handler = new FakeHttpHandler("/api/tags", HttpStatusCode.OK, json);
        var sut = MakeProvider(handler);

        var models = await sut.ListModelsAsync(CancellationToken.None);

        Assert.Equal(2, models.Count);
        Assert.Contains(models, m => m.Id.Value == "ollama/llama3.2");
        Assert.Contains(models, m => m.Id.Value == "ollama/nomic-embed-text");
        Assert.All(models, m => Assert.True(m.Capabilities.HasFlag(ModelCapabilities.Chat)));
        Assert.All(models, m => Assert.True(m.Capabilities.HasFlag(ModelCapabilities.Embedding)));
    }

    [Fact]
    public async Task ListModelsAsync_HttpFailure_LogsWarningAndReturnsEmpty()
    {
        var handler = new FakeHttpHandler("/api/tags", HttpStatusCode.ServiceUnavailable, string.Empty);
        var sut = MakeProvider(handler);

        var models = await sut.ListModelsAsync(CancellationToken.None);

        Assert.Empty(models);
    }

    [Fact]
    public void GetChatModel_CorrectPrefix_ReturnsOllamaChatModel()
    {
        var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("ollama/llama3.2");

        var model = sut.GetChatModel(modelId);

        Assert.NotNull(model);
    }

    [Fact]
    public void GetChatModel_WrongPrefix_ReturnsNull()
    {
        var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("openai/gpt-4o");

        var model = sut.GetChatModel(modelId);

        Assert.Null(model);
    }

    [Fact]
    public void GetEmbeddingModel_CorrectPrefix_ReturnsOllamaEmbeddingModel()
    {
        var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("ollama/nomic-embed-text");

        var model = sut.GetEmbeddingModel(modelId);

        Assert.NotNull(model);
    }

    [Fact]
    public void GetEmbeddingModel_WrongPrefix_ReturnsNull()
    {
        var sut = MakeProvider(new NotCalledHandler());
        var modelId = ModelId.Create("openai/text-embedding-3-small");

        var model = sut.GetEmbeddingModel(modelId);

        Assert.Null(model);
    }

    [Fact]
    public void GetReranker_AlwaysReturnsNull()
    {
        var sut = MakeProvider(new NotCalledHandler());
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
                      Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
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
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Providers.Ollama.Tests/ --filter "FullyQualifiedName~OllamaModelProviderTests" -v n
```

Expected: compile errors — `OllamaModelProvider` not found.

- [ ] **Step 3: Write OllamaModelProvider**

Implement `OllamaModelProvider : IModelProvider` (public sealed) satisfying the tests:
- Constructor: `(OllamaOptions options, ILogger<OllamaModelProvider> logger, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)` — null-guard options and logger; fall back to `NullLoggerFactory.Instance` when loggerFactory is null; create `HttpClient` with `BaseAddress` and `Timeout` from options when httpClient is null
- `Descriptor`: `ProviderId = "ollama"`, `DisplayName = "Ollama"`, `Capabilities = Chat | Embedding`, `Version = "1.0"`
- `ListModelsAsync`: create `OllamaApiClient(_httpClient)`; call `ListLocalModelsAsync`; map each model to `ModelDescriptor` with `Id = ModelId.Create($"ollama/{m.Name}")`; on any exception except `OperationCanceledException`, log warning and return empty list
- `GetChatModel(ModelId)`: return `OllamaChatModel` if `id.ProviderPrefix == "ollama"`, else null; use `ILoggerFactory` to create child logger
- `GetEmbeddingModel(ModelId)`: return `OllamaEmbeddingModel` if prefix matches, else null
- `GetReranker`: always null

> **Note:** The `MakeProvider` test helper should pass `NullLoggerFactory.Instance` as the loggerFactory argument alongside the fake `HttpClient`.

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Providers.Ollama.Tests/ --filter "FullyQualifiedName~OllamaModelProviderTests" -v n
```

Expected: 8 tests PASS.

- [ ] **Step 5: Build check**

```
dotnet build src/Ferret.Providers.Ollama/ -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.Providers.Ollama/OllamaModelProvider.cs tests/Ferret.Providers.Ollama.Tests/OllamaModelProviderTests.cs
git commit -m "feat(sprint-12): OllamaModelProvider — IModelProvider impl with ListModelsAsync and fake HTTP tests"
```

---

### Task 3: OllamaChatModel + OllamaEmbeddingModel

Implements the two model classes. `OllamaChatModel` maps `ChatRequest` → OllamaSharp `ChatRequest`, streams chunks, and assembles a final `ChatResponse`. `OllamaEmbeddingModel` maps `EmbeddingRequest` → OllamaSharp `EmbedRequest` and maps the response to `EmbeddingResult`. Both classes take an `HttpClient` so tests can inject the fake handler.

**Files:**
- Create: `src/Ferret.Providers.Ollama/OllamaChatModel.cs`
- Create: `src/Ferret.Providers.Ollama/OllamaEmbeddingModel.cs`
- Create: `tests/Ferret.Providers.Ollama.Tests/OllamaChatModelTests.cs`
- Create: `tests/Ferret.Providers.Ollama.Tests/OllamaEmbeddingModelTests.cs`

**Interfaces:**
- Consumes (from s1): `IChatModel`, `IEmbeddingModel`, `ChatRequest`, `ChatResponse`, `ChatMessage`, `ChatRole (Ferret)`, `ChatResponseChunk`, `FinishReason`, `TokenUsage`, `EmbeddingRequest`, `EmbeddingResult`, `ModelDescriptor`, `ModelId`, `ModelCapabilities`
- Consumes (OllamaSharp — internal only): `OllamaApiClient`, `OllamaSharp.Models.Chat.ChatRequest`, `OllamaSharp.Models.Chat.Message`, `OllamaSharp.Models.Chat.ChatRole`, `OllamaSharp.Models.EmbedRequest`, `OllamaSharp.Models.EmbedResponse`
- Produces: `OllamaChatModel : IChatModel`, `OllamaEmbeddingModel : IEmbeddingModel`

- [ ] **Step 1: Write the failing chat model tests**

```csharp
// tests/Ferret.Providers.Ollama.Tests/OllamaChatModelTests.cs
using System.Net;
using System.Text;
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.Ollama;
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
        // Ollama /api/chat streams newline-delimited JSON objects
        var ndjson = new StringBuilder();
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"Hello"},"done":false}""");
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":" world"},"done":false}""");
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":""},"done":true,"done_reason":"stop"}""");

        var handler = new FakeStreamingHandler("/api/chat", ndjson.ToString());
        var sut = MakeModel(handler);

        var request = new ChatRequest
        {
            Messages = [ChatMessage.User("hi")]
        };

        var response = await sut.ChatAsync(request, CancellationToken.None);

        Assert.Contains("Hello", response.Content);
        Assert.Contains("world", response.Content);
        Assert.Equal(FinishReason.Stop, response.FinishReason);
    }

    [Fact]
    public async Task ChatStreamAsync_FakeStreamingResponse_YieldsChunks()
    {
        var ndjson = new StringBuilder();
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"chunk1"},"done":false}""");
        ndjson.AppendLine("""{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"chunk2"},"done":true,"done_reason":"stop"}""");

        var handler = new FakeStreamingHandler("/api/chat", ndjson.ToString());
        var sut = MakeModel(handler);

        var request = new ChatRequest { Messages = [ChatMessage.User("hi")] };
        var chunks = new List<ChatResponseChunk>();

        await foreach (var chunk in sut.ChatStreamAsync(request, CancellationToken.None))
            chunks.Add(chunk);

        Assert.Equal(2, chunks.Count);
        Assert.Equal("chunk1", chunks[0].Delta);
        Assert.Equal("chunk2", chunks[1].Delta);
        Assert.Equal(FinishReason.Stop, chunks[1].FinishReason);
    }

    [Fact]
    public async Task ChatAsync_EmptyMessages_StillCallsApi()
    {
        // Empty message list should not throw — let Ollama decide the error
        var ndjson = """{"model":"llama3.2","created_at":"2026-01-01T00:00:00Z","message":{"role":"assistant","content":"ok"},"done":true,"done_reason":"stop"}""";
        var handler = new FakeStreamingHandler("/api/chat", ndjson);
        var sut = MakeModel(handler);

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
                      Content = new StringContent(ndjsonBody, Encoding.UTF8, "application/x-ndjson")
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
```

- [ ] **Step 2: Write the failing embedding model tests**

```csharp
// tests/Ferret.Providers.Ollama.Tests/OllamaEmbeddingModelTests.cs
using System.Net;
using System.Text;
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.Ollama;
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
        Assert.False(sut.Descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
    }

    [Fact]
    public async Task EmbedAsync_FakeResponse_ReturnsEmbeddingResult()
    {
        var json = """
            {
              "model": "nomic-embed-text",
              "embeddings": [[0.1, 0.2, 0.3, 0.4]],
              "total_duration": 1000000,
              "load_duration": 500000,
              "prompt_eval_count": 5
            }
            """;
        var handler = new FakeHandler("/api/embed", json);
        var sut = MakeModel(handler);

        var request = new EmbeddingRequest { Text = "hello world" };
        var result = await sut.EmbedAsync(request, CancellationToken.None);

        Assert.Equal(4, result.Vector.Length);
        Assert.Equal(5, result.TokenCount);
        Assert.Equal("ollama/nomic-embed-text", result.ModelId.Value);
        Assert.Equal(0.1f, result.Vector.Span[0], precision: 5);
    }

    [Fact]
    public async Task EmbedBatchAsync_MultipleRequests_ReturnsResultsInOrder()
    {
        var json = """
            {
              "model": "nomic-embed-text",
              "embeddings": [[0.5, 0.6]],
              "prompt_eval_count": 3
            }
            """;
        var handler = new FakeHandler("/api/embed", json);
        var sut = MakeModel(handler);

        var requests = new List<EmbeddingRequest>
        {
            new() { Text = "first" },
            new() { Text = "second" }
        };

        var results = await sut.EmbedBatchAsync(requests, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(2, r.Vector.Length));
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class FakeHandler(string expectedPath, string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.RequestUri?.AbsolutePath == expectedPath
                ? new HttpResponseMessage(HttpStatusCode.OK)
                  {
                      Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
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
```

- [ ] **Step 3: Run tests to verify they fail**

```
dotnet test tests/Ferret.Providers.Ollama.Tests/ --filter "FullyQualifiedName~OllamaChatModelTests|FullyQualifiedName~OllamaEmbeddingModelTests" -v n
```

Expected: compile errors — `OllamaChatModel`, `OllamaEmbeddingModel` not found.

- [ ] **Step 4: Write OllamaChatModel**

Implement `OllamaChatModel : IChatModel` (internal sealed) satisfying the tests:
- Constructor: `(string modelName, OllamaOptions options, ILogger<OllamaChatModel> logger, HttpClient httpClient)` — null/empty-guard all; build `Descriptor` from a backing field computed at construction (`Id = "ollama/{modelName}"`, `Capabilities = Chat`)
- `ChatAsync`: consume `ChatStreamAsync`, assemble content with `StringBuilder`, capture last `FinishReason`, return `ChatResponse` with `TokenUsage.Zero`
- `ChatStreamAsync`: create `OllamaApiClient(_httpClient)`; map `request.Messages` to OllamaSharp `Message` list (map Ferret `ChatRole` → OllamaSharp `ChatRole`); call `client.ChatAsync(ollamaRequest, ct)`; for each non-null chunk yield `ChatResponseChunk { Delta = chunk.Message?.Content ?? "", FinishReason = chunk.Done ? FinishReason.Stop : null }`

> **OllamaSharp API note:** Verify the actual `ChatResponseStream` property names from the installed package — in particular how `done_reason` is exposed. Default to `FinishReason.Stop` when `Done == true` if no typed property is available.

- [ ] **Step 5: Write OllamaEmbeddingModel**

Implement `OllamaEmbeddingModel : IEmbeddingModel` (internal sealed) satisfying the tests:
- Constructor: `(string modelName, OllamaOptions options, ILogger<OllamaEmbeddingModel> logger, HttpClient httpClient)` — null/empty-guard all; build `Descriptor` from backing field (`Id = "ollama/{modelName}"`, `Capabilities = Embedding` only — not Chat)
- `EmbedAsync`: create `OllamaApiClient(_httpClient)`; build `EmbedRequest { Model = _modelName, Input = [request.Text] }`; call `client.EmbedAsync`; take `Embeddings?.FirstOrDefault() ?? []` as the vector; map `PromptEvalCount` → `TokenCount`

> **OllamaSharp API note:** Verify `EmbedResponse` property names from the installed package before implementation. The API reference at the top of this plan lists `EmbedResponse.Embeddings (List<float[]>)` and `EmbedResponse.PromptEvalCount (int?)` — confirm these match what's in the installed 5.x package.

- `EmbedBatchAsync`: sequential fallback — `foreach` over requests, call `EmbedAsync`, collect results

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Providers.Ollama.Tests/ -v n
```

Expected: all tests PASS (target: ~15 tests across the three test files).

- [ ] **Step 7: Build check**

```
dotnet build src/Ferret.Providers.Ollama/ -v n
```

- [ ] **Step 8: Commit**

```
git add src/Ferret.Providers.Ollama/OllamaChatModel.cs src/Ferret.Providers.Ollama/OllamaEmbeddingModel.cs tests/Ferret.Providers.Ollama.Tests/OllamaChatModelTests.cs tests/Ferret.Providers.Ollama.Tests/OllamaEmbeddingModelTests.cs
git commit -m "feat(sprint-12): OllamaChatModel + OllamaEmbeddingModel — IChatModel and IEmbeddingModel with fake HTTP tests"
```

---

### Task 4: OllamaProviderModule + Ferret.sln Integration

`OllamaProviderModule` is the DI wiring point: it reads `OllamaOptions`, conditionally registers `OllamaModelProvider` as an `IModelProvider`, and configures the `HttpClient`. The project is then added to `Ferret.sln` and the test project to the solution's test folder. A full solution test run verifies no regressions.

**Files:**
- Create: `src/Ferret.Providers.Ollama/OllamaProviderModule.cs`
- Modify: `src/Ferret.sln`

**Interfaces:**
- Consumes: `IServiceCollection`, `IConfiguration`, `OllamaOptions`, `OllamaModelProvider`, `IModelProvider`, `ILogger<OllamaModelProvider>`, `ILoggerFactory`
- Produces: `OllamaProviderModule` (public static class with `ConfigureServices` method)

> No unit tests for `OllamaProviderModule` — DI composition is exercised by the full solution build and the architecture tests in `Ferret.Architecture.Tests` (added in s6 CLI wireup).

- [ ] **Step 1: Write OllamaProviderModule**

Implement `OllamaProviderModule` (public static class) with extension method `AddOllamaProvider(this IServiceCollection services, IConfiguration configuration)`:
- Null-guard both parameters
- Bind `OllamaOptions` from `configuration.GetSection("Ferret:Ai:Providers:Ollama")`, defaulting to `new OllamaOptions()` if absent
- Return early if `!options.Enabled`
- Register a named `HttpClient` configured with `BaseAddress` and `Timeout` from options
- Register `IModelProvider` as singleton via factory: resolve `IHttpClientFactory`, `ILogger<OllamaModelProvider>`, `ILoggerFactory` from `sp`; construct and return `OllamaModelProvider`

> Check `OpenAiProviderModule` (s4) for the parallel pattern — both modules follow the same structure.

- [ ] **Step 2: Add projects to Ferret.sln**

```
dotnet sln src/Ferret.sln add src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj
```

- [ ] **Step 3: Full solution build**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds with no errors or warnings.

- [ ] **Step 4: Full solution test run**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. The Ollama provider tests use fake HTTP — no Ollama process is needed.

- [ ] **Step 5: Provider-specific test run (fast verification)**

```
dotnet test tests/Ferret.Providers.Ollama.Tests/ -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.Providers.Ollama/OllamaProviderModule.cs src/Ferret.sln
git commit -m "feat(sprint-12): OllamaProviderModule — DI wiring + sln integration; full solution tests pass"
```

---

## Completion Checklist

Before marking s3 complete, verify each item:

- [ ] `dotnet build src/Ferret.Providers.Ollama/ -v n` — succeeds
- [ ] `dotnet test tests/Ferret.Providers.Ollama.Tests/ -v n` — all tests PASS
- [ ] `dotnet test src/Ferret.sln -v n` — all tests PASS (no regressions)
- [ ] No `OllamaSharp.*` namespace in any file outside `src/Ferret.Providers.Ollama/`
- [ ] No running Ollama process required by any test
- [ ] All four files committed with `feat(sprint-12):` prefix
- [ ] `OllamaModelProvider` registered as `IModelProvider` in `OllamaProviderModule`
- [ ] `GetReranker` returns null (Ollama does not support reranking)
- [ ] `EmbedBatchAsync` implemented sequentially (parallelisation is a future optimisation)
- [ ] `CancellationToken` propagated through all async paths

## s3 → s6 Handoff

`OllamaProviderModule.AddOllamaProvider(services, configuration)` is the single call that s6 (CLI Wireup) makes to activate Ollama. s6 will also add the architecture test that asserts no `OllamaSharp.*` types appear outside `Ferret.Providers.Ollama`.
