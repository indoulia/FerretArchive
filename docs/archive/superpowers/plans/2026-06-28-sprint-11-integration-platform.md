# Sprint 11 — Host Platform (MCP Runtime v1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose Ferret's platform capabilities (search, document retrieval, workspace status) through a stdio MCP runtime so any MCP-compatible AI host (Claude Code, Claude Desktop, Cursor) can query the workspace.

**Architecture:** `Ferret.Mcp` is a protocol adapter — it owns no business logic. `IMcpTool` / `IMcpResource` implementations delegate to existing platform services (`ISearchService`, `IDocumentService`, `IIndexEngine`, `IWorkspaceContext`, `IConnectorRegistry`). The `ModelContextProtocol` NuGet SDK is isolated to `Transport/Stdio/`; no SDK types leak outside that folder. `ServeCliModule` composes the MCP runtime into the CLI host via `ferret serve`.

**Tech Stack:** .NET 9, C# 13, xUnit, `Microsoft.Data.Sqlite`, `ModelContextProtocol` NuGet (stdio SDK), `System.Text.Json` (BCL), `System.CommandLine 2.0 beta`

## Global Constraints

- Sprint 10 must be fully implemented before Sprint 11. Assumes `ISearchService` in `Ferret.Core.Search`, `SqliteKeywordSearchEngine` in `Ferret.Search`, and `SearchCliModule` added to `Program.cs`.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-11):`, `test(sprint-11):`, `chore(sprint-11):`, `docs(sprint-11):`.
- No MCP SDK types (`ModelContextProtocol.*`) outside `src/Ferret.Mcp/Transport/Stdio/`.
- No `Ferret.Application` layer in Sprint 11 — MCP tools call platform services directly.
- `stdout` belongs to MCP protocol. All Ferret diagnostic output goes to `stderr`.
- Every registry is immutable after startup (built once, never mutated at runtime).
- Architecture tests must pass: `dotnet test tests/Ferret.Architecture.Tests/ -v n`.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.
- `CommandResult.Success` and `CommandResult.Failure` are static properties (no parentheses).
- DB path constant: `Path.Combine(workspaceRoot, ".ferret", "indexes", "keyword", "keyword-index.db")` — use `WorkspaceLayout.RootDirectoryName`, `IndexLayout.IndexDirectoryName`, `IndexLayout.KeywordDirectoryName`, `IndexLayout.KeywordDatabaseFileName`.

---

## File Structure Map

```
src/Ferret.Core/
  Search/
    IDocumentService.cs          [NEW — Task 1] document retrieval by ID

src/Ferret.Indexing/
  DocumentService.cs             [NEW — Task 1] SQLite implementation of IDocumentService

tests/Ferret.Indexing.Tests/
  DocumentServiceTests.cs        [NEW — Task 1]

docs/adr/
  0016-integration-platform-architecture.md   [NEW — Task 2]
  0017-mcp-runtime-architecture.md            [NEW — Task 2]
  0018-application-layer-reserved.md          [NEW — Task 2]

src/Ferret.Mcp/
  Ferret.Mcp.csproj              [MODIFY — Task 3] add NuGet ref + InternalsVisibleTo

  Protocol/
    McpArguments.cs              [NEW — Task 4] Ferret-owned arg container (no SDK types)
    McpContent.cs                [NEW — Task 4] MCP content item
    McpToolResult.cs             [NEW — Task 4] tool execution result
    McpResourceContent.cs        [NEW — Task 4] resource read result
    McpToolDescriptor.cs         [NEW — Task 4] tool metadata
    McpResourceDescriptor.cs     [NEW — Task 4] resource metadata
    McpTransportDescriptor.cs    [NEW — Task 4] transport metadata
    IMcpTool.cs                  [NEW — Task 4] tool execution contract
    IMcpResource.cs              [NEW — Task 4] resource read contract
    IMcpTransport.cs             [NEW — Task 4] transport run contract
    IMcpRuntime.cs               [NEW — Task 4] runtime run contract
    IMcpErrorMapper.cs           [NEW — Task 4] exception → MCP error contract

  Registry/
    IMcpToolRegistry.cs          [NEW — Task 5] tool lookup contract
    IMcpResourceRegistry.cs      [NEW — Task 5] resource lookup contract
    McpToolRegistryBuilder.cs    [NEW — Task 5] internal fluent builder
    McpResourceRegistryBuilder.cs [NEW — Task 5]
    McpToolRegistry.cs           [NEW — Task 5] sealed immutable impl
    McpResourceRegistry.cs       [NEW — Task 5]

  Tools/
    SearchTool.cs                [NEW — Task 6] delegates to ISearchService
    ReadDocumentTool.cs          [NEW — Task 6] delegates to IDocumentService
    WorkspaceStatusTool.cs       [NEW — Task 6] delegates to IWorkspaceContext + IIndexEngine

  Resources/
    WorkspaceStatusResource.cs   [NEW — Task 7]
    IndexStatsResource.cs        [NEW — Task 7]
    ConnectorsResource.cs        [NEW — Task 7]

  Transport/
    Stdio/
      McpArgumentsFactory.cs     [NEW — Task 8] SDK JsonElement → McpArguments
      SdkToolAdapter.cs          [NEW — Task 8] IMcpTool → SDK handler
      SdkResourceAdapter.cs      [NEW — Task 8] IMcpResource → SDK handler
      McpErrorMapper.cs          [NEW — Task 8] exception → McpToolResult.Error
      SdkRuntimeAdapter.cs       [NEW — Task 8] creates + wires SDK McpServer
      StdioTransport.cs          [NEW — Task 8] implements IMcpTransport via SDK stdio

  Runtime/
    McpRuntime.cs                [NEW — Task 9] implements IMcpRuntime

  McpModule.cs                   [MODIFY — Task 10] becomes real DI composition root

tests/Ferret.Mcp.Tests/
  Protocol/
    McpArgumentsTests.cs         [NEW — Task 4]
    McpToolResultTests.cs        [NEW — Task 4]
  Registry/
    McpToolRegistryTests.cs      [NEW — Task 5]
    McpResourceRegistryTests.cs  [NEW — Task 5]
  Tools/
    SearchToolTests.cs           [NEW — Task 6]
    ReadDocumentToolTests.cs     [NEW — Task 6]
    WorkspaceStatusToolTests.cs  [NEW — Task 6]
  Resources/
    WorkspaceStatusResourceTests.cs [NEW — Task 7]
    IndexStatsResourceTests.cs      [NEW — Task 7]
    ConnectorsResourceTests.cs      [NEW — Task 7]
  Runtime/
    McpRuntimeTests.cs           [NEW — Task 9]

src/Ferret.Cli/
  Commands/Serve/
    ServeCliModule.cs            [NEW — Task 11]
    ServeCommandHandler.cs       [NEW — Task 11]
  Program.cs                     [MODIFY — Task 11]
  Ferret.Cli.csproj              [MODIFY — Task 11] add Ferret.Mcp ref

tests/Ferret.Architecture.Tests/
  McpArchitectureTests.cs        [NEW — Task 12]

tests/Ferret.Mcp.Tests/
  Integration/
    McpHostIntegrationTests.cs   [NEW — Task 13]
```

---

### Task 4: Protocol Contracts — Ferret-Owned Types and Interfaces

Defines all Ferret-owned MCP types. Nothing in this task references `ModelContextProtocol.*`. Every interface, record, and class here is pure Ferret domain. These are the contracts that tools, resources, registries, and the runtime all program against.

**Files:**
- Create: `src/Ferret.Mcp/Protocol/McpArguments.cs`
- Create: `src/Ferret.Mcp/Protocol/McpContent.cs`
- Create: `src/Ferret.Mcp/Protocol/McpToolResult.cs`
- Create: `src/Ferret.Mcp/Protocol/McpResourceContent.cs`
- Create: `src/Ferret.Mcp/Protocol/McpToolDescriptor.cs`
- Create: `src/Ferret.Mcp/Protocol/McpResourceDescriptor.cs`
- Create: `src/Ferret.Mcp/Protocol/McpTransportDescriptor.cs`
- Create: `src/Ferret.Mcp/Protocol/IMcpTool.cs`
- Create: `src/Ferret.Mcp/Protocol/IMcpResource.cs`
- Create: `src/Ferret.Mcp/Protocol/IMcpTransport.cs`
- Create: `src/Ferret.Mcp/Protocol/IMcpRuntime.cs`
- Create: `src/Ferret.Mcp/Protocol/IMcpErrorMapper.cs`
- Create: `tests/Ferret.Mcp.Tests/Protocol/McpArgumentsTests.cs`
- Create: `tests/Ferret.Mcp.Tests/Protocol/McpToolResultTests.cs`

**Interfaces:**
- Consumes: nothing external
- Produces: `McpArguments`, `McpContent`, `McpToolResult`, `McpResourceContent`, `McpToolDescriptor`, `McpResourceDescriptor`, `McpTransportDescriptor`, `IMcpTool`, `IMcpResource`, `IMcpTransport`, `IMcpRuntime`, `IMcpErrorMapper`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Mcp.Tests/Protocol/McpArgumentsTests.cs
using Ferret.Mcp.Protocol;
using Xunit;

namespace Ferret.Mcp.Tests.Protocol;

public sealed class McpArgumentsTests
{
    [Fact]
    public void GetString_ExistingKey_ReturnsValue()
    {
        var args = McpArguments.From(("key", "value"));
        Assert.Equal("value", args.GetString("key"));
    }

    [Fact]
    public void GetString_MissingKey_ReturnsNull()
    {
        var args = McpArguments.Empty;
        Assert.Null(args.GetString("missing"));
    }

    [Fact]
    public void GetRequiredString_MissingKey_Throws()
    {
        var args = McpArguments.Empty;
        Assert.Throws<InvalidOperationException>(() => args.GetRequiredString("required"));
    }

    [Fact]
    public void TryGetInt32_ValidInteger_ReturnsTrueAndValue()
    {
        var args = McpArguments.From(("count", "42"));
        Assert.True(args.TryGetInt32("count", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetInt32_MissingKey_ReturnsFalse()
    {
        var args = McpArguments.Empty;
        Assert.False(args.TryGetInt32("missing", out _));
    }
}
```

```csharp
// tests/Ferret.Mcp.Tests/Protocol/McpToolResultTests.cs
using Ferret.Mcp.Protocol;
using Xunit;

namespace Ferret.Mcp.Tests.Protocol;

public sealed class McpToolResultTests
{
    [Fact]
    public void Success_SetsIsErrorFalse_AndTextContent()
    {
        var result = McpToolResult.Success("hello");
        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal("text", result.Content[0].Type);
        Assert.Equal("hello", result.Content[0].Text);
    }

    [Fact]
    public void Error_SetsIsErrorTrue_AndTextContent()
    {
        var result = McpToolResult.Error("bad input");
        Assert.True(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal("bad input", result.Content[0].Text);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Protocol" -v n
```

Expected: compile errors — types not found.

- [ ] **Step 3: Write all protocol types**

```csharp
// src/Ferret.Mcp/Protocol/McpArguments.cs
namespace Ferret.Mcp.Protocol;

public sealed class McpArguments
{
    private readonly IReadOnlyDictionary<string, string> _values;

    internal McpArguments(IReadOnlyDictionary<string, string> values) => _values = values;

    public static McpArguments Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    internal static McpArguments From(params (string key, string value)[] pairs) =>
        new(pairs.ToDictionary(p => p.key, p => p.value, StringComparer.Ordinal));

    public string? GetString(string name) =>
        _values.TryGetValue(name, out var v) ? v : null;

    public string GetRequiredString(string name) =>
        GetString(name) ?? throw new InvalidOperationException($"Required MCP argument '{name}' is missing.");

    public bool TryGetInt32(string name, out int value)
    {
        var s = GetString(name);
        return int.TryParse(s, out value);
    }
}
```

```csharp
// src/Ferret.Mcp/Protocol/McpContent.cs
namespace Ferret.Mcp.Protocol;

public sealed record McpContent
{
    public required string Type { get; init; }
    public string? Text { get; init; }

    public static McpContent FromText(string text) => new() { Type = "text", Text = text };
}
```

```csharp
// src/Ferret.Mcp/Protocol/McpToolResult.cs
namespace Ferret.Mcp.Protocol;

public sealed record McpToolResult
{
    public required IReadOnlyList<McpContent> Content { get; init; }
    public bool IsError { get; init; }

    public static McpToolResult Success(string text) =>
        new() { Content = [McpContent.FromText(text)], IsError = false };

    public static McpToolResult Error(string message) =>
        new() { Content = [McpContent.FromText(message)], IsError = true };
}
```

```csharp
// src/Ferret.Mcp/Protocol/McpResourceContent.cs
namespace Ferret.Mcp.Protocol;

public sealed record McpResourceContent
{
    public required string ResourceUri { get; init; }
    public required string MimeType { get; init; }
    public required string Text { get; init; }
}
```

```csharp
// src/Ferret.Mcp/Protocol/McpToolDescriptor.cs
namespace Ferret.Mcp.Protocol;

public sealed record McpToolDescriptor
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? InputSchemaJson { get; init; }
}
```

```csharp
// src/Ferret.Mcp/Protocol/McpResourceDescriptor.cs
namespace Ferret.Mcp.Protocol;

public sealed record McpResourceDescriptor
{
    public required string ResourceUri { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string MimeType { get; init; } = "application/json";
}
```

```csharp
// src/Ferret.Mcp/Protocol/McpTransportDescriptor.cs
namespace Ferret.Mcp.Protocol;

public sealed record McpTransportDescriptor
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}
```

```csharp
// src/Ferret.Mcp/Protocol/IMcpTool.cs
namespace Ferret.Mcp.Protocol;

public interface IMcpTool
{
    McpToolDescriptor Descriptor { get; }
    Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct);
}
```

```csharp
// src/Ferret.Mcp/Protocol/IMcpResource.cs
namespace Ferret.Mcp.Protocol;

public interface IMcpResource
{
    McpResourceDescriptor Descriptor { get; }
    Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct);
}
```

```csharp
// src/Ferret.Mcp/Protocol/IMcpTransport.cs
using Ferret.Mcp.Registry;

namespace Ferret.Mcp.Protocol;

public interface IMcpTransport
{
    McpTransportDescriptor Descriptor { get; }
    Task RunAsync(IMcpToolRegistry tools, IMcpResourceRegistry resources, CancellationToken ct);
}
```

```csharp
// src/Ferret.Mcp/Protocol/IMcpRuntime.cs
namespace Ferret.Mcp.Protocol;

public interface IMcpRuntime
{
    Task RunAsync(CancellationToken ct);
}
```

```csharp
// src/Ferret.Mcp/Protocol/IMcpErrorMapper.cs
namespace Ferret.Mcp.Protocol;

public interface IMcpErrorMapper
{
    McpToolResult MapException(Exception ex);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Protocol" -v n
```

Expected: 7 tests PASS.

- [ ] **Step 5: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.Mcp/Protocol/ tests/Ferret.Mcp.Tests/Protocol/
git commit -m "feat(sprint-11): Ferret.Mcp protocol contracts — McpArguments, IMcpTool, IMcpResource, IMcpTransport, IMcpRuntime"
```

---

### Task 5: Registry — IMcpToolRegistry + IMcpResourceRegistry

Immutable registries built once at startup. Public interfaces; internal builders and implementations. Tools and resources are added at composition time; nothing can be added at runtime.

**Files:**
- Create: `src/Ferret.Mcp/Registry/IMcpToolRegistry.cs`
- Create: `src/Ferret.Mcp/Registry/IMcpResourceRegistry.cs`
- Create: `src/Ferret.Mcp/Registry/McpToolRegistryBuilder.cs`
- Create: `src/Ferret.Mcp/Registry/McpResourceRegistryBuilder.cs`
- Create: `src/Ferret.Mcp/Registry/McpToolRegistry.cs`
- Create: `src/Ferret.Mcp/Registry/McpResourceRegistry.cs`
- Create: `tests/Ferret.Mcp.Tests/Registry/McpToolRegistryTests.cs`
- Create: `tests/Ferret.Mcp.Tests/Registry/McpResourceRegistryTests.cs`

**Interfaces:**
- Consumes: `IMcpTool`, `IMcpResource`, `McpToolDescriptor`, `McpResourceDescriptor` from Task 4
- Produces: `IMcpToolRegistry`, `IMcpResourceRegistry`, `McpToolRegistryBuilder`, `McpResourceRegistryBuilder`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Mcp.Tests/Registry/McpToolRegistryTests.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using Xunit;

namespace Ferret.Mcp.Tests.Registry;

public sealed class McpToolRegistryTests
{
    private static IMcpTool MakeTool(string name) => new FakeTool(name);

    [Fact]
    public void GetAll_ReturnsAllDescriptors()
    {
        var registry = new McpToolRegistryBuilder()
            .Add(MakeTool("search"))
            .Add(MakeTool("read_document"))
            .Build();

        var all = registry.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, d => d.Name == "search");
        Assert.Contains(all, d => d.Name == "read_document");
    }

    [Fact]
    public void GetByName_ExistingTool_ReturnsTool()
    {
        var registry = new McpToolRegistryBuilder().Add(MakeTool("search")).Build();
        Assert.NotNull(registry.GetByName("search"));
    }

    [Fact]
    public void GetByName_MissingTool_ReturnsNull()
    {
        var registry = new McpToolRegistryBuilder().Build();
        Assert.Null(registry.GetByName("not_found"));
    }

    private sealed class FakeTool(string name) : IMcpTool
    {
        public McpToolDescriptor Descriptor { get; } = new() { Name = name, Description = "test" };
        public Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct) =>
            Task.FromResult(McpToolResult.Success("ok"));
    }
}
```

```csharp
// tests/Ferret.Mcp.Tests/Registry/McpResourceRegistryTests.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using Xunit;

namespace Ferret.Mcp.Tests.Registry;

public sealed class McpResourceRegistryTests
{
    private static IMcpResource MakeResource(string uri, string name) => new FakeResource(uri, name);

    [Fact]
    public void GetAll_ReturnsAllDescriptors()
    {
        var registry = new McpResourceRegistryBuilder()
            .Add(MakeResource("workspace://status", "workspace_status"))
            .Build();

        var all = registry.GetAll();
        Assert.Single(all);
        Assert.Equal("workspace://status", all[0].ResourceUri);
    }

    [Fact]
    public void GetByUri_ExistingResource_ReturnsResource()
    {
        var registry = new McpResourceRegistryBuilder()
            .Add(MakeResource("workspace://status", "workspace_status"))
            .Build();

        Assert.NotNull(registry.GetByUri("workspace://status"));
    }

    [Fact]
    public void GetByUri_MissingResource_ReturnsNull()
    {
        var registry = new McpResourceRegistryBuilder().Build();
        Assert.Null(registry.GetByUri("workspace://none"));
    }

    private sealed class FakeResource(string uri, string name) : IMcpResource
    {
        public McpResourceDescriptor Descriptor { get; } = new()
        {
            ResourceUri = uri, Name = name, Description = "test"
        };
        public Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct) =>
            Task.FromResult(new McpResourceContent { ResourceUri = resourceUri, MimeType = "application/json", Text = "{}" });
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Registry" -v n
```

Expected: compile errors.

- [ ] **Step 3: Write registry interfaces**

```csharp
// src/Ferret.Mcp/Registry/IMcpToolRegistry.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

public interface IMcpToolRegistry
{
    IReadOnlyList<McpToolDescriptor> GetAll();
    IMcpTool? GetByName(string name);
}
```

```csharp
// src/Ferret.Mcp/Registry/IMcpResourceRegistry.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

public interface IMcpResourceRegistry
{
    IReadOnlyList<McpResourceDescriptor> GetAll();
    IMcpResource? GetByUri(string resourceUri);
}
```

- [ ] **Step 4: Write registry implementations**

```csharp
// src/Ferret.Mcp/Registry/McpToolRegistryBuilder.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

internal sealed class McpToolRegistryBuilder
{
    private readonly List<IMcpTool> _tools = [];

    internal McpToolRegistryBuilder Add(IMcpTool tool)
    {
        _tools.Add(tool);
        return this;
    }

    internal IMcpToolRegistry Build() => new McpToolRegistry(_tools);
}
```

```csharp
// src/Ferret.Mcp/Registry/McpToolRegistry.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

internal sealed class McpToolRegistry : IMcpToolRegistry
{
    private readonly IReadOnlyList<McpToolDescriptor> _descriptors;
    private readonly IReadOnlyDictionary<string, IMcpTool> _byName;

    internal McpToolRegistry(IEnumerable<IMcpTool> tools)
    {
        var list = tools.ToList();
        _descriptors = list.Select(t => t.Descriptor).ToList();
        _byName = list.ToDictionary(t => t.Descriptor.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<McpToolDescriptor> GetAll() => _descriptors;

    public IMcpTool? GetByName(string name) =>
        _byName.TryGetValue(name, out var tool) ? tool : null;
}
```

```csharp
// src/Ferret.Mcp/Registry/McpResourceRegistryBuilder.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

internal sealed class McpResourceRegistryBuilder
{
    private readonly List<IMcpResource> _resources = [];

    internal McpResourceRegistryBuilder Add(IMcpResource resource)
    {
        _resources.Add(resource);
        return this;
    }

    internal IMcpResourceRegistry Build() => new McpResourceRegistry(_resources);
}
```

```csharp
// src/Ferret.Mcp/Registry/McpResourceRegistry.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

internal sealed class McpResourceRegistry : IMcpResourceRegistry
{
    private readonly IReadOnlyList<McpResourceDescriptor> _descriptors;
    private readonly IReadOnlyDictionary<string, IMcpResource> _byUri;

    internal McpResourceRegistry(IEnumerable<IMcpResource> resources)
    {
        var list = resources.ToList();
        _descriptors = list.Select(r => r.Descriptor).ToList();
        _byUri = list.ToDictionary(r => r.Descriptor.ResourceUri, StringComparer.Ordinal);
    }

    public IReadOnlyList<McpResourceDescriptor> GetAll() => _descriptors;

    public IMcpResource? GetByUri(string resourceUri) =>
        _byUri.TryGetValue(resourceUri, out var resource) ? resource : null;
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Registry" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Mcp/Registry/ tests/Ferret.Mcp.Tests/Registry/
git commit -m "feat(sprint-11): Ferret.Mcp registry — IMcpToolRegistry, IMcpResourceRegistry, immutable impls"
```

---

### Task 6: MCP Tools — SearchTool, ReadDocumentTool, WorkspaceStatusTool

Three `IMcpTool` implementations. Each is a thin adapter: receives `McpArguments`, calls a platform service, formats the result as `McpToolResult`. No MCP SDK types referenced here.

**Files:**
- Create: `src/Ferret.Mcp/Tools/SearchTool.cs`
- Create: `src/Ferret.Mcp/Tools/ReadDocumentTool.cs`
- Create: `src/Ferret.Mcp/Tools/WorkspaceStatusTool.cs`
- Create: `tests/Ferret.Mcp.Tests/Tools/SearchToolTests.cs`
- Create: `tests/Ferret.Mcp.Tests/Tools/ReadDocumentToolTests.cs`
- Create: `tests/Ferret.Mcp.Tests/Tools/WorkspaceStatusToolTests.cs`

**Interfaces:**
- Consumes (from Sprint 10): `ISearchService.SearchAsync(string rawQuery, SearchOptions options, CancellationToken ct) → Task<SearchServiceResult>`; `SearchServiceResult.Hits (IReadOnlyList<SearchHit>)`, `SearchHit.DocumentId`, `SearchHit.CanonicalUri (string)`, `SearchHit.Score (double)`, `SearchHit.Snippet (string)`, `SearchHit.Title (string?)`, `SearchOptions.MaxResults (int)`, `SearchOptions.HighlightEnabled (bool)`
- Consumes (Task 1): `IDocumentService.GetAsync(DocumentId, CancellationToken) → Task<Document?>`
- Consumes (existing): `IWorkspaceContext`, `IIndexEngine.GetStatsAsync(CancellationToken) → Task<IndexStats>`, `IndexStats.DocumentCount`, `IndexStats.IndexSizeBytes`, `IndexStats.LastIndexedAt`, `IndexStats.TotalChars`
- Produces: `SearchTool : IMcpTool`, `ReadDocumentTool : IMcpTool`, `WorkspaceStatusTool : IMcpTool`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Mcp.Tests/Tools/SearchToolTests.cs
using Ferret.Core.Documents;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class SearchToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithResults_ReturnsFormattedHits()
    {
        var service = new FakeSearchService([new SearchHit
        {
            DocumentId = DocumentId.Create("doc-1"),
            CanonicalUri = "file:///src/Main.cs",
            Score = 0.9,
            Snippet = "some relevant code",
            Title = "Main.cs"
        }]);
        var sut = new SearchTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("query", "relevant")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("Main.cs", result.Content[0].Text);
        Assert.Contains("relevant code", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_NoResults_ReturnsNoResultsMessage()
    {
        var service = new FakeSearchService([]);
        var sut = new SearchTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("query", "nothing")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("No results", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_MissingQueryArgument_Throws()
    {
        var sut = new SearchTool(new FakeSearchService([]));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None));
    }

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new SearchTool(new FakeSearchService([]));
        Assert.Equal("search", sut.Descriptor.Name);
    }

    private sealed class FakeSearchService(IReadOnlyList<SearchHit> hits) : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options, CancellationToken ct) =>
            Task.FromResult(new SearchServiceResult
            {
                Hits = hits,
                TotalCount = hits.Count,
                Query = rawQuery,
                ElapsedTime = TimeSpan.Zero
            });
    }
}
```

```csharp
// tests/Ferret.Mcp.Tests/Tools/ReadDocumentToolTests.cs
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class ReadDocumentToolTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingDocument_ReturnsContent()
    {
        var doc = MakeDocument("doc-1", "hello world", "Hello");
        var service = new FakeDocumentService(doc);
        var sut = new ReadDocumentTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("document_id", "doc-1")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("hello world", result.Content[0].Text);
        Assert.Contains("Hello", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_MissingDocument_ReturnsError()
    {
        var service = new FakeDocumentService(null);
        var sut = new ReadDocumentTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("document_id", "missing")), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content[0].Text);
    }

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new ReadDocumentTool(new FakeDocumentService(null));
        Assert.Equal("read_document", sut.Descriptor.Name);
    }

    private static Document MakeDocument(string id, string plainText, string title) => new()
    {
        Id = DocumentId.Create(id),
        SourceAssetId = AssetId.Create(id),
        ConnectorId = ConnectorId.Create("fs"),
        InstanceId = ConnectorInstanceId.Create("fs-1"),
        MediaType = "text/plain",
        Kind = DocumentKind.Code,
        PlainText = plainText,
        Title = title,
        ProducedAt = DateTimeOffset.UtcNow
    };

    private sealed class FakeDocumentService(Document? document) : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct) =>
            Task.FromResult(document);
    }
}
```

```csharp
// tests/Ferret.Mcp.Tests/Tools/WorkspaceStatusToolTests.cs
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class WorkspaceStatusToolTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsJsonWithWorkspaceInfo()
    {
        var context = new FakeWorkspaceContext();
        var engine = new FakeIndexEngine(new IndexStats
        {
            DocumentCount = 42,
            TotalChars = 100000,
            LastIndexedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
            IndexSizeBytes = 512000
        });
        var sut = new WorkspaceStatusTool(context, engine);

        var result = await sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None);

        Assert.False(result.IsError);
        var text = result.Content[0].Text!;
        Assert.Contains("42", text);
        Assert.Contains("test-workspace", text);
    }

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new WorkspaceStatusTool(new FakeWorkspaceContext(), new FakeIndexEngine(default!));
        Assert.Equal("workspace_status", sut.Descriptor.Name);
    }

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test-workspace");
        public WorkspacePath WorkspaceRoot => WorkspacePath.Create(Path.GetTempPath());
    }

    private sealed class FakeIndexEngine(IndexStats stats) : IIndexEngine
    {
        public Task WriteAsync(Document doc, CancellationToken ct) => Task.CompletedTask;
        public Task<IndexStats> GetStatsAsync(CancellationToken ct) => Task.FromResult(stats);
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Tools" -v n
```

Expected: compile errors — tool classes not found.

- [ ] **Step 3: Write SearchTool**

```csharp
// src/Ferret.Mcp/Tools/SearchTool.cs
using System.Text;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

public sealed class SearchTool : IMcpTool
{
    private readonly ISearchService _searchService;

    public SearchTool(ISearchService searchService)
    {
        ArgumentNullException.ThrowIfNull(searchService);
        _searchService = searchService;
    }

    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "search",
        Description = "Search the Ferret workspace index for relevant documents and code.",
        InputSchemaJson = """{"type":"object","properties":{"query":{"type":"string","description":"Full-text search query"},"max_results":{"type":"integer","description":"Maximum results to return (default: 10)"}},"required":["query"]}"""
    };

    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        var query = arguments.GetRequiredString("query");
        var maxResults = arguments.TryGetInt32("max_results", out var n) ? n : 10;

        var options = new SearchOptions { MaxResults = maxResults, HighlightEnabled = true };
        var result = await _searchService.SearchAsync(query, options, ct).ConfigureAwait(false);

        if (result.Hits.Count == 0)
            return McpToolResult.Success($"No results found for: {query}");

        var sb = new StringBuilder();
        sb.AppendLine($"Found {result.Hits.Count} result(s) for: {query}");
        sb.AppendLine();

        for (var i = 0; i < result.Hits.Count; i++)
        {
            var hit = result.Hits[i];
            sb.AppendLine($"[{i + 1}] {hit.Title ?? hit.DocumentId.Value}");
            sb.AppendLine($"    URI: {hit.CanonicalUri}");
            sb.AppendLine($"    Score: {hit.Score:F3}");
            sb.AppendLine($"    {hit.Snippet}");
            sb.AppendLine();
        }

        return McpToolResult.Success(sb.ToString().TrimEnd());
    }
}
```

- [ ] **Step 4: Write ReadDocumentTool**

```csharp
// src/Ferret.Mcp/Tools/ReadDocumentTool.cs
using System.Text;
using Ferret.Core.Documents;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

public sealed class ReadDocumentTool : IMcpTool
{
    private readonly IDocumentService _documentService;

    public ReadDocumentTool(IDocumentService documentService)
    {
        ArgumentNullException.ThrowIfNull(documentService);
        _documentService = documentService;
    }

    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "read_document",
        Description = "Retrieve the full text content of a document by its ID (obtained from the search tool).",
        InputSchemaJson = """{"type":"object","properties":{"document_id":{"type":"string","description":"Document ID from a search result"}},"required":["document_id"]}"""
    };

    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        var rawId = arguments.GetRequiredString("document_id");
        var id = DocumentId.Create(rawId);

        var document = await _documentService.GetAsync(id, ct).ConfigureAwait(false);
        if (document is null)
            return McpToolResult.Error($"Document not found: {rawId}");

        var sb = new StringBuilder();
        if (document.Title is not null)
        {
            sb.AppendLine($"# {document.Title}");
            sb.AppendLine();
        }
        sb.Append(document.PlainText);

        return McpToolResult.Success(sb.ToString().TrimEnd());
    }
}
```

- [ ] **Step 5: Write WorkspaceStatusTool**

```csharp
// src/Ferret.Mcp/Tools/WorkspaceStatusTool.cs
using System.Text.Json;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

public sealed class WorkspaceStatusTool : IMcpTool
{
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IIndexEngine _indexEngine;

    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public WorkspaceStatusTool(IWorkspaceContext workspaceContext, IIndexEngine indexEngine)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(indexEngine);
        _workspaceContext = workspaceContext;
        _indexEngine = indexEngine;
    }

    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "workspace_status",
        Description = "Get the current Ferret workspace status including index statistics.",
        InputSchemaJson = """{"type":"object","properties":{}}"""
    };

    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        var stats = await _indexEngine.GetStatsAsync(ct).ConfigureAwait(false);

        var payload = new
        {
            workspaceId = _workspaceContext.WorkspaceId.Value,
            workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath,
            documentCount = stats.DocumentCount,
            indexSizeBytes = stats.IndexSizeBytes,
            lastIndexedAt = stats.LastIndexedAt,
            totalChars = stats.TotalChars
        };

        return McpToolResult.Success(JsonSerializer.Serialize(payload, s_jsonOptions));
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Tools" -v n
```

Expected: 8 tests PASS.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Mcp/Tools/ tests/Ferret.Mcp.Tests/Tools/
git commit -m "feat(sprint-11): MCP tools — SearchTool, ReadDocumentTool, WorkspaceStatusTool"
```

---

### Task 1: Sprint 10 Reconciliation — IDocumentService + DocumentService

Adds document retrieval by ID — a cross-platform service needed by MCP's `read_document` tool and future REST/Agent hosts. `IDocumentService` is a platform service (not MCP-specific); it lives in `Ferret.Core.Search` alongside `ISearchService`. The implementation queries the existing SQLite `documents` table written by Sprint 9.

**Files:**
- Create: `src/Ferret.Core/Search/IDocumentService.cs`
- Create: `src/Ferret.Indexing/DocumentService.cs`
- Create: `tests/Ferret.Indexing.Tests/DocumentServiceTests.cs`

**Interfaces:**
- Consumes: `DocumentId` (Ferret.Core.Documents), `Document` (Ferret.Core.Documents), `Microsoft.Data.Sqlite` (already in Ferret.Indexing)
- Produces: `IDocumentService.GetAsync(DocumentId id, CancellationToken ct) → Task<Document?>`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Ferret.Indexing.Tests/DocumentServiceTests.cs
using Ferret.Core.Documents;
using Ferret.Core.Search;
using Ferret.Indexing;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Indexing.Tests;

public sealed class DocumentServiceTests : IAsyncDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _connection;

    public DocumentServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"docservice-test-{Guid.NewGuid()}.db");
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        CreateSchema(_connection);
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS documents (
                id TEXT NOT NULL PRIMARY KEY,
                connector_id TEXT NOT NULL,
                instance_id TEXT NOT NULL,
                media_type TEXT NOT NULL,
                kind INTEGER NOT NULL,
                plain_text TEXT NOT NULL,
                title TEXT,
                produced_at INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static void SeedDocument(SqliteConnection connection, string id, string plainText, string? title = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO documents (id, connector_id, instance_id, media_type, kind, plain_text, title, produced_at)
            VALUES ($id, 'fs', 'fs-1', 'text/plain', 0, $text, $title, $ts)
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$text", plainText);
        cmd.Parameters.AddWithValue("$title", (object?)title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task GetAsync_ExistingDocument_ReturnsDocument()
    {
        SeedDocument(_connection, "doc-001", "hello world", "Hello");
        _connection.Close(); // allow DocumentService to open its own connection

        var sut = new DocumentService(_dbPath);
        var doc = await sut.GetAsync(DocumentId.Create("doc-001"), CancellationToken.None);

        Assert.NotNull(doc);
        Assert.Equal("doc-001", doc.Id.Value);
        Assert.Equal("hello world", doc.PlainText);
        Assert.Equal("Hello", doc.Title);
    }

    [Fact]
    public async Task GetAsync_MissingDocument_ReturnsNull()
    {
        _connection.Close();

        var sut = new DocumentService(_dbPath);
        var doc = await sut.GetAsync(DocumentId.Create("no-such-doc"), CancellationToken.None);

        Assert.Null(doc);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "FullyQualifiedName~DocumentServiceTests" -v n
```

Expected: compile error — `IDocumentService`, `DocumentService`, `DocumentId.Create` not found.

- [ ] **Step 3: Write IDocumentService**

```csharp
// src/Ferret.Core/Search/IDocumentService.cs
using Ferret.Core.Documents;

namespace Ferret.Core.Search;

public interface IDocumentService
{
    Task<Document?> GetAsync(DocumentId id, CancellationToken ct);
}
```

- [ ] **Step 4: Write DocumentService**

```csharp
// src/Ferret.Indexing/DocumentService.cs
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Search;
using Microsoft.Data.Sqlite;

namespace Ferret.Indexing;

public sealed class DocumentService : IDocumentService
{
    private readonly string _dbPath;

    public DocumentService(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        _dbPath = dbPath;
    }

    public async Task<Document?> GetAsync(DocumentId id, CancellationToken ct)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT id, connector_id, instance_id, media_type, kind, plain_text, title, produced_at " +
            "FROM documents WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return new Document
        {
            Id = DocumentId.Create(reader.GetString(0)),
            ConnectorId = ConnectorId.Create(reader.GetString(1)),
            InstanceId = ConnectorInstanceId.Create(reader.GetString(2)),
            SourceAssetId = AssetId.Create(reader.GetString(0)), // Sprint 9: id == SourceAssetId
            MediaType = reader.GetString(3),
            Kind = (DocumentKind)reader.GetInt32(4),
            PlainText = reader.GetString(5),
            Title = reader.IsDBNull(6) ? null : reader.GetString(6),
            ProducedAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)),
        };
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "FullyQualifiedName~DocumentServiceTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 6: Verify full solution still compiles and tests pass**

```
dotnet test src/Ferret.sln -v n
```

- [ ] **Step 7: Commit**

```
git add src/Ferret.Core/Search/IDocumentService.cs src/Ferret.Indexing/DocumentService.cs tests/Ferret.Indexing.Tests/DocumentServiceTests.cs
git commit -m "feat(sprint-11): IDocumentService + DocumentService — document retrieval by ID"
```

---

### Task 2: ADRs — 0016, 0017, 0018

Documentation-only task. Three ADR files establish the architectural foundation for Sprint 11 and reserve namespace for future sprints. No tests required.

**Files:**
- Create: `docs/adr/0016-integration-platform-architecture.md`
- Create: `docs/adr/0017-mcp-runtime-architecture.md`
- Create: `docs/adr/0018-application-layer-reserved.md`

- [ ] **Step 1: Write ADR-0016**

```markdown
# ADR-0016 — Integration Platform Architecture

**Status:** Accepted  
**Date:** 2026-06-28  
**Sprint:** 11

## Context

Ferret needs to expose its platform capabilities to external AI hosts (Claude Code, Claude Desktop, Cursor). Adding MCP directly risks treating it as the architecture rather than an adapter.

## Decision

Adopt the **Ferret Host Architecture Pattern**: `Capabilities → Platform Services → Hosts → Protocols`.

**8+1 Architectural Principles:**

1. **Platform services are host-independent.** `ISearchService`, `IDocumentService`, `IIndexEngine`, and `IWorkspaceContext` know nothing about MCP, REST, or any other protocol.
2. **Hosts are adapters.** A Host translates a protocol request into a platform service call. It owns translation; it does not own logic.
3. **One integration technology per package.** `Ferret.Mcp` contains only MCP. A future `Ferret.Rest` would be separate.
4. **External SDKs are quarantined.** SDK types from `ModelContextProtocol` are confined to `Transport/Stdio/`. Nothing outside that folder imports SDK namespaces.
5. **Contracts are Ferret-owned.** `IMcpTool`, `IMcpResource`, `McpArguments`, `McpToolResult` are Ferret types. The SDK adapter translates between Ferret contracts and SDK wire types.
6. **Capabilities are host-independent.** Adding a new Host does not change platform service interfaces.
7. **Protocol translation is one-to-one.** Each SDK request maps to exactly one Ferret call. Adapters do not aggregate, cache, or orchestrate.
8. **Hosts are launchers, not owners.** `ServeCliModule` starts the runtime; it does not own or embed it. `ferret serve` is a launcher, not a server.

**Principle 9 (Platform First):** When evaluating any feature request, ask "does this belong in the Platform (usable by all hosts) or in the Host (specific to one protocol)?" Default to Platform.

## Consequences

- `Ferret.Application` layer deferred until Sprint 12/13 (see ADR-0018).
- All future hosts (REST, Web UI, Agent) follow the same adapter pattern.
- Architecture tests enforce that no SDK types leak outside `Transport/Stdio/`.

## Milestone

M3 — Multi-Host Platform checkpoint after Sprint 11.
```

- [ ] **Step 2: Write ADR-0017**

```markdown
# ADR-0017 — MCP Runtime Architecture

**Status:** Accepted  
**Date:** 2026-06-28  
**Sprint:** 11

## Context

Sprint 11 delivers `Ferret.Mcp`: an MCP stdio runtime. Key decisions about transport isolation, registry design, and SDK boundary must be recorded.

## Decision

1. **Stdio transport only in Sprint 11.** HTTP transport reserved for a future sprint (`Transport/Http/`).
2. **SDK confined to `Transport/Stdio/`.** `McpArgumentsFactory`, `SdkToolAdapter`, `SdkResourceAdapter`, `SdkRuntimeAdapter`, `StdioTransport` are the only files that import `ModelContextProtocol.*` namespaces.
3. **Immutable registries.** `IMcpToolRegistry` and `IMcpResourceRegistry` are built once at startup via internal builders and are never mutated at runtime.
4. **Stateless adapters.** SDK adapter classes (`SdkToolAdapter`, `SdkResourceAdapter`) are stateless static translators. No shared mutable state.
5. **Runtime independence.** `McpRuntime` depends on `IMcpTransport` (Ferret interface), not on SDK types. Swapping the transport does not change the runtime.
6. **Startup validation.** `McpRuntime.RunAsync` validates registries before starting the transport. An empty tool registry is a startup error.
7. **One runtime per process.** `IMcpRuntime` is registered as a singleton. Starting multiple runtimes in one process is unsupported.

## Consequences

- `IMcpTransport` is the seam: everything above it is pure Ferret; everything below `Transport/Stdio/` is SDK.
- Future sprint: `HttpTransport` implements `IMcpTransport` without touching `McpRuntime` or the tool/resource layer.
```

- [ ] **Step 3: Write ADR-0018**

```markdown
# ADR-0018 — Application Layer Reserved (Ferret.Application)

**Status:** Reserved  
**Date:** 2026-06-28

## Context

Multiple hosts (MCP, REST, future UI) may need shared orchestration — context assembly, cross-service queries, reusable platform concerns above individual service boundaries.

## Decision

`Ferret.Application` namespace is reserved. It will be introduced when a reusable platform concern is identified that multiple hosts need and that cannot be placed in an existing platform service.

**Trigger for introduction:** A feature or behavior is needed by ≥2 independent hosts and does not fit `Ferret.Core`, `Ferret.Search`, or any existing platform package.

## Consequences

- Sprint 11 MCP tools call platform services directly (no application layer).
- Premature introduction would add a layer with no distinct responsibility.
- This ADR is superseded when `Ferret.Application` is created.
```

- [ ] **Step 4: Commit**

```
git add docs/adr/0016-integration-platform-architecture.md docs/adr/0017-mcp-runtime-architecture.md docs/adr/0018-application-layer-reserved.md
git commit -m "docs(sprint-11): ADR-0016 Integration Platform Architecture, ADR-0017 MCP Runtime Architecture, ADR-0018 Application Layer Reserved"
```

---

### Task 3: Ferret.Mcp Project Setup

Adds the `ModelContextProtocol` NuGet package to `Ferret.Mcp` and adds `InternalsVisibleTo` so tests can access internal types. No new source files — just project file changes.

**Files:**
- Modify: `src/Ferret.Mcp/Ferret.Mcp.csproj`
- Modify: `tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj` (verify test deps are present)

- [ ] **Step 1: Read the current csproj**

```
cat src/Ferret.Mcp/Ferret.Mcp.csproj
cat tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj
```

- [ ] **Step 2: Update Ferret.Mcp.csproj**

Add to `src/Ferret.Mcp/Ferret.Mcp.csproj` — inside the root `<Project>` element:

```xml
<ItemGroup>
  <PackageReference Include="ModelContextProtocol" Version="0.*" />
</ItemGroup>

<ItemGroup>
  <InternalsVisibleTo Include="Ferret.Mcp.Tests" />
</ItemGroup>
```

> **Note:** Check NuGet for the latest stable `ModelContextProtocol` package version. As of Sprint 11 planning this is `0.*` — replace with the exact latest stable (e.g., `0.1.0`).

- [ ] **Step 3: Restore and verify compile**

```
dotnet restore src/Ferret.Mcp/Ferret.Mcp.csproj
dotnet build src/Ferret.Mcp/Ferret.Mcp.csproj -v n
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

```
git add src/Ferret.Mcp/Ferret.Mcp.csproj
git commit -m "chore(sprint-11): Ferret.Mcp — add ModelContextProtocol NuGet ref + InternalsVisibleTo"
```

---

### Task 7: MCP Resources — WorkspaceStatusResource, IndexStatsResource, ConnectorsResource

Three `IMcpResource` implementations exposing read-only workspace state as MCP resources. Resources are identified by URI (e.g., `workspace://status`). No SDK types referenced here.

**Files:**
- Create: `src/Ferret.Mcp/Resources/WorkspaceStatusResource.cs`
- Create: `src/Ferret.Mcp/Resources/IndexStatsResource.cs`
- Create: `src/Ferret.Mcp/Resources/ConnectorsResource.cs`
- Create: `tests/Ferret.Mcp.Tests/Resources/WorkspaceStatusResourceTests.cs`
- Create: `tests/Ferret.Mcp.Tests/Resources/IndexStatsResourceTests.cs`
- Create: `tests/Ferret.Mcp.Tests/Resources/ConnectorsResourceTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceContext` (Ferret.Core.Workspace), `IIndexEngine` (Ferret.Core.Indexing), `IConnectorRegistry` (Ferret.Core.Connectors), `ConnectorDescriptor`, `IndexStats`
- Produces: `WorkspaceStatusResource : IMcpResource`, `IndexStatsResource : IMcpResource`, `ConnectorsResource : IMcpResource`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Mcp.Tests/Resources/WorkspaceStatusResourceTests.cs
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Mcp.Resources;
using Xunit;

namespace Ferret.Mcp.Tests.Resources;

public sealed class WorkspaceStatusResourceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsJsonWithWorkspaceInfo()
    {
        var sut = new WorkspaceStatusResource(new FakeWorkspaceContext(), new FakeIndexEngine());

        var content = await sut.ReadAsync("workspace://status", CancellationToken.None);

        Assert.Equal("workspace://status", content.ResourceUri);
        Assert.Equal("application/json", content.MimeType);
        Assert.Contains("test-workspace", content.Text);
        Assert.Contains("documentCount", content.Text);
    }

    [Fact]
    public void Descriptor_HasCorrectUri()
    {
        var sut = new WorkspaceStatusResource(new FakeWorkspaceContext(), new FakeIndexEngine());
        Assert.Equal("workspace://status", sut.Descriptor.ResourceUri);
    }

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test-workspace");
        public WorkspacePath WorkspaceRoot => WorkspacePath.Create(Path.GetTempPath());
    }

    private sealed class FakeIndexEngine : IIndexEngine
    {
        public Task WriteAsync(Document doc, CancellationToken ct) => Task.CompletedTask;
        public Task<IndexStats> GetStatsAsync(CancellationToken ct) => Task.FromResult(new IndexStats
        {
            DocumentCount = 5,
            TotalChars = 1000,
            IndexSizeBytes = 4096,
            LastIndexedAt = DateTimeOffset.UtcNow
        });
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
```

```csharp
// tests/Ferret.Mcp.Tests/Resources/IndexStatsResourceTests.cs
using Ferret.Core.Indexing;
using Ferret.Mcp.Resources;
using Xunit;

namespace Ferret.Mcp.Tests.Resources;

public sealed class IndexStatsResourceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsJsonWithStats()
    {
        var engine = new FakeIndexEngine(documentCount: 100);
        var sut = new IndexStatsResource(engine);

        var content = await sut.ReadAsync("workspace://index/stats", CancellationToken.None);

        Assert.Equal("workspace://index/stats", content.ResourceUri);
        Assert.Contains("100", content.Text);
        Assert.Contains("documentCount", content.Text);
    }

    [Fact]
    public void Descriptor_HasCorrectUri()
    {
        var sut = new IndexStatsResource(new FakeIndexEngine(0));
        Assert.Equal("workspace://index/stats", sut.Descriptor.ResourceUri);
    }

    private sealed class FakeIndexEngine(long documentCount) : IIndexEngine
    {
        public Task WriteAsync(Document doc, CancellationToken ct) => Task.CompletedTask;
        public Task<IndexStats> GetStatsAsync(CancellationToken ct) => Task.FromResult(new IndexStats
        {
            DocumentCount = documentCount,
            TotalChars = 0,
            IndexSizeBytes = 0,
            LastIndexedAt = DateTimeOffset.MinValue
        });
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
```

```csharp
// tests/Ferret.Mcp.Tests/Resources/ConnectorsResourceTests.cs
using Ferret.Core.Connectors;
using Ferret.Mcp.Resources;
using Xunit;

namespace Ferret.Mcp.Tests.Resources;

public sealed class ConnectorsResourceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsJsonWithConnectorList()
    {
        var registry = new FakeConnectorRegistry([
            MakeDescriptor("filesystem", "Filesystem")
        ]);
        var sut = new ConnectorsResource(registry);

        var content = await sut.ReadAsync("workspace://connectors", CancellationToken.None);

        Assert.Equal("workspace://connectors", content.ResourceUri);
        Assert.Contains("filesystem", content.Text);
    }

    [Fact]
    public void Descriptor_HasCorrectUri()
    {
        var sut = new ConnectorsResource(new FakeConnectorRegistry([]));
        Assert.Equal("workspace://connectors", sut.Descriptor.ResourceUri);
    }

    private static ConnectorDescriptor MakeDescriptor(string id, string name) =>
        new ConnectorDescriptor(
            ConnectorId.Create(id),
            new ConnectorMetadata(name, ConnectorType.Source, "v1"),
            ConnectorCapabilities.None);

    private sealed class FakeConnectorRegistry(IReadOnlyList<ConnectorDescriptor> descriptors) : IConnectorRegistry
    {
        public IReadOnlyList<ConnectorDescriptor> GetAll() => descriptors;
        public ConnectorDescriptor? GetById(ConnectorId id) => null;
        public bool IsRegistered(ConnectorId id) => false;
        public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) => [];
    }
}
```

> **Note:** The exact `ConnectorDescriptor` constructor and `ConnectorMetadata` fields must match Sprint 8's implementation. Check `src/Ferret.ConnectorPlatform/ConnectorDescriptor.cs` and adjust the fake factory method if needed.

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Resources" -v n
```

Expected: compile errors — resource classes not found.

- [ ] **Step 3: Write WorkspaceStatusResource**

```csharp
// src/Ferret.Mcp/Resources/WorkspaceStatusResource.cs
using System.Text.Json;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Resources;

public sealed class WorkspaceStatusResource : IMcpResource
{
    private readonly IWorkspaceContext _workspaceContext;
    private readonly IIndexEngine _indexEngine;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public WorkspaceStatusResource(IWorkspaceContext workspaceContext, IIndexEngine indexEngine)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(indexEngine);
        _workspaceContext = workspaceContext;
        _indexEngine = indexEngine;
    }

    public McpResourceDescriptor Descriptor { get; } = new()
    {
        ResourceUri = "workspace://status",
        Name = "workspace_status",
        Description = "Current Ferret workspace status and index statistics.",
        MimeType = "application/json"
    };

    public async Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct)
    {
        var stats = await _indexEngine.GetStatsAsync(ct).ConfigureAwait(false);
        var text = JsonSerializer.Serialize(new
        {
            workspaceId = _workspaceContext.WorkspaceId.Value,
            workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath,
            documentCount = stats.DocumentCount,
            indexSizeBytes = stats.IndexSizeBytes,
            lastIndexedAt = stats.LastIndexedAt,
            totalChars = stats.TotalChars
        }, s_jsonOptions);
        return new McpResourceContent { ResourceUri = resourceUri, MimeType = "application/json", Text = text };
    }
}
```

- [ ] **Step 4: Write IndexStatsResource**

```csharp
// src/Ferret.Mcp/Resources/IndexStatsResource.cs
using System.Text.Json;
using Ferret.Core.Indexing;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Resources;

public sealed class IndexStatsResource : IMcpResource
{
    private readonly IIndexEngine _indexEngine;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public IndexStatsResource(IIndexEngine indexEngine)
    {
        ArgumentNullException.ThrowIfNull(indexEngine);
        _indexEngine = indexEngine;
    }

    public McpResourceDescriptor Descriptor { get; } = new()
    {
        ResourceUri = "workspace://index/stats",
        Name = "index_stats",
        Description = "Ferret keyword index statistics.",
        MimeType = "application/json"
    };

    public async Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct)
    {
        var stats = await _indexEngine.GetStatsAsync(ct).ConfigureAwait(false);
        var text = JsonSerializer.Serialize(new
        {
            documentCount = stats.DocumentCount,
            totalChars = stats.TotalChars,
            indexSizeBytes = stats.IndexSizeBytes,
            lastIndexedAt = stats.LastIndexedAt
        }, s_jsonOptions);
        return new McpResourceContent { ResourceUri = resourceUri, MimeType = "application/json", Text = text };
    }
}
```

- [ ] **Step 5: Write ConnectorsResource**

```csharp
// src/Ferret.Mcp/Resources/ConnectorsResource.cs
using System.Text.Json;
using Ferret.Core.Connectors;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Resources;

public sealed class ConnectorsResource : IMcpResource
{
    private readonly IConnectorRegistry _connectorRegistry;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public ConnectorsResource(IConnectorRegistry connectorRegistry)
    {
        ArgumentNullException.ThrowIfNull(connectorRegistry);
        _connectorRegistry = connectorRegistry;
    }

    public McpResourceDescriptor Descriptor { get; } = new()
    {
        ResourceUri = "workspace://connectors",
        Name = "connectors",
        Description = "Registered Ferret connectors and their capabilities.",
        MimeType = "application/json"
    };

    public Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct)
    {
        var connectors = _connectorRegistry.GetAll()
            .Select(d => new
            {
                id = d.Id.Value,
                name = d.Metadata.DisplayName,
                connectorType = d.Metadata.ConnectorType.ToString()
            })
            .ToList();

        var text = JsonSerializer.Serialize(connectors, s_jsonOptions);
        return Task.FromResult(new McpResourceContent
        {
            ResourceUri = resourceUri,
            MimeType = "application/json",
            Text = text
        });
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~Resources" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Mcp/Resources/ tests/Ferret.Mcp.Tests/Resources/
git commit -m "feat(sprint-11): MCP resources — WorkspaceStatusResource, IndexStatsResource, ConnectorsResource"
```

---

### Task 8: SDK Adapter Layer — Transport/Stdio/

The only files in the entire solution that import `ModelContextProtocol.*`. These are stateless translators: SDK types in, Ferret types out (and vice versa). `StdioTransport` implements `IMcpTransport` and is the seam between the Ferret runtime and the MCP wire protocol.

> **CRITICAL:** Before writing code, install the `ModelContextProtocol` NuGet package and check the exact types available: `dotnet add package ModelContextProtocol --version <latest-stable>`. Verify the exact class names for `McpServer`, `StdioServerTransport`, `Implementation`, `Tool`, `Resource`, `Content`, `CallToolResult`, `ReadResourceResult`, and the handler delegate signatures. Adjust the code below to match the installed SDK version.

**Files:**
- Create: `src/Ferret.Mcp/Transport/Stdio/McpArgumentsFactory.cs`
- Create: `src/Ferret.Mcp/Transport/Stdio/SdkToolAdapter.cs`
- Create: `src/Ferret.Mcp/Transport/Stdio/SdkResourceAdapter.cs`
- Create: `src/Ferret.Mcp/Transport/Stdio/McpErrorMapper.cs`
- Create: `src/Ferret.Mcp/Transport/Stdio/SdkRuntimeAdapter.cs`
- Create: `src/Ferret.Mcp/Transport/Stdio/StdioTransport.cs`

**Interfaces:**
- Consumes: `IMcpTool`, `IMcpResource`, `IMcpTransport`, `IMcpErrorMapper`, `McpArguments`, `McpToolResult`, `McpContent`, `McpResourceContent`, `McpToolDescriptor`, `McpResourceDescriptor`, `McpTransportDescriptor`, `IMcpToolRegistry`, `IMcpResourceRegistry`
- Produces: `StdioTransport : IMcpTransport`, `McpErrorMapper : IMcpErrorMapper`; internal: `McpArgumentsFactory`, `SdkToolAdapter`, `SdkResourceAdapter`, `SdkRuntimeAdapter`

> **No tests for Transport/Stdio/ in isolation** — the SDK transport requires real stdin/stdout. Coverage comes from Task 13 (host integration tests). What can be tested: `McpErrorMapper` and `McpArgumentsFactory` if they have no SDK dependencies.

- [ ] **Step 1: Write McpArgumentsFactory**

```csharp
// src/Ferret.Mcp/Transport/Stdio/McpArgumentsFactory.cs
using System.Text.Json;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Transport.Stdio;

// Converts SDK argument dictionaries to Ferret McpArguments.
// All JsonElement access is confined to this file.
internal static class McpArgumentsFactory
{
    internal static McpArguments From(IDictionary<string, JsonElement>? sdkArgs)
    {
        if (sdkArgs is null or { Count: 0 })
            return McpArguments.Empty;

        var dict = new Dictionary<string, string>(sdkArgs.Count, StringComparer.Ordinal);
        foreach (var kv in sdkArgs)
        {
            dict[kv.Key] = kv.Value.ValueKind == JsonValueKind.String
                ? kv.Value.GetString() ?? string.Empty
                : kv.Value.GetRawText();
        }
        return new McpArguments(dict);
    }
}
```

- [ ] **Step 2: Write McpErrorMapper**

```csharp
// src/Ferret.Mcp/Transport/Stdio/McpErrorMapper.cs
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Transport.Stdio;

internal sealed class McpErrorMapper : IMcpErrorMapper
{
    public McpToolResult MapException(Exception ex) => ex switch
    {
        OperationCanceledException => McpToolResult.Error("Operation cancelled."),
        ArgumentException argEx => McpToolResult.Error($"Invalid argument: {argEx.Message}"),
        InvalidOperationException opEx => McpToolResult.Error($"Tool error: {opEx.Message}"),
        _ => McpToolResult.Error($"Unexpected error ({ex.GetType().Name}): {ex.Message}")
    };
}
```

- [ ] **Step 3: Write McpErrorMapper test**

```csharp
// tests/Ferret.Mcp.Tests/Transport/McpErrorMapperTests.cs
using Ferret.Mcp.Transport.Stdio;
using Xunit;

namespace Ferret.Mcp.Tests.Transport;

public sealed class McpErrorMapperTests
{
    private readonly McpErrorMapper _sut = new();

    [Fact]
    public void MapException_OperationCancelled_ReturnsErrorResult()
    {
        var result = _sut.MapException(new OperationCanceledException());
        Assert.True(result.IsError);
        Assert.Contains("cancelled", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapException_ArgumentException_ReturnsErrorWithMessage()
    {
        var result = _sut.MapException(new ArgumentException("bad param"));
        Assert.True(result.IsError);
        Assert.Contains("bad param", result.Content[0].Text);
    }

    [Fact]
    public void MapException_GenericException_ReturnsErrorWithTypeName()
    {
        var result = _sut.MapException(new InvalidDataException("data broken"));
        Assert.True(result.IsError);
        Assert.Contains("InvalidDataException", result.Content[0].Text);
    }
}
```

- [ ] **Step 4: Run McpErrorMapper test**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~McpErrorMapper" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Write SdkToolAdapter**

> Verify SDK types against installed package. The types below follow the ModelContextProtocol SDK conventions — adjust if your installed version uses different names.

```csharp
// src/Ferret.Mcp/Transport/Stdio/SdkToolAdapter.cs
using System.Text.Json;
using Ferret.Mcp.Protocol;
using ModelContextProtocol.Protocol.Types;   // VERIFY: Tool, Content, CallToolResult

namespace Ferret.Mcp.Transport.Stdio;

internal static class SdkToolAdapter
{
    internal static Tool ToSdkTool(McpToolDescriptor descriptor)
    {
        var tool = new Tool
        {
            Name = descriptor.Name,
            Description = descriptor.Description
        };

        if (descriptor.InputSchemaJson is not null)
        {
            // InputSchema is the JSON schema for tool parameters
            tool.InputSchema = JsonSerializer.Deserialize<JsonElement>(descriptor.InputSchemaJson);
        }

        return tool;
    }

    internal static CallToolResult ToSdkResult(McpToolResult result) => new()
    {
        Content = result.Content
            .Select(c => new Content { Type = c.Type, Text = c.Text })
            .ToList(),
        IsError = result.IsError
    };
}
```

- [ ] **Step 6: Write SdkResourceAdapter**

```csharp
// src/Ferret.Mcp/Transport/Stdio/SdkResourceAdapter.cs
using Ferret.Mcp.Protocol;
using ModelContextProtocol.Protocol.Types;   // VERIFY: Resource, ResourceContents, ReadResourceResult

namespace Ferret.Mcp.Transport.Stdio;

internal static class SdkResourceAdapter
{
    internal static Resource ToSdkResource(McpResourceDescriptor descriptor) => new()
    {
        Uri = descriptor.ResourceUri,
        Name = descriptor.Name,
        Description = descriptor.Description,
        MimeType = descriptor.MimeType
    };

    internal static ReadResourceResult ToSdkResult(McpResourceContent content) => new()
    {
        Contents =
        [
            new ResourceContents
            {
                Uri = content.ResourceUri,
                MimeType = content.MimeType,
                Text = content.Text
            }
        ]
    };
}
```

- [ ] **Step 7: Write SdkRuntimeAdapter**

```csharp
// src/Ferret.Mcp/Transport/Stdio/SdkRuntimeAdapter.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using ModelContextProtocol.Protocol.Types;   // VERIFY: all protocol types
using ModelContextProtocol.Server;           // VERIFY: McpServer, McpServerOptions, Implementation

namespace Ferret.Mcp.Transport.Stdio;

// Wires the Ferret tool/resource registries to the SDK McpServer.
// All SDK handler delegates live here.
internal static class SdkRuntimeAdapter
{
    internal static async Task RunAsync(
        object sdkTransport,            // VERIFY: ITransport or McpServerTransport from SDK
        IMcpToolRegistry tools,
        IMcpResourceRegistry resources,
        McpServerOptions options,
        IMcpErrorMapper errorMapper,
        CancellationToken ct)
    {
        // VERIFY: McpServer constructor signature and RunAsync method in the installed SDK.
        // The pattern below is representative — update to match the actual SDK API.
        await using var server = new McpServer((/* ITransport */ sdkTransport as dynamic)!, options);

        server.ListToolsHandler = (req, token) => ValueTask.FromResult(new ListToolsResult
        {
            Tools = tools.GetAll().Select(SdkToolAdapter.ToSdkTool).ToList()
        });

        server.CallToolHandler = async (req, token) =>
        {
            var name = req.Params?.Name ?? string.Empty;
            var tool = tools.GetByName(name);
            if (tool is null)
                return new CallToolResult
                {
                    Content = [new Content { Type = "text", Text = $"Unknown tool: {name}" }],
                    IsError = true
                };

            try
            {
                var args = McpArgumentsFactory.From(req.Params?.Arguments);
                var result = await tool.ExecuteAsync(args, token).ConfigureAwait(false);
                return SdkToolAdapter.ToSdkResult(result);
            }
            catch (Exception ex)
            {
                return SdkToolAdapter.ToSdkResult(errorMapper.MapException(ex));
            }
        };

        server.ListResourcesHandler = (req, token) => ValueTask.FromResult(new ListResourcesResult
        {
            Resources = resources.GetAll().Select(SdkResourceAdapter.ToSdkResource).ToList()
        });

        server.ReadResourceHandler = async (req, token) =>
        {
            var uri = req.Params?.Uri ?? string.Empty;
            var resource = resources.GetByUri(uri);
            if (resource is null)
                return new ReadResourceResult
                {
                    Contents = [new ResourceContents { Uri = uri, MimeType = "text/plain", Text = $"Unknown resource: {uri}" }]
                };

            var content = await resource.ReadAsync(uri, token).ConfigureAwait(false);
            return SdkResourceAdapter.ToSdkResult(content);
        };

        await server.RunAsync(ct).ConfigureAwait(false);
    }
}
```

- [ ] **Step 8: Write StdioTransport**

```csharp
// src/Ferret.Mcp/Transport/Stdio/StdioTransport.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using ModelContextProtocol.Server;           // VERIFY: McpServerOptions, Implementation
using ModelContextProtocol.Protocol.Transport; // VERIFY: StdioServerTransport

namespace Ferret.Mcp.Transport.Stdio;

internal sealed class StdioTransport : IMcpTransport
{
    private readonly IMcpErrorMapper _errorMapper;
    private const string ServerName = "ferret";
    private const string ServerVersion = "0.11.0";

    public StdioTransport(IMcpErrorMapper errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        _errorMapper = errorMapper;
    }

    public McpTransportDescriptor Descriptor { get; } = new()
    {
        Name = "stdio",
        Description = "MCP stdio transport (stdin/stdout)"
    };

    public async Task RunAsync(IMcpToolRegistry tools, IMcpResourceRegistry resources, CancellationToken ct)
    {
        // stdout belongs to MCP protocol — all Ferret output goes to stderr
        await Console.Error.WriteLineAsync($"Ferret MCP Runtime {ServerVersion} ready (stdio transport)").ConfigureAwait(false);

        var options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = ServerName, Version = ServerVersion }
        };

        // VERIFY: StdioServerTransport constructor in installed SDK version
        var sdkTransport = new StdioServerTransport(ServerName);

        await SdkRuntimeAdapter.RunAsync(sdkTransport, tools, resources, options, _errorMapper, ct)
            .ConfigureAwait(false);
    }
}
```

- [ ] **Step 9: Build to verify SDK types resolve**

```
dotnet build src/Ferret.Mcp/Ferret.Mcp.csproj -v n
```

Expected: build succeeds. If SDK types don't exist under the expected namespaces, look up the actual SDK API with `dotnet package search ModelContextProtocol` and adjust import namespaces in `SdkToolAdapter.cs`, `SdkResourceAdapter.cs`, `SdkRuntimeAdapter.cs`, and `StdioTransport.cs`.

- [ ] **Step 10: Run all current tests**

```
dotnet test tests/Ferret.Mcp.Tests/ -v n
```

Expected: all tests PASS (transport layer tests excluded — no SDK stdin/stdout in unit tests).

- [ ] **Step 11: Commit**

```
git add src/Ferret.Mcp/Transport/ tests/Ferret.Mcp.Tests/Transport/
git commit -m "feat(sprint-11): MCP SDK adapter layer — StdioTransport, SdkRuntimeAdapter, McpArgumentsFactory"
```

---

### Task 9: McpRuntime

`McpRuntime` implements `IMcpRuntime`. It validates registries at startup, then delegates to `IMcpTransport`. It is the only component that controls the startup sequence.

**Files:**
- Create: `src/Ferret.Mcp/Runtime/McpRuntime.cs`
- Create: `tests/Ferret.Mcp.Tests/Runtime/McpRuntimeTests.cs`

**Interfaces:**
- Consumes: `IMcpTransport`, `IMcpToolRegistry`, `IMcpResourceRegistry`, `IMcpRuntime`, `McpToolDescriptor`
- Produces: `McpRuntime : IMcpRuntime`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Mcp.Tests/Runtime/McpRuntimeTests.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using Ferret.Mcp.Runtime;
using Xunit;

namespace Ferret.Mcp.Tests.Runtime;

public sealed class McpRuntimeTests
{
    [Fact]
    public async Task RunAsync_EmptyToolRegistry_ThrowsBeforeCallingTransport()
    {
        var transport = new FakeTransport();
        var tools = new FakeToolRegistry([]);
        var resources = new FakeResourceRegistry();
        var sut = new McpRuntime(transport, tools, resources);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RunAsync(CancellationToken.None));

        Assert.False(transport.WasCalled);
    }

    [Fact]
    public async Task RunAsync_WithTools_CallsTransport()
    {
        var transport = new FakeTransport();
        var tools = new FakeToolRegistry([new McpToolDescriptor { Name = "test", Description = "test" }]);
        var resources = new FakeResourceRegistry();
        var sut = new McpRuntime(transport, tools, resources);

        await sut.RunAsync(CancellationToken.None);

        Assert.True(transport.WasCalled);
    }

    [Fact]
    public void Constructor_NullTransport_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new McpRuntime(null!, new FakeToolRegistry([]), new FakeResourceRegistry()));
    }

    private sealed class FakeTransport : IMcpTransport
    {
        public bool WasCalled { get; private set; }
        public McpTransportDescriptor Descriptor { get; } = new() { Name = "fake", Description = "test transport" };
        public Task RunAsync(IMcpToolRegistry tools, IMcpResourceRegistry resources, CancellationToken ct)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeToolRegistry(IReadOnlyList<McpToolDescriptor> descriptors) : IMcpToolRegistry
    {
        public IReadOnlyList<McpToolDescriptor> GetAll() => descriptors;
        public IMcpTool? GetByName(string name) => null;
    }

    private sealed class FakeResourceRegistry : IMcpResourceRegistry
    {
        public IReadOnlyList<McpResourceDescriptor> GetAll() => [];
        public IMcpResource? GetByUri(string resourceUri) => null;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~McpRuntimeTests" -v n
```

Expected: compile error — `McpRuntime` not found.

- [ ] **Step 3: Write McpRuntime**

```csharp
// src/Ferret.Mcp/Runtime/McpRuntime.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;

namespace Ferret.Mcp.Runtime;

internal sealed class McpRuntime : IMcpRuntime
{
    private readonly IMcpTransport _transport;
    private readonly IMcpToolRegistry _tools;
    private readonly IMcpResourceRegistry _resources;

    public McpRuntime(IMcpTransport transport, IMcpToolRegistry tools, IMcpResourceRegistry resources)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(resources);
        _transport = transport;
        _tools = tools;
        _resources = resources;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (_tools.GetAll().Count == 0)
            throw new InvalidOperationException("MCP runtime cannot start: no tools registered.");

        await _transport.RunAsync(_tools, _resources, ct).ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~McpRuntimeTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Full solution tests pass**

```
dotnet test src/Ferret.sln -v n
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.Mcp/Runtime/McpRuntime.cs tests/Ferret.Mcp.Tests/Runtime/McpRuntimeTests.cs
git commit -m "feat(sprint-11): McpRuntime — startup validation + IMcpTransport delegation"
```

---

### Task 10: McpModule DI Composition Root

Updates the stub `McpModule` into a real static composition root. Registers all MCP-specific services (tools, resources, registries, error mapper, transport, runtime). Assumes platform services (`ISearchService`, `IDocumentService`, `IIndexEngine`, `IWorkspaceContext`, `IConnectorRegistry`) are already registered by the host (ServeCliModule + Sprint 10 SearchCliModule).

**Files:**
- Modify: `src/Ferret.Mcp/McpModule.cs`

**Interfaces:**
- Consumes: all types from Tasks 4-9; `IServiceCollection` from `Microsoft.Extensions.DependencyInjection`
- Produces: `McpModule.ConfigureServices(IServiceCollection services)` — internal static method called by `ServeCliModule`

> No unit tests for McpModule — composition is validated by Task 13 (host integration tests).

- [ ] **Step 1: Read the current McpModule.cs**

```
cat src/Ferret.Mcp/McpModule.cs
```

- [ ] **Step 2: Replace with the full composition root**

```csharp
// src/Ferret.Mcp/McpModule.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using Ferret.Mcp.Resources;
using Ferret.Mcp.Runtime;
using Ferret.Mcp.Tools;
using Ferret.Mcp.Transport.Stdio;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Mcp;

internal static class McpModule
{
    // Registers all MCP protocol + runtime services.
    // Platform services (ISearchService, IDocumentService, IIndexEngine,
    // IWorkspaceContext, IConnectorRegistry) must already be in the container.
    internal static void ConfigureServices(IServiceCollection services)
    {
        // Tools — registered first so registries can resolve them
        services.AddSingleton<SearchTool>();
        services.AddSingleton<ReadDocumentTool>();
        services.AddSingleton<WorkspaceStatusTool>();

        // Resources
        services.AddSingleton<WorkspaceStatusResource>();
        services.AddSingleton<IndexStatsResource>();
        services.AddSingleton<ConnectorsResource>();

        // Immutable registries — built once at startup via factory methods
        services.AddSingleton<IMcpToolRegistry>(sp => new McpToolRegistryBuilder()
            .Add(sp.GetRequiredService<SearchTool>())
            .Add(sp.GetRequiredService<ReadDocumentTool>())
            .Add(sp.GetRequiredService<WorkspaceStatusTool>())
            .Build());

        services.AddSingleton<IMcpResourceRegistry>(sp => new McpResourceRegistryBuilder()
            .Add(sp.GetRequiredService<WorkspaceStatusResource>())
            .Add(sp.GetRequiredService<IndexStatsResource>())
            .Add(sp.GetRequiredService<ConnectorsResource>())
            .Build());

        // Transport infrastructure
        services.AddSingleton<IMcpErrorMapper, McpErrorMapper>();
        services.AddSingleton<IMcpTransport, StdioTransport>();

        // Runtime — entry point for ServeCommandHandler
        services.AddSingleton<IMcpRuntime, McpRuntime>();
    }
}
```

- [ ] **Step 3: Build to verify compilation**

```
dotnet build src/Ferret.Mcp/Ferret.Mcp.csproj -v n
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

```
git add src/Ferret.Mcp/McpModule.cs
git commit -m "feat(sprint-11): McpModule — DI composition root for MCP runtime services"
```

---

### Task 11: ServeCliModule + ServeCommandHandler + Program.cs

Wires the MCP runtime into the CLI host. `ServeCliModule` registers `IDocumentService` and calls `McpModule.ConfigureServices`. `ServeCommandHandler` resolves `IMcpRuntime` via constructor injection and calls `RunAsync`. `Program.cs` adds `ServeCliModule` to the module array.

**Files:**
- Create: `src/Ferret.Cli/Commands/Serve/ServeCliModule.cs`
- Create: `src/Ferret.Cli/Commands/Serve/ServeCommandHandler.cs`
- Modify: `src/Ferret.Cli/Program.cs`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`

**Interfaces:**
- Consumes: `CliModuleBase`, `ICliModule`, `CommandDefinition`, `CommandMetadata`, `ICommandHandler`, `IFerretContext`, `CommandResult.Success`, `IWorkspaceContext`, `IDocumentService`, `DocumentService`, `IndexLayout`, `WorkspaceLayout`, `McpModule`, `IMcpRuntime`
- Produces: `ServeCliModule : CliModuleBase`, `ServeCommandHandler : ICommandHandler`

- [ ] **Step 1: Add Ferret.Mcp reference to Ferret.Cli.csproj**

Open `src/Ferret.Cli/Ferret.Cli.csproj` and add inside the `<ItemGroup>` with project references:

```xml
<ProjectReference Include="..\Ferret.Mcp\Ferret.Mcp.csproj" />
```

- [ ] **Step 2: Write ServeCliModule**

```csharp
// src/Ferret.Cli/Commands/Serve/ServeCliModule.cs
using Ferret.Cli.Cli;
using Ferret.Core.Indexing;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Ferret.Mcp;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Serve;

internal sealed class ServeCliModule : CliModuleBase
{
    private readonly IWorkspaceContext _workspaceContext;

    public ServeCliModule(IWorkspaceContext workspaceContext)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        _workspaceContext = workspaceContext;
    }

    public override string Name => "ferret.serve";
    public override string Description => "MCP runtime host — exposes Ferret capabilities to AI hosts via stdio.";

    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("serve", "Start the Ferret MCP runtime (stdio). Connect via MCP client using server name 'ferret'."),
            typeof(ServeCommandHandler));
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var dbPath = System.IO.Path.Combine(
            _workspaceContext.WorkspaceRoot.FullPath,
            WorkspaceLayout.RootDirectoryName,
            IndexLayout.IndexDirectoryName,
            IndexLayout.KeywordDirectoryName,
            IndexLayout.KeywordDatabaseFileName);

        // IDocumentService — reads documents by ID from the keyword-index SQLite database.
        // ISearchService is registered by SearchCliModule (Sprint 10). Other platform services
        // (IIndexEngine, IWorkspaceContext, IConnectorRegistry) are registered by their respective modules.
        services.AddSingleton<IDocumentService>(_ => new DocumentService(dbPath));

        McpModule.ConfigureServices(services);

        services.AddSingleton<ServeCommandHandler>();
    }
}
```

- [ ] **Step 3: Write ServeCommandHandler**

```csharp
// src/Ferret.Cli/Commands/Serve/ServeCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Mcp.Protocol;

namespace Ferret.Cli.Commands.Serve;

internal sealed class ServeCommandHandler : ICommandHandler
{
    private readonly IMcpRuntime _runtime;

    public ServeCommandHandler(IMcpRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        await _runtime.RunAsync(context.CancellationToken).ConfigureAwait(false);
        return CommandResult.Success;
    }
}
```

- [ ] **Step 4: Update Program.cs**

In `src/Ferret.Cli/Program.cs`, add the using and the module:

```csharp
// Add at top with other usings:
using Ferret.Cli.Commands.Serve;

// Add to the modules array in RootCommandFactory.Build([...]):
new ServeCliModule(workspaceContext),
```

The full `Program.cs` after changes:

```csharp
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Connector;
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Serve;
using Ferret.Cli.Commands.Workspace;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.ParserPlatform;
using Ferret.Workspace;

// Build IWorkspaceContext once from CWD — Sprint 10 will read workspace ID from workspace.json.
var workspaceRoot = WorkspacePath.Create(Directory.GetCurrentDirectory());
var workspaceId = WorkspaceId.Create("default");
IWorkspaceContext workspaceContext = new DefaultWorkspaceContext(workspaceId, workspaceRoot);

var filesystemConfig = new FilesystemConnectorConfiguration { RootPath = workspaceRoot.FullPath };
var filesystemFactory = new FilesystemConnectorFactory(filesystemConfig, new MimeTypeResolver());

return await RootCommandFactory.Build([
    new CoreCliModule(),
    new WorkspaceCliModule(),
    new ConnectorCliModule([filesystemFactory]),
    new IndexCliModule(workspaceContext),
    new SearchCliModule(workspaceContext),  // Sprint 10 — registers ISearchService
    new ServeCliModule(workspaceContext),   // Sprint 11
]).InvokeAsync(args).ConfigureAwait(false);
```

> **Note:** `SearchCliModule` must exist before this change (Sprint 10 deliverable). If Sprint 10 is not yet complete, add `ServeCliModule` but leave `SearchCliModule` commented out until Sprint 10 ships.

- [ ] **Step 5: Build the full solution**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds with no errors.

- [ ] **Step 6: Smoke test `ferret --help`**

```
dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- --help
```

Expected: output includes `serve` command in the list.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Cli/Commands/Serve/ src/Ferret.Cli/Program.cs src/Ferret.Cli/Ferret.Cli.csproj
git commit -m "feat(sprint-11): ServeCliModule + ServeCommandHandler — ferret serve command"
```

---

### Task 12: Architecture Tests — Ferret.Mcp Rules

Adds three architecture rules to `Ferret.Architecture.Tests` enforcing the SDK isolation invariant and dependency direction. Follows the pattern in `ConnectorArchitectureTests.cs` (which uses reflection).

**Files:**
- Create: `tests/Ferret.Architecture.Tests/McpArchitectureTests.cs`

**Interfaces:**
- Consumes: `IMcpTool`, `IMcpRuntime`, `StdioTransport` (via assembly), `System.Reflection`
- Produces: executable architecture rules

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Architecture.Tests/McpArchitectureTests.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Transport.Stdio;
using System.Reflection;
using Xunit;

namespace Ferret.Architecture.Tests;

public sealed class McpArchitectureTests
{
    private static readonly Assembly McpAssembly = typeof(IMcpRuntime).Assembly;
    private const string SdkAssemblyPrefix = "ModelContextProtocol";

    [Fact]
    public void NoMcpTypeOutsideTransportStdio_ReferencesAnySdkAssembly()
    {
        // All direct field types from SDK must live in Transport.Stdio namespace
        var violations = McpAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Transport.Stdio") != true)
            .SelectMany(t => t.GetFields(
                BindingFlags.NonPublic | BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.Static))
            .Where(f => f.FieldType.Assembly.GetName().Name
                ?.StartsWith(SdkAssemblyPrefix, StringComparison.Ordinal) == true)
            .Select(f => $"{f.DeclaringType?.FullName}.{f.Name} : {f.FieldType.Name}")
            .ToList();

        Assert.True(violations.Count == 0,
            $"SDK types found outside Transport.Stdio:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void AllMcpTools_ImplementIMcpTool()
    {
        var toolsNamespace = "Ferret.Mcp.Tools";
        var tools = McpAssembly.GetTypes()
            .Where(t => t.Namespace == toolsNamespace && t is { IsClass: true, IsAbstract: false })
            .ToList();

        Assert.NotEmpty(tools);

        foreach (var tool in tools)
        {
            Assert.True(
                typeof(IMcpTool).IsAssignableFrom(tool),
                $"{tool.Name} in {toolsNamespace} does not implement IMcpTool");
        }
    }

    [Fact]
    public void AllMcpResources_ImplementIMcpResource()
    {
        var resourcesNamespace = "Ferret.Mcp.Resources";
        var resources = McpAssembly.GetTypes()
            .Where(t => t.Namespace == resourcesNamespace && t is { IsClass: true, IsAbstract: false })
            .ToList();

        Assert.NotEmpty(resources);

        foreach (var resource in resources)
        {
            Assert.True(
                typeof(IMcpResource).IsAssignableFrom(resource),
                $"{resource.Name} in {resourcesNamespace} does not implement IMcpResource");
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Architecture.Tests/ --filter "FullyQualifiedName~McpArchitecture" -v n
```

Expected: compile or test failure (Ferret.Mcp assembly not yet referenced by architecture tests project — add it if needed).

- [ ] **Step 3: Add Ferret.Mcp reference to Ferret.Architecture.Tests.csproj if missing**

Check if `Ferret.Architecture.Tests.csproj` already references `Ferret.Mcp`. If not:

```xml
<ProjectReference Include="..\..\src\Ferret.Mcp\Ferret.Mcp.csproj" />
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Architecture.Tests/ --filter "FullyQualifiedName~McpArchitecture" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Full architecture tests still pass**

```
dotnet test tests/Ferret.Architecture.Tests/ -v n
```

- [ ] **Step 6: Commit**

```
git add tests/Ferret.Architecture.Tests/McpArchitectureTests.cs tests/Ferret.Architecture.Tests/
git commit -m "test(sprint-11): architecture rules — SDK isolation + IMcpTool/IMcpResource contracts"
```

---

### Task 13: Host Integration Tests

Validates that `McpModule.ConfigureServices` correctly composes the full MCP runtime from platform service dependencies. Uses fake implementations of platform services (no real SQLite, no real filesystem). Tests verify that the DI container resolves correctly, tools execute, and resources return valid JSON.

**Files:**
- Create: `tests/Ferret.Mcp.Tests/Integration/McpHostIntegrationTests.cs`

**Interfaces:**
- Consumes: `McpModule` (internal via InternalsVisibleTo), all platform service interfaces, `IMcpRuntime`, `IMcpToolRegistry`, `IMcpResourceRegistry`, `McpArguments`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Ferret.Mcp.Tests/Integration/McpHostIntegrationTests.cs
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Mcp;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferret.Mcp.Tests.Integration;

public sealed class McpHostIntegrationTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        // Platform services — fake implementations for integration testing
        services.AddSingleton<IWorkspaceContext>(new FakeWorkspaceContext());
        services.AddSingleton<IIndexEngine>(new FakeIndexEngine());
        services.AddSingleton<IConnectorRegistry>(new FakeConnectorRegistry());
        services.AddSingleton<ISearchService>(new FakeSearchService());
        services.AddSingleton<IDocumentService>(new FakeDocumentService());

        McpModule.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void ConfigureServices_ResolvesIMcpRuntime()
    {
        using var provider = BuildProvider();
        var runtime = provider.GetRequiredService<IMcpRuntime>();
        Assert.NotNull(runtime);
    }

    [Fact]
    public void ConfigureServices_ToolRegistryHasThreeTools()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<IMcpToolRegistry>();
        var tools = registry.GetAll();
        Assert.Equal(3, tools.Count);
        Assert.Contains(tools, t => t.Name == "search");
        Assert.Contains(tools, t => t.Name == "read_document");
        Assert.Contains(tools, t => t.Name == "workspace_status");
    }

    [Fact]
    public void ConfigureServices_ResourceRegistryHasThreeResources()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<IMcpResourceRegistry>();
        var resources = registry.GetAll();
        Assert.Equal(3, resources.Count);
        Assert.Contains(resources, r => r.ResourceUri == "workspace://status");
        Assert.Contains(resources, r => r.ResourceUri == "workspace://index/stats");
        Assert.Contains(resources, r => r.ResourceUri == "workspace://connectors");
    }

    [Fact]
    public async Task WorkspaceStatusTool_Execute_ReturnsValidJson()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<IMcpToolRegistry>();
        var tool = registry.GetByName("workspace_status");
        Assert.NotNull(tool);

        var result = await tool!.ExecuteAsync(McpArguments.Empty, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("test-workspace", result.Content[0].Text);
    }

    [Fact]
    public async Task SearchTool_Execute_ReturnsNoResultsForUnknownQuery()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<IMcpToolRegistry>();
        var tool = registry.GetByName("search");
        Assert.NotNull(tool);

        var result = await tool!.ExecuteAsync(McpArguments.From(("query", "xyz_not_found")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("No results", result.Content[0].Text);
    }

    [Fact]
    public async Task ReadDocumentTool_Execute_ReturnsErrorForMissingDoc()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<IMcpToolRegistry>();
        var tool = registry.GetByName("read_document");
        Assert.NotNull(tool);

        var result = await tool!.ExecuteAsync(McpArguments.From(("document_id", "no-such-doc")), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content[0].Text);
    }

    [Fact]
    public async Task WorkspaceStatusResource_Read_ReturnsJsonContent()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<IMcpResourceRegistry>();
        var resource = registry.GetByUri("workspace://status");
        Assert.NotNull(resource);

        var content = await resource!.ReadAsync("workspace://status", CancellationToken.None);

        Assert.Equal("application/json", content.MimeType);
        Assert.Contains("test-workspace", content.Text);
    }

    // ── Fake platform services ──────────────────────────────────────────────

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test-workspace");
        public WorkspacePath WorkspaceRoot => WorkspacePath.Create(Path.GetTempPath());
    }

    private sealed class FakeIndexEngine : IIndexEngine
    {
        public Task WriteAsync(Document doc, CancellationToken ct) => Task.CompletedTask;
        public Task<IndexStats> GetStatsAsync(CancellationToken ct) => Task.FromResult(new IndexStats
        {
            DocumentCount = 10,
            TotalChars = 5000,
            IndexSizeBytes = 8192,
            LastIndexedAt = DateTimeOffset.UtcNow
        });
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeConnectorRegistry : IConnectorRegistry
    {
        public IReadOnlyList<ConnectorDescriptor> GetAll() => [];
        public ConnectorDescriptor? GetById(ConnectorId id) => null;
        public bool IsRegistered(ConnectorId id) => false;
        public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) => [];
    }

    private sealed class FakeSearchService : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options, CancellationToken ct) =>
            Task.FromResult(new SearchServiceResult
            {
                Hits = [],
                TotalCount = 0,
                Query = rawQuery,
                ElapsedTime = TimeSpan.Zero
            });
    }

    private sealed class FakeDocumentService : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct) =>
            Task.FromResult<Document?>(null);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~McpHostIntegration" -v n
```

Expected: compile errors — `McpModule` is internal; verify `InternalsVisibleTo` is set (Task 3).

- [ ] **Step 3: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~McpHostIntegration" -v n
```

Expected: 7 tests PASS.

- [ ] **Step 4: Full solution test run**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. Record the new test count.

- [ ] **Step 5: Commit**

```
git add tests/Ferret.Mcp.Tests/Integration/McpHostIntegrationTests.cs
git commit -m "test(sprint-11): host integration tests — McpModule DI composition + all tools and resources"
```

---

### Task 14: Sprint Tag + PROJECT-STATE Update

Closes Sprint 11. Update living documents and tag the release.

**Files:**
- Modify: `docs/000-Overview/PROJECT-STATE.md`
- Modify: `docs/001-Product/ROADMAP-001.md`

- [ ] **Step 1: Update PROJECT-STATE.md**

In the "Current Sprint" section, add Sprint 11 entry:

```markdown
## Sprint 11 — Host Platform (MCP Runtime v1)

**Status:** Complete  
**Tag:** `v0.11.0-sprint11`  
**Date:** 2026-06-28

### Delivered

- `IDocumentService` in `Ferret.Core.Search` + `DocumentService` in `Ferret.Indexing`
- ADR-0016: Integration Platform Architecture (Host Architecture Pattern, 9 principles)
- ADR-0017: MCP Runtime Architecture (stdio, SDK isolation, immutable registries)
- ADR-0018: `Ferret.Application` namespace reserved
- `Ferret.Mcp` project: `ModelContextProtocol` NuGet SDK, isolated to `Transport/Stdio/`
- Protocol contracts: `McpArguments`, `McpContent`, `McpToolResult`, `McpResourceContent`, `IMcpTool`, `IMcpResource`, `IMcpTransport`, `IMcpRuntime`, `IMcpErrorMapper`
- Registries: `IMcpToolRegistry`, `IMcpResourceRegistry` (immutable after startup)
- Tools: `SearchTool` (→ ISearchService), `ReadDocumentTool` (→ IDocumentService), `WorkspaceStatusTool` (→ IWorkspaceContext + IIndexEngine)
- Resources: `WorkspaceStatusResource`, `IndexStatsResource`, `ConnectorsResource`
- SDK adapter layer (Transport/Stdio/): `McpArgumentsFactory`, `SdkToolAdapter`, `SdkResourceAdapter`, `McpErrorMapper`, `SdkRuntimeAdapter`, `StdioTransport`
- `McpRuntime`: startup validation + IMcpTransport delegation
- `McpModule`: DI composition root
- `ServeCliModule` + `ServeCommandHandler`: `ferret serve` command
- Architecture tests: SDK isolation enforcement, IMcpTool/IMcpResource contract rules
- Host integration tests: full DI composition verified with fake platform services

### What a new user can do after Sprint 11

Run `ferret serve` to start an MCP stdio runtime. Point any MCP-compatible AI host (Claude Code, Claude Desktop, Cursor) at `ferret` — the AI host can then call `search`, `read_document`, and `workspace_status` tools or read the `workspace://status`, `workspace://index/stats`, and `workspace://connectors` resources.
```

Update the "Active ADRs" table:

```markdown
| ADR-0014 | Document Processing Architecture | Accepted |
| ADR-0016 | Integration Platform Architecture | Accepted |
| ADR-0017 | MCP Runtime Architecture | Accepted |
| ADR-0018 | Application Layer Reserved | Reserved |
```

Update CLI Commands table:

```markdown
| `ferret serve` (MCP) | Shipped | Sprint 11 |
```

Update current version:

```
| **Current version** | 0.11.0 (Sprint 11 complete) |
| **Current sprint**  | Sprint 12 — Context Intelligence (planned) |
```

- [ ] **Step 2: Update ROADMAP-001.md**

Move Sprint 11 from Planned to Completed section. Update the Sprint 11 entry:

```markdown
### Sprint 11 — Host Platform (MCP Runtime v1) [✅ Complete]

**Goal:** A user can point any MCP-compatible AI host at Ferret and get context-aware answers.

**Delivered:** `ferret serve` command; MCP stdio runtime; `search`, `read_document`, `workspace_status` tools; `workspace://status`, `workspace://index/stats`, `workspace://connectors` resources; host architecture pattern (ADR-0016, ADR-0017).

**Tag:** `v0.11.0-sprint11`

**M3 Checkpoint:** Multi-Host Platform — Ferret now exposes capabilities through the Host Architecture Pattern. All future hosts (REST, Web UI, Agent) follow the same adapter pattern.
```

- [ ] **Step 3: Commit documents**

```
git add docs/000-Overview/PROJECT-STATE.md docs/001-Product/ROADMAP-001.md
git commit -m "docs(sprint-11): PROJECT-STATE + ROADMAP-001 updated for Sprint 11 completion"
```

- [ ] **Step 4: Run the full test suite one final time**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. Note the final test count.

- [ ] **Step 5: Tag the sprint**

```
git tag v0.11.0-sprint11
git log --oneline -5
```

Verify the tag points to the correct commit.

---

## Execution Order Note

The tasks in this file appear in writing order, not strict execution order. The correct implementation sequence is:

```
Task 1  → Task 2  → Task 3  → Task 4  → Task 5  → Task 6
→ Task 7  → Task 8  → Task 9  → Task 10 → Task 11 → Task 12
→ Task 13 → Task 14
```

Tasks 1-3 are prerequisites. Tasks 4-10 build Ferret.Mcp layer by layer. Task 11 wires it into the CLI. Tasks 12-13 verify correctness. Task 14 closes the sprint.

---

## Self-Review

### Spec Coverage

Reviewing `docs/superpowers/specs/2026-06-28-sprint-11-integration-platform-design.md` against this plan:

| Spec Section | Coverage |
|---|---|
| S1 Sprint Overview | Task 14 (PROJECT-STATE/ROADMAP update) |
| S2 Architecture (principles, dependency rules) | Task 12 (architecture tests enforce rules) |
| S3 Contract Design (McpArguments, McpToolResult, registries) | Tasks 4-5 |
| S4 Tools + IDocumentService | Tasks 1, 6 |
| S5 Transport (SDK isolation, StdioTransport) | Task 8 |
| S6 Hosting (McpModule, ServeCliModule, startup banner) | Tasks 10-11 |
| S7 ADRs + dependency gates | Tasks 2, 3 |
| S8 Exit Criteria | Tasks 12-13 (functional + architectural) |
| Milestone M3 | Task 14 (noted in ROADMAP) |
| Ferret Host Architecture Pattern | ADR-0016 (Task 2) + architecture tests (Task 12) |

**Gaps checked and resolved:**
- `McpErrorMapper` — covered in Task 8 (implemented + tested)
- `McpTransportDescriptor` — defined in Task 4, used by `StdioTransport`
- Startup banner to stderr — in `StdioTransport.RunAsync` (Task 8)
- Registry immutability — builders are internal; `Build()` returns sealed impl (Task 5)
- `OpenStreamAsync` — intentionally deferred (YAGNI, not needed by any Sprint 11 tool)
- `Principle 9 (Platform First)` — documented in ADR-0016 (Task 2)

### Placeholder Scan

No TBDs or incomplete steps found. All code blocks contain actual implementation. SDK-specific VERIFY comments indicate where the implementer must check the exact API against the installed package version — this is not a placeholder but a required action due to API variability.

### Type Consistency

Verified cross-task type usage:
- `McpArguments.From(params (string, string)[])` — defined in Task 4 (internal), used in Task 6 tool tests ✓
- `McpToolResult.Success(string)` / `Error(string)` — defined in Task 4, used in Tasks 6, 8, 9 ✓
- `IMcpToolRegistry.GetByName(string)` — defined in Task 5, used in Task 9 (McpRuntime), Task 13 ✓
- `McpModule.ConfigureServices(IServiceCollection)` — defined in Task 10, called in Task 11 (ServeCliModule) and Task 13 (integration tests) ✓
- `CommandResult.Success` (no parentheses) — static property per Global Constraints ✓
- `DocumentId.Create(string)` — used in Tasks 1 and 6 (ReadDocumentTool) consistently ✓
