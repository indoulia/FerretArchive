# Sprint 12 Sub-plan 4 — OpenAI Provider (`Ferret.Providers.OpenAi`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver `Ferret.Providers.OpenAi` — the OpenAI-backed `IModelProvider` implementation. Wraps the official `OpenAI` NuGet SDK behind Ferret's `IChatModel` / `IEmbeddingModel` contracts so no `OpenAI.*` types leak outside this package.

**Architecture:** `OpenAiModelProvider` is a thin catalog. It returns a fixed list of well-known OpenAI model descriptors without calling the `/models` API at startup (avoids network at startup, follows Version Gate Rule — no LLM calls during Sprint 12). `OpenAiChatModel` and `OpenAiEmbeddingModel` map Ferret request/response types to OpenAI SDK calls. The SDK isolation boundary is enforced: no `OpenAI.*` namespace appears outside `Ferret.Providers.OpenAi`.

**Tech Stack:** .NET 9, C# 13, xUnit, `OpenAI` NuGet SDK (latest stable — verify version before pinning in csproj)

## Global Constraints

- Sprint 12 s1 (`Ferret.Core.Ai` contracts) and s2 (`Ferret.Models` model platform) must be complete before this sub-plan.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`, `test(sprint-12):`, `chore(sprint-12):`.
- No `OpenAI.*` types outside `src/Ferret.Providers.OpenAi/`. Architecture tests enforce this.
- Version Gate Rule: zero LLM API calls during `dotnet test`. All HTTP integration tests use `[Fact(Skip = "Requires OpenAI API key")]`.
- Build command: `dotnet build src/Ferret.Providers.OpenAi/ -v n`
- Test command: `dotnet test tests/Ferret.Providers.OpenAi.Tests/ -v n`
- Full solution: `dotnet test src/Ferret.sln -v n`

---

## File Structure Map

```
src/Ferret.Providers.OpenAi/
  Ferret.Providers.OpenAi.csproj    [NEW — Task 1] refs OpenAI NuGet + Ferret.Core + Ferret.Configuration.AI
  OpenAiModelProvider.cs            [NEW — Task 2] IModelProvider, fixed model catalog
  OpenAiChatModel.cs                [NEW — Task 3] IChatModel — chat + streaming
  OpenAiEmbeddingModel.cs           [NEW — Task 3] IEmbeddingModel — embed + batch
  OpenAiProviderModule.cs           [NEW — Task 4] DI registration, conditional on Enabled

tests/Ferret.Providers.OpenAi.Tests/
  Ferret.Providers.OpenAi.Tests.csproj  [NEW — Task 1]
  OpenAiModelProviderTests.cs           [NEW — Task 2] pure unit tests, no network
  OpenAiChatModelTests.cs               [NEW — Task 3] unit tests + skipped integration tests
  OpenAiEmbeddingModelTests.cs          [NEW — Task 3] unit tests + skipped integration tests
  OpenAiProviderModuleTests.cs          [NEW — Task 4] DI wiring tests

src/Ferret.sln                          [MODIFY — Task 4] add two new projects
```

---

### Task 1: Project Setup

Creates `Ferret.Providers.OpenAi` and its test project. References the official `OpenAI` NuGet package (SDK isolation — only this project may reference it). Adds `InternalsVisibleTo` for tests.

**Files:**
- Create: `src/Ferret.Providers.OpenAi/Ferret.Providers.OpenAi.csproj`
- Create: `tests/Ferret.Providers.OpenAi.Tests/Ferret.Providers.OpenAi.Tests.csproj`

**Interfaces:**
- Consumes: `Ferret.Core` (Ferret.Core.Ai contracts from s1), `Ferret.Configuration.AI` (OpenAiOptions from s2), `OpenAI` NuGet package
- Produces: compilable empty project shell

> **CRITICAL — Before writing csproj:** Run `dotnet package search OpenAI --source https://api.nuget.org/v3/index.json` to confirm the latest stable version of the `OpenAI` package. As of Sprint 12 planning, expect `2.x`. Pin to the exact latest stable (e.g., `2.1.0`). Do NOT use a wildcard version.

- [ ] **Step 1: Verify the OpenAI NuGet package version**

```powershell
dotnet package search OpenAI --take 5
```

Note the latest stable version. Use it in Step 2.

- [ ] **Step 2: Create src/Ferret.Providers.OpenAi/Ferret.Providers.OpenAi.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Providers.OpenAi</AssemblyName>
    <RootNamespace>Ferret.Providers.OpenAi</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Configuration.AI\Ferret.Configuration.AI.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Version managed by Directory.Packages.props — add OpenAI entry there if absent -->
    <PackageReference Include="OpenAI" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Ferret.Providers.OpenAi.Tests" />
  </ItemGroup>

</Project>
```

> **Note:** Add `<PackageVersion Include="OpenAI" Version="..." />` to `Directory.Packages.props` if absent. Run `dotnet package search OpenAI --take 5` to confirm the latest stable version before pinning.

- [ ] **Step 3: Create tests/Ferret.Providers.OpenAi.Tests/Ferret.Providers.OpenAi.Tests.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Providers.OpenAi.Tests</AssemblyName>
    <RootNamespace>Ferret.Providers.OpenAi.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Providers.OpenAi\Ferret.Providers.OpenAi.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Restore and verify compile**

```
dotnet restore src/Ferret.Providers.OpenAi/Ferret.Providers.OpenAi.csproj
dotnet build src/Ferret.Providers.OpenAi/ -v n
```

Expected: build succeeds (empty project, no source files yet).

- [ ] **Step 5: Commit**

```
git add src/Ferret.Providers.OpenAi/ tests/Ferret.Providers.OpenAi.Tests/
git commit -m "chore(sprint-12): Ferret.Providers.OpenAi project setup — OpenAI NuGet ref + test project"
```

---

### Task 2: OpenAiModelProvider + Fixed Model Catalog

`OpenAiModelProvider` is the entry point for the OpenAI sub-system. It holds a fixed catalog of well-known OpenAI models (no network call at startup). It vends `OpenAiChatModel` and `OpenAiEmbeddingModel` instances on demand via `GetChatModel` and `GetEmbeddingModel`. `GetReranker` always returns null (OpenAI has no dedicated reranker endpoint).

**Files:**
- Create: `src/Ferret.Providers.OpenAi/OpenAiModelProvider.cs`
- Create: `tests/Ferret.Providers.OpenAi.Tests/OpenAiModelProviderTests.cs`

**Interfaces:**
- Consumes (from s1): `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `ModelDescriptor`, `ProviderDescriptor`, `ModelId`, `ProviderId`, `ModelCapabilities`
- Consumes: `OpenAiOptions` (from Ferret.Configuration.AI, s2)
- Produces: `OpenAiModelProvider : IModelProvider`

The fixed catalog:

| ModelId | Capabilities | ContextWindow |
|---|---|---|
| `openai/gpt-4o` | Chat | 128000 |
| `openai/gpt-4o-mini` | Chat | 128000 |
| `openai/text-embedding-3-small` | Embedding | 8192 |
| `openai/text-embedding-3-large` | Embedding | 8192 |

`ListModelsAsync` returns these four descriptors immediately — no HTTP call.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Providers.OpenAi.Tests/OpenAiModelProviderTests.cs
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiModelProviderTests
{
    private static OpenAiModelProvider MakeProvider() =>
        new(new OpenAiOptions { Enabled = true, ApiKey = "sk-test" },
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
        // Verifies the catalog is static — pass an invalid API key and no exception is thrown
        var sut = new OpenAiModelProvider(
            new OpenAiOptions { Enabled = true, ApiKey = "invalid-key-no-network" },
            NullLogger<OpenAiModelProvider>.Instance);
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.Equal(4, models.Count); // succeeds without network
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
        // text-embedding-3-small is not a chat model
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
        var reranker = sut.GetReranker(ModelId.Create("openai/gpt-4o"));
        Assert.Null(reranker);
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
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Providers.OpenAi.Tests/ --filter "FullyQualifiedName~OpenAiModelProviderTests" -v n
```

Expected: compile errors — `OpenAiModelProvider` not found.

- [ ] **Step 3: Write OpenAiModelProvider**

Implement `OpenAiModelProvider : IModelProvider` (public sealed) satisfying the tests:
- Constructor: `(OpenAiOptions options, ILogger<OpenAiModelProvider> logger)` — null-guard both. If child model loggers are needed, accept `ILoggerFactory` as a third parameter or use typed loggers injected separately — match whichever pattern compiles cleanly.
- `Descriptor`: `ProviderId = "openai"`, `DisplayName = "OpenAI"`, `Capabilities = Chat | Embedding`, `Version = "1.0.0"`
- `ListModelsAsync`: return the static catalog immediately (no network call). The catalog contains exactly the 4 models shown in the table (2 chat, 2 embedding) with `ContextWindow` values as specified.
- `GetChatModel`: return `OpenAiChatModel` for chat model IDs prefixed `openai/` that match the catalog; return null for embedding model IDs or unknown prefixes.
- `GetEmbeddingModel`: return `OpenAiEmbeddingModel` for embedding model IDs prefixed `openai/` that match the catalog; return null otherwise.
- `GetReranker`: always null.
- Internal sets of chat/embedding model local names drive routing — match the catalog entries exactly.

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Providers.OpenAi.Tests/ --filter "FullyQualifiedName~OpenAiModelProviderTests" -v n
```

Expected: 15 tests PASS.

- [ ] **Step 5: Full build check**

```
dotnet build src/Ferret.Providers.OpenAi/ -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.Providers.OpenAi/OpenAiModelProvider.cs tests/Ferret.Providers.OpenAi.Tests/OpenAiModelProviderTests.cs
git commit -m "feat(sprint-12): OpenAiModelProvider — fixed model catalog, no network at startup"
```

---

### Task 3: OpenAiChatModel + OpenAiEmbeddingModel

Two sealed model implementations. Both receive an `OpenAiOptions` and use the `OpenAI` SDK internally. No SDK types appear in method signatures — all SDK usage is confined to method bodies. Unit tests verify routing logic and descriptor behavior without calling the API. Integration tests (which do call the API) are marked `Skip`.

**Files:**
- Create: `src/Ferret.Providers.OpenAi/OpenAiChatModel.cs`
- Create: `src/Ferret.Providers.OpenAi/OpenAiEmbeddingModel.cs`
- Create: `tests/Ferret.Providers.OpenAi.Tests/OpenAiChatModelTests.cs`
- Create: `tests/Ferret.Providers.OpenAi.Tests/OpenAiEmbeddingModelTests.cs`

**Interfaces:**
- Consumes (from s1): `IChatModel`, `IEmbeddingModel`, `ChatRequest`, `ChatResponse`, `ChatMessage`, `ChatRole`, `ChatResponseChunk`, `FinishReason`, `TokenUsage`, `EmbeddingRequest`, `EmbeddingResult`, `ModelDescriptor`, `ModelId`, `ProviderId`, `ModelCapabilities`
- Consumes (OpenAI SDK — VERIFY exact types against the installed package version):
  - `OpenAI.OpenAIClient` — top-level client; constructor: `new OpenAIClient(ApiKeyCredential, OpenAIClientOptions?)`
  - `OpenAI.ApiKeyCredential` — wraps the API key string
  - `OpenAI.OpenAIClientOptions` — allows setting `Endpoint` for base URL override; `NetworkTimeout`
  - `OpenAI.Chat.ChatClient` — obtained via `client.GetChatClient(modelName)`
  - `OpenAI.Chat.ChatCompletion` — result of `ChatClient.CompleteChatAsync(...)`
  - `OpenAI.Chat.ChatCompletionOptions` — request options (Temperature, MaxOutputTokenCount)
  - `OpenAI.Chat.ChatMessage` (SDK type) — base class; use `SystemChatMessage`, `UserChatMessage`, `AssistantChatMessage`
  - `OpenAI.Chat.StreamingChatCompletionUpdate` — chunk type from `CompleteChatStreamingAsync`
  - `OpenAI.Embeddings.EmbeddingClient` — obtained via `client.GetEmbeddingClient(modelName)`
  - `OpenAI.Embeddings.Embedding` — result of `EmbeddingClient.GenerateEmbeddingAsync(...)`
  - `OpenAI.Embeddings.EmbeddingGenerationOptions` — request options
- Produces: `OpenAiChatModel : IChatModel`, `OpenAiEmbeddingModel : IEmbeddingModel`

> **CRITICAL — Before writing implementation:** Verify the exact SDK API by checking the OpenAI .NET SDK README on GitHub (https://github.com/openai/openai-dotnet) and the installed package's IntelliSense. Key verification points:
> - Confirm `new OpenAIClient(new ApiKeyCredential(key), options)` is the correct constructor
> - Confirm `client.GetChatClient(modelName)` and `client.GetEmbeddingClient(modelName)` exist
> - Confirm `CompleteChatAsync(IEnumerable<ChatMessage>, ChatCompletionOptions?, CancellationToken)` signature
> - Confirm `CompleteChatStreamingAsync(...)` returns `AsyncCollectionResult<StreamingChatCompletionUpdate>`
> - Confirm `GenerateEmbeddingAsync(string, EmbeddingGenerationOptions?, CancellationToken)` signature
> - Confirm `StreamingChatCompletionUpdate.ContentUpdate` and `.FinishReason` property names
> - Confirm `ChatCompletion.Content[0].Text`, `.Usage.InputTokenCount`, `.Usage.OutputTokenCount`, `.FinishReason`
> - Confirm `Embedding.ToFloats()` returns the embedding vector

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Providers.OpenAi.Tests/OpenAiChatModelTests.cs
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiChatModelTests
{
    private static OpenAiChatModel MakeModel(string modelName = "gpt-4o") =>
        new(modelName,
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

    // Integration tests — require a real API key; skipped in CI
    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ChatAsync_SimpleRequest_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiChatModel("gpt-4o-mini",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiChatModel>.Instance);

        var request = new ChatRequest
        {
            Messages = [ChatMessage.User("Say hello in one word.")],
            MaxTokens = 10,
            Temperature = 0.0
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
        var model = new OpenAiChatModel("gpt-4o-mini",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiChatModel>.Instance);

        var request = new ChatRequest
        {
            Messages = [ChatMessage.User("Count to 3.")],
            MaxTokens = 20,
            Temperature = 0.0
        };

        var chunks = new List<ChatResponseChunk>();
        await foreach (var chunk in model.ChatStreamAsync(request, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.NotEmpty(chunks);
    }
}
```

```csharp
// tests/Ferret.Providers.OpenAi.Tests/OpenAiEmbeddingModelTests.cs
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiEmbeddingModelTests
{
    private static OpenAiEmbeddingModel MakeModel(string modelName = "text-embedding-3-small") =>
        new(modelName,
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

    // Integration tests — require a real API key; skipped in CI
    [Fact(Skip = "Requires OpenAI API key")]
    public async Task EmbedAsync_ReturnsNonEmptyVector()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiEmbeddingModel("text-embedding-3-small",
            new OpenAiOptions { Enabled = true, ApiKey = apiKey },
            NullLogger<OpenAiEmbeddingModel>.Instance);

        var request = new EmbeddingRequest { Text = "hello world" };
        var result = await model.EmbedAsync(request, CancellationToken.None);

        Assert.Equal(1536, result.Vector.Length); // text-embedding-3-small produces 1536-dimensional vectors
        Assert.True(result.TokenCount > 0);
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task EmbedBatchAsync_ReturnsOneResultPerRequest()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-invalid";
        var model = new OpenAiEmbeddingModel("text-embedding-3-small",
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
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Providers.OpenAi.Tests/ --filter "FullyQualifiedName~OpenAiChatModelTests|FullyQualifiedName~OpenAiEmbeddingModelTests" -v n
```

Expected: compile errors — `OpenAiChatModel` and `OpenAiEmbeddingModel` not found.

- [ ] **Step 3: Write OpenAiChatModel**

> **Before writing:** Check the installed OpenAI NuGet package's actual API surface — use IntelliSense or the package README. Do NOT assume property names, method signatures, or type names from training data; the SDK evolves. Adapt the implementation to what is actually installed while preserving Ferret contracts.

Implement `OpenAiChatModel : IChatModel` (public sealed) satisfying the tests:
- Constructor: `(string modelName, OpenAiOptions options, ILogger<OpenAiChatModel> logger)` — null-guard all parameters
- `Descriptor`: built at construction — `ModelId.Create($"openai/{modelName}")`, `ProviderId.Create("openai")`, `ModelCapabilities.Chat`
- `ChatAsync`: create the SDK chat client from `OpenAiOptions` (API key, base URL, timeout); map Ferret `ChatRequest.Messages` to SDK message types (System/User/Assistant); map temperature and max tokens; call completion; map result content, finish reason, and token counts to `ChatResponse`
- `ChatStreamAsync`: stream SDK completion updates; yield `ChatResponseChunk` per update (delta text + nullable finish reason)
- Map Ferret `ChatRole` → SDK message types; map SDK finish reason → Ferret `FinishReason`
- All SDK types confined to method bodies — no `OpenAI.*` namespace in public surface

- [ ] **Step 4: Write OpenAiEmbeddingModel**

> **Before writing:** Verify the embedding API surface from the installed package — method name for single embedding generation, how the vector is exposed (as array, `ReadOnlyMemory<float>`, or a helper method), and how token counts are reported. Adapt to what is installed.

Implement `OpenAiEmbeddingModel : IEmbeddingModel` (public sealed) satisfying the tests:
- Constructor: `(string modelName, OpenAiOptions options, ILogger<OpenAiEmbeddingModel> logger)` — null-guard all parameters
- `Descriptor`: built at construction — `ModelId.Create($"openai/{modelName}")`, `ProviderId.Create("openai")`, `ModelCapabilities.Embedding`
- `EmbedAsync`: create the SDK embedding client from `OpenAiOptions`; call the embedding API with `request.Text`; map the returned vector and token count to `EmbeddingResult`
- `EmbedBatchAsync`: sequential fallback — call `EmbedAsync` per request, respect cancellation
- All SDK types confined to method bodies — no `OpenAI.*` namespace in public surface

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Providers.OpenAi.Tests/ --filter "FullyQualifiedName~OpenAiChatModelTests|FullyQualifiedName~OpenAiEmbeddingModelTests" -v n
```

Expected: 10 tests PASS (5 unit tests per class); 4 integration tests are skipped.

- [ ] **Step 6: Full project build**

```
dotnet build src/Ferret.Providers.OpenAi/ -v n
```

- [ ] **Step 7: Commit**

```
git add src/Ferret.Providers.OpenAi/OpenAiChatModel.cs src/Ferret.Providers.OpenAi/OpenAiEmbeddingModel.cs tests/Ferret.Providers.OpenAi.Tests/OpenAiChatModelTests.cs tests/Ferret.Providers.OpenAi.Tests/OpenAiEmbeddingModelTests.cs
git commit -m "feat(sprint-12): OpenAiChatModel + OpenAiEmbeddingModel — SDK-backed chat and embedding"
```

---

### Task 4: OpenAiProviderModule + Solution Wiring

Registers `OpenAiModelProvider` as `IModelProvider` in the DI container, conditional on `OpenAiOptions.Enabled`. Binds `OpenAiOptions` from configuration. Adds both new projects to `Ferret.sln`.

**Files:**
- Create: `src/Ferret.Providers.OpenAi/OpenAiProviderModule.cs`
- Create: `tests/Ferret.Providers.OpenAi.Tests/OpenAiProviderModuleTests.cs`
- Modify: `src/Ferret.sln` — add `Ferret.Providers.OpenAi` and `Ferret.Providers.OpenAi.Tests`

**Interfaces:**
- Consumes: `IModelProvider`, `OpenAiOptions`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Configuration`
- Produces: `OpenAiProviderModule` (static extension method on `IServiceCollection`)

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Providers.OpenAi.Tests/OpenAiProviderModuleTests.cs
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Ferret.Providers.OpenAi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiProviderModuleTests
{
    [Fact]
    public void AddOpenAiProvider_WhenEnabled_RegistersIModelProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:Enabled"] = "true",
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "sk-test-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenAiProvider(config);

        var provider = services.BuildServiceProvider();
        var providers = provider.GetServices<IModelProvider>().ToList();

        Assert.Contains(providers, p => p is OpenAiModelProvider);
    }

    [Fact]
    public void AddOpenAiProvider_WhenDisabled_DoesNotRegisterIModelProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:Enabled"] = "false",
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "sk-test-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenAiProvider(config);

        var provider = services.BuildServiceProvider();
        var providers = provider.GetServices<IModelProvider>().ToList();

        Assert.DoesNotContain(providers, p => p is OpenAiModelProvider);
    }

    [Fact]
    public void AddOpenAiProvider_WhenEnabled_CanResolveOpenAiModelProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:Enabled"] = "true",
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "sk-test-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenAiProvider(config);

        var provider = services.BuildServiceProvider();
        var openAiProvider = provider.GetServices<IModelProvider>()
            .OfType<OpenAiModelProvider>()
            .SingleOrDefault();

        Assert.NotNull(openAiProvider);
        Assert.Equal("openai", openAiProvider.Descriptor.Id.Value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Providers.OpenAi.Tests/ --filter "FullyQualifiedName~OpenAiProviderModuleTests" -v n
```

Expected: compile errors — `AddOpenAiProvider` not found.

- [ ] **Step 3: Write OpenAiProviderModule**

```csharp
// src/Ferret.Providers.OpenAi/OpenAiProviderModule.cs
using Ferret.Configuration.Ai;
using Ferret.Core.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ferret.Providers.OpenAi;

/// <summary>DI registration module for the OpenAI provider.</summary>
public static class OpenAiProviderModule
{
    private const string ConfigSection = "Ferret:Ai:Providers:OpenAi";

    /// <summary>
    /// Registers <see cref="OpenAiModelProvider"/> as <see cref="IModelProvider"/> if
    /// <see cref="OpenAiOptions.Enabled"/> is true.
    /// </summary>
    public static IServiceCollection AddOpenAiProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(ConfigSection).Get<OpenAiOptions>()
                      ?? new OpenAiOptions();

        if (!options.Enabled)
            return services;

        services.AddSingleton<IModelProvider>(sp =>
            new OpenAiModelProvider(options, sp.GetRequiredService<ILogger<OpenAiModelProvider>>()));

        return services;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Providers.OpenAi.Tests/ --filter "FullyQualifiedName~OpenAiProviderModuleTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Add projects to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Providers.OpenAi/Ferret.Providers.OpenAi.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Providers.OpenAi.Tests/Ferret.Providers.OpenAi.Tests.csproj
```

- [ ] **Step 6: Full solution build and test**

```
dotnet build src/Ferret.sln -v n
dotnet test tests/Ferret.Providers.OpenAi.Tests/ -v n
```

Expected: all non-skipped tests PASS.

- [ ] **Step 7: Run full solution test suite**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. The 4 integration tests are skipped, not failed.

- [ ] **Step 8: Commit**

```
git add src/Ferret.Providers.OpenAi/OpenAiProviderModule.cs tests/Ferret.Providers.OpenAi.Tests/OpenAiProviderModuleTests.cs src/Ferret.sln
git commit -m "feat(sprint-12): OpenAiProviderModule + solution wiring — conditional DI registration"
```

---

## Sub-Plan Completion Checklist

- [ ] `dotnet test tests/Ferret.Providers.OpenAi.Tests/ -v n` — all non-skipped tests PASS
- [ ] `dotnet build src/Ferret.sln -v n` — zero errors, zero warnings
- [ ] No `OpenAI.*` types appear in public method signatures outside `Ferret.Providers.OpenAi`
- [ ] `ListModelsAsync` returns exactly 4 descriptors with no network call
- [ ] `GetChatModel("openai/gpt-4o")` returns non-null; `GetChatModel("ollama/llama3.2")` returns null
- [ ] `GetChatModel("openai/text-embedding-3-small")` returns null (embedding model, not chat)
- [ ] `GetEmbeddingModel("openai/gpt-4o")` returns null (chat model, not embedding)
- [ ] `GetReranker(...)` always returns null
- [ ] `OpenAiProviderModule` registers `IModelProvider` only when `Enabled = true`
- [ ] Both projects added to `Ferret.sln`
- [ ] SDK verification notes reviewed and implementation adjusted to match installed package version
- [ ] `dotnet test src/Ferret.sln -v n` — full solution PASS

## Notes for s6 (CLI Wireup)

Sub-plan s6 (`ModelsCliModule`) will wire `AddOpenAiProvider` into `Program.cs`. It must call:

```csharp
services.AddOpenAiProvider(configuration);
```

alongside `AddOllamaProvider`. The `ModelRegistry` in `Ferret.Models` discovers all `IModelProvider` registrations via `IEnumerable<IModelProvider>` injection, so no additional changes to `ModelPlatformModule` are needed once both provider modules are registered.
