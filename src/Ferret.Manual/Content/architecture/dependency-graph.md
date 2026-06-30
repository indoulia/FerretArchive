# Dependency Graph

Ferret enforces strict one-way dependencies. Lower-layer packages never reference higher-layer packages. This is verified by architecture tests.

## Package Reference Diagram

```
Ferret.Cli
  ├── Ferret.Runtime
  ├── Ferret.Hosting
  ├── Ferret.Workspace
  ├── Ferret.Indexing
  ├── Ferret.Search
  ├── Ferret.Models
  ├── Ferret.Prompts
  ├── Ferret.Mcp
  ├── Ferret.Manual
  ├── Ferret.Ai
  ├── Ferret.Providers.Ollama
  └── Ferret.Providers.OpenAi

Ferret.Mcp
  ├── Ferret.Core       (IMcpTool, McpArguments, McpToolResult)
  └── Ferret.Search     (ISearchService)

Ferret.Indexing
  ├── Ferret.Core       (IIndexPipeline, IContentParser)
  └── Ferret.Workspace  (IWorkspaceLocator)

Ferret.Search
  └── Ferret.Core       (ISearchProvider, SearchQuery, SearchResult)

Ferret.Workspace
  └── Ferret.Core       (IWorkspaceEngine, WorkspaceContext)

Ferret.Models
  └── Ferret.Core       (IModelProvider, IModelRegistry)

Ferret.Prompts
  └── Ferret.Core       (IPromptRegistry, IPromptRenderer)

Ferret.Providers.Ollama
  └── Ferret.Core       (IModelProvider, IChatModel, IEmbeddingModel)

Ferret.Providers.OpenAi
  └── Ferret.Core       (IModelProvider, IChatModel, IEmbeddingModel)

Ferret.Runtime
  └── Ferret.Core       (IRuntimeHost, IModule, IModuleRegistry)

Ferret.Core
  └── (no Ferret dependencies — only BCL and Microsoft.Extensions.*)
```

## Key Boundaries

**Ferret.Core has no Ferret dependencies.** Every other package depends on `Ferret.Core` for contracts. This is the single point of truth for all interfaces.

**Provider packages are isolated.** `Ferret.Providers.Ollama` references `OllamaSharp`; `Ferret.Providers.OpenAi` references `OpenAI`. Neither SDK type leaks outside its provider package (enforced by ADR-0019).

**MCP SDK is confined.** `ModelContextProtocol.*` types appear only in `Ferret.Mcp/Transport/Stdio/`. Nothing outside that folder imports SDK namespaces (enforced by ADR-0017).

**No circular dependencies.** Architecture tests assert zero cycles between Ferret packages.

## Architecture Tests

`Ferret.Architecture.Tests` enforces these boundaries continuously:

```csharp
// Example: Ferret.Core must not reference feature packages
[Fact]
public void Core_Has_No_Feature_Dependencies()
{
    typeof(FerretException).Assembly
        .GetReferencedAssemblies()
        .Should().NotContain(a => a.Name!.StartsWith("Ferret.Indexing"));
}
```

## Related

- [Platform Overview](platform-overview) — the full layer stack
- [Extension Points](extension-points) — how to add a new package correctly
