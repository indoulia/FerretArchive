# Sprint 12 Sub-Plan 6 — CLI Wireup, Architecture Tests, Solution Wiring, and ADRs

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete Sprint 12 by wiring the AI Platform and Prompt Platform into the CLI (`ferret models`, `ferret prompt`), enforcing SDK isolation via architecture tests, adding all new projects to the solution, writing ADR-0019 and ADR-0020, and tagging the sprint.

**Architecture:** `ModelsCliModule` and `PromptCliModule` are thin CLI adapters — they own no business logic. `ModelsListCommandHandler` and `ModelsInfoCommandHandler` read from `IModelRegistry` via constructor injection. `PromptListCommandHandler` reads from `IPromptRegistry`. Architecture tests enforce the SDK isolation rule from ADR-0019: `OllamaSharp.*` confined to `Ferret.Providers.Ollama`, `OpenAI.*` confined to `Ferret.Providers.OpenAi`, `Ferret.Core.Ai` has zero external package references.

**Tech Stack:** .NET 9, C# 13, xUnit, `System.CommandLine 2.0 beta`, `Microsoft.Extensions.DependencyInjection`, `System.Reflection` (architecture tests)

## Global Constraints

- s1, s2, s3, s4, s5 must be fully complete before s6. All new packages must compile.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`, `test(sprint-12):`, `docs(sprint-12):`, `chore(sprint-12):`.
- No vendor SDK types (`OllamaSharp.*`, `OpenAI.*`) outside their respective provider packages.
- No LLM API calls at runtime — Sprint 12 version gate: platform wiring only.
- Architecture tests must pass: `dotnet test tests/Ferret.Architecture.Tests/ -v n`.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.
- CLI output format must match spec exactly — tabular columns, fixed header separator.
- `CommandResult.Success` and `CommandResult.Failure` are static properties (no parentheses).

---

## File Structure Map

```
docs/adr/
  0019-ai-platform-architecture.md        [NEW — Task 1]
  0020-prompt-platform-architecture.md    [NEW — Task 1]

src/Ferret.Cli/
  Commands/Models/
    ModelsCliModule.cs                    [NEW — Task 2]
    ModelsListCommandHandler.cs           [NEW — Task 2]
    ModelsInfoCommandHandler.cs           [NEW — Task 2]
    ModelsListViewModel.cs                [NEW — Task 2]
    ModelsInfoViewModel.cs                [NEW — Task 2]
  Commands/Prompt/
    PromptCliModule.cs                    [NEW — Task 3]
    PromptListCommandHandler.cs           [NEW — Task 3]
  Program.cs                             [MODIFY — Tasks 2, 3] register new CLI modules
  Ferret.Cli.csproj                      [MODIFY — Tasks 2, 3] add Ferret.Models, Ferret.Prompts refs

tests/Ferret.Architecture.Tests/
  AiPlatformArchitectureTests.cs         [NEW — Task 4]
  Ferret.Architecture.Tests.csproj       [MODIFY — Task 4] add provider assembly refs

src/Ferret.sln                           [MODIFY — Task 5] add all new Sprint 12 projects
```

---

### Task 1: ADR-0019 and ADR-0020

Writes the two architectural decision records produced by Sprint 12. ADR-0019 captures the AI Platform Architecture decisions; ADR-0020 captures the Prompt Platform Architecture decisions. Both are status: Accepted at the time of writing.

**Files:**
- Create: `docs/adr/0019-ai-platform-architecture.md`
- Create: `docs/adr/0020-prompt-platform-architecture.md`

> No tests — documentation only.

- [ ] **Step 1: Write ADR-0019**

```markdown
# ADR-0019: AI Platform Architecture

**Status:** Accepted  
**Sprint:** 12  
**Date:** 2026-06-29

## Context

Sprint 12 introduces a first-class AI capability layer to Ferret. Multiple vendor AI SDKs (OllamaSharp, OpenAI, and future others) must be integrated without leaking vendor types into shared platform code. Provider capabilities (chat, embedding, reranking, vision) must be composable independently. The model registry must be stable after startup — no runtime mutations.

## Decisions

### 1. Ferret owns all AI contracts; vendor SDKs are confined to provider packages

All AI interfaces (`IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `IVisionModel`) and value types (`ModelId`, `ProviderId`, `ModelCapabilities`, `ModelDescriptor`) live in `Ferret.Core.Ai`. This namespace has **zero** external package references. Vendor SDKs (`OllamaSharp`, `OpenAI`) are referenced only from their respective provider packages (`Ferret.Providers.Ollama`, `Ferret.Providers.OpenAi`). No type from `OllamaSharp.*` or `OpenAI.*` namespaces appears outside its provider package.

### 2. `IModelProvider` is the unit of registration; capabilities are independent interfaces

Providers implement `IModelProvider` and vend capability implementations (`IChatModel`, `IEmbeddingModel`) on request. Capability interfaces are independent — a provider may implement chat but not embedding. Consumers depend on `IChatModel` or `IEmbeddingModel`, not on the provider directly. This enables per-model capability composition without inheritance hierarchies.

### 3. `ModelRegistry` is immutable after startup

`ModelRegistry` is built from `IEnumerable<IModelProvider>` at DI construction time. After startup, no provider or model can be added or removed. This matches the immutability pattern established for `IMcpToolRegistry` (ADR-0017). Runtime correctness guarantees derive from startup-time invariants, not from concurrent mutation guards.

### 4. Model routing is configuration-driven (`AiOptions.DefaultChatModel`)

`ModelRouter` reads `AiOptions.DefaultChatModel` and `AiOptions.DefaultEmbeddingModel` at construction. Resolution delegates to `IModelRegistry`. This keeps routing logic out of business code — callers ask for "the default chat model" and the router resolves provider + capability from configuration. Per-call overrides are possible via `ModelId` parameters on request types.

### 5. Sprint 12 version gate: no LLM calls at runtime

Sprint 12 wires the platform and exposes it via CLI (`ferret models list`, `ferret models info`). No prompt is sent to any model during `dotnet test` or normal `ferret models` usage. The version gate is: zero LLM API calls during Sprint 12. Architecture tests enforce that provider packages are correctly isolated.

## Consequences

- All future AI features in Sprints 13+ build on `Ferret.Core.Ai` contracts, never on vendor SDKs directly.
- Adding a new provider (Anthropic, Cohere, etc.) requires only a new provider package; no changes to `Ferret.Core.Ai`, `Ferret.Models`, or `Ferret.Prompts`.
- Architecture tests in `Ferret.Architecture.Tests` enforce the SDK isolation boundary continuously.
- Null memory implementations (`NullConversationMemory`, `NullWorkspaceMemory`, `NullTaskMemory`) are the defaults until Sprint 15 provides real implementations.
```

- [ ] **Step 2: Write ADR-0020**

```markdown
# ADR-0020: Prompt Platform Architecture

**Status:** Accepted  
**Sprint:** 12  
**Date:** 2026-06-29

## Context

Sprint 12 introduces a prompt template system that feature packages use to register and render structured prompts. Templates need versioning, variable substitution, and validation before Sprint 13 begins assembling context prompts. The renderer must be stateless so it can be shared across concurrent requests.

## Decisions

### 1. Templates use `{{variable}}` substitution; missing required variables are errors

`PromptTemplate` declares `RequiredVariables: IReadOnlyList<string>`. `IPromptRenderer.Render(template, variables)` substitutes all `{{variable}}` placeholders. If any required variable is absent from `PromptVariables`, `Render` throws `PromptRenderException`. `Validate(template, variables)` returns the list of missing required variables without throwing — callers use it for pre-flight checks.

### 2. `PromptRegistry` is immutable after startup

`PromptRegistry` is built from `IEnumerable<PromptTemplate>` at DI construction time and is immutable thereafter. Feature packages register templates via DI — the registry collects them at startup. This matches the immutability pattern established for `IMcpToolRegistry` (ADR-0017) and `ModelRegistry` (ADR-0019).

### 3. Templates are registered via DI (`IEnumerable<PromptTemplate>`)

Feature packages call `services.AddSingleton<PromptTemplate>(new PromptTemplate { ... })` or equivalent. `PromptsModule` collects all registered `PromptTemplate` instances from the container and passes them to `PromptRegistryBuilder`. This is the same pattern used for `IMcpTool` registrations in ADR-0017 — no central registry of template names, no magic string lookups at registration time.

### 4. Renderer is stateless

`PromptRenderer : IPromptRenderer` has no instance state. It receives all inputs via method parameters. This allows a single singleton instance to serve concurrent calls safely without locking. `PromptVariables` is an immutable builder — `.Set(key, value)` returns a new instance.

## Consequences

- Feature packages own their templates — no coupling to a central template list.
- Sprint 13 (context assembly) adds templates for workspace-context and file-summary prompts via `services.AddSingleton<PromptTemplate>(...)`.
- `ferret prompt list` shows all registered templates; Sprint 12 shows the empty-state message because no templates are registered until Sprint 13.
- Missing required variables are caught at render time, not at registration time, which keeps template registration cheap.
```

- [ ] **Step 3: Commit**

```
git add docs/adr/0019-ai-platform-architecture.md docs/adr/0020-prompt-platform-architecture.md
git commit -m "docs(sprint-12): ADR-0019 AI Platform Architecture, ADR-0020 Prompt Platform Architecture"
```

---

### Task 2: CLI — `ferret models`

Adds `ModelsCliModule`, `ModelsListCommandHandler`, `ModelsInfoCommandHandler`, and view models. `ModelsCliModule` registers the `ferret models` root command and its subcommands. Command handlers read from `IModelRegistry` via constructor injection. Output is tabular text — no JSON by default.

**Files:**
- Create: `src/Ferret.Cli/Commands/Models/ModelsListViewModel.cs`
- Create: `src/Ferret.Cli/Commands/Models/ModelsInfoViewModel.cs`
- Create: `src/Ferret.Cli/Commands/Models/ModelsListCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Models/ModelsInfoCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Models/ModelsCliModule.cs`
- Modify: `src/Ferret.Cli/Program.cs`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`

**Interfaces:**
- Consumes: `CliModuleBase`, `CommandDefinition`, `CommandMetadata`, `ICommandHandler`, `IFerretContext`, `CommandResult`, `IModelRegistry`, `ModelDescriptor`, `ModelCapabilities` from `Ferret.Core.Ai` / `Ferret.Models`
- Produces: `ModelsCliModule : CliModuleBase`, `ModelsListCommandHandler : ICommandHandler`, `ModelsInfoCommandHandler : ICommandHandler`

- [ ] **Step 1: Add Ferret.Models reference to Ferret.Cli.csproj**

Open `src/Ferret.Cli/Ferret.Cli.csproj` and add inside the project references `<ItemGroup>`:

```xml
<ProjectReference Include="..\Ferret.Models\Ferret.Models.csproj" />
```

- [ ] **Step 2: Restore and verify compile**

```
dotnet restore src/Ferret.Cli/Ferret.Cli.csproj
dotnet build src/Ferret.Cli/Ferret.Cli.csproj -v n
```

Expected: build succeeds.

- [ ] **Step 3: Write view models**

```csharp
// src/Ferret.Cli/Commands/Models/ModelsListViewModel.cs
namespace Ferret.Cli.Commands.Models;

internal sealed record ModelsListViewModel
{
    public required string Provider { get; init; }
    public required string ModelId { get; init; }
    public required string Capabilities { get; init; }
    public required string ContextWindow { get; init; }
}
```

```csharp
// src/Ferret.Cli/Commands/Models/ModelsInfoViewModel.cs
namespace Ferret.Cli.Commands.Models;

internal sealed record ModelsInfoViewModel
{
    public required string ModelId { get; init; }
    public required string Provider { get; init; }
    public required string Capabilities { get; init; }
    public required string ContextWindow { get; init; }
    public required string Status { get; init; }
}
```

- [ ] **Step 4: Write the failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/Models/ModelsListCommandHandlerTests.cs
using Ferret.Cli.Commands.Models;
using Ferret.Cli.Cli;
using Ferret.Core.Ai.Models;
using Ferret.Models;
using Xunit;

namespace Ferret.Cli.Tests.Commands.Models;

public sealed class ModelsListCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_NoModels_WritesEmptyStateMessage()
    {
        var registry = new FakeModelRegistry([]);
        var writer = new StringWriter();
        var sut = new ModelsListCommandHandler(registry);
        var context = new FakeFerretContext(writer);

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("No models", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithModels_WritesTabularOutput()
    {
        var descriptor = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "llama3.2",
            Capabilities = ModelCapabilities.Chat,
            ContextWindow = 131072
        };
        var registry = new FakeModelRegistry([descriptor]);
        var writer = new StringWriter();
        var sut = new ModelsListCommandHandler(registry);
        var context = new FakeFerretContext(writer);

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var output = writer.ToString();
        Assert.Contains("ollama", output);
        Assert.Contains("ollama/llama3.2", output);
        Assert.Contains("Chat", output);
    }

    private sealed class FakeModelRegistry(IReadOnlyList<ModelDescriptor> models) : IModelRegistry
    {
        public IReadOnlyList<ModelDescriptor> GetModels() => models;
        public ModelDescriptor? GetModel(ModelId id) => models.FirstOrDefault(m => m.Id == id);
        public IModelProvider? GetProvider(ProviderId id) => null;
        public IReadOnlyList<ProviderDescriptor> GetProviders() => [];
    }

    private sealed class FakeFerretContext(TextWriter writer) : IFerretContext
    {
        public TextWriter Out => writer;
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
```

```csharp
// tests/Ferret.Cli.Tests/Commands/Models/ModelsInfoCommandHandlerTests.cs
using Ferret.Cli.Commands.Models;
using Ferret.Cli.Cli;
using Ferret.Core.Ai.Models;
using Ferret.Models;
using Xunit;

namespace Ferret.Cli.Tests.Commands.Models;

public sealed class ModelsInfoCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_KnownModel_WritesDetailOutput()
    {
        var modelId = ModelId.Create("ollama/llama3.2");
        var descriptor = new ModelDescriptor
        {
            Id = modelId,
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "llama3.2",
            Capabilities = ModelCapabilities.Chat,
            ContextWindow = 131072
        };
        var registry = new FakeModelRegistry([descriptor]);
        var writer = new StringWriter();
        var sut = new ModelsInfoCommandHandler(registry, "ollama/llama3.2");
        var context = new FakeFerretContext(writer);

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var output = writer.ToString();
        Assert.Contains("ollama/llama3.2", output);
        Assert.Contains("Chat", output);
        Assert.Contains("128,000", output);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownModel_WritesErrorAndReturnsFailure()
    {
        var registry = new FakeModelRegistry([]);
        var writer = new StringWriter();
        var sut = new ModelsInfoCommandHandler(registry, "unknown/model");
        var context = new FakeFerretContext(writer);

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains("not found", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeModelRegistry(IReadOnlyList<ModelDescriptor> models) : IModelRegistry
    {
        public IReadOnlyList<ModelDescriptor> GetModels() => models;
        public ModelDescriptor? GetModel(ModelId id) => models.FirstOrDefault(m => m.Id == id);
        public IModelProvider? GetProvider(ProviderId id) => null;
        public IReadOnlyList<ProviderDescriptor> GetProviders() => [];
    }

    private sealed class FakeFerretContext(TextWriter writer) : IFerretContext
    {
        public TextWriter Out => writer;
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FullyQualifiedName~Models" -v n
```

Expected: compile errors — handler classes not found.

- [ ] **Step 6: Write ModelsListCommandHandler**

Implement `ModelsListCommandHandler : ICommandHandler` satisfying the tests:
- Constructor: `(IModelRegistry registry)` — null-guard
- Empty case: write `"No models are registered. Configure providers in .ferret/config.json."` and return `CommandResult.Success`
- Non-empty case: write tabular output matching the format shown below. **Reuse existing CLI table formatting utilities in `Ferret.Cli` if they exist** (check `Commands/` for any shared table/column helper); otherwise implement column-aligned output with dynamic widths:

```
Provider   Model                           Capabilities    Context
--------   -----                           ------------    -------
ollama     ollama/llama3.2                 Chat            128k
```

- Capabilities: comma-separated flag names (Chat, Embedding, Reranking, Vision) or "None"
- Context window: `{n}k` for values ≥ 1000 tokens, raw number otherwise, `—` if null

- [ ] **Step 7: Write ModelsInfoCommandHandler**

Implement `ModelsInfoCommandHandler : ICommandHandler` satisfying the tests:
- Constructor: `(IModelRegistry registry, string modelIdArg)` — null-guard both
- Look up `ModelId.Create(modelIdArg)` in the registry
- If not found: write `"Model '{id}' not found. Run 'ferret models list' to see available models."` and return `CommandResult.Failure`
- If found: write labeled detail lines matching the test assertions (Model, Provider, Capabilities, Context, Status). Context in `N0` tokens format (e.g. `128,000 tokens`) or `—` if null.

- [ ] **Step 8: Write ModelsCliModule**

Implement `ModelsCliModule : CliModuleBase` (internal sealed):
- `Name`: `"ferret.models"`, `Description`: `"AI model registry commands."`
- `GetCommands()`: yield two `CommandDefinition`s — `"models list"` (no args) and `"models info"` (with a `model-id` argument)
- `ConfigureServices`: call `ModelPlatformModule.ConfigureServices(services)`; register `ModelsListCommandHandler` as singleton; register `ModelsInfoCommandHandler` following the **same argument-binding pattern used by existing CLI modules** (check `SearchCliModule` or `IndexCliModule` for how `System.CommandLine` argument values are passed to handler constructors at invocation time)

- [ ] **Step 9: Run tests to verify they pass**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FullyQualifiedName~Models" -v n
```

Expected: all models handler tests PASS.

- [ ] **Step 10: Update Program.cs**

Add `ModelsCliModule` to the modules array in `src/Ferret.Cli/Program.cs`:

```csharp
// Add at top with other usings:
using Ferret.Cli.Commands.Models;

// Add to the modules array in RootCommandFactory.Build([...]):
new ModelsCliModule(),
```

The `Program.cs` modules array after this change:

```csharp
return await RootCommandFactory.Build([
    new CoreCliModule(),
    new WorkspaceCliModule(),
    new ConnectorCliModule([filesystemFactory]),
    new IndexCliModule(workspaceContext),
    new SearchCliModule(),
    new ServeCliModule(),
    new ModelsCliModule(),
]).InvokeAsync(args).ConfigureAwait(false);
```

- [ ] **Step 11: Smoke test `ferret models list`**

```
dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- models list
```

Expected Sprint 12 output (no providers configured by default):

```
No models are registered. Configure providers in .ferret/config.json.
```

- [ ] **Step 12: Build the full solution**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds with no errors.

- [ ] **Step 13: Commit**

```
git add src/Ferret.Cli/Commands/Models/ src/Ferret.Cli/Program.cs src/Ferret.Cli/Ferret.Cli.csproj
git commit -m "feat(sprint-12): ModelsCliModule — ferret models list + ferret models info commands"
```

---

### Task 3: CLI — `ferret prompt`

Adds `PromptCliModule` and `PromptListCommandHandler`. `PromptListCommandHandler` reads from `IPromptRegistry` and writes tabular output (or the empty-state message when no templates are registered). Sprint 12 ships with no templates registered, so the empty-state path is the primary tested scenario.

**Files:**
- Create: `src/Ferret.Cli/Commands/Prompt/PromptListCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Prompt/PromptCliModule.cs`
- Modify: `src/Ferret.Cli/Program.cs`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`

**Interfaces:**
- Consumes: `CliModuleBase`, `CommandDefinition`, `CommandMetadata`, `ICommandHandler`, `IFerretContext`, `CommandResult`, `IPromptRegistry`, `PromptTemplate` from `Ferret.Prompts`
- Produces: `PromptCliModule : CliModuleBase`, `PromptListCommandHandler : ICommandHandler`

- [ ] **Step 1: Add Ferret.Prompts reference to Ferret.Cli.csproj**

Open `src/Ferret.Cli/Ferret.Cli.csproj` and add inside the project references `<ItemGroup>`:

```xml
<ProjectReference Include="..\Ferret.Prompts\Ferret.Prompts.csproj" />
```

- [ ] **Step 2: Restore and verify compile**

```
dotnet restore src/Ferret.Cli/Ferret.Cli.csproj
dotnet build src/Ferret.Cli/Ferret.Cli.csproj -v n
```

Expected: build succeeds.

- [ ] **Step 3: Write the failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/Prompt/PromptListCommandHandlerTests.cs
using Ferret.Cli.Commands.Prompt;
using Ferret.Cli.Cli;
using Ferret.Prompts;
using Xunit;

namespace Ferret.Cli.Tests.Commands.Prompt;

public sealed class PromptListCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_NoTemplates_WritesEmptyStateMessage()
    {
        var registry = new FakePromptRegistry([]);
        var writer = new StringWriter();
        var sut = new PromptListCommandHandler(registry);
        var context = new FakeFerretContext(writer);

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("No prompt templates", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplates_WritesTabularOutput()
    {
        var template = new PromptTemplate
        {
            Name = "workspace-context",
            Version = "1.0.0",
            Template = "Hello {{workspace_name}}",
            RequiredVariables = ["workspace_name"],
            Description = "Context assembly prompt"
        };
        var registry = new FakePromptRegistry([template]);
        var writer = new StringWriter();
        var sut = new PromptListCommandHandler(registry);
        var context = new FakeFerretContext(writer);

        var result = await sut.ExecuteAsync(context);

        Assert.Equal(CommandResult.Success, result);
        var output = writer.ToString();
        Assert.Contains("workspace-context", output);
        Assert.Contains("1.0.0", output);
        Assert.Contains("workspace_name", output);
    }

    private sealed class FakePromptRegistry(IReadOnlyList<PromptTemplate> templates) : IPromptRegistry
    {
        public IReadOnlyList<PromptTemplate> GetAll() => templates;
        public PromptTemplate? GetByName(string name) => templates.FirstOrDefault(t => t.Name == name);
    }

    private sealed class FakeFerretContext(TextWriter writer) : IFerretContext
    {
        public TextWriter Out => writer;
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FullyQualifiedName~PromptList" -v n
```

Expected: compile errors — handler class not found.

- [ ] **Step 5: Write PromptListCommandHandler**

Implement `PromptListCommandHandler : ICommandHandler` satisfying the tests:
- Constructor: `(IPromptRegistry registry)` — null-guard
- Empty case: write `"No prompt templates are registered. Templates are added by feature packages."` and return `CommandResult.Success`
- Non-empty case: write tabular output matching the format shown below. **Reuse existing CLI table formatting utilities in `Ferret.Cli` if available**; otherwise implement column-aligned output with dynamic widths:

```
Name                    Version    Variables              Description
----                    -------    ---------              -----------
workspace-context       1.0.0      workspace_name,files   Context assembly prompt
```

- Variables: comma-separated `RequiredVariables` or `—` if none

- [ ] **Step 6: Write PromptCliModule**

```csharp
// src/Ferret.Cli/Commands/Prompt/PromptCliModule.cs
using Ferret.Cli.Cli;
using Ferret.Prompts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Cli.Commands.Prompt;

/// <summary>Registers the <c>ferret prompt</c> command and subcommands.</summary>
internal sealed class PromptCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.prompt";

    /// <inheritdoc/>
    public override string Description => "Prompt template commands.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("prompt list", "List all registered prompt templates."),
            typeof(PromptListCommandHandler));
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        PromptsModule.ConfigureServices(services);
        services.AddSingleton<PromptListCommandHandler>();
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FullyQualifiedName~PromptList" -v n
```

Expected: all prompt handler tests PASS.

- [ ] **Step 8: Update Program.cs**

Add `PromptCliModule` to the modules array in `src/Ferret.Cli/Program.cs`:

```csharp
// Add at top with other usings:
using Ferret.Cli.Commands.Prompt;

// Add to the modules array in RootCommandFactory.Build([...]):
new PromptCliModule(),
```

The `Program.cs` modules array after this change:

```csharp
return await RootCommandFactory.Build([
    new CoreCliModule(),
    new WorkspaceCliModule(),
    new ConnectorCliModule([filesystemFactory]),
    new IndexCliModule(workspaceContext),
    new SearchCliModule(),
    new ServeCliModule(),
    new ModelsCliModule(),
    new PromptCliModule(),
]).InvokeAsync(args).ConfigureAwait(false);
```

- [ ] **Step 9: Smoke test `ferret prompt list`**

```
dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- prompt list
```

Expected Sprint 12 output:

```
No prompt templates are registered. Templates are added by feature packages.
```

- [ ] **Step 10: Smoke test `ferret --help`**

```
dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- --help
```

Expected: output includes `models` and `prompt` commands in the list.

- [ ] **Step 11: Build the full solution**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds with no errors.

- [ ] **Step 12: Commit**

```
git add src/Ferret.Cli/Commands/Prompt/ src/Ferret.Cli/Program.cs src/Ferret.Cli/Ferret.Cli.csproj
git commit -m "feat(sprint-12): PromptCliModule — ferret prompt list command"
```

---

### Task 4: Architecture Tests — AI Platform SDK Isolation Rules

Adds `AiPlatformArchitectureTests.cs` to `Ferret.Architecture.Tests`, enforcing the SDK isolation invariant from ADR-0019. Follows the same reflection-based pattern as `McpArchitectureTests.cs`.

**Files:**
- Create: `tests/Ferret.Architecture.Tests/AiPlatformArchitectureTests.cs`
- Modify: `tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj` (add provider assembly refs if missing)

**Interfaces:**
- Consumes: `Ferret.Models` (for `IModelRegistry` anchor), `Ferret.Prompts` (for `IPromptRegistry` anchor), `Ferret.Providers.Ollama` (for `OllamaModelProvider` anchor), `Ferret.Providers.OpenAi` (for `OpenAiModelProvider` anchor), `Ferret.Core.Ai` (for `IModelProvider` anchor), `System.Reflection`
- Produces: executable architecture rules

- [ ] **Step 1: Add project references to Ferret.Architecture.Tests.csproj if missing**

Check current `tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj`. If any of these refs are absent, add them:

```xml
<ProjectReference Include="..\..\src\Ferret.Models\Ferret.Models.csproj" />
<ProjectReference Include="..\..\src\Ferret.Prompts\Ferret.Prompts.csproj" />
<ProjectReference Include="..\..\src\Ferret.Providers.Ollama\Ferret.Providers.Ollama.csproj" />
<ProjectReference Include="..\..\src\Ferret.Providers.OpenAi\Ferret.Providers.OpenAi.csproj" />
```

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/Ferret.Architecture.Tests/AiPlatformArchitectureTests.cs
using System.Reflection;
using Ferret.Core.Ai.Interfaces;
using Ferret.Models;
using Ferret.Prompts;
using Ferret.Providers.Ollama;
using Ferret.Providers.OpenAi;
using Xunit;

namespace Ferret.Architecture.Tests;

/// <summary>Architectural compliance tests enforcing ADR-0019 AI Platform SDK isolation rules.</summary>
public sealed class AiPlatformArchitectureTests
{
    private const string OllamaPrefix = "OllamaSharp";
    private const string OpenAiPrefix = "OpenAI";

    private static readonly Assembly ModelsAssembly = typeof(IModelRegistry).Assembly;
    private static readonly Assembly PromptsAssembly = typeof(IPromptRegistry).Assembly;
    private static readonly Assembly OllamaAssembly = typeof(OllamaModelProvider).Assembly;
    private static readonly Assembly OpenAiAssembly = typeof(OpenAiModelProvider).Assembly;

    /// <summary>No type in Ferret.Models or Ferret.Prompts may reference OllamaSharp or OpenAI namespace types.</summary>
    [Fact]
    public void ModelsAndPrompts_MustNot_Reference_VendorSdkTypes()
    {
        var assemblies = new[] { ModelsAssembly, PromptsAssembly };
        var violations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => TypeReferencesVendorSdk(t))
            .Select(t => $"{t.Assembly.GetName().Name}/{t.FullName}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Ferret.Models and Ferret.Prompts must not reference vendor SDK types. Violating types:\n{string.Join("\n", violations)}");
    }

    /// <summary>No type in Ferret.Core.Ai references any external NuGet assembly (only BCL allowed).</summary>
    [Fact]
    public void CoreAi_MustNot_Reference_AnyExternalPackage()
    {
        // Ferret.Core.Ai is compiled into the Ferret.Core assembly — anchor via IModelProvider
        var coreAiAssembly = typeof(IModelProvider).Assembly;
        var bcl = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System", "System.Core", "System.Runtime", "System.Collections",
            "System.Threading", "System.Threading.Tasks", "System.Linq",
            "System.Text", "System.IO", "mscorlib", "netstandard"
        };

        var violations = coreAiAssembly
            .GetReferencedAssemblies()
            .Where(a =>
            {
                var name = a.Name ?? string.Empty;
                return !name.StartsWith("Ferret.", StringComparison.Ordinal)
                    && !name.StartsWith("System.", StringComparison.Ordinal)
                    && !name.StartsWith("Microsoft.", StringComparison.Ordinal)
                    && !bcl.Contains(name);
            })
            .Select(a => a.Name!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Ferret.Core.Ai must not reference external packages. Found references to:\n{string.Join("\n", violations)}");
    }

    /// <summary>OllamaSharp types must only appear in Ferret.Providers.Ollama.</summary>
    [Fact]
    public void OllamaSharp_Types_MustOnly_ExistIn_OllamaProviderAssembly()
    {
        // Verify OllamaSharp types do not appear in the Models or Prompts assemblies
        var forbidden = new[] { ModelsAssembly, PromptsAssembly };
        var violations = forbidden
            .SelectMany(a => a.GetTypes())
            .Where(t => TypeReferencesNamespace(t, OllamaPrefix))
            .Select(t => $"{t.Assembly.GetName().Name}/{t.FullName}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"OllamaSharp types must not appear outside Ferret.Providers.Ollama. Violating types:\n{string.Join("\n", violations)}");
    }

    /// <summary>OpenAI types must only appear in Ferret.Providers.OpenAi.</summary>
    [Fact]
    public void OpenAi_Types_MustOnly_ExistIn_OpenAiProviderAssembly()
    {
        var forbidden = new[] { ModelsAssembly, PromptsAssembly };
        var violations = forbidden
            .SelectMany(a => a.GetTypes())
            .Where(t => TypeReferencesNamespace(t, OpenAiPrefix))
            .Select(t => $"{t.Assembly.GetName().Name}/{t.FullName}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"OpenAI types must not appear outside Ferret.Providers.OpenAi. Violating types:\n{string.Join("\n", violations)}");
    }

    /// <summary>All IModelProvider implementations must be sealed.</summary>
    [Fact]
    public void IModelProvider_Implementations_Must_Be_Sealed()
    {
        var providerInterface = typeof(IModelProvider);
        var assemblies = new[] { OllamaAssembly, OpenAiAssembly };

        var violations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => providerInterface.IsAssignableFrom(t) && t.IsClass && !t.IsSealed)
            .Select(t => t.FullName!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"IModelProvider implementations must be sealed. Non-sealed: {string.Join(", ", violations)}");
    }

    private static bool TypeReferencesVendorSdk(Type type) =>
        TypeReferencesNamespace(type, OllamaPrefix) || TypeReferencesNamespace(type, OpenAiPrefix);

    private static bool TypeReferencesNamespace(Type type, string prefix)
    {
        try
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .Any(f => f.FieldType.FullName?.StartsWith(prefix, StringComparison.Ordinal) ?? false)
                    || type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .SelectMany(m => m.GetParameters())
                        .Any(p => p.ParameterType.FullName?.StartsWith(prefix, StringComparison.Ordinal) ?? false)
                    || type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .Any(m => m.ReturnType.FullName?.StartsWith(prefix, StringComparison.Ordinal) ?? false);
        }
        catch (ReflectionTypeLoadException)
        {
            return false;
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```
dotnet test tests/Ferret.Architecture.Tests/ --filter "FullyQualifiedName~AiPlatformArchitecture" -v n
```

Expected: compile errors if assembly refs are missing, or test failures if isolation is not yet in place. Fix compile errors first (Step 1 above), then re-run.

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Architecture.Tests/ --filter "FullyQualifiedName~AiPlatformArchitecture" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Full architecture tests still pass**

```
dotnet test tests/Ferret.Architecture.Tests/ -v n
```

Expected: all architecture tests PASS (including McpArchitectureTests from Sprint 11).

- [ ] **Step 6: Commit**

```
git add tests/Ferret.Architecture.Tests/AiPlatformArchitectureTests.cs tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj
git commit -m "test(sprint-12): architecture rules — SDK isolation for AI Platform (ADR-0019)"
```

---

### Task 5: Solution Wiring + Full Sprint Validation

Adds all new Sprint 12 projects to `Ferret.sln`, runs a full build, runs all tests, and verifies sprint completion. This is the integration checkpoint — everything must pass before the sprint tag is applied.

**Files:**
- Modify: `src/Ferret.sln`

- [ ] **Step 1: Add all new Sprint 12 projects to the solution**

Run from the repo root (check that each project file exists before adding):

```
dotnet sln src/Ferret.sln add src/Ferret.Core/Ferret.Core.csproj
dotnet sln src/Ferret.sln add src/Ferret.Models/Ferret.Models.csproj
dotnet sln src/Ferret.sln add src/Ferret.Prompts/Ferret.Prompts.csproj
dotnet sln src/Ferret.sln add src/Ferret.AI/Ferret.AI.csproj
dotnet sln src/Ferret.sln add src/Ferret.Configuration.AI/Ferret.Configuration.AI.csproj
dotnet sln src/Ferret.sln add src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj
dotnet sln src/Ferret.sln add src/Ferret.Providers.OpenAi/Ferret.Providers.OpenAi.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Models.Tests/Ferret.Models.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Prompts.Tests/Ferret.Prompts.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Providers.OpenAi.Tests/Ferret.Providers.OpenAi.Tests.csproj
```

> **Note:** `Ferret.Core.csproj` is likely already in the solution — `dotnet sln add` is idempotent, so running it again is safe. Skip any project that does not exist on disk yet (indicates the corresponding sub-plan is not complete — do not proceed until all s1–s5 projects are present).

- [ ] **Step 2: Verify the solution file lists all expected projects**

```
dotnet sln src/Ferret.sln list
```

Confirm the following project paths appear in the output:
- `src/Ferret.Core/Ferret.Core.csproj`
- `src/Ferret.Models/Ferret.Models.csproj`
- `src/Ferret.Prompts/Ferret.Prompts.csproj`
- `src/Ferret.AI/Ferret.AI.csproj`
- `src/Ferret.Configuration.AI/Ferret.Configuration.AI.csproj`
- `src/Ferret.Providers.Ollama/Ferret.Providers.Ollama.csproj`
- `src/Ferret.Providers.OpenAi/Ferret.Providers.OpenAi.csproj`
- `src/Ferret.Cli/Ferret.Cli.csproj`
- `tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj`
- `tests/Ferret.Models.Tests/Ferret.Models.Tests.csproj`
- `tests/Ferret.Prompts.Tests/Ferret.Prompts.Tests.csproj`
- `tests/Ferret.Providers.Ollama.Tests/Ferret.Providers.Ollama.Tests.csproj`
- `tests/Ferret.Providers.OpenAi.Tests/Ferret.Providers.OpenAi.Tests.csproj`

- [ ] **Step 3: Full solution build**

```
dotnet build src/Ferret.sln -v n
```

Expected: all projects build with zero errors. Record any warnings for the sprint summary commit message.

- [ ] **Step 4: Full solution test run**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. Record the total test count (it will increase significantly from Sprint 11's baseline).

- [ ] **Step 5: Architecture tests pass specifically**

```
dotnet test tests/Ferret.Architecture.Tests/ -v n
```

Expected: all architecture tests pass (MCP + AI Platform rules).

- [ ] **Step 6: Apply sprint tag**

```
git tag v0.12.0-sprint12
```

- [ ] **Step 7: Commit solution wiring**

```
git add src/Ferret.sln
git commit -m "chore(sprint-12): add Sprint 12 projects to Ferret.sln — AI Platform, Providers, Prompts"
```

---

### Task 6: Sprint Summary Commit

Closes Sprint 12. Updates living documents (PROJECT-STATE.md, ROADMAP) and stages the `.claude/` worktree state alongside the documentation updates in the final commit. The sprint tag (`v0.12.0-sprint12`) is applied in Task 5 Step 6 — this task commits the docs and state only.

**Files:**
- Modify: `docs/000-Overview/PROJECT-STATE.md`
- Modify: `docs/001-Product/ROADMAP-001.md` (if it exists)
- Stage: `.claude/` (settings, memory, worktree state)

- [ ] **Step 1: Update PROJECT-STATE.md**

Add a Sprint 12 entry in the "Completed Sprints" section:

```markdown
## Sprint 12 — AI Platform Foundation

**Status:** Complete  
**Tag:** `v0.12.0-sprint12`  
**Date:** 2026-06-29

### Delivered

- `Ferret.Core.Ai` — AI contracts: `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `IVisionModel`, memory interfaces, null memory implementations, all value types and request/response models
- `Ferret.Models` — `ModelRegistry`, `ModelRouter`, `ModelPlatformModule`, `IModelRegistry`, `IModelRouter`
- `Ferret.Prompts` — `PromptTemplate`, `PromptVariables`, `PromptRegistry`, `PromptRenderer`, `IPromptRegistry`, `IPromptRenderer`, `PromptRenderException`, `PromptsModule`
- `Ferret.AI` — empty orchestration scaffold; `AiModule` stub
- `Ferret.Configuration.AI` — `AiOptions`, `OllamaOptions`, `OpenAiOptions`, `ProviderOptions`, `AiConfigurationModule`
- `Ferret.Providers.Ollama` — `OllamaModelProvider`, `OllamaChatModel`, `OllamaEmbeddingModel`, `OllamaProviderModule`
- `Ferret.Providers.OpenAi` — `OpenAiModelProvider`, `OpenAiChatModel`, `OpenAiEmbeddingModel`, `OpenAiProviderModule`
- `ModelsCliModule` — `ferret models list`, `ferret models info <model-id>`
- `PromptCliModule` — `ferret prompt list`
- ADR-0019: AI Platform Architecture
- ADR-0020: Prompt Platform Architecture
- `AiPlatformArchitectureTests` — SDK isolation rules for Ollama + OpenAI providers

### Version Gate

Zero LLM API calls in Sprint 12. `ferret models list` reads from `ModelRegistry`; no model inference occurs. Real AI features begin in Sprint 13 (context assembly) and Sprint 14 (`ferret ask`).
```

- [ ] **Step 2: Update ROADMAP if it exists**

If `docs/001-Product/ROADMAP-001.md` exists, mark Sprint 12 as Complete and update the current sprint pointer to Sprint 13.

- [ ] **Step 3: Commit sprint summary**

```
git add docs/000-Overview/PROJECT-STATE.md docs/001-Product/ROADMAP-001.md .claude/
git commit -m "docs(sprint-12): PROJECT-STATE — Sprint 12 AI Platform Foundation complete"
```

---

## Completion Checklist

Before declaring Sprint 12 complete:

- [ ] `dotnet build src/Ferret.sln -v n` — zero errors
- [ ] `dotnet test src/Ferret.sln -v n` — all tests pass
- [ ] `dotnet test tests/Ferret.Architecture.Tests/ -v n` — all architecture tests pass (MCP + AI Platform)
- [ ] `dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- models list` — returns no-models message or tabular output
- [ ] `dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- prompt list` — returns empty-state message
- [ ] `dotnet run --project src/Ferret.Cli/Ferret.Cli.csproj -- --help` — lists `models` and `prompt` commands
- [ ] Git tag `v0.12.0-sprint12` applied
- [ ] ADR-0019 and ADR-0020 committed to `docs/adr/`
- [ ] PROJECT-STATE.md updated
