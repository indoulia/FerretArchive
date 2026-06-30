# Sprint 12 Sub-plan 1 — AI Core Contracts

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the `Ferret.Core.Ai` namespace — all AI contracts, value types, request/response models, descriptor types, memory interfaces, and null implementations — as a zero-dependency addition to `Ferret.Core`. This is the contract layer every Sprint 12 sub-plan (s2–s6) depends on.

**Architecture:** All new types live under `Ferret.Core/Ai/`. No new project, no new NuGet references. Three sub-folders: `Models/` (value types, enums, request/response records), `Interfaces/` (provider and memory contracts), `NullImplementations/` (null-object implementations of memory interfaces). Tests go in `tests/Ferret.Core.Tests/Ai/` using the existing `Ferret.Core.Tests` project.

**Tech Stack:** .NET 9, C# 13, xUnit (tests already in `Ferret.Core.Tests`). No external NuGet references — Ferret.Core must remain zero-dependency.

## Global Constraints

- Sprint 11 must be fully implemented before Sprint 12. Assumes `ferret serve` is working.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`, `test(sprint-12):`.
- Namespaces: `Ferret.Core.Ai.Models`, `Ferret.Core.Ai.Interfaces`, `Ferret.Core.Ai.NullImplementations`.
- No external NuGet references in `Ferret.Core`. Zero vendor SDK types.
- All IDs (`ModelId`, `ProviderId`) are `readonly record struct` with private constructors and a `Create(string)` factory — consistent with Ferret's typed-ID pattern.
- Build command: `dotnet build src/Ferret.sln -v n`
- Test command (task-level): `dotnet test tests/Ferret.Core.Tests/ -v n`
- Full test: `dotnet test src/Ferret.sln -v n`

---

## File Structure Map

```
src/Ferret.Core/
  Ai/
    Models/
      ModelId.cs                  [NEW — Task 1]
      ProviderId.cs               [NEW — Task 1]
      ChatRole.cs                 [NEW — Task 1]
      ModelCapabilities.cs        [NEW — Task 1]
      FinishReason.cs             [NEW — Task 1]
      ChatMessage.cs              [NEW — Task 2]
      ChatRequest.cs              [NEW — Task 2]
      ChatResponse.cs             [NEW — Task 2]
      ChatResponseChunk.cs        [NEW — Task 2]
      TokenUsage.cs               [NEW — Task 2]
      EmbeddingRequest.cs         [NEW — Task 2]
      EmbeddingResult.cs          [NEW — Task 2]
      RerankRequest.cs            [NEW — Task 2]
      RerankItem.cs               [NEW — Task 2]
      RerankResult.cs             [NEW — Task 2]
      ModelDescriptor.cs          [NEW — Task 3]
      ProviderDescriptor.cs       [NEW — Task 3]
      ConversationTurn.cs         [NEW — Task 3]
      MemoryEntry.cs              [NEW — Task 3]
    Interfaces/
      IModelProvider.cs           [NEW — Task 4]
      IChatModel.cs               [NEW — Task 4]
      IEmbeddingModel.cs          [NEW — Task 4]
      IReranker.cs                [NEW — Task 4]
      IVisionModel.cs             [NEW — Task 4]
      IConversationMemory.cs      [NEW — Task 5]
      IWorkspaceMemory.cs         [NEW — Task 5]
      ITaskMemory.cs              [NEW — Task 5]
    NullImplementations/
      NullConversationMemory.cs   [NEW — Task 5]
      NullWorkspaceMemory.cs      [NEW — Task 5]
      NullTaskMemory.cs           [NEW — Task 5]

tests/Ferret.Core.Tests/
  Ai/
    ModelIdTests.cs               [NEW — Task 1]
    ProviderIdTests.cs            [NEW — Task 1]
    ModelCapabilitiesTests.cs     [NEW — Task 1]
    ChatMessageTests.cs           [NEW — Task 2]
    EmbeddingResultTests.cs       [NEW — Task 2]
    RerankResultTests.cs          [NEW — Task 2]
    ModelDescriptorTests.cs       [NEW — Task 3]
    ConversationTurnTests.cs      [NEW — Task 3]
    NullMemoryTests.cs            [NEW — Task 5]
```

---

### Task 1: Value Types and Enums — ModelId, ProviderId, ChatRole, ModelCapabilities, FinishReason

Establishes the fundamental value types that every other AI type depends on. `ModelId` and `ProviderId` follow Ferret's typed-ID discipline as `readonly record struct` with private constructors and `Create` factories. Enums are plain C# enums in their own files.

**Files:**
- Create: `src/Ferret.Core/Ai/Models/ModelId.cs`
- Create: `src/Ferret.Core/Ai/Models/ProviderId.cs`
- Create: `src/Ferret.Core/Ai/Models/ChatRole.cs`
- Create: `src/Ferret.Core/Ai/Models/ModelCapabilities.cs`
- Create: `src/Ferret.Core/Ai/Models/FinishReason.cs`
- Create: `tests/Ferret.Core.Tests/Ai/ModelIdTests.cs`
- Create: `tests/Ferret.Core.Tests/Ai/ProviderIdTests.cs`
- Create: `tests/Ferret.Core.Tests/Ai/ModelCapabilitiesTests.cs`

**Interfaces:**
- Consumes: nothing external
- Produces: `ModelId`, `ProviderId`, `ChatRole`, `ModelCapabilities`, `FinishReason`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Ai/ModelIdTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ModelIdTests
{
    [Fact]
    public void Create_ReturnsModelIdWithValue()
    {
        var id = ModelId.Create("ollama/llama3.2");
        Assert.Equal("ollama/llama3.2", id.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("ollama/llama3.2", ModelId.Create("ollama/llama3.2").ToString());
    }

    [Fact]
    public void ProviderPrefix_SplitsOnSlash()
    {
        Assert.Equal("ollama", ModelId.Create("ollama/llama3.2").ProviderPrefix);
    }

    [Fact]
    public void LocalName_SplitsOnSlash()
    {
        Assert.Equal("llama3.2", ModelId.Create("ollama/llama3.2").LocalName);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(ModelId.Create("openai/gpt-4o"), ModelId.Create("openai/gpt-4o"));
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        Assert.NotEqual(ModelId.Create("openai/gpt-4o"), ModelId.Create("openai/gpt-4o-mini"));
    }
}
```

```csharp
// tests/Ferret.Core.Tests/Ai/ProviderIdTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ProviderIdTests
{
    [Fact]
    public void Create_ReturnsProviderIdWithValue()
    {
        var id = ProviderId.Create("ollama");
        Assert.Equal("ollama", id.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("openai", ProviderId.Create("openai").ToString());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(ProviderId.Create("ollama"), ProviderId.Create("ollama"));
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        Assert.NotEqual(ProviderId.Create("ollama"), ProviderId.Create("openai"));
    }
}
```

```csharp
// tests/Ferret.Core.Tests/Ai/ModelCapabilitiesTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ModelCapabilitiesTests
{
    [Fact]
    public void None_IsZero()
    {
        Assert.Equal(0, (int)ModelCapabilities.None);
    }

    [Fact]
    public void Flags_CanBeCombined()
    {
        var caps = ModelCapabilities.Chat | ModelCapabilities.Vision;
        Assert.True(caps.HasFlag(ModelCapabilities.Chat));
        Assert.True(caps.HasFlag(ModelCapabilities.Vision));
        Assert.False(caps.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public void Chat_IsOne()
    {
        Assert.Equal(1, (int)ModelCapabilities.Chat);
    }

    [Fact]
    public void Embedding_IsTwo()
    {
        Assert.Equal(2, (int)ModelCapabilities.Embedding);
    }

    [Fact]
    public void Reranking_IsFour()
    {
        Assert.Equal(4, (int)ModelCapabilities.Reranking);
    }

    [Fact]
    public void Vision_IsEight()
    {
        Assert.Equal(8, (int)ModelCapabilities.Vision);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: compile errors — types not found.

- [ ] **Step 3: Write ModelId**

```csharp
// src/Ferret.Core/Ai/Models/ModelId.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Strongly-typed model identifier in the format "provider/model-name" (e.g. "ollama/llama3.2").</summary>
public readonly record struct ModelId
{
    private ModelId(string value) => Value = value;

    /// <summary>The raw string value.</summary>
    public string Value { get; }

    /// <summary>Creates a <see cref="ModelId"/> from a raw string value.</summary>
    public static ModelId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ModelId(value);
    }

    /// <summary>The provider prefix — the segment before the first '/'.</summary>
    public string ProviderPrefix
    {
        get
        {
            var slash = Value.IndexOf('/');
            return slash < 0 ? Value : Value[..slash];
        }
    }

    /// <summary>The local model name — the segment after the first '/'.</summary>
    public string LocalName
    {
        get
        {
            var slash = Value.IndexOf('/');
            return slash < 0 ? Value : Value[(slash + 1)..];
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

- [ ] **Step 4: Write ProviderId**

```csharp
// src/Ferret.Core/Ai/Models/ProviderId.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Strongly-typed provider identifier (e.g. "ollama", "openai").</summary>
public readonly record struct ProviderId
{
    private ProviderId(string value) => Value = value;

    /// <summary>The raw string value.</summary>
    public string Value { get; }

    /// <summary>Creates a <see cref="ProviderId"/> from a raw string value.</summary>
    public static ProviderId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ProviderId(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

- [ ] **Step 5: Write enums**

```csharp
// src/Ferret.Core/Ai/Models/ChatRole.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Role of a participant in a chat conversation.</summary>
public enum ChatRole
{
    System,
    User,
    Assistant
}
```

```csharp
// src/Ferret.Core/Ai/Models/ModelCapabilities.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Capability flags describing what a model can do.</summary>
[Flags]
public enum ModelCapabilities
{
    None      = 0,
    Chat      = 1,
    Embedding = 2,
    Reranking = 4,
    Vision    = 8
}
```

```csharp
// src/Ferret.Core/Ai/Models/FinishReason.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Reason a model stopped generating tokens.</summary>
public enum FinishReason
{
    Stop,
    Length,
    ToolCalls,
    ContentFilter,
    Error
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: 16 tests PASS.

- [ ] **Step 7: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds, 0 errors.

- [ ] **Step 8: Commit**

```
git add src/Ferret.Core/Ai/Models/ModelId.cs src/Ferret.Core/Ai/Models/ProviderId.cs src/Ferret.Core/Ai/Models/ChatRole.cs src/Ferret.Core/Ai/Models/ModelCapabilities.cs src/Ferret.Core/Ai/Models/FinishReason.cs tests/Ferret.Core.Tests/Ai/ModelIdTests.cs tests/Ferret.Core.Tests/Ai/ProviderIdTests.cs tests/Ferret.Core.Tests/Ai/ModelCapabilitiesTests.cs
git commit -m "feat(sprint-12): Ferret.Core.Ai value types — ModelId, ProviderId, ChatRole, ModelCapabilities, FinishReason"
```

---

### Task 2: Message, Request, and Response Types

All the data transfer types for chat, embedding, and reranking. These are immutable C# `record` types. `ChatMessage` has static factory methods. `RerankResult.Items` is ordered by descending score by convention (enforced by the constructor).

**Files:**
- Create: `src/Ferret.Core/Ai/Models/ChatMessage.cs`
- Create: `src/Ferret.Core/Ai/Models/ChatRequest.cs`
- Create: `src/Ferret.Core/Ai/Models/ChatResponse.cs`
- Create: `src/Ferret.Core/Ai/Models/ChatResponseChunk.cs`
- Create: `src/Ferret.Core/Ai/Models/TokenUsage.cs`
- Create: `src/Ferret.Core/Ai/Models/EmbeddingRequest.cs`
- Create: `src/Ferret.Core/Ai/Models/EmbeddingResult.cs`
- Create: `src/Ferret.Core/Ai/Models/RerankRequest.cs`
- Create: `src/Ferret.Core/Ai/Models/RerankItem.cs`
- Create: `src/Ferret.Core/Ai/Models/RerankResult.cs`
- Create: `tests/Ferret.Core.Tests/Ai/ChatMessageTests.cs`
- Create: `tests/Ferret.Core.Tests/Ai/EmbeddingResultTests.cs`
- Create: `tests/Ferret.Core.Tests/Ai/RerankResultTests.cs`

**Interfaces:**
- Consumes: `ModelId`, `ChatRole`, `FinishReason` from Task 1
- Produces: `ChatMessage`, `ChatRequest`, `ChatResponse`, `ChatResponseChunk`, `TokenUsage`, `EmbeddingRequest`, `EmbeddingResult`, `RerankRequest`, `RerankItem`, `RerankResult`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Ai/ChatMessageTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ChatMessageTests
{
    [Fact]
    public void System_SetsRoleAndContent()
    {
        var msg = ChatMessage.System("you are a helpful assistant");
        Assert.Equal(ChatRole.System, msg.Role);
        Assert.Equal("you are a helpful assistant", msg.Content);
    }

    [Fact]
    public void User_SetsRoleAndContent()
    {
        var msg = ChatMessage.User("hello");
        Assert.Equal(ChatRole.User, msg.Role);
        Assert.Equal("hello", msg.Content);
    }

    [Fact]
    public void Assistant_SetsRoleAndContent()
    {
        var msg = ChatMessage.Assistant("hi there");
        Assert.Equal(ChatRole.Assistant, msg.Role);
        Assert.Equal("hi there", msg.Content);
    }

    [Fact]
    public void ChatRequest_DefaultTemperature_IsPointSeven()
    {
        var request = new ChatRequest { Messages = [ChatMessage.User("hello")] };
        Assert.Equal(0.7, request.Temperature);
    }

    [Fact]
    public void ChatRequest_MaxTokens_DefaultsToNull()
    {
        var request = new ChatRequest { Messages = [ChatMessage.User("hello")] };
        Assert.Null(request.MaxTokens);
    }

    [Fact]
    public void ChatResponseChunk_NullableFinishReason()
    {
        var chunk = new ChatResponseChunk { Delta = "hello" };
        Assert.Null(chunk.FinishReason);
    }
}
```

```csharp
// tests/Ferret.Core.Tests/Ai/EmbeddingResultTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class EmbeddingResultTests
{
    [Fact]
    public void EmbeddingResult_PreservesVector()
    {
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var result = new EmbeddingResult
        {
            Vector = vector,
            ModelId = ModelId.Create("ollama/nomic-embed-text"),
            TokenCount = 5
        };
        Assert.Equal(3, result.Vector.Length);
    }

    [Fact]
    public void EmbeddingRequest_ModelId_DefaultsToNull()
    {
        var req = new EmbeddingRequest { Text = "hello" };
        Assert.Null(req.ModelId);
    }
}
```

```csharp
// tests/Ferret.Core.Tests/Ai/RerankResultTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class RerankResultTests
{
    [Fact]
    public void RerankResult_Items_AreOrderedByDescendingScore()
    {
        var items = new[]
        {
            new RerankItem { Document = "b", Score = 0.5, Index = 1 },
            new RerankItem { Document = "a", Score = 0.9, Index = 0 },
            new RerankItem { Document = "c", Score = 0.2, Index = 2 }
        };
        var result = RerankResult.Create(items);
        Assert.Equal(0.9, result.Items[0].Score);
        Assert.Equal(0.5, result.Items[1].Score);
        Assert.Equal(0.2, result.Items[2].Score);
    }

    [Fact]
    public void RerankItem_PreservesOriginalIndex()
    {
        var item = new RerankItem { Document = "doc", Score = 0.8, Index = 3 };
        Assert.Equal(3, item.Index);
    }

    [Fact]
    public void RerankRequest_ModelId_DefaultsToNull()
    {
        var req = new RerankRequest { Query = "q", Documents = ["a", "b"] };
        Assert.Null(req.ModelId);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: compile errors — new types not found.

- [ ] **Step 3: Write chat types**

```csharp
// src/Ferret.Core/Ai/Models/ChatMessage.cs
namespace Ferret.Core.Ai.Models;

/// <summary>A single message in a chat conversation.</summary>
public sealed record ChatMessage
{
    public required ChatRole Role { get; init; }
    public required string Content { get; init; }

    public static ChatMessage System(string content) =>
        new() { Role = ChatRole.System, Content = content };

    public static ChatMessage User(string content) =>
        new() { Role = ChatRole.User, Content = content };

    public static ChatMessage Assistant(string content) =>
        new() { Role = ChatRole.Assistant, Content = content };
}
```

```csharp
// src/Ferret.Core/Ai/Models/ChatRequest.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Input to a chat model call.</summary>
public sealed record ChatRequest
{
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public string? ModelId { get; init; }
    public double Temperature { get; init; } = 0.7;
    public int? MaxTokens { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/TokenUsage.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Token consumption for a single model call.</summary>
public sealed record TokenUsage
{
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required int TotalTokens { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/ChatResponse.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Complete response from a chat model.</summary>
public sealed record ChatResponse
{
    public required string Content { get; init; }
    public required FinishReason FinishReason { get; init; }
    public required TokenUsage Usage { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/ChatResponseChunk.cs
namespace Ferret.Core.Ai.Models;

/// <summary>A single streamed chunk from a chat model.</summary>
public sealed record ChatResponseChunk
{
    public required string Delta { get; init; }
    public FinishReason? FinishReason { get; init; }
}
```

- [ ] **Step 4: Write embedding types**

```csharp
// src/Ferret.Core/Ai/Models/EmbeddingRequest.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Input to an embedding model call.</summary>
public sealed record EmbeddingRequest
{
    public required string Text { get; init; }
    public string? ModelId { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/EmbeddingResult.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Result from an embedding model call.</summary>
public sealed record EmbeddingResult
{
    public required ReadOnlyMemory<float> Vector { get; init; }
    public required ModelId ModelId { get; init; }
    public required int TokenCount { get; init; }
}
```

- [ ] **Step 5: Write rerank types**

```csharp
// src/Ferret.Core/Ai/Models/RerankRequest.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Input to a reranker call.</summary>
public sealed record RerankRequest
{
    public required string Query { get; init; }
    public required IReadOnlyList<string> Documents { get; init; }
    public string? ModelId { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/RerankItem.cs
namespace Ferret.Core.Ai.Models;

/// <summary>A single scored document from a reranker.</summary>
public sealed record RerankItem
{
    public required string Document { get; init; }
    public required double Score { get; init; }
    /// <summary>Original zero-based position of this document in the input list.</summary>
    public required int Index { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/RerankResult.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Result from a reranker — items ordered by descending score.</summary>
public sealed record RerankResult
{
    public required IReadOnlyList<RerankItem> Items { get; init; }

    /// <summary>Creates a <see cref="RerankResult"/> with items sorted by descending score.</summary>
    public static RerankResult Create(IEnumerable<RerankItem> items) =>
        new() { Items = items.OrderByDescending(i => i.Score).ToList() };
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: all AI tests PASS (16 from Task 1 + 11 new = 27 total).

- [ ] **Step 7: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 8: Commit**

```
git add src/Ferret.Core/Ai/Models/ChatMessage.cs src/Ferret.Core/Ai/Models/ChatRequest.cs src/Ferret.Core/Ai/Models/ChatResponse.cs src/Ferret.Core/Ai/Models/ChatResponseChunk.cs src/Ferret.Core/Ai/Models/TokenUsage.cs src/Ferret.Core/Ai/Models/EmbeddingRequest.cs src/Ferret.Core/Ai/Models/EmbeddingResult.cs src/Ferret.Core/Ai/Models/RerankRequest.cs src/Ferret.Core/Ai/Models/RerankItem.cs src/Ferret.Core/Ai/Models/RerankResult.cs tests/Ferret.Core.Tests/Ai/ChatMessageTests.cs tests/Ferret.Core.Tests/Ai/EmbeddingResultTests.cs tests/Ferret.Core.Tests/Ai/RerankResultTests.cs
git commit -m "feat(sprint-12): Ferret.Core.Ai message/request/response types — ChatMessage, ChatRequest, ChatResponse, EmbeddingResult, RerankResult"
```

---

### Task 3: Descriptor Types and Memory Types

Descriptor types describe providers and models at startup (used by `ModelRegistry` in s2). Memory types are the data records used by the memory interfaces (Task 5). `ConversationTurn.Create` is a static factory that generates a new `Guid` and sets `DateTimeOffset.UtcNow`.

**Files:**
- Create: `src/Ferret.Core/Ai/Models/ModelDescriptor.cs`
- Create: `src/Ferret.Core/Ai/Models/ProviderDescriptor.cs`
- Create: `src/Ferret.Core/Ai/Models/ConversationTurn.cs`
- Create: `src/Ferret.Core/Ai/Models/MemoryEntry.cs`
- Create: `tests/Ferret.Core.Tests/Ai/ModelDescriptorTests.cs`
- Create: `tests/Ferret.Core.Tests/Ai/ConversationTurnTests.cs`

**Interfaces:**
- Consumes: `ModelId`, `ProviderId`, `ModelCapabilities`, `ChatRole` from Task 1
- Produces: `ModelDescriptor`, `ProviderDescriptor`, `ConversationTurn`, `MemoryEntry`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Ai/ModelDescriptorTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ModelDescriptorTests
{
    [Fact]
    public void ModelDescriptor_PreservesId()
    {
        var id = ModelId.Create("ollama/llama3.2");
        var descriptor = new ModelDescriptor
        {
            Id = id,
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat
        };
        Assert.Equal(id, descriptor.Id);
    }

    [Fact]
    public void ModelDescriptor_ContextWindow_DefaultsToNull()
    {
        var descriptor = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat
        };
        Assert.Null(descriptor.ContextWindow);
    }

    [Fact]
    public void ProviderDescriptor_PreservesCapabilities()
    {
        var descriptor = new ProviderDescriptor
        {
            Id = ProviderId.Create("ollama"),
            DisplayName = "Ollama",
            Capabilities = ModelCapabilities.Chat | ModelCapabilities.Embedding,
            Version = "0.1.0"
        };
        Assert.True(descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }
}
```

```csharp
// tests/Ferret.Core.Tests/Ai/ConversationTurnTests.cs
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ConversationTurnTests
{
    [Fact]
    public void Create_GeneratesNewGuid()
    {
        var a = ConversationTurn.Create(ChatRole.User, "hello");
        var b = ConversationTurn.Create(ChatRole.User, "hello");
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Create_SetsRoleAndContent()
    {
        var turn = ConversationTurn.Create(ChatRole.Assistant, "hi");
        Assert.Equal(ChatRole.Assistant, turn.Role);
        Assert.Equal("hi", turn.Content);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var turn = ConversationTurn.Create(ChatRole.User, "test");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.InRange(turn.CreatedAt, before, after);
    }

    [Fact]
    public void MemoryEntry_PreservesTagsAndContent()
    {
        var entry = new MemoryEntry
        {
            Key = "sprint-context",
            Tags = ["sprint", "context"],
            Content = "Sprint 12 is AI platform",
            CreatedAt = DateTimeOffset.UtcNow
        };
        Assert.Equal(2, entry.Tags.Count);
        Assert.Equal("sprint-context", entry.Key);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: compile errors — new types not found.

- [ ] **Step 3: Write descriptor types**

```csharp
// src/Ferret.Core/Ai/Models/ModelDescriptor.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Immutable description of a model's identity and capabilities.</summary>
public sealed record ModelDescriptor
{
    public required ModelId Id { get; init; }
    public required ProviderId ProviderId { get; init; }
    public required string DisplayName { get; init; }
    public required ModelCapabilities Capabilities { get; init; }
    public long? ContextWindow { get; init; }
    public string? Description { get; init; }
}
```

```csharp
// src/Ferret.Core/Ai/Models/ProviderDescriptor.cs
namespace Ferret.Core.Ai.Models;

/// <summary>Immutable description of a provider's identity and aggregate capabilities.</summary>
public sealed record ProviderDescriptor
{
    public required ProviderId Id { get; init; }
    public required string DisplayName { get; init; }
    public required ModelCapabilities Capabilities { get; init; }
    public required string Version { get; init; }
}
```

- [ ] **Step 4: Write memory types**

```csharp
// src/Ferret.Core/Ai/Models/ConversationTurn.cs
namespace Ferret.Core.Ai.Models;

/// <summary>A single turn in a tracked conversation.</summary>
public sealed record ConversationTurn
{
    public required Guid Id { get; init; }
    public required ChatRole Role { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Creates a new turn with a generated ID and the current UTC time.</summary>
    public static ConversationTurn Create(ChatRole role, string content) => new()
    {
        Id = Guid.NewGuid(),
        Role = role,
        Content = content,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
```

```csharp
// src/Ferret.Core/Ai/Models/MemoryEntry.cs
namespace Ferret.Core.Ai.Models;

/// <summary>A tagged key-value memory entry used by workspace and task memory.</summary>
public sealed record MemoryEntry
{
    public required string Key { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: all AI tests PASS (27 from Tasks 1–2 + 7 new = 34 total).

- [ ] **Step 6: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 7: Commit**

```
git add src/Ferret.Core/Ai/Models/ModelDescriptor.cs src/Ferret.Core/Ai/Models/ProviderDescriptor.cs src/Ferret.Core/Ai/Models/ConversationTurn.cs src/Ferret.Core/Ai/Models/MemoryEntry.cs tests/Ferret.Core.Tests/Ai/ModelDescriptorTests.cs tests/Ferret.Core.Tests/Ai/ConversationTurnTests.cs
git commit -m "feat(sprint-12): Ferret.Core.Ai descriptor types and memory types — ModelDescriptor, ProviderDescriptor, ConversationTurn, MemoryEntry"
```

---

### Task 4: Model Provider Interfaces — IModelProvider, IChatModel, IEmbeddingModel, IReranker, IVisionModel

The five provider-side interfaces. These are pure contracts — no implementation in `Ferret.Core`. `IVisionModel` is reserved: it declares only a `Descriptor` property and no methods. No tests are written for pure interfaces; the build check verifies they compile correctly and the architecture tests (in `Ferret.Architecture.Tests`) will verify no SDK types leak in from the other side.

**Files:**
- Create: `src/Ferret.Core/Ai/Interfaces/IModelProvider.cs`
- Create: `src/Ferret.Core/Ai/Interfaces/IChatModel.cs`
- Create: `src/Ferret.Core/Ai/Interfaces/IEmbeddingModel.cs`
- Create: `src/Ferret.Core/Ai/Interfaces/IReranker.cs`
- Create: `src/Ferret.Core/Ai/Interfaces/IVisionModel.cs`

**Interfaces:**
- Consumes: `ModelId`, `ProviderId`, `ModelDescriptor`, `ProviderDescriptor`, `ChatRequest`, `ChatResponse`, `ChatResponseChunk`, `EmbeddingRequest`, `EmbeddingResult`, `RerankRequest`, `RerankResult` from Tasks 1–3
- Produces: `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `IVisionModel`

- [ ] **Step 1: Write IModelProvider**

```csharp
// src/Ferret.Core/Ai/Interfaces/IModelProvider.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Contract for an AI model provider. Vends typed model handles and lists available models.
/// Implementations live in Ferret.Providers.* packages; this interface is Ferret-owned.
/// </summary>
public interface IModelProvider
{
    /// <summary>Provider identity and aggregate capabilities.</summary>
    ProviderDescriptor Descriptor { get; }

    /// <summary>Returns a chat model handle, or null if the model is not available from this provider.</summary>
    IChatModel? GetChatModel(ModelId modelId);

    /// <summary>Returns an embedding model handle, or null if the model is not available from this provider.</summary>
    IEmbeddingModel? GetEmbeddingModel(ModelId modelId);

    /// <summary>Returns a reranker handle, or null if the model is not available from this provider.</summary>
    IReranker? GetReranker(ModelId modelId);

    /// <summary>Lists all models available from this provider. Unreachable providers return an empty list.</summary>
    Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct);
}
```

- [ ] **Step 2: Write IChatModel**

```csharp
// src/Ferret.Core/Ai/Interfaces/IChatModel.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>Chat and streaming chat contract for a single model.</summary>
public interface IChatModel
{
    ModelDescriptor Descriptor { get; }

    /// <summary>Sends a chat request and returns the complete response.</summary>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct);

    /// <summary>Sends a chat request and streams response chunks.</summary>
    IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(ChatRequest request, CancellationToken ct);
}
```

- [ ] **Step 3: Write IEmbeddingModel**

```csharp
// src/Ferret.Core/Ai/Interfaces/IEmbeddingModel.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>Text embedding contract for a single model.</summary>
public interface IEmbeddingModel
{
    ModelDescriptor Descriptor { get; }

    /// <summary>Embeds a single text input.</summary>
    Task<EmbeddingResult> EmbedAsync(EmbeddingRequest request, CancellationToken ct);

    /// <summary>Embeds a batch of text inputs in a single call.</summary>
    Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests, CancellationToken ct);
}
```

- [ ] **Step 4: Write IReranker and IVisionModel**

```csharp
// src/Ferret.Core/Ai/Interfaces/IReranker.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>Query-document reranking contract for a single model.</summary>
public interface IReranker
{
    ModelDescriptor Descriptor { get; }

    /// <summary>Reranks the documents in the request by relevance to the query.</summary>
    Task<RerankResult> RerankAsync(RerankRequest request, CancellationToken ct);
}
```

```csharp
// src/Ferret.Core/Ai/Interfaces/IVisionModel.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Vision model contract — reserved for Sprint 15+. No methods in Sprint 12.
/// Implementations will accept image inputs when introduced.
/// </summary>
public interface IVisionModel
{
    ModelDescriptor Descriptor { get; }
}
```

- [ ] **Step 5: Build to verify interfaces compile**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds, 0 errors.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Core/Ai/Interfaces/IModelProvider.cs src/Ferret.Core/Ai/Interfaces/IChatModel.cs src/Ferret.Core/Ai/Interfaces/IEmbeddingModel.cs src/Ferret.Core/Ai/Interfaces/IReranker.cs src/Ferret.Core/Ai/Interfaces/IVisionModel.cs
git commit -m "feat(sprint-12): Ferret.Core.Ai provider interfaces — IModelProvider, IChatModel, IEmbeddingModel, IReranker, IVisionModel"
```

---

### Task 5: Memory Interfaces and Null Implementations

Three memory interfaces and their null-object implementations. The null implementations are the defaults registered by `ModelPlatformModule` (s2) until Sprint 15 provides real storage. Tests verify the null implementations behave correctly — AddAsync no-ops, GetRecentAsync returns empty, GetAsync returns null, SearchAsync returns empty.

**Files:**
- Create: `src/Ferret.Core/Ai/Interfaces/IConversationMemory.cs`
- Create: `src/Ferret.Core/Ai/Interfaces/IWorkspaceMemory.cs`
- Create: `src/Ferret.Core/Ai/Interfaces/ITaskMemory.cs`
- Create: `src/Ferret.Core/Ai/NullImplementations/NullConversationMemory.cs`
- Create: `src/Ferret.Core/Ai/NullImplementations/NullWorkspaceMemory.cs`
- Create: `src/Ferret.Core/Ai/NullImplementations/NullTaskMemory.cs`
- Create: `tests/Ferret.Core.Tests/Ai/NullMemoryTests.cs`

**Interfaces:**
- Consumes: `ConversationTurn`, `MemoryEntry`, `ChatRole` from Tasks 1 and 3
- Produces: `IConversationMemory`, `IWorkspaceMemory`, `ITaskMemory`, `NullConversationMemory`, `NullWorkspaceMemory`, `NullTaskMemory`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Ai/NullMemoryTests.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Core.Ai.NullImplementations;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class NullMemoryTests
{
    // --- IConversationMemory ---

    [Fact]
    public async Task NullConversationMemory_AddAsync_DoesNotThrow()
    {
        IConversationMemory sut = new NullConversationMemory();
        var turn = ConversationTurn.Create(ChatRole.User, "hello");
        await sut.AddAsync(turn, CancellationToken.None); // must not throw
    }

    [Fact]
    public async Task NullConversationMemory_GetRecentAsync_ReturnsEmpty()
    {
        IConversationMemory sut = new NullConversationMemory();
        var result = await sut.GetRecentAsync(10, CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task NullConversationMemory_ClearAsync_DoesNotThrow()
    {
        IConversationMemory sut = new NullConversationMemory();
        await sut.ClearAsync(CancellationToken.None); // must not throw
    }

    // --- IWorkspaceMemory ---

    [Fact]
    public async Task NullWorkspaceMemory_SaveAsync_DoesNotThrow()
    {
        IWorkspaceMemory sut = new NullWorkspaceMemory();
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await sut.SaveAsync(entry, CancellationToken.None);
    }

    [Fact]
    public async Task NullWorkspaceMemory_GetAsync_ReturnsNull()
    {
        IWorkspaceMemory sut = new NullWorkspaceMemory();
        var result = await sut.GetAsync("any-key", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task NullWorkspaceMemory_SearchAsync_ReturnsEmpty()
    {
        IWorkspaceMemory sut = new NullWorkspaceMemory();
        var result = await sut.SearchAsync(["tag1"], CancellationToken.None);
        Assert.Empty(result);
    }

    // --- ITaskMemory ---

    [Fact]
    public async Task NullTaskMemory_SaveAsync_DoesNotThrow()
    {
        ITaskMemory sut = new NullTaskMemory();
        var entry = new MemoryEntry
        {
            Key = "k",
            Tags = [],
            Content = "c",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await sut.SaveAsync(entry, CancellationToken.None);
    }

    [Fact]
    public async Task NullTaskMemory_GetAsync_ReturnsNull()
    {
        ITaskMemory sut = new NullTaskMemory();
        var result = await sut.GetAsync("any-key", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task NullTaskMemory_SearchAsync_ReturnsEmpty()
    {
        ITaskMemory sut = new NullTaskMemory();
        var result = await sut.SearchAsync(["tag1"], CancellationToken.None);
        Assert.Empty(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~NullMemory" -v n
```

Expected: compile errors — interfaces and null implementations not found.

- [ ] **Step 3: Write memory interfaces**

```csharp
// src/Ferret.Core/Ai/Interfaces/IConversationMemory.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Tracks conversation turns for an ongoing session.
/// Default implementation is <see cref="NullImplementations.NullConversationMemory"/>;
/// real storage is introduced in Sprint 15.
/// </summary>
public interface IConversationMemory
{
    Task AddAsync(ConversationTurn turn, CancellationToken ct);
    Task<IReadOnlyList<ConversationTurn>> GetRecentAsync(int count, CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
}
```

```csharp
// src/Ferret.Core/Ai/Interfaces/IWorkspaceMemory.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Persistent key-value memory scoped to the workspace.
/// Default implementation is <see cref="NullImplementations.NullWorkspaceMemory"/>;
/// real storage is introduced in Sprint 15.
/// </summary>
public interface IWorkspaceMemory
{
    Task SaveAsync(MemoryEntry entry, CancellationToken ct);
    Task<MemoryEntry?> GetAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(IReadOnlyList<string> tags, CancellationToken ct);
}
```

```csharp
// src/Ferret.Core/Ai/Interfaces/ITaskMemory.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Short-lived key-value memory scoped to a single task execution.
/// Default implementation is <see cref="NullImplementations.NullTaskMemory"/>;
/// real storage is introduced in Sprint 15.
/// </summary>
public interface ITaskMemory
{
    Task SaveAsync(MemoryEntry entry, CancellationToken ct);
    Task<MemoryEntry?> GetAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(IReadOnlyList<string> tags, CancellationToken ct);
}
```

- [ ] **Step 4: Write null implementations**

```csharp
// src/Ferret.Core/Ai/NullImplementations/NullConversationMemory.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.NullImplementations;

/// <summary>No-op conversation memory — used by default until Sprint 15 provides real storage.</summary>
public sealed class NullConversationMemory : IConversationMemory
{
    public Task AddAsync(ConversationTurn turn, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<ConversationTurn>> GetRecentAsync(int count, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ConversationTurn>>([]);

    public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
}
```

```csharp
// src/Ferret.Core/Ai/NullImplementations/NullWorkspaceMemory.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.NullImplementations;

/// <summary>No-op workspace memory — used by default until Sprint 15 provides real storage.</summary>
public sealed class NullWorkspaceMemory : IWorkspaceMemory
{
    public Task SaveAsync(MemoryEntry entry, CancellationToken ct) => Task.CompletedTask;

    public Task<MemoryEntry?> GetAsync(string key, CancellationToken ct) =>
        Task.FromResult<MemoryEntry?>(null);

    public Task<IReadOnlyList<MemoryEntry>> SearchAsync(IReadOnlyList<string> tags, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MemoryEntry>>([]);
}
```

```csharp
// src/Ferret.Core/Ai/NullImplementations/NullTaskMemory.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.NullImplementations;

/// <summary>No-op task memory — used by default until Sprint 15 provides real storage.</summary>
public sealed class NullTaskMemory : ITaskMemory
{
    public Task SaveAsync(MemoryEntry entry, CancellationToken ct) => Task.CompletedTask;

    public Task<MemoryEntry?> GetAsync(string key, CancellationToken ct) =>
        Task.FromResult<MemoryEntry?>(null);

    public Task<IReadOnlyList<MemoryEntry>> SearchAsync(IReadOnlyList<string> tags, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MemoryEntry>>([]);
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Ai" -v n
```

Expected: all AI tests PASS (34 from Tasks 1–3 + 9 new = 43 total).

- [ ] **Step 6: Full solution test**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS, 0 failures.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Core/Ai/Interfaces/IConversationMemory.cs src/Ferret.Core/Ai/Interfaces/IWorkspaceMemory.cs src/Ferret.Core/Ai/Interfaces/ITaskMemory.cs src/Ferret.Core/Ai/NullImplementations/NullConversationMemory.cs src/Ferret.Core/Ai/NullImplementations/NullWorkspaceMemory.cs src/Ferret.Core/Ai/NullImplementations/NullTaskMemory.cs tests/Ferret.Core.Tests/Ai/NullMemoryTests.cs
git commit -m "feat(sprint-12): Ferret.Core.Ai memory interfaces and null implementations — IConversationMemory, IWorkspaceMemory, ITaskMemory, NullConversationMemory, NullWorkspaceMemory, NullTaskMemory"
```

---

## Completion Checklist

After all five tasks complete:

- [ ] All 43 tests in `tests/Ferret.Core.Tests/Ai/` pass
- [ ] Full solution passes: `dotnet test src/Ferret.sln -v n`
- [ ] `Ferret.Core` has zero new external NuGet references (inspect `.csproj`)
- [ ] All types are in their correct namespaces: `Ferret.Core.Ai.Models`, `Ferret.Core.Ai.Interfaces`, `Ferret.Core.Ai.NullImplementations`
- [ ] `ModelId` and `ProviderId` have private constructors and `Create` factories
- [ ] `IVisionModel` exists with only `Descriptor` — no methods
- [ ] `NullConversationMemory`, `NullWorkspaceMemory`, `NullTaskMemory` all compiled and tested
- [ ] Sprint 12 s2 (Model Platform) can now reference `Ferret.Core.Ai.Interfaces.IModelProvider` and all dependent types
- [ ] Sprint 12 s5 (Prompt Platform) can now reference `Ferret.Core.Ai.Models.ChatMessage` and related types

**Sub-plans unblocked by s1 completion:** s2 (Model Platform), s5 (Prompt Platform) — both can begin immediately after s1 merges.
