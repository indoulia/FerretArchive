# Sprint 13 Sub-plan 3 — MCP Context Tool + CLI Wire-up

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose `IContextAssembler` through two channels: (1) the `ferret_context` MCP tool in `Ferret.Mcp`, and (2) the `ferret context <query>` CLI command in `Ferret.Cli`. After this sub-plan, `ferret serve` exposes `ferret_context` to Claude, and `ferret context "auth"` prints assembled context to the terminal.

**Architecture:** One new `IMcpTool` implementation in `Ferret.Mcp/Tools/ContextTool.cs` registered via `McpModule`. One new `ContextCliModule` + `ContextAssembleCommandHandler` in `Ferret.Cli/Commands/Context/`. The `context` empty-group stub is removed from `CoreCliModule`. `Program.cs` registers `ContextCliModule`.

**Tech Stack:** .NET 9, C# 13, `System.CommandLine`. Tests in `Ferret.Mcp.Tests` and `Ferret.Cli.Tests`.

## Global Constraints

- Sprint 13 s2 must be merged before starting s3.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-13):`, `test(sprint-13):`.
- `ContextTool` MCP tool name is exactly `"ferret_context"` (underscore, not hyphen).
- `ContextTool` never throws — all errors return `McpToolResult.Error(message)`.
- `ContextAssembleCommandHandler` uses `IContextAssembler` injected via DI.
- The `context` empty-group stub in `CoreCliModule` is removed in this sub-plan.
- Build command: `dotnet build src/Ferret.sln -v n`
- Test command: `dotnet test tests/Ferret.Mcp.Tests/ -v n` and `dotnet test tests/Ferret.Cli.Tests/ -v n`
- Full test: `dotnet test src/Ferret.sln -v n`

---

## File Structure Map

```
src/Ferret.Mcp/
  Tools/
    ContextTool.cs                      [NEW — Task 1]
  McpModule.cs                          [MODIFY — Task 1]

tests/Ferret.Mcp.Tests/
  Tools/
    ContextToolTests.cs                 [NEW — Task 1]

src/Ferret.Cli/
  Commands/
    Context/
      ContextCliModule.cs               [NEW — Task 2]
      ContextAssembleCommandHandler.cs  [NEW — Task 2]
    CoreCliModule.cs                    [MODIFY — Task 2]
  Program.cs                           [MODIFY — Task 2]

tests/Ferret.Cli.Tests/
  Commands/
    Context/
      ContextCliModuleTests.cs          [NEW — Task 2]
```

---

### Task 1: ContextTool (MCP)

Adds `ferret_context` to the MCP tool registry. Accepts `query`, optional `max_tokens` (default 8000), optional `max_documents` (default 10). Returns `ContextPackage.ToPromptString()` on success; `McpToolResult.Error` on failure.

**Files:**
- Create: `src/Ferret.Mcp/Tools/ContextTool.cs`
- Modify: `src/Ferret.Mcp/McpModule.cs`
- Create: `tests/Ferret.Mcp.Tests/Tools/ContextToolTests.cs`

**Interfaces:**
- Consumes: `IContextAssembler`, `ContextRequest`, `ContextPackage`, `McpArguments`, `McpToolResult`, `McpToolDescriptor`
- Produces: `ContextTool : IMcpTool`; updated `McpModule`

- [ ] **Step 1: Add `Ferret.AI` reference to `Ferret.Mcp.csproj`**

Read `src/Ferret.Mcp/Ferret.Mcp.csproj`. Add the reference to `Ferret.AI`:

```xml
<!-- ADD to the ItemGroup with ProjectReferences -->
<ProjectReference Include="..\Ferret.AI\Ferret.AI.csproj" />
```

Verify:
```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds.

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/Ferret.Mcp.Tests/Tools/ContextToolTests.cs
using Ferret.Core.Context;
using Ferret.Core.Primitives;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class ContextToolTests
{
    private sealed class StubContextAssembler(ContextPackage pkg) : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromResult(pkg);
    }

    private sealed class FailingContextAssembler : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromException<ContextPackage>(new InvalidOperationException("index offline"));
    }

    private static ContextPackage EmptyPackage(string query) => new()
    {
        Query = query,
        Documents = [],
        TotalTokenEstimate = 0,
        DocumentsConsidered = 0,
        DocumentsIncluded = 0,
        AssembledAt = DateTimeOffset.UtcNow,
    };

    private static ContextPackage PackageWithDoc(string query, string docId, string content) => new()
    {
        Query = query,
        Documents =
        [
            new ContextDocument
            {
                DocumentId = DocumentId.Create(docId),
                CanonicalUri = new Uri($"filesystem:///{docId}"),
                DisplayName = docId,
                Content = content,
                Score = 0.9f,
                TokenEstimate = content.Length / 4 + 1,
                Source = ContextDocumentSource.FullDocument,
            }
        ],
        TotalTokenEstimate = content.Length / 4 + 1,
        DocumentsConsidered = 1,
        DocumentsIncluded = 1,
        AssembledAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Descriptor_ToolName_IsFeretContext()
    {
        var tool = new ContextTool(new StubContextAssembler(EmptyPackage("q")));
        Assert.Equal("ferret_context", tool.Descriptor.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ValidQuery_ReturnsPromptString()
    {
        var pkg = PackageWithDoc("auth", "src/auth.cs", "public class Auth {}");
        var tool = new ContextTool(new StubContextAssembler(pkg));
        var args = McpArguments.FromDictionary(new Dictionary<string, object?> { ["query"] = "auth" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("auth", result.Content);
        Assert.Contains("src/auth.cs", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_NoResults_ReturnsSuccessWithMessage()
    {
        var tool = new ContextTool(new StubContextAssembler(EmptyPackage("nothing")));
        var args = McpArguments.FromDictionary(new Dictionary<string, object?> { ["query"] = "nothing" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_AssemblerThrows_ReturnsError()
    {
        var tool = new ContextTool(new FailingContextAssembler());
        var args = McpArguments.FromDictionary(new Dictionary<string, object?> { ["query"] = "test" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("index offline", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_CustomMaxTokens_PassedToAssembler()
    {
        ContextRequest? captured = null;
        var assembler = new CapturingAssembler(req =>
        {
            captured = req;
            return EmptyPackage(req.Query);
        });
        var tool = new ContextTool(assembler);
        var args = McpArguments.FromDictionary(new Dictionary<string, object?>
        {
            ["query"] = "auth",
            ["max_tokens"] = 4000,
        });

        await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(4000, captured!.MaxTokens);
    }

    private sealed class CapturingAssembler(Func<ContextRequest, ContextPackage> factory) : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromResult(factory(request));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~ContextTool" -v n
```

Expected: compile errors — `ContextTool` not found.

- [ ] **Step 4: Write ContextTool**

```csharp
// src/Ferret.Mcp/Tools/ContextTool.cs
using Ferret.Core.Context;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that assembles a complete, deduplicated, token-budgeted context package for a query.</summary>
public sealed class ContextTool : IMcpTool
{
    private readonly IContextAssembler _assembler;

    /// <summary>Initializes a new instance of the <see cref="ContextTool"/> class.</summary>
    /// <param name="assembler">The context assembly pipeline.</param>
    public ContextTool(IContextAssembler assembler)
    {
        ArgumentNullException.ThrowIfNull(assembler);
        _assembler = assembler;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "ferret_context",
        Description = "Assemble a complete, deduplicated, token-budgeted context package for a query. Returns formatted document context ready for AI consumption.",
        InputSchemaJson = """
            {
              "type": "object",
              "properties": {
                "query": {
                  "type": "string",
                  "description": "The query to assemble context for"
                },
                "max_tokens": {
                  "type": "integer",
                  "description": "Maximum token budget for the assembled context (default: 8000)"
                },
                "max_documents": {
                  "type": "integer",
                  "description": "Maximum number of documents to include (default: 10)"
                }
              },
              "required": ["query"]
            }
            """,
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var query = arguments.GetRequiredString("query");
        var maxTokens = arguments.TryGetInt32("max_tokens", out var t) ? t : 8000;
        var maxDocuments = arguments.TryGetInt32("max_documents", out var d) ? d : 10;

        var request = new ContextRequest
        {
            Query = query,
            MaxTokens = maxTokens,
            MaxDocuments = maxDocuments,
        };

        try
        {
            var package = await _assembler.AssembleAsync(request, ct).ConfigureAwait(false);
            return McpToolResult.Success(package.ToPromptString());
        }
        catch (Exception ex)
        {
            return McpToolResult.Error($"Context assembly failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 5: Register ContextTool in McpModule**

Read `src/Ferret.Mcp/McpModule.cs`. Add one line to the `ConfigureServices` method:

```csharp
// ADD after the existing tool registrations:
services.AddSingleton<IMcpTool, ContextTool>();
```

Full updated `McpModule.cs`:

```csharp
// src/Ferret.Mcp/McpModule.cs
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Resources;
using Ferret.Mcp.Runtime;
using Ferret.Mcp.Tools;
using Ferret.Mcp.Transport.Stdio;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Mcp;

/// <summary>Registers Ferret.Mcp services into a <see cref="IServiceCollection"/>.</summary>
public static class McpModule
{
    /// <summary>Registers MCP tools, resources, transport, and runtime as singletons.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMcpTool, SearchTool>();
        services.AddSingleton<IMcpTool, ReadDocumentTool>();
        services.AddSingleton<IMcpTool, WorkspaceStatusTool>();
        services.AddSingleton<IMcpTool, ContextTool>();

        services.AddSingleton<IMcpResource, WorkspaceStatusResource>();
        services.AddSingleton<IMcpResource, IndexStatsResource>();
        services.AddSingleton<IMcpResource, ConnectorsResource>();

        services.TryAddSingleton<IMcpTransport, StdioTransport>();
        services.TryAddSingleton<IMcpRuntime, McpRuntime>();

        return services;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Mcp.Tests/ --filter "FullyQualifiedName~ContextTool" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 7: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 8: Commit**

```
git add src/Ferret.Mcp/Tools/ContextTool.cs src/Ferret.Mcp/McpModule.cs tests/Ferret.Mcp.Tests/Tools/ContextToolTests.cs
git commit -m "feat(sprint-13): ContextTool MCP tool — ferret_context exposes IContextAssembler to AI hosts"
```

---

### Task 2: ContextCliModule + ferret context command

Adds `ferret context <query>` to the CLI. Removes the placeholder `context` stub from `CoreCliModule`. Registers `ContextCliModule` in `Program.cs`.

**Files:**
- Create: `src/Ferret.Cli/Commands/Context/ContextCliModule.cs`
- Create: `src/Ferret.Cli/Commands/Context/ContextAssembleCommandHandler.cs`
- Modify: `src/Ferret.Cli/Commands/CoreCliModule.cs` (remove `context` empty-group stub)
- Modify: `src/Ferret.Cli/Program.cs` (register `ContextCliModule`)
- Create: `tests/Ferret.Cli.Tests/Commands/Context/ContextCliModuleTests.cs`

**Interfaces:**
- Consumes: `IContextAssembler`, `ContextRequest`, `ContextPackage`, `IFerretContext`, `ICommandHandler`, `CliModuleBase`
- Produces: `ContextAssembleCommandHandler`, `ContextCliModule`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/Context/ContextCliModuleTests.cs
using Ferret.Cli.Commands.Context;
using Ferret.Core.Context;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Cli.Tests.Commands.Context;

public sealed class ContextCliModuleTests
{
    [Fact]
    public void ContextCliModule_HasContextCommand()
    {
        var module = new ContextCliModule();
        var commands = module.GetCommands().ToList();
        Assert.Contains(commands, c => c.Metadata.Name == "context");
    }

    [Fact]
    public void ContextCliModule_ContextCommand_HasQueryArgument()
    {
        var module = new ContextCliModule();
        var contextCmd = module.GetCommands().First(c => c.Metadata.Name == "context");
        Assert.NotNull(contextCmd.Arguments);
        Assert.Contains(contextCmd.Arguments!, a => a.Name == "query");
    }

    [Fact]
    public void ContextCliModule_ContextCommand_HasHandlerType()
    {
        var module = new ContextCliModule();
        var contextCmd = module.GetCommands().First(c => c.Metadata.Name == "context");
        Assert.Equal(typeof(ContextAssembleCommandHandler), contextCmd.HandlerType);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FullyQualifiedName~ContextCliModule" -v n
```

Expected: compile errors — `ContextCliModule` not found.

- [ ] **Step 3: Add Ferret.AI reference to Ferret.Cli.csproj**

Read `src/Ferret.Cli/Ferret.Cli.csproj`. Add the reference to `Ferret.AI`:

```xml
<!-- ADD to the ItemGroup with ProjectReferences -->
<ProjectReference Include="..\Ferret.AI\Ferret.AI.csproj" />
```

Build to verify:
```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 4: Write ContextAssembleCommandHandler**

```csharp
// src/Ferret.Cli/Commands/Context/ContextAssembleCommandHandler.cs
using System.Globalization;
using System.Text;
using Ferret.Cli.Cli;
using Ferret.Core.Context;

namespace Ferret.Cli.Commands.Context;

/// <summary>Handles the <c>ferret context &lt;query&gt;</c> command.</summary>
internal sealed class ContextAssembleCommandHandler : ICommandHandler
{
    private readonly IContextAssembler _assembler;

    /// <summary>Initializes a new instance of the <see cref="ContextAssembleCommandHandler"/> class.</summary>
    public ContextAssembleCommandHandler(IContextAssembler assembler)
    {
        ArgumentNullException.ThrowIfNull(assembler);
        _assembler = assembler;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var query = context.GetOption<string>("query");
        if (string.IsNullOrWhiteSpace(query))
        {
            context.Formatter.WriteLine("Usage: ferret context <query>");
            return CommandResult.Failure;
        }

        var request = new ContextRequest { Query = query };

        try
        {
            var package = await _assembler.AssembleAsync(request, context.CancellationToken)
                .ConfigureAwait(false);

            context.Formatter.WriteLine(CultureInfo.InvariantCulture,
                $"Assembled {package.DocumentsIncluded} document(s) (~{package.TotalTokenEstimate} tokens) from {package.DocumentsConsidered} search hit(s).");
            context.Formatter.WriteLine();

            foreach (var doc in package.Documents)
            {
                var label = doc.Title is not null
                    ? $"{doc.DisplayName} — {doc.Title}"
                    : doc.DisplayName;

                context.Formatter.WriteLine(CultureInfo.InvariantCulture,
                    $"[{package.Documents.IndexOf(doc) + 1}] {label} (score: {doc.Score:F3}, ~{doc.TokenEstimate} tokens)");

                var preview = doc.Content.Length > 500
                    ? doc.Content[..500] + "..."
                    : doc.Content;
                context.Formatter.WriteLine(preview);
                context.Formatter.WriteLine();
            }

            return CommandResult.Success;
        }
        catch (Exception ex)
        {
            context.Formatter.WriteError($"Context assembly failed: {ex.Message}");
            return CommandResult.Failure;
        }
    }
}
```

- [ ] **Step 5: Write ContextCliModule**

```csharp
// src/Ferret.Cli/Commands/Context/ContextCliModule.cs
using Ferret.AI;
using Ferret.Cli.Cli;
using Ferret.Core.Context;
using Ferret.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Context;

/// <summary>CLI module for the <c>ferret context &lt;query&gt;</c> command.
/// Registers <see cref="IContextAssembler"/> and its dependencies.</summary>
internal sealed class ContextCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.context";

    /// <inheritdoc/>
    public override string Description => "Assemble context from the workspace for a query.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("context", "Assemble token-budgeted context from the workspace for a query."),
            typeof(ContextAssembleCommandHandler),
            Options:
            [
                new OptionDefinition("--max-tokens", "Token budget for assembled context (default: 8000).", typeof(int)),
                new OptionDefinition("--max-documents", "Maximum documents to include (default: 10).", typeof(int)),
            ])
            .WithArgument("query", "Search query to assemble context for");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Search services (required by ContextAssembler via ISearchService)
        services.AddSingleton<Ferret.Core.Search.IQueryParser, QueryParser>();
        services.AddSingleton<Ferret.Core.Search.ISearchProvider, Ferret.Search.Providers.Bm25.Bm25SearchProvider>();
        services.AddSingleton<Ferret.Core.Search.ISearchService, SearchService>();

        // Context assembly services
        AiModule.ConfigureServices(services);

        services.AddSingleton<ContextAssembleCommandHandler>();
    }
}
```

- [ ] **Step 6: Remove the context stub from CoreCliModule**

Read `src/Ferret.Cli/Commands/CoreCliModule.cs`. Find and remove the `context` empty-group stub (lines that yield the `context` `CommandDefinition.EmptyGroup`):

Remove this block from `GetCommands()`:
```csharp
yield return CommandDefinition.EmptyGroup(
    "context",
    "ContextOS integration.",
    "Sprint 9",
    ["context switch", "context list"]);
```

- [ ] **Step 7: Register ContextCliModule in Program.cs**

Read `src/Ferret.Cli/Program.cs`. Add `ContextCliModule` to the module list.

Add the using:
```csharp
using Ferret.Cli.Commands.Context;
```

Add to the module list:
```csharp
new ContextCliModule(),
```

Full updated `Program.cs`:

```csharp
// src/Ferret.Cli/Program.cs
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Connector;
using Ferret.Cli.Commands.Context;
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Serve;
using Ferret.Cli.Commands.Workspace;
using Ferret.Cli.Search;
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
    new SearchCliModule(),
    new ContextCliModule(),
    new ServeCliModule(),
]).InvokeAsync(args).ConfigureAwait(false);
```

- [ ] **Step 8: Run tests to verify they pass**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FullyQualifiedName~ContextCliModule" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 9: Full solution test**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS, 0 failures.

- [ ] **Step 10: Verify CLI command is registered**

```
dotnet run --project src/Ferret.Cli -- context --help
```

Expected output contains:
```
Assemble token-budgeted context from the workspace for a query.
```

- [ ] **Step 11: Commit**

```
git add src/Ferret.Cli/Commands/Context/ContextCliModule.cs src/Ferret.Cli/Commands/Context/ContextAssembleCommandHandler.cs src/Ferret.Cli/Commands/CoreCliModule.cs src/Ferret.Cli/Program.cs tests/Ferret.Cli.Tests/Commands/Context/ContextCliModuleTests.cs
git commit -m "feat(sprint-13): ContextCliModule — ferret context <query> command; remove context stub from CoreCliModule"
```

---

## Completion Checklist

After both tasks complete:

- [ ] All new tests pass: `dotnet test tests/Ferret.Mcp.Tests/ -v n` + `dotnet test tests/Ferret.Cli.Tests/ -v n`
- [ ] Full solution passes: `dotnet test src/Ferret.sln -v n`
- [ ] `ContextTool.Descriptor.Name` is `"ferret_context"` (exact)
- [ ] `ContextTool.ExecuteAsync` never throws — all errors return `McpToolResult.Error`
- [ ] `ferret context --help` lists the `query` argument
- [ ] `ferret serve` lists `ferret_context` among available tools (verify with `ferret models list` or MCP inspector)
- [ ] The `context` placeholder stub is removed from `CoreCliModule.GetCommands()`
- [ ] `ContextCliModule` is registered in `Program.cs`
- [ ] `AiModule.ConfigureServices` is called from `ContextCliModule.ConfigureServices`
- [ ] Sprint 13 tag: `git tag v0.13.0-sprint13`

**Sprint 13 is complete when s1 + s2 + s3 all pass and the sprint tag is applied.**
