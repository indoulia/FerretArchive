# Sprint 13 Sub-plan 1 — Context Core Contracts

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the `Ferret.Core.Context` namespace — all context assembly contracts, value types, and the `IContextAssembler` interface — as a zero-dependency addition to `Ferret.Core`. This is the contract layer Sub-plans s2 and s3 depend on.

**Architecture:** All new types live under `src/Ferret.Core/Context/`. No new project, no new NuGet references. Two files: value/data types and the assembler interface. Tests go in `tests/Ferret.Core.Tests/Context/` using the existing `Ferret.Core.Tests` project.

**Tech Stack:** .NET 9, C# 13, xUnit (tests already in `Ferret.Core.Tests`). No external NuGet references — `Ferret.Core` must remain zero-dependency.

## Global Constraints

- Sprint 12 must be fully implemented before Sprint 13.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-13):`, `test(sprint-13):`.
- Namespaces: `Ferret.Core.Context`.
- No external NuGet references in `Ferret.Core`. Zero vendor SDK types.
- `ContextRequest` defaults: `MaxTokens = 8000`, `MaxDocuments = 10`, `IncludeSections = true`.
- `ContextPackage.ToPromptString()` must produce a deterministic, formatted string — see spec.
- Build command: `dotnet build src/Ferret.sln -v n`
- Test command (task-level): `dotnet test tests/Ferret.Core.Tests/ -v n`
- Full test: `dotnet test src/Ferret.sln -v n`

---

## File Structure Map

```
src/Ferret.Core/
  Context/
    ContextDocumentSource.cs      [NEW — Task 1]
    ContextRequest.cs             [NEW — Task 1]
    ContextDocument.cs            [NEW — Task 1]
    ContextPackage.cs             [NEW — Task 2]
    IContextAssembler.cs          [NEW — Task 2]

tests/Ferret.Core.Tests/
  Context/
    ContextRequestTests.cs        [NEW — Task 1]
    ContextDocumentTests.cs       [NEW — Task 1]
    ContextPackageTests.cs        [NEW — Task 2]
```

---

### Task 1: Value Types — ContextDocumentSource, ContextRequest, ContextDocument

Establishes the fundamental request and document types. `ContextRequest` is an immutable record with default values. `ContextDocument` carries the assembled content and token estimate for a single document.

**Files:**
- Create: `src/Ferret.Core/Context/ContextDocumentSource.cs`
- Create: `src/Ferret.Core/Context/ContextRequest.cs`
- Create: `src/Ferret.Core/Context/ContextDocument.cs`
- Create: `tests/Ferret.Core.Tests/Context/ContextRequestTests.cs`
- Create: `tests/Ferret.Core.Tests/Context/ContextDocumentTests.cs`

**Interfaces:**
- Consumes: `Ferret.Core.Primitives.DocumentId` (existing)
- Produces: `ContextDocumentSource`, `ContextRequest`, `ContextDocument`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Context/ContextRequestTests.cs
using Ferret.Core.Context;
using Xunit;

namespace Ferret.Core.Tests.Context;

public sealed class ContextRequestTests
{
    [Fact]
    public void Create_MinimalQuery_HasDefaults()
    {
        var req = new ContextRequest { Query = "authentication" };
        Assert.Equal("authentication", req.Query);
        Assert.Equal(8000, req.MaxTokens);
        Assert.Equal(10, req.MaxDocuments);
        Assert.True(req.IncludeSections);
    }

    [Fact]
    public void Create_CustomValues_ArePreserved()
    {
        var req = new ContextRequest
        {
            Query = "database migrations",
            MaxTokens = 4000,
            MaxDocuments = 5,
            IncludeSections = false,
        };
        Assert.Equal(4000, req.MaxTokens);
        Assert.Equal(5, req.MaxDocuments);
        Assert.False(req.IncludeSections);
    }

    [Fact]
    public void Query_CannotBeNull()
    {
        // record init will throw if Query is null because it's required
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = new ContextRequest { Query = null! };
        });
    }
}
```

```csharp
// tests/Ferret.Core.Tests/Context/ContextDocumentTests.cs
using Ferret.Core.Context;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Core.Tests.Context;

public sealed class ContextDocumentTests
{
    [Fact]
    public void ContextDocument_PreservesAllFields()
    {
        var id = DocumentId.Create("doc-1");
        var uri = new Uri("filesystem:///src/auth.cs");
        var doc = new ContextDocument
        {
            DocumentId = id,
            CanonicalUri = uri,
            DisplayName = "src/auth.cs",
            Title = "Authentication Service",
            Content = "public class AuthService { }",
            Score = 0.91f,
            TokenEstimate = 7,
            Source = ContextDocumentSource.FullDocument,
        };

        Assert.Equal("doc-1", doc.DocumentId.Value);
        Assert.Equal(uri, doc.CanonicalUri);
        Assert.Equal("src/auth.cs", doc.DisplayName);
        Assert.Equal("Authentication Service", doc.Title);
        Assert.Equal(0.91f, doc.Score);
        Assert.Equal(7, doc.TokenEstimate);
        Assert.Equal(ContextDocumentSource.FullDocument, doc.Source);
    }

    [Fact]
    public void ContextDocument_Title_CanBeNull()
    {
        var doc = new ContextDocument
        {
            DocumentId = DocumentId.Create("doc-2"),
            CanonicalUri = new Uri("filesystem:///src/util.cs"),
            DisplayName = "src/util.cs",
            Title = null,
            Content = "// utility",
            Score = 0.5f,
            TokenEstimate = 2,
            Source = ContextDocumentSource.Section,
        };

        Assert.Null(doc.Title);
        Assert.Equal(ContextDocumentSource.Section, doc.Source);
    }

    [Fact]
    public void ContextDocumentSource_HasExpectedValues()
    {
        Assert.Equal(0, (int)ContextDocumentSource.FullDocument);
        Assert.Equal(1, (int)ContextDocumentSource.Section);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Context" -v n
```

Expected: compile errors — types not found.

- [ ] **Step 3: Write ContextDocumentSource**

```csharp
// src/Ferret.Core/Context/ContextDocumentSource.cs
namespace Ferret.Core.Context;

/// <summary>Indicates whether a <see cref="ContextDocument"/> contains a full document or a single section.</summary>
public enum ContextDocumentSource
{
    FullDocument = 0,
    Section = 1,
}
```

- [ ] **Step 4: Write ContextRequest**

```csharp
// src/Ferret.Core/Context/ContextRequest.cs
namespace Ferret.Core.Context;

/// <summary>Input to the context assembly pipeline.</summary>
public sealed record ContextRequest
{
    /// <summary>The query to search for and assemble context around.</summary>
    public required string Query { get; init; }

    /// <summary>Maximum token budget for the assembled context. Approximated at 4 chars per token.</summary>
    public int MaxTokens { get; init; } = 8000;

    /// <summary>Maximum number of documents to include in the context package.</summary>
    public int MaxDocuments { get; init; } = 10;

    /// <summary>When true, prefer section-level content over full document text for large documents.</summary>
    public bool IncludeSections { get; init; } = true;
}
```

- [ ] **Step 5: Write ContextDocument**

```csharp
// src/Ferret.Core/Context/ContextDocument.cs
using Ferret.Core.Primitives;

namespace Ferret.Core.Context;

/// <summary>A single assembled document in a <see cref="ContextPackage"/>.</summary>
public sealed record ContextDocument
{
    /// <summary>The document identifier from the index.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>The canonical URI for this document (e.g. filesystem:///src/auth.cs).</summary>
    public required Uri CanonicalUri { get; init; }

    /// <summary>Human-readable label (e.g. relative file path).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Document title extracted by the parser, if available.</summary>
    public string? Title { get; init; }

    /// <summary>The assembled content — full document text or section text.</summary>
    public required string Content { get; init; }

    /// <summary>Relevance score from the search provider.</summary>
    public required float Score { get; init; }

    /// <summary>Estimated token count for <see cref="Content"/> using the 4-chars-per-token approximation.</summary>
    public required int TokenEstimate { get; init; }

    /// <summary>Whether this document contains full text or a single section.</summary>
    public required ContextDocumentSource Source { get; init; }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Context" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 7: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds, 0 errors.

- [ ] **Step 8: Commit**

```
git add src/Ferret.Core/Context/ContextDocumentSource.cs src/Ferret.Core/Context/ContextRequest.cs src/Ferret.Core/Context/ContextDocument.cs tests/Ferret.Core.Tests/Context/ContextRequestTests.cs tests/Ferret.Core.Tests/Context/ContextDocumentTests.cs
git commit -m "feat(sprint-13): Ferret.Core.Context value types — ContextDocumentSource, ContextRequest, ContextDocument"
```

---

### Task 2: ContextPackage and IContextAssembler

`ContextPackage` is the output of the assembly pipeline. `ToPromptString()` renders the package as a formatted string suitable for injection into an AI prompt. `IContextAssembler` is the single interface that orchestrates the pipeline.

**Files:**
- Create: `src/Ferret.Core/Context/ContextPackage.cs`
- Create: `src/Ferret.Core/Context/IContextAssembler.cs`
- Create: `tests/Ferret.Core.Tests/Context/ContextPackageTests.cs`

**Interfaces:**
- Consumes: `ContextDocument`, `ContextDocumentSource` from Task 1
- Produces: `ContextPackage`, `IContextAssembler`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Context/ContextPackageTests.cs
using Ferret.Core.Context;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Core.Tests.Context;

public sealed class ContextPackageTests
{
    private static ContextDocument MakeDoc(string id, string display, string content, float score) =>
        new()
        {
            DocumentId = DocumentId.Create(id),
            CanonicalUri = new Uri($"filesystem:///{display}"),
            DisplayName = display,
            Title = null,
            Content = content,
            Score = score,
            TokenEstimate = content.Length / 4 + 1,
            Source = ContextDocumentSource.FullDocument,
        };

    [Fact]
    public void ContextPackage_PreservesFields()
    {
        var docs = new[]
        {
            MakeDoc("doc-1", "src/auth.cs", "public class Auth { }", 0.9f),
            MakeDoc("doc-2", "src/user.cs", "public class User { }", 0.7f),
        };
        var pkg = new ContextPackage
        {
            Query = "authentication",
            Documents = docs,
            TotalTokenEstimate = 12,
            DocumentsConsidered = 5,
            DocumentsIncluded = 2,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("authentication", pkg.Query);
        Assert.Equal(2, pkg.Documents.Count);
        Assert.Equal(12, pkg.TotalTokenEstimate);
        Assert.Equal(5, pkg.DocumentsConsidered);
        Assert.Equal(2, pkg.DocumentsIncluded);
    }

    [Fact]
    public void ToPromptString_ContainsQueryAndDocuments()
    {
        var docs = new[]
        {
            MakeDoc("doc-1", "src/auth.cs", "public class Auth { }", 0.9f),
        };
        var pkg = new ContextPackage
        {
            Query = "authentication",
            Documents = docs,
            TotalTokenEstimate = 6,
            DocumentsConsidered = 1,
            DocumentsIncluded = 1,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        var result = pkg.ToPromptString();

        Assert.Contains("authentication", result);
        Assert.Contains("src/auth.cs", result);
        Assert.Contains("public class Auth", result);
    }

    [Fact]
    public void ToPromptString_NumbersDocuments()
    {
        var docs = new[]
        {
            MakeDoc("doc-1", "src/a.cs", "content a", 0.9f),
            MakeDoc("doc-2", "src/b.cs", "content b", 0.7f),
        };
        var pkg = new ContextPackage
        {
            Query = "test",
            Documents = docs,
            TotalTokenEstimate = 10,
            DocumentsConsidered = 2,
            DocumentsIncluded = 2,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        var result = pkg.ToPromptString();

        Assert.Contains("[1]", result);
        Assert.Contains("[2]", result);
    }

    [Fact]
    public void ToPromptString_EmptyDocuments_ReturnsQueryHeader()
    {
        var pkg = new ContextPackage
        {
            Query = "no results",
            Documents = [],
            TotalTokenEstimate = 0,
            DocumentsConsidered = 0,
            DocumentsIncluded = 0,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        var result = pkg.ToPromptString();

        Assert.Contains("no results", result);
        Assert.DoesNotContain("[1]", result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Context" -v n
```

Expected: compile errors — `ContextPackage` and `IContextAssembler` not found.

- [ ] **Step 3: Write ContextPackage**

```csharp
// src/Ferret.Core/Context/ContextPackage.cs
using System.Globalization;
using System.Text;

namespace Ferret.Core.Context;

/// <summary>
/// The assembled context package — the output of <see cref="IContextAssembler.AssembleAsync"/>.
/// Contains deduplicated, token-budgeted documents and provides <see cref="ToPromptString"/> for prompt injection.
/// </summary>
public sealed record ContextPackage
{
    /// <summary>The original query used to assemble this package.</summary>
    public required string Query { get; init; }

    /// <summary>The included documents, ordered by descending relevance score.</summary>
    public required IReadOnlyList<ContextDocument> Documents { get; init; }

    /// <summary>Approximate total token count across all included documents.</summary>
    public required int TotalTokenEstimate { get; init; }

    /// <summary>Total search hits considered before token budget was applied.</summary>
    public required int DocumentsConsidered { get; init; }

    /// <summary>Number of documents included after deduplication and token budget.</summary>
    public required int DocumentsIncluded { get; init; }

    /// <summary>UTC timestamp when this package was assembled.</summary>
    public required DateTimeOffset AssembledAt { get; init; }

    /// <summary>
    /// Renders the context package as a formatted string ready for injection into an AI prompt.
    /// Format:
    ///   # Context for: "{query}"
    ///   (empty line)
    ///   ## [N] {display_name} (score: {score:F3})
    ///   {content}
    ///   (empty line between documents)
    /// </summary>
    public string ToPromptString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Context for: \"{Query}\"");

        if (Documents.Count == 0)
        {
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine();

        for (var i = 0; i < Documents.Count; i++)
        {
            var doc = Documents[i];
            var label = doc.Title is not null
                ? $"{doc.DisplayName} — {doc.Title}"
                : doc.DisplayName;

            sb.AppendLine(CultureInfo.InvariantCulture,
                $"## [{i + 1}] {label} (score: {doc.Score:F3})");
            sb.AppendLine();
            sb.AppendLine(doc.Content);

            if (i < Documents.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }
}
```

- [ ] **Step 4: Write IContextAssembler**

```csharp
// src/Ferret.Core/Context/IContextAssembler.cs
namespace Ferret.Core.Context;

/// <summary>
/// Orchestrates the context assembly pipeline: search → expand → deduplicate → token-budget → package.
/// The default implementation lives in <c>Ferret.AI.ContextAssembler</c>.
/// </summary>
public interface IContextAssembler
{
    /// <summary>
    /// Assembles a context package for the given request.
    /// </summary>
    /// <param name="request">The context assembly parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ContextPackage"/> with deduplicated, token-budgeted documents.</returns>
    Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct);
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Context" -v n
```

Expected: 9 tests PASS (5 from Task 1 + 4 new = 9 total).

- [ ] **Step 6: Full solution test**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS, 0 failures.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Core/Context/ContextPackage.cs src/Ferret.Core/Context/IContextAssembler.cs tests/Ferret.Core.Tests/Context/ContextPackageTests.cs
git commit -m "feat(sprint-13): Ferret.Core.Context — ContextPackage with ToPromptString, IContextAssembler"
```

---

## Completion Checklist

After both tasks complete:

- [ ] All 9 tests in `tests/Ferret.Core.Tests/Context/` pass
- [ ] Full solution passes: `dotnet test src/Ferret.sln -v n`
- [ ] `Ferret.Core` has zero new external NuGet references (inspect `.csproj`)
- [ ] `ContextRequest.MaxTokens` defaults to 8000
- [ ] `ContextRequest.MaxDocuments` defaults to 10
- [ ] `ContextRequest.IncludeSections` defaults to true
- [ ] `ContextPackage.ToPromptString()` includes query, numbered documents, and content
- [ ] `IContextAssembler` is in `Ferret.Core.Context` namespace
- [ ] Sprint 13 s2 (Assembly Engine) can now reference `IContextAssembler`, `ContextRequest`, `ContextPackage`, `ContextDocument`

**Sub-plans unblocked by s1 completion:** s2 (Assembly Engine), s3 (MCP + CLI) — both can begin immediately after s1 merges.
