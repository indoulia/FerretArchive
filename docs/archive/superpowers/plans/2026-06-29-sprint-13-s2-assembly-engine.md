# Sprint 13 Sub-plan 2 — Context Assembly Engine

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `ContextAssembler` and its supporting utilities in `Ferret.AI`, create the `Ferret.AI.Tests` test project, and register everything via `AiModule`. After this sub-plan, calling `IContextAssembler.AssembleAsync` produces a correct `ContextPackage` from any working workspace.

**Architecture:** All implementation lives in `src/Ferret.AI/Context/`. A new `tests/Ferret.AI.Tests/` project holds tests. No new NuGet packages beyond the existing `Ferret.AI.csproj` references. `AiModule` is updated to register `IContextAssembler → ContextAssembler` and the supporting services.

**Tech Stack:** .NET 9, C# 13, xUnit. `Ferret.AI` may reference `Ferret.Core`, `Ferret.Models`, `Ferret.Search`, and `Ferret.Indexing`.

## Global Constraints

- Sprint 13 s1 must be merged before starting s2.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-13):`, `test(sprint-13):`.
- Namespace: `Ferret.AI.Context`.
- `TokenEstimator.Estimate(text)` returns `Math.Max(1, text.Length / 4)`. It is a pure static method — no instance, no DI.
- `ContextDeduplicator.Deduplicate` is a pure function: same input always produces same output. No DI.
- `DocumentExpander` is DI-registered. Parallelism is capped at 5 concurrent `IDocumentService.GetAsync` calls.
- `ContextAssembler` adds documents in descending score order until `MaxTokens` or `MaxDocuments` is reached.
- Build command: `dotnet build src/Ferret.sln -v n`
- Test command (task-level): `dotnet test tests/Ferret.AI.Tests/ -v n`
- Full test: `dotnet test src/Ferret.sln -v n`

---

## File Structure Map

```
src/Ferret.AI/
  Context/
    TokenEstimator.cs          [NEW — Task 1]
    ContextDeduplicator.cs     [NEW — Task 2]
    DocumentExpander.cs        [NEW — Task 3]
    ContentFilter.cs           [NEW — Task 4]
    ContextAssembler.cs        [NEW — Task 5]
  AiModule.cs                  [MODIFY — Task 5]

tests/Ferret.AI.Tests/
  Ferret.AI.Tests.csproj       [NEW — Task 1]
  Context/
    TokenEstimatorTests.cs     [NEW — Task 1]
    ContextDeduplicatorTests.cs [NEW — Task 2]
    DocumentExpanderTests.cs   [NEW — Task 3]
    ContentFilterTests.cs      [NEW — Task 4]
    ContextAssemblerTests.cs   [NEW — Task 5]
```

---

### Task 1: Create Ferret.AI.Tests Project + TokenEstimator

Creates the test project (needed by all subsequent tasks) and the `TokenEstimator` — a pure static utility that approximates token count as `text.Length / 4`.

**Files:**
- Create: `tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj`
- Create: `src/Ferret.AI/Context/TokenEstimator.cs`
- Create: `tests/Ferret.AI.Tests/Context/TokenEstimatorTests.cs`

**Interfaces:**
- Consumes: nothing external
- Produces: `TokenEstimator` (static class with `Estimate(string) → int`)

- [ ] **Step 1: Create the test project**

```xml
<!-- tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" PrivateAssets="all" />
    <PackageReference Include="coverlet.collector" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Ferret.AI/Ferret.AI.csproj" />
    <ProjectReference Include="../../src/Ferret.Core/Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add test project to solution**

```
dotnet sln src/Ferret.sln add tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj
```

Expected: `Project added to the solution.`

- [ ] **Step 3: Write the failing tests**

```csharp
// tests/Ferret.AI.Tests/Context/TokenEstimatorTests.cs
using Ferret.AI.Context;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class TokenEstimatorTests
{
    [Fact]
    public void Estimate_EmptyString_ReturnsOne()
    {
        Assert.Equal(1, TokenEstimator.Estimate(""));
    }

    [Fact]
    public void Estimate_FourCharString_ReturnsOne()
    {
        Assert.Equal(1, TokenEstimator.Estimate("abcd"));
    }

    [Fact]
    public void Estimate_EightCharString_ReturnsTwo()
    {
        Assert.Equal(2, TokenEstimator.Estimate("abcdefgh"));
    }

    [Fact]
    public void Estimate_OneCharString_ReturnsOne()
    {
        Assert.Equal(1, TokenEstimator.Estimate("a"));
    }

    [Fact]
    public void Estimate_HundredCharString_ReturnsTwentyFive()
    {
        Assert.Equal(25, TokenEstimator.Estimate(new string('x', 100)));
    }

    [Fact]
    public void Estimate_NullString_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => TokenEstimator.Estimate(null!));
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~TokenEstimator" -v n
```

Expected: compile errors — `TokenEstimator` not found.

- [ ] **Step 5: Write TokenEstimator**

```csharp
// src/Ferret.AI/Context/TokenEstimator.cs
namespace Ferret.AI.Context;

/// <summary>
/// Approximates token count using the 4-characters-per-token heuristic.
/// Returns at least 1 for any non-null input, including empty strings.
/// Suitable for token budget enforcement; not a replacement for model-specific tokenizers.
/// </summary>
public static class TokenEstimator
{
    /// <summary>
    /// Estimates the token count for <paramref name="text"/>.
    /// Formula: <c>Math.Max(1, text.Length / 4)</c>.
    /// </summary>
    /// <param name="text">The text to estimate. Must not be null.</param>
    /// <returns>Estimated token count, minimum 1.</returns>
    public static int Estimate(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Math.Max(1, text.Length / 4);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~TokenEstimator" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 7: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds, 0 errors.

- [ ] **Step 8: Commit**

```
git add tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj src/Ferret.AI/Context/TokenEstimator.cs tests/Ferret.AI.Tests/Context/TokenEstimatorTests.cs
git commit -m "feat(sprint-13): Ferret.AI.Tests project + TokenEstimator — 4-chars-per-token approximation"
```

---

### Task 2: ContextDeduplicator

Removes duplicate search hits by `DocumentId`. First occurrence (highest score) wins. Pure function — no DI, no state.

**Files:**
- Create: `src/Ferret.AI/Context/ContextDeduplicator.cs`
- Create: `tests/Ferret.AI.Tests/Context/ContextDeduplicatorTests.cs`

**Interfaces:**
- Consumes: `Ferret.Core.Search.SearchHit` (existing), `Ferret.Core.Primitives.DocumentId` (existing)
- Produces: `ContextDeduplicator` with `Deduplicate(IReadOnlyList<SearchHit>) → IReadOnlyList<SearchHit>`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.AI.Tests/Context/ContextDeduplicatorTests.cs
using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class ContextDeduplicatorTests
{
    private static SearchHit MakeHit(string docId, float score) =>
        new FileSearchHit
        {
            DocumentId = DocumentId.Create(docId),
            ConnectorInstanceId = ConnectorInstanceId.Create("test"),
            CanonicalUri = new Uri($"filesystem:///{docId}"),
            DisplayName = docId,
            Kind = SearchHitKind.File,
            Score = score,
            Snippet = new HighlightedText { Spans = [] },
        };

    [Fact]
    public void Deduplicate_NoDuplicates_ReturnsSameCount()
    {
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.7f) };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Deduplicate_WithDuplicate_ReturnsFirstOccurrence()
    {
        var hits = new[]
        {
            MakeHit("a", 0.9f),
            MakeHit("a", 0.5f), // duplicate — should be removed
            MakeHit("b", 0.7f),
        };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Equal(2, result.Count);
        Assert.Equal(0.9f, result[0].Score); // first occurrence kept
    }

    [Fact]
    public void Deduplicate_AllDuplicates_ReturnsOne()
    {
        var hits = new[]
        {
            MakeHit("a", 0.9f),
            MakeHit("a", 0.7f),
            MakeHit("a", 0.5f),
        };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Single(result);
    }

    [Fact]
    public void Deduplicate_EmptyList_ReturnsEmpty()
    {
        var result = ContextDeduplicator.Deduplicate([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Deduplicate_PreservesInputOrder()
    {
        var hits = new[]
        {
            MakeHit("c", 0.6f),
            MakeHit("a", 0.9f),
            MakeHit("b", 0.7f),
        };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Equal("c", result[0].DocumentId.Value);
        Assert.Equal("a", result[1].DocumentId.Value);
        Assert.Equal("b", result[2].DocumentId.Value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~ContextDeduplicator" -v n
```

Expected: compile errors — `ContextDeduplicator` not found.

- [ ] **Step 3: Write ContextDeduplicator**

```csharp
// src/Ferret.AI/Context/ContextDeduplicator.cs
using Ferret.Core.Search;

namespace Ferret.AI.Context;

/// <summary>
/// Removes duplicate search hits by document ID, preserving the first occurrence.
/// Pure function — no DI, no state, safe to call from any thread.
/// </summary>
public static class ContextDeduplicator
{
    /// <summary>
    /// Returns a new list with duplicate <see cref="SearchHit"/> entries removed.
    /// When the same <see cref="Ferret.Core.Primitives.DocumentId"/> appears more than once,
    /// the first occurrence is kept and subsequent occurrences are discarded.
    /// Input order is preserved.
    /// </summary>
    /// <param name="hits">The search hits to deduplicate.</param>
    /// <returns>A new list with at most one entry per document ID.</returns>
    public static IReadOnlyList<SearchHit> Deduplicate(IReadOnlyList<SearchHit> hits)
    {
        ArgumentNullException.ThrowIfNull(hits);

        if (hits.Count == 0)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<SearchHit>(hits.Count);

        foreach (var hit in hits)
        {
            if (seen.Add(hit.DocumentId.Value))
            {
                result.Add(hit);
            }
        }

        return result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~ContextDeduplicator" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.AI/Context/ContextDeduplicator.cs tests/Ferret.AI.Tests/Context/ContextDeduplicatorTests.cs
git commit -m "feat(sprint-13): ContextDeduplicator — removes duplicate SearchHit entries by DocumentId"
```

---

### Task 3: DocumentExpander

Resolves `SearchHit[]` to full `Document[]` via `IDocumentService`. Fetches in parallel (max 5 concurrent). Missing documents are logged and skipped.

**Files:**
- Create: `src/Ferret.AI/Context/DocumentExpander.cs`
- Create: `tests/Ferret.AI.Tests/Context/DocumentExpanderTests.cs`

**Interfaces:**
- Consumes: `Ferret.Core.Search.SearchHit`, `Ferret.Core.Search.IDocumentService` (existing), `Ferret.Core.Documents.Document` (existing)
- Produces: `DocumentExpander` — DI-registered, constructor takes `IDocumentService, ILogger<DocumentExpander>`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.AI.Tests/Context/DocumentExpanderTests.cs
using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class DocumentExpanderTests
{
    private static SearchHit MakeHit(string docId) =>
        new FileSearchHit
        {
            DocumentId = DocumentId.Create(docId),
            ConnectorInstanceId = ConnectorInstanceId.Create("test"),
            CanonicalUri = new Uri($"filesystem:///{docId}"),
            DisplayName = docId,
            Kind = SearchHitKind.File,
            Score = 0.9f,
            Snippet = new HighlightedText { Spans = [] },
        };

    private static Document MakeDocument(string docId, string text) =>
        new()
        {
            Id = DocumentId.Create(docId),
            SourceAssetId = AssetId.Create(docId),
            ConnectorId = ConnectorId.Create("filesystem"),
            InstanceId = ConnectorInstanceId.Create("test"),
            MediaType = "text/plain",
            Kind = DocumentKind.Text,
            PlainText = text,
            ProducedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task ExpandAsync_AllHitsFound_ReturnsAllDocuments()
    {
        var docService = new StubDocumentService(new Dictionary<string, Document>
        {
            ["doc-a"] = MakeDocument("doc-a", "content a"),
            ["doc-b"] = MakeDocument("doc-b", "content b"),
        });
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);

        var hits = new[] { MakeHit("doc-a"), MakeHit("doc-b") };
        var result = await expander.ExpandAsync(hits, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Id.Value == "doc-a");
        Assert.Contains(result, d => d.Id.Value == "doc-b");
    }

    [Fact]
    public async Task ExpandAsync_MissingDocument_IsSkipped()
    {
        var docService = new StubDocumentService(new Dictionary<string, Document>
        {
            ["doc-a"] = MakeDocument("doc-a", "content a"),
            // doc-b is missing
        });
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);

        var hits = new[] { MakeHit("doc-a"), MakeHit("doc-b") };
        var result = await expander.ExpandAsync(hits, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("doc-a", result[0].Id.Value);
    }

    [Fact]
    public async Task ExpandAsync_EmptyHits_ReturnsEmpty()
    {
        var docService = new StubDocumentService(new Dictionary<string, Document>());
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);

        var result = await expander.ExpandAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    private sealed class StubDocumentService(Dictionary<string, Document> store) : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct)
        {
            store.TryGetValue(id.Value, out var doc);
            return Task.FromResult(doc);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~DocumentExpander" -v n
```

Expected: compile errors — `DocumentExpander` not found.

- [ ] **Step 3: Update Ferret.AI.csproj to add Search reference**

The current `Ferret.AI.csproj` only references `Ferret.Core` and `Ferret.Models`. `IDocumentService` is in `Ferret.Core.Search`, so it's already available. However `ISearchService` (used by `ContextAssembler`) is in `Ferret.Search`. Add that reference now.

Read current csproj:
```
src/Ferret.AI/Ferret.AI.csproj
```

Current content:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.AI</AssemblyName>
    <RootNamespace>Ferret.AI</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Models\Ferret.Models.csproj" />
  </ItemGroup>
</Project>
```

Add `Ferret.Search` and `Ferret.Indexing` references:

```xml
<!-- src/Ferret.AI/Ferret.AI.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.AI</AssemblyName>
    <RootNamespace>Ferret.AI</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Models\Ferret.Models.csproj" />
    <ProjectReference Include="..\Ferret.Search\Ferret.Search.csproj" />
    <ProjectReference Include="..\Ferret.Indexing\Ferret.Indexing.csproj" />
  </ItemGroup>

</Project>
```

Also update `tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj` to add the same references (so test stubs can use `IDocumentService`, `ConnectorInstanceId`, etc.):

```xml
<!-- tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" PrivateAssets="all" />
    <PackageReference Include="coverlet.collector" PrivateAssets="all" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Ferret.AI/Ferret.AI.csproj" />
    <ProjectReference Include="../../src/Ferret.Core/Ferret.Core.csproj" />
    <ProjectReference Include="../../src/Ferret.Search/Ferret.Search.csproj" />
    <ProjectReference Include="../../src/Ferret.Indexing/Ferret.Indexing.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Write DocumentExpander**

```csharp
// src/Ferret.AI/Context/DocumentExpander.cs
using Ferret.Core.Documents;
using Ferret.Core.Search;
using Microsoft.Extensions.Logging;

namespace Ferret.AI.Context;

/// <summary>
/// Resolves <see cref="SearchHit"/> instances to full <see cref="Document"/> objects
/// via <see cref="IDocumentService"/>. Fetches in parallel (max 5 concurrent).
/// Missing documents are logged at Warning level and excluded from the result.
/// </summary>
public sealed class DocumentExpander
{
    private const int MaxConcurrency = 5;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentExpander> _logger;

    /// <summary>Initializes a new instance of the <see cref="DocumentExpander"/> class.</summary>
    public DocumentExpander(IDocumentService documentService, ILogger<DocumentExpander> logger)
    {
        ArgumentNullException.ThrowIfNull(documentService);
        ArgumentNullException.ThrowIfNull(logger);
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the full document for each hit in parallel.
    /// Hits whose documents cannot be found are silently excluded from the result.
    /// </summary>
    /// <param name="hits">Search hits to expand.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full documents for every hit that was found in the document store.</returns>
    public async Task<IReadOnlyList<Document>> ExpandAsync(
        IReadOnlyList<SearchHit> hits, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(hits);

        if (hits.Count == 0)
        {
            return [];
        }

        var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        var tasks = hits.Select(hit => FetchOneAsync(hit, semaphore, ct)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.Where(d => d is not null).Select(d => d!).ToList();
    }

    private async Task<Document?> FetchOneAsync(
        SearchHit hit, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var document = await _documentService.GetAsync(hit.DocumentId, ct).ConfigureAwait(false);
            if (document is null)
            {
                _logger.LogWarning("Document not found during context expansion: {DocumentId}", hit.DocumentId.Value);
            }

            return document;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~DocumentExpander" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 7: Commit**

```
git add src/Ferret.AI/Ferret.AI.csproj tests/Ferret.AI.Tests/Ferret.AI.Tests.csproj src/Ferret.AI/Context/DocumentExpander.cs tests/Ferret.AI.Tests/Context/DocumentExpanderTests.cs
git commit -m "feat(sprint-13): DocumentExpander — parallel document fetch with 5-concurrent cap, missing-doc skip"
```

---

### Task 4: ContentFilter

Removes low-quality documents from the expanded set before the token budget is applied. Three rules: empty/whitespace content, content under 50 characters, and content-duplicate detection via a composite fingerprint. Pure static class — no DI, no I/O.

**Files:**
- Create: `src/Ferret.AI/Context/ContentFilter.cs`
- Create: `tests/Ferret.AI.Tests/Context/ContentFilterTests.cs`

**Interfaces:**
- Consumes: `Ferret.Core.Documents.Document` (existing)
- Produces: `ContentFilter` (static class with `Filter(IReadOnlyList<Document>) → IReadOnlyList<Document>`)

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.AI.Tests/Context/ContentFilterTests.cs
using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class ContentFilterTests
{
    private static Document MakeDocument(string id, string plainText, DocumentKind kind = DocumentKind.Code) =>
        new()
        {
            Id = DocumentId.Create(id),
            SourceAssetId = AssetId.Create(id),
            ConnectorId = ConnectorId.Create("filesystem"),
            InstanceId = ConnectorInstanceId.Create("test"),
            MediaType = "text/plain",
            Kind = kind,
            PlainText = plainText,
            ProducedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public void Filter_EmptyContent_IsExcluded()
    {
        var docs = new[] { MakeDocument("a", "") };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_WhitespaceContent_IsExcluded()
    {
        var docs = new[] { MakeDocument("a", "   \n\t  ") };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ContentUnder50Chars_IsExcluded()
    {
        var docs = new[] { MakeDocument("a", "short") };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ContentExactly50Chars_IsExcluded()
    {
        // 50 chars after trim is the boundary — must be > 50 to pass
        var docs = new[] { MakeDocument("a", new string('x', 50)) };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ContentOver50Chars_IsIncluded()
    {
        var docs = new[] { MakeDocument("a", new string('x', 51)) };
        var result = ContentFilter.Filter(docs);
        Assert.Single(result);
    }

    [Fact]
    public void Filter_NormalDocument_IsIncluded()
    {
        var content = "public class AuthService { private readonly IUserRepository _repo; }";
        var docs = new[] { MakeDocument("a", content) };
        var result = ContentFilter.Filter(docs);
        Assert.Single(result);
    }

    [Fact]
    public void Filter_ContentDuplicate_SecondIsExcluded()
    {
        var content = new string('x', 100);
        var docs = new[]
        {
            MakeDocument("a", content),
            MakeDocument("b", content), // same content, different id
        };
        var result = ContentFilter.Filter(docs);
        Assert.Single(result);
        Assert.Equal("a", result[0].Id.Value); // first wins
    }

    [Fact]
    public void Filter_DistinctContent_BothIncluded()
    {
        var docs = new[]
        {
            MakeDocument("a", new string('a', 100)),
            MakeDocument("b", new string('b', 100)),
        };
        var result = ContentFilter.Filter(docs);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Filter_EmptyList_ReturnsEmpty()
    {
        var result = ContentFilter.Filter([]);
        Assert.Empty(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~ContentFilter" -v n
```

Expected: compile errors — `ContentFilter` not found.

- [ ] **Step 3: Write ContentFilter**

```csharp
// src/Ferret.AI/Context/ContentFilter.cs
using Ferret.Core.Documents;

namespace Ferret.AI.Context;

/// <summary>
/// Removes low-quality documents from the expanded set before token budget is applied.
/// Three exclusion rules (all applied in order):
///   1. Empty or whitespace-only content
///   2. Content length under 50 characters after trimming
///   3. Content duplicate — same (length, first-200-chars) fingerprint already seen in this pass
/// Pure static function — no DI, no I/O, no state between calls.
/// </summary>
public static class ContentFilter
{
    private const int MinContentLength = 50;
    private const int FingerprintPrefixLength = 200;

    /// <summary>
    /// Filters <paramref name="documents"/>, returning only those that pass all quality rules.
    /// First occurrence of a content fingerprint wins; subsequent documents with the same fingerprint are dropped.
    /// </summary>
    /// <param name="documents">The expanded documents to filter.</param>
    /// <returns>A new list containing only the documents that passed all rules, in input order.</returns>
    public static IReadOnlyList<Document> Filter(IReadOnlyList<Document> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        if (documents.Count == 0)
        {
            return [];
        }

        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Document>(documents.Count);

        foreach (var doc in documents)
        {
            // Rule 1: empty or whitespace
            if (string.IsNullOrWhiteSpace(doc.PlainText))
            {
                continue;
            }

            var trimmed = doc.PlainText.Trim();

            // Rule 2: too small
            if (trimmed.Length <= MinContentLength)
            {
                continue;
            }

            // Rule 3: content duplicate
            var prefix = trimmed[..Math.Min(FingerprintPrefixLength, trimmed.Length)];
            var fingerprint = $"{trimmed.Length}:{prefix}";
            if (!seenFingerprints.Add(fingerprint))
            {
                continue;
            }

            result.Add(doc);
        }

        return result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~ContentFilter" -v n
```

Expected: 9 tests PASS.

- [ ] **Step 5: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.AI/Context/ContentFilter.cs tests/Ferret.AI.Tests/Context/ContentFilterTests.cs
git commit -m "feat(sprint-13): ContentFilter — removes empty, too-small, and content-duplicate documents before token budget"
```

---

### Task 5: ContextAssembler + AiModule Registration

The pipeline orchestrator. Calls `ISearchService`, deduplicates, expands, filters, applies token budget, returns `ContextPackage`. Updates `AiModule` to register all context assembly services.

**Files:**
- Create: `src/Ferret.AI/Context/ContextAssembler.cs`
- Modify: `src/Ferret.AI/AiModule.cs`
- Create: `tests/Ferret.AI.Tests/Context/ContextAssemblerTests.cs`

**Interfaces:**
- Consumes: `ISearchService`, `IDocumentService`, `TokenEstimator`, `ContextDeduplicator`, `DocumentExpander`, `ContentFilter`, `ContextRequest`, `ContextPackage`, `ContextDocument`
- Produces: `ContextAssembler : IContextAssembler`; updated `AiModule`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.AI.Tests/Context/ContextAssemblerTests.cs
using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Context;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class ContextAssemblerTests
{
    // --- Stubs ---

    private sealed class StubSearchService(IReadOnlyList<SearchHit> hits) : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
            Task.FromResult(BuildResult(rawQuery, hits));

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            Task.FromResult(BuildResult(query.OriginalText, hits));

        private static SearchServiceResult BuildResult(string query, IReadOnlyList<SearchHit> hits)
        {
            var parsedQuery = new SearchQuery
            {
                OriginalText = query,
                Root = new KeywordExpression(query),
            };
            var result = new SearchResult
            {
                Hits = hits,
                TotalHits = hits.Count,
                ReturnedHits = hits.Count,
            };
            var execInfo = new SearchExecutionInfo
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "stub",
                Duration = TimeSpan.Zero,
                DocumentsScanned = hits.Count,
                IndexVersion = 0,
            };
            return SearchServiceResult.Success(parsedQuery, result, execInfo, new SearchProviderDescriptor
            {
                Id = "stub",
                DisplayName = "Stub",
                Capabilities = new SearchCapabilities(),
            });
        }
    }

    private sealed class StubDocumentService(Dictionary<string, Document> store) : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct)
        {
            store.TryGetValue(id.Value, out var doc);
            return Task.FromResult(doc);
        }
    }

    private static SearchHit MakeHit(string docId, float score) =>
        new FileSearchHit
        {
            DocumentId = DocumentId.Create(docId),
            ConnectorInstanceId = ConnectorInstanceId.Create("test"),
            CanonicalUri = new Uri($"filesystem:///{docId}"),
            DisplayName = docId,
            Kind = SearchHitKind.File,
            Score = score,
            Snippet = new HighlightedText { Spans = [] },
        };

    private static Document MakeDocument(string docId, string text) =>
        new()
        {
            Id = DocumentId.Create(docId),
            SourceAssetId = AssetId.Create(docId),
            ConnectorId = ConnectorId.Create("filesystem"),
            InstanceId = ConnectorInstanceId.Create("test"),
            MediaType = "text/plain",
            Kind = DocumentKind.Text,
            PlainText = text,
            ProducedAt = DateTimeOffset.UtcNow,
        };

    private static ContextAssembler BuildAssembler(
        IReadOnlyList<SearchHit> hits,
        Dictionary<string, Document> docs)
    {
        var searchService = new StubSearchService(hits);
        var docService = new StubDocumentService(docs);
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);
        return new ContextAssembler(searchService, expander, NullLogger<ContextAssembler>.Instance);
    }

    // --- Tests ---

    [Fact]
    public async Task AssembleAsync_TwoDocuments_ReturnsBothInPackage()
    {
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.7f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", "content for a"),
            ["b"] = MakeDocument("b", "content for b"),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal("test", pkg.Query);
        Assert.Equal(2, pkg.DocumentsIncluded);
        Assert.Equal(2, pkg.Documents.Count);
    }

    [Fact]
    public async Task AssembleAsync_TokenBudget_StopsWhenBudgetExceeded()
    {
        // "x" * 40 = 10 tokens per doc. Budget 15 → only 1 fits.
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.7f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('x', 40)),
            ["b"] = MakeDocument("b", new string('x', 40)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test", MaxTokens = 15 };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(1, pkg.DocumentsIncluded);
    }

    [Fact]
    public async Task AssembleAsync_MaxDocuments_LimitsCount()
    {
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.8f), MakeHit("c", 0.7f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", "short"),
            ["b"] = MakeDocument("b", "short"),
            ["c"] = MakeDocument("c", "short"),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test", MaxDocuments = 2 };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(2, pkg.DocumentsIncluded);
    }

    [Fact]
    public async Task AssembleAsync_DuplicateHits_DeduplicatedBeforeExpansion()
    {
        var hits = new[]
        {
            MakeHit("a", 0.9f),
            MakeHit("a", 0.5f), // duplicate
        };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", "content"),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(1, pkg.DocumentsIncluded);
    }

    [Fact]
    public async Task AssembleAsync_NoSearchResults_ReturnsEmptyPackage()
    {
        var assembler = BuildAssembler([], new Dictionary<string, Document>());
        var request = new ContextRequest { Query = "nothing" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(0, pkg.DocumentsIncluded);
        Assert.Equal("nothing", pkg.Query);
    }

    [Fact]
    public async Task AssembleAsync_DocumentsOrderedByDescendingScore()
    {
        var hits = new[] { MakeHit("b", 0.7f), MakeHit("a", 0.9f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", "high score"),
            ["b"] = MakeDocument("b", "lower score"),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "order" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal("a", pkg.Documents[0].DocumentId.Value);
        Assert.Equal("b", pkg.Documents[1].DocumentId.Value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.AI.Tests/ --filter "FullyQualifiedName~ContextAssembler" -v n
```

Expected: compile errors — `ContextAssembler` not found.

- [ ] **Step 3: Write ContextAssembler**

```csharp
// src/Ferret.AI/Context/ContextAssembler.cs
using Ferret.Core.Context;
using Ferret.Core.Search;
using Microsoft.Extensions.Logging;

namespace Ferret.AI.Context;

/// <summary>
/// Implements the context assembly pipeline:
///   1. Search — call ISearchService with the query
///   2. Deduplicate — remove repeated DocumentIds (first occurrence wins)
///   3. Expand — fetch full Document for each unique hit
///   4. Sort — order by descending score
///   5. Budget — add documents until MaxTokens or MaxDocuments is reached
///   6. Package — wrap results in a ContextPackage
/// </summary>
public sealed class ContextAssembler : IContextAssembler
{
    private readonly ISearchService _searchService;
    private readonly DocumentExpander _expander;
    private readonly ILogger<ContextAssembler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ContextAssembler"/> class.</summary>
    public ContextAssembler(
        ISearchService searchService,
        DocumentExpander expander,
        ILogger<ContextAssembler> logger)
    {
        ArgumentNullException.ThrowIfNull(searchService);
        ArgumentNullException.ThrowIfNull(expander);
        ArgumentNullException.ThrowIfNull(logger);
        _searchService = searchService;
        _expander = expander;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Step 1: Search
        var options = new SearchOptions { MaxResults = request.MaxDocuments * 2 };
        var searchResult = await _searchService.SearchAsync(request.Query, options).ConfigureAwait(false);

        var allHits = searchResult.IsSuccess ? searchResult.Hits : (IReadOnlyList<Core.Search.SearchHit>)[];
        var documentsConsidered = allHits.Count;

        _logger.LogDebug("Context assembly: {HitCount} hits for query '{Query}'", allHits.Count, request.Query);

        // Step 2: Deduplicate
        var uniqueHits = ContextDeduplicator.Deduplicate(allHits);

        // Step 3: Expand
        var documents = await _expander.ExpandAsync(uniqueHits, ct).ConfigureAwait(false);

        // Step 4: Filter — remove empty, too-small, and content-duplicate documents
        var filtered = ContentFilter.Filter(documents);
        _logger.LogDebug("Context assembly: {FilteredCount}/{ExpandedCount} documents passed content filter", filtered.Count, documents.Count);

        // Build a score lookup from hits (keyed by DocumentId value)
        var scoreByDocId = uniqueHits
            .ToDictionary(h => h.DocumentId.Value, h => h.Score, StringComparer.Ordinal);

        // Step 5: Sort filtered documents by descending score
        var sorted = filtered
            .Select(doc => (doc, score: scoreByDocId.TryGetValue(doc.Id.Value, out var s) ? s : 0f))
            .OrderByDescending(x => x.score)
            .ToList();

        // Step 6: Apply token budget and document count limit
        var included = new List<ContextDocument>(request.MaxDocuments);
        var totalTokens = 0;

        foreach (var (doc, score) in sorted)
        {
            if (included.Count >= request.MaxDocuments)
            {
                break;
            }

            var content = doc.PlainText;
            var tokenEstimate = TokenEstimator.Estimate(content);

            if (totalTokens + tokenEstimate > request.MaxTokens && included.Count > 0)
            {
                break;
            }

            included.Add(new ContextDocument
            {
                DocumentId = doc.Id,
                CanonicalUri = new Uri($"filesystem:///{doc.Id.Value}"),
                DisplayName = doc.Id.Value,
                Title = doc.Title,
                Content = content,
                Score = score,
                TokenEstimate = tokenEstimate,
                Source = Core.Context.ContextDocumentSource.FullDocument,
            });

            totalTokens += tokenEstimate;
        }

        // Step 7: Package
        return new ContextPackage
        {
            Query = request.Query,
            Documents = included,
            TotalTokenEstimate = totalTokens,
            DocumentsConsidered = documentsConsidered,
            DocumentsIncluded = included.Count,
            AssembledAt = DateTimeOffset.UtcNow,
        };
    }
}
```

- [ ] **Step 4: Update AiModule**

```csharp
// src/Ferret.AI/AiModule.cs
using Ferret.AI.Context;
using Ferret.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.AI;

/// <summary>Registers Ferret.AI services into the DI container.</summary>
public static class AiModule
{
    /// <summary>Configures Ferret.AI context assembly services.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<DocumentExpander>();
        services.AddSingleton<IContextAssembler, ContextAssembler>();

        return services;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.AI.Tests/ -v n
```

Expected: all Ferret.AI.Tests PASS (6 + 5 + 3 + 6 = 20 total).

- [ ] **Step 6: Full solution test**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS, 0 failures.

- [ ] **Step 7: Commit**

```
git add src/Ferret.AI/Context/ContextAssembler.cs src/Ferret.AI/AiModule.cs tests/Ferret.AI.Tests/Context/ContextAssemblerTests.cs
git commit -m "feat(sprint-13): ContextAssembler — search→dedup→expand→filter→budget→package pipeline; AiModule registration"
```

---

## Completion Checklist

After all five tasks complete:

- [ ] All 29 tests in `tests/Ferret.AI.Tests/` pass
- [ ] Full solution passes: `dotnet test src/Ferret.sln -v n`
- [ ] `Ferret.AI.Tests` project is added to `src/Ferret.sln`
- [ ] `TokenEstimator.Estimate("")` returns 1 (not 0)
- [ ] `ContextDeduplicator.Deduplicate` preserves input order
- [ ] `DocumentExpander` caps at 5 concurrent `GetAsync` calls
- [ ] `ContextAssembler` adds documents in descending score order
- [ ] `ContextAssembler` stops adding when `MaxTokens` exceeded
- [ ] `ContextAssembler` stops adding when `MaxDocuments` reached
- [ ] `AiModule.ConfigureServices` registers `IContextAssembler → ContextAssembler`
- [ ] Sprint 13 s3 (MCP + CLI wire-up) can now inject `IContextAssembler` via DI

**Sub-plan unblocked by s2 completion:** s3 (MCP Context Tool + CLI wire-up).
