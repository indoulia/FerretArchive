# Sprint 12 Design Specification: AI Platform Foundation

**Project:** Ferret (ContextOS)
**Date:** 2026-06-29
**Status:** Authoritative
**Sprint tag:** `v0.12.0-sprint12`

---

## Executive Summary

Sprint 12 introduces the Ferret AI Platform — the architectural substrate on which all future AI-powered features are built. It transforms Ferret from a Knowledge Platform into an Enterprise Intelligence Platform by adding a first-class AI capability layer: provider abstraction, model registry and routing, prompt template platform, and memory abstractions.

The sprint delivers no end-user AI features. There is no `ferret ask`, no chat, no semantic search, no summarisation. Those features arrive in Sprints 13–16. What Sprint 12 delivers is the platform they all depend on — exactly the discipline applied in Sprints 8 (Connector Platform), 9 (Content Ingestion), 10 (Search Platform), and 11 (Host Platform).

The user story is: a developer runs `ferret models list` and sees Ollama and OpenAI models available. They run `ferret models info ollama/llama3.2` and see capabilities, context window, and provider details. The AI Platform is live. Everything from Sprint 13 onward builds on it.

---

## Architectural Outcomes

1. **Established the AI Provider Abstraction** — `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `IVisionModel` as Ferret-owned contracts in `Ferret.Core.Ai`
2. **Introduced the Model Platform** — immutable `ModelRegistry`, configuration-driven `ModelRouter`, DI-composed via `ModelPlatformModule`
3. **Delivered the Prompt Platform** — `PromptTemplate`, `PromptRegistry`, `PromptRenderer`, `PromptVariables` as a full template system
4. **Introduced Memory Abstractions** — `IConversationMemory`, `IWorkspaceMemory`, `ITaskMemory` with null implementations; real implementations in Sprint 15
5. **Established SDK Isolation Boundary** — vendor SDK types (OllamaSharp, OpenAI) confined to provider packages; `Ferret.Core.Ai` has zero external dependencies
6. **Scaffolded `Ferret.AI`** — empty orchestration package as the home for Sprint 13 context assembly
7. **Reserved `ADR-0019`, `ADR-0020`** — AI Platform Architecture and Prompt Platform Architecture

---

## Section 1: Sprint Identity

### 1.1 Sprint Name and Tag

**Name:** Sprint 12 – AI Platform Foundation
**Tag:** `v0.12.0-sprint12`

### 1.2 Theme

> Introduce AI as a first-class platform capability, not as a bolt-on feature.

### 1.3 Sprint Goal

> Deliver the AI Platform Foundation: provider abstraction, model registry and routing, prompt platform, memory abstractions, and initial CLI commands — so that every future AI feature has a stable, vendor-independent substrate to build on.

### 1.4 User Story

A developer configures Ferret with a local Ollama instance. They run `ferret models list` and see all available Ollama models. They run `ferret models info ollama/llama3.2` and see the model's capabilities, context window, and provider status. They run `ferret prompt list` and see the prompt templates registered for their workspace. The AI Platform is operational — ready for Sprint 13 to assemble context and Sprint 14 to deliver `ferret ask`.

### 1.5 What a New User Can Do After Sprint 12

Configure AI providers in `.ferret/config.json`, run `ferret models list` to enumerate available models from Ollama and OpenAI, and inspect individual model capabilities with `ferret models info`. The AI Platform substrate is live; it is ready to be used by Sprint 13's context assembly feature.

### 1.6 Non-Goals

Sprint 12 explicitly does not deliver:

- `ferret ask` or any chat command
- Semantic or vector search
- Vector store or embedding storage
- Context assembly or prompt composition (Sprint 13)
- Conversation history storage (Sprint 15)
- Knowledge graph (Sprint 16)
- REST or HTTP AI endpoints
- Multi-provider routing beyond simple default lookup
- Streaming chat CLI output
- Any MCP tool backed by AI

**Version Gate Rule:** Sprint 12 must not introduce any feature that requires a model to be called at runtime. The platform is wired up and CLI-queryable; no prompt is sent to any model during Sprint 12.

---

## Section 2: Architecture

### 2.1 Position in the Platform

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Presentation / Integration Hosts                   │
│   Ferret.Cli        Ferret.Mcp       Future REST    Future Web UI  │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
┌────────────────────────────────▼────────────────────────────────────┐
│                     AI Platform (Sprint 12 NEW)                     │
│   Ferret.AI (scaffold)    Ferret.Models    Ferret.Prompts           │
│   Ferret.Configuration.AI                                           │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
┌────────────────────────────────▼────────────────────────────────────┐
│                          Platform Services                          │
│   Ferret.Search     Ferret.Indexing     Ferret.ConnectorPlatform    │
│   Ferret.Workspace  Ferret.ParserPlatform                           │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
┌────────────────────────────────▼────────────────────────────────────┐
│                   Ferret.Core (zero-dependency contracts)           │
│   Ferret.Core.Ai (NEW)   Ferret.Core.Search   Ferret.Core.Connectors│
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 New Packages

| Package | Namespace | Purpose |
|---|---|---|
| `Ferret.Core` (modified) | `Ferret.Core.Ai` | AI contracts — interfaces, value types, request/response models |
| `Ferret.Models` | `Ferret.Models` | Model registry, routing, DI composition |
| `Ferret.Prompts` | `Ferret.Prompts` | Prompt templates, registry, renderer |
| `Ferret.AI` | `Ferret.AI` | AI orchestration scaffold (empty Sprint 12; Sprint 13 adds context assembly) |
| `Ferret.Configuration.AI` | `Ferret.Configuration.Ai` | AI configuration binding (`AiOptions`) |
| `Ferret.Providers.Ollama` | `Ferret.Providers.Ollama` | Ollama provider — OllamaSharp-backed |
| `Ferret.Providers.OpenAi` | `Ferret.Providers.OpenAi` | OpenAI provider — OpenAI NuGet-backed |

Test packages: `Ferret.Models.Tests`, `Ferret.Prompts.Tests`, `Ferret.Providers.Ollama.Tests`, `Ferret.Providers.OpenAi.Tests`.

### 2.3 SDK Isolation Boundary

The SDK isolation rule established for MCP (ADR-0017) applies identically:

- `Ferret.Core.Ai` has **zero** external package references. It is the contract layer.
- `Ferret.Models`, `Ferret.Prompts`, `Ferret.AI`, `Ferret.Configuration.AI` reference only `Ferret.Core` and Microsoft.Extensions packages.
- `Ferret.Providers.Ollama` is the **only** package that references `OllamaSharp` (or equivalent Ollama SDK).
- `Ferret.Providers.OpenAi` is the **only** package that references the `OpenAI` NuGet package.
- No type from `OllamaSharp.*` or `OpenAI.*` namespaces appears outside its respective provider package.

Architecture tests in `Ferret.Architecture.Tests` enforce this boundary.

---

## Section 3: Core AI Contracts (`Ferret.Core.Ai`)

### 3.1 Value Types

| Type | Format | Example |
|---|---|---|
| `ModelId` | Fully-qualified string: `{provider}/{model}` | `"ollama/llama3.2"`, `"openai/gpt-4o"` |
| `ProviderId` | Simple string identifier | `"ollama"`, `"openai"`, `"anthropic"` |
| `ChatRole` | Enum | `System`, `User`, `Assistant` |
| `ModelCapabilities` | Flags enum | `Chat`, `Embedding`, `Reranking`, `Vision` |
| `FinishReason` | Enum | `Stop`, `Length`, `ToolCalls`, `ContentFilter`, `Error` |

### 3.2 Message and Request Types

| Type | Key Properties |
|---|---|
| `ChatMessage` | `Role (ChatRole)`, `Content (string)` — factory methods: `ChatMessage.System(text)`, `.User(text)`, `.Assistant(text)` |
| `ChatRequest` | `Messages (IReadOnlyList<ChatMessage>)`, `ModelId? (string)`, `Temperature (double, default 0.7)`, `MaxTokens (int?)` |
| `ChatResponse` | `Content (string)`, `FinishReason`, `Usage (TokenUsage)` |
| `ChatResponseChunk` | `Delta (string)`, `FinishReason?` — emitted by streaming API |
| `TokenUsage` | `InputTokens (int)`, `OutputTokens (int)`, `TotalTokens (int)` |
| `EmbeddingRequest` | `Text (string)`, `ModelId? (string)` |
| `EmbeddingResult` | `Vector (ReadOnlyMemory<float>)`, `ModelId`, `TokenCount (int)` |
| `RerankRequest` | `Query (string)`, `Documents (IReadOnlyList<string>)`, `ModelId? (string)` |
| `RerankItem` | `Document (string)`, `Score (double)`, `Index (int)` |
| `RerankResult` | `Items (IReadOnlyList<RerankItem>)` ordered by descending score |

### 3.3 Descriptor Types

| Type | Key Properties |
|---|---|
| `ModelDescriptor` | `Id (ModelId)`, `ProviderId`, `DisplayName (string)`, `Capabilities (ModelCapabilities)`, `ContextWindow (long?)`, `Description (string?)` |
| `ProviderDescriptor` | `Id (ProviderId)`, `DisplayName (string)`, `Capabilities (ModelCapabilities)`, `Version (string)` |

### 3.4 Interfaces

| Interface | Capability |
|---|---|
| `IModelProvider` | Provider registration contract — lists models, vends `IChatModel`/`IEmbeddingModel`/`IReranker` |
| `IChatModel` | Chat and streaming chat |
| `IEmbeddingModel` | Single and batch text embedding |
| `IReranker` | Query-document reranking |
| `IVisionModel` | Vision capability (reserved for Sprint 15+; no implementation in Sprint 12) |
| `IConversationMemory` | Add/get/clear conversation turns |
| `IWorkspaceMemory` | Save/get/search workspace-scoped key-value memory |
| `ITaskMemory` | Save/get/search task-scoped key-value memory |

### 3.5 Memory Types

| Type | Key Properties |
|---|---|
| `ConversationTurn` | `Id (Guid)`, `Role (ChatRole)`, `Content (string)`, `CreatedAt (DateTimeOffset)` |
| `MemoryEntry` | `Key (string)`, `Tags (IReadOnlyList<string>)`, `Content (string)`, `CreatedAt (DateTimeOffset)` |

### 3.6 Null Memory Implementations (in `Ferret.Core.Ai`)

Sprint 12 provides null-object implementations alongside the interfaces:

- `NullConversationMemory : IConversationMemory` — `AddAsync` no-ops, `GetRecentAsync` returns empty list, `ClearAsync` no-ops
- `NullWorkspaceMemory : IWorkspaceMemory` — same pattern
- `NullTaskMemory : ITaskMemory` — same pattern

These are registered by default until Sprint 15 provides real implementations.

---

## Section 4: Model Platform (`Ferret.Models`)

### 4.1 AiOptions

`AiOptions` is a POCO bound from `Ferret:Ai` configuration:

```
AiOptions
  DefaultChatModel: string        // "ollama/llama3.2"
  DefaultEmbeddingModel: string   // "ollama/nomic-embed-text"
  DefaultReranker: string?        // null
  Providers: Dictionary<string, ProviderOptions>
    Ollama: OllamaOptions
      Enabled: bool
      BaseUrl: string
      TimeoutSeconds: int
    OpenAi: OpenAiOptions
      Enabled: bool
      ApiKey: string
      BaseUrl: string
      TimeoutSeconds: int
```

### 4.2 ModelRegistry

Built from `IEnumerable<IModelProvider>` at DI construction time. Immutable after startup.

- `GetProviders() → IReadOnlyList<ProviderDescriptor>` — all registered providers
- `GetProvider(ProviderId) → IModelProvider?` — look up by ID
- `GetModel(ModelId) → ModelDescriptor?` — look up cached descriptor
- `GetModels() → IReadOnlyList<ModelDescriptor>` — all cached descriptors

On startup, `ModelRegistry` calls `ListModelsAsync` on each provider. If a provider is unreachable, its models are excluded and a warning is logged; other providers continue normally.

### 4.3 ModelRouter

Reads `AiOptions.DefaultChatModel` and `AiOptions.DefaultEmbeddingModel` at construction. Delegates to `IModelRegistry` to resolve providers.

- `GetDefaultChatModel() → IChatModel` — throws `ModelNotFoundException` if default is not available
- `GetChatModel(ModelId) → IChatModel?` — returns null if model not found
- `GetDefaultEmbeddingModel() → IEmbeddingModel` — same pattern
- `GetEmbeddingModel(ModelId) → IEmbeddingModel?`

---

## Section 5: Prompt Platform (`Ferret.Prompts`)

### 5.1 PromptTemplate

```
PromptTemplate
  Name: string                    // e.g., "workspace-context"
  Version: string                 // semantic version, e.g., "1.0.0"
  Template: string                // raw template with {{variable}} placeholders
  RequiredVariables: IReadOnlyList<string>  // variables that must be present to render
  Description: string?
```

Templates are registered in DI as `IEnumerable<PromptTemplate>` — feature packages register their own templates. The registry collects them at startup.

### 5.2 PromptVariables

Fluent builder:
```
PromptVariables.Empty
  .Set("workspace_name", "my-workspace")
  .Set("file_list", "src/Main.cs\nsrc/App.cs")
```

Exposes `TryGet(name) → string?` and `GetRequired(name) → string` (throws if absent).

### 5.3 IPromptRenderer

- `Render(template, variables) → string` — substitutes `{{variable}}` placeholders; throws `PromptRenderException` if any `RequiredVariable` is absent
- `Validate(template, variables) → IReadOnlyList<string>` — returns list of missing required variables (empty = valid)

---

## Section 6: Provider Implementations

### 6.1 Ollama Provider (`Ferret.Providers.Ollama`)

**NuGet dependency:** `OllamaSharp` (latest stable)

- `OllamaModelProvider : IModelProvider` — discovers models via Ollama's `/api/tags` endpoint; registers `OllamaChatModel` and `OllamaEmbeddingModel` for each model
- `OllamaChatModel : IChatModel` — delegates to `OllamaSharp.OllamaApiClient`; supports both `ChatAsync` and `ChatStreamAsync`
- `OllamaEmbeddingModel : IEmbeddingModel` — delegates to Ollama's `/api/embeddings` endpoint

Constructor receives `OllamaOptions` (base URL, timeout). No Ollama SDK types appear outside `Ferret.Providers.Ollama`.

### 6.2 OpenAI Provider (`Ferret.Providers.OpenAi`)

**NuGet dependency:** `OpenAI` (official package, latest stable)

- `OpenAiModelProvider : IModelProvider` — registers a fixed catalog of well-known model IDs (`gpt-4o`, `gpt-4o-mini`, `text-embedding-3-small`, `text-embedding-3-large`); calls `/models` endpoint to verify availability
- `OpenAiChatModel : IChatModel` — delegates to `OpenAI.Chat.ChatClient`; supports both `ChatAsync` and `ChatStreamAsync`
- `OpenAiEmbeddingModel : IEmbeddingModel` — delegates to `OpenAI.Embeddings.EmbeddingClient`

Constructor receives `OpenAiOptions` (API key, base URL, timeout). No OpenAI SDK types appear outside `Ferret.Providers.OpenAi`.

---

## Section 7: CLI Commands

### 7.1 `ferret models`

```
ferret models list                  List all available models
ferret models info <model-id>       Show model details and provider status
```

**`ferret models list` output:**

```
Provider   Model                       Capabilities      Context
-------    -----                       ------------      -------
ollama     ollama/llama3.2             Chat              128k
ollama     ollama/nomic-embed-text     Embedding         8k
openai     openai/gpt-4o               Chat              128k
openai     openai/text-embedding-3-small Embedding       8k
```

**`ferret models info ollama/llama3.2` output:**

```
Model:       ollama/llama3.2
Provider:    Ollama (http://localhost:11434)
Capabilities: Chat, Streaming
Context:     128,000 tokens
Status:      Available
```

### 7.2 `ferret prompt`

```
ferret prompt list                  List all registered prompt templates
ferret prompt show <name>           Show template content and required variables
```

**`ferret prompt list` output (Sprint 12 — no templates registered yet):**

```
No prompt templates are registered. Templates are added by feature packages.
```

---

## Section 8: File Structure Map

```
src/Ferret.Core/
  Ai/
    Interfaces/
      IModelProvider.cs           [NEW]
      IChatModel.cs               [NEW]
      IEmbeddingModel.cs          [NEW]
      IReranker.cs                [NEW]
      IVisionModel.cs             [NEW — reserved, empty]
      IConversationMemory.cs      [NEW]
      IWorkspaceMemory.cs         [NEW]
      ITaskMemory.cs              [NEW]
    Models/
      ModelId.cs                  [NEW]
      ProviderId.cs               [NEW]
      ChatRole.cs                 [NEW]
      ModelCapabilities.cs        [NEW]
      FinishReason.cs             [NEW]
      ModelDescriptor.cs          [NEW]
      ProviderDescriptor.cs       [NEW]
      ChatMessage.cs              [NEW]
      ChatRequest.cs              [NEW]
      ChatResponse.cs             [NEW]
      ChatResponseChunk.cs        [NEW]
      TokenUsage.cs               [NEW]
      EmbeddingRequest.cs         [NEW]
      EmbeddingResult.cs          [NEW]
      RerankRequest.cs            [NEW]
      RerankItem.cs               [NEW]
      RerankResult.cs             [NEW]
      ConversationTurn.cs         [NEW]
      MemoryEntry.cs              [NEW]
    NullImplementations/
      NullConversationMemory.cs   [NEW]
      NullWorkspaceMemory.cs      [NEW]
      NullTaskMemory.cs           [NEW]

src/Ferret.Models/
  Ferret.Models.csproj            [NEW]
  IModelRegistry.cs               [NEW]
  IModelRouter.cs                 [NEW]
  ModelRegistry.cs                [NEW]
  ModelRouter.cs                  [NEW]
  ModelPlatformModule.cs          [NEW]

src/Ferret.Prompts/
  Ferret.Prompts.csproj           [NEW]
  PromptTemplate.cs               [NEW]
  PromptVariables.cs              [NEW]
  PromptVersion.cs                [NEW — semantic version for templates]
  IPromptRegistry.cs              [NEW]
  PromptRegistry.cs               [NEW]
  IPromptRenderer.cs              [NEW]
  PromptRenderer.cs               [NEW]
  Exceptions/
    PromptRenderException.cs      [NEW]
  PromptsModule.cs                [NEW]

src/Ferret.AI/
  Ferret.AI.csproj                [NEW]
  AiModule.cs                     [NEW — scaffold only]

src/Ferret.Configuration.AI/
  Ferret.Configuration.AI.csproj  [NEW]
  AiOptions.cs                    [NEW]
  ProviderOptions.cs              [NEW]
  OllamaOptions.cs                [NEW]
  OpenAiOptions.cs                [NEW]
  AiConfigurationModule.cs        [NEW]

src/Ferret.Providers.Ollama/
  Ferret.Providers.Ollama.csproj  [NEW — refs OllamaSharp]
  OllamaModelProvider.cs          [NEW]
  OllamaChatModel.cs              [NEW]
  OllamaEmbeddingModel.cs         [NEW]
  OllamaProviderModule.cs         [NEW]

src/Ferret.Providers.OpenAi/
  Ferret.Providers.OpenAi.csproj  [NEW — refs OpenAI NuGet]
  OpenAiModelProvider.cs          [NEW]
  OpenAiChatModel.cs              [NEW]
  OpenAiEmbeddingModel.cs         [NEW]
  OpenAiProviderModule.cs         [NEW]

src/Ferret.Cli/
  Commands/Models/
    ModelsCliModule.cs            [NEW]
    ModelsListCommandHandler.cs   [NEW]
    ModelsInfoCommandHandler.cs   [NEW]
    ModelsListViewModel.cs        [NEW]
    ModelsInfoViewModel.cs        [NEW]
  Commands/Prompt/
    PromptCliModule.cs            [NEW]
    PromptListCommandHandler.cs   [NEW]
  Program.cs                     [MODIFY — register new CLI modules]
  Ferret.Cli.csproj              [MODIFY — add Ferret.Models, Ferret.Prompts refs]

tests/Ferret.Models.Tests/
  ModelRegistryTests.cs           [NEW]
  ModelRouterTests.cs             [NEW]

tests/Ferret.Prompts.Tests/
  PromptTemplateTests.cs          [NEW]
  PromptVariablesTests.cs         [NEW]
  PromptRegistryTests.cs          [NEW]
  PromptRendererTests.cs          [NEW]

tests/Ferret.Providers.Ollama.Tests/
  OllamaChatModelTests.cs         [NEW — uses fake HTTP handler]
  OllamaEmbeddingModelTests.cs    [NEW]
  OllamaModelProviderTests.cs     [NEW]

tests/Ferret.Providers.OpenAi.Tests/
  OpenAiChatModelTests.cs         [NEW — uses fake HTTP handler]
  OpenAiEmbeddingModelTests.cs    [NEW]

tests/Ferret.Architecture.Tests/
  AiPlatformArchitectureTests.cs  [NEW]

src/Ferret.sln                   [MODIFY — add 7 new projects]
```

---

## Section 9: Global Constraints

- Sprint 11 must be fully implemented before Sprint 12. Assumes `ferret serve` is working.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`, `test(sprint-12):`, `chore(sprint-12):`, `docs(sprint-12):`.
- No vendor SDK types (`OllamaSharp.*`, `OpenAI.*`) outside their respective provider packages.
- No model is called at runtime in Sprint 12. The Version Gate Rule is: zero LLM API calls during `dotnet test`.
- The null memory implementations in `Ferret.Core.Ai` must be used by default until Sprint 15 overrides them.
- Architecture tests must pass: `dotnet test tests/Ferret.Architecture.Tests/ -v n`.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.
- `ModelRegistry` is immutable after startup — no public mutating methods.
- All provider packages must handle `CancellationToken` correctly; operations must not hang if the token is cancelled.

---

## Section 10: ADRs Produced by Sprint 12

| ADR | Title | Key Decisions |
|---|---|---|
| ADR-0019 | AI Platform Architecture | Provider isolation, capability composition, immutable registry, configuration-driven routing |
| ADR-0020 | Prompt Platform Architecture | Template versioning, required-variable enforcement, renderer as stateless service |

---

## Section 11: Sub-Plans

Sprint 12 is implemented as six ordered sub-plans:

| Sub-Plan | File | Prerequisite |
|---|---|---|
| s1 | `2026-06-29-sprint-12-s1-ai-core-contracts.md` | Sprint 11 complete |
| s2 | `2026-06-29-sprint-12-s2-model-platform.md` | s1 complete |
| s3 | `2026-06-29-sprint-12-s3-ollama-provider.md` | s2 complete |
| s4 | `2026-06-29-sprint-12-s4-openai-provider.md` | s2 complete (parallel with s3) |
| s5 | `2026-06-29-sprint-12-s5-prompt-platform.md` | s1 complete (parallel with s2) |
| s6 | `2026-06-29-sprint-12-s6-cli-wireup.md` | s2, s3, s4, s5 complete |

s3 and s4 can be implemented in parallel (different packages, no shared state).
s5 can be implemented in parallel with s2 (depends only on s1 contracts).
