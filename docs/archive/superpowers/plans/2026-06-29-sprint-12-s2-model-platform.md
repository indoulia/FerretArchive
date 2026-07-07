# Sprint 12 Sub-plan 2: Model Platform

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver `Ferret.Configuration.AI` (POCO options binding), `Ferret.Models` (immutable `ModelRegistry`, configuration-driven `ModelRouter`, `ModelNotFoundException`, DI composition via `ModelPlatformModule`), and the `Ferret.AI` empty scaffold package.

**Architecture:** `Ferret.Configuration.AI` owns all POCO options classes and the `AiConfigurationModule` that binds them from `Ferret:Ai`. `Ferret.Models` depends on `Ferret.Configuration.AI` and `Ferret.Core` AI contracts (from s1); it owns the registry and router. No vendor SDK types appear in any of these packages — SDK isolation is fully in the provider packages (s3, s4). `Ferret.AI` is a placeholder that Sprint 13 fills with context assembly.

**Tech Stack:** .NET 9, C# 13, xUnit, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`

## Global Constraints

- Sprint 12 s1 (AI Core Contracts) must be fully implemented before s2. Assumes all types in `Ferret.Core.Ai` are available: `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `ModelId`, `ProviderId`, `ModelDescriptor`, `ProviderDescriptor`, `ModelCapabilities`.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`, `test(sprint-12):`, `chore(sprint-12):`, `docs(sprint-12):`.
- No vendor SDK types in `Ferret.Configuration.AI`, `Ferret.Models`, or `Ferret.AI`.
- No model is called at runtime in Sprint 12. Zero LLM API calls during `dotnet test`.
- `ModelRegistry` is immutable after construction — no public mutating methods.
- `ModelRegistry` construction calls `ListModelsAsync` on each provider; use the static `CreateAsync` factory pattern (async construction) to avoid blocking a constructor.
- Every registry is immutable after startup (built once, never mutated at runtime).
- Architecture tests must pass: `dotnet test tests/Ferret.Architecture.Tests/ -v n`.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.
- Build command: `dotnet build src/Ferret.sln -v n`.

---

## File Structure Map

```
src/Ferret.Configuration.AI/
  Ferret.Configuration.AI.csproj    [NEW — Task 1]
  AiOptions.cs                      [NEW — Task 1]
  ProviderOptions.cs                [NEW — Task 1]
  OllamaOptions.cs                  [NEW — Task 1]
  OpenAiOptions.cs                  [NEW — Task 1]
  AiConfigurationModule.cs          [NEW — Task 1]

tests/Ferret.Configuration.AI.Tests/
  Ferret.Configuration.AI.Tests.csproj [NEW — Task 1]
  AiOptionsTests.cs                 [NEW — Task 1]
  AiConfigurationModuleTests.cs     [NEW — Task 1]

src/Ferret.Models/
  Ferret.Models.csproj              [NEW — Task 2]
  IModelRegistry.cs                 [NEW — Task 2]
  ModelRegistry.cs                  [NEW — Task 2]

tests/Ferret.Models.Tests/
  Ferret.Models.Tests.csproj        [NEW — Task 2]
  ModelRegistryTests.cs             [NEW — Task 2]

src/Ferret.Models/
  IModelRouter.cs                   [NEW — Task 3]
  ModelRouter.cs                    [NEW — Task 3]
  Exceptions/
    ModelNotFoundException.cs       [NEW — Task 3]

tests/Ferret.Models.Tests/
  ModelRouterTests.cs               [NEW — Task 3]

src/Ferret.Models/
  ModelPlatformModule.cs            [NEW — Task 4]

src/Ferret.AI/
  Ferret.AI.csproj                  [NEW — Task 4]
  AiModule.cs                       [NEW — Task 4]

src/Ferret.sln                      [MODIFY — Task 4] add 3 new projects
```

---

### Task 1: `Ferret.Configuration.AI` — AiOptions, OllamaOptions, OpenAiOptions, AiConfigurationModule

POCO options classes that bind from the `Ferret:Ai` configuration section. No logic lives here — only defaults and DI registration. `AiConfigurationModule` uses `IOptions<AiOptions>` to expose configuration to downstream services.

**Files:**
- Create: `src/Ferret.Configuration.AI/Ferret.Configuration.AI.csproj`
- Create: `src/Ferret.Configuration.AI/ProviderOptions.cs`
- Create: `src/Ferret.Configuration.AI/AiOptions.cs`
- Create: `src/Ferret.Configuration.AI/OllamaOptions.cs`
- Create: `src/Ferret.Configuration.AI/OpenAiOptions.cs`
- Create: `src/Ferret.Configuration.AI/AiConfigurationModule.cs`
- Create: `tests/Ferret.Configuration.AI.Tests/Ferret.Configuration.AI.Tests.csproj`
- Create: `tests/Ferret.Configuration.AI.Tests/AiOptionsTests.cs`
- Create: `tests/Ferret.Configuration.AI.Tests/AiConfigurationModuleTests.cs`

**Interfaces:**
- Consumes: `Microsoft.Extensions.Options`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Ferret.Core` (shared base types if needed)
- Produces: `AiOptions`, `ProviderOptions`, `OllamaOptions`, `OpenAiOptions`, `AiConfigurationModule`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Configuration.AI.Tests/AiOptionsTests.cs
using Ferret.Configuration.Ai;
using Xunit;

namespace Ferret.Configuration.Ai.Tests;

public sealed class AiOptionsTests
{
    [Fact]
    public void AiOptions_Defaults_AreCorrect()
    {
        var options = new AiOptions();

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
        Assert.Equal("ollama/nomic-embed-text", options.DefaultEmbeddingModel);
        Assert.Null(options.DefaultReranker);
        Assert.NotNull(options.Providers);
        Assert.Empty(options.Providers);
    }

    [Fact]
    public void OllamaOptions_Defaults_AreCorrect()
    {
        var options = new OllamaOptions();

        Assert.True(options.Enabled);
        Assert.Equal("http://localhost:11434", options.BaseUrl);
        Assert.Equal(120, options.TimeoutSeconds);
        Assert.Null(options.ApiKey);
    }

    [Fact]
    public void OpenAiOptions_Defaults_AreCorrect()
    {
        var options = new OpenAiOptions();

        Assert.True(options.Enabled);
        Assert.Equal("https://api.openai.com/v1", options.BaseUrl);
        Assert.Equal(60, options.TimeoutSeconds);
    }

    [Fact]
    public void ProviderOptions_Defaults_AreCorrect()
    {
        var options = new ProviderOptions();

        Assert.True(options.Enabled);
        Assert.Equal(string.Empty, options.BaseUrl);
        Assert.Null(options.ApiKey);
        Assert.Equal(60, options.TimeoutSeconds);
    }
}
```

```csharp
// tests/Ferret.Configuration.AI.Tests/AiConfigurationModuleTests.cs
using Ferret.Configuration.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ferret.Configuration.Ai.Tests;

public sealed class AiConfigurationModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersAiOptions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:DefaultChatModel"] = "ollama/llama3.2",
                ["Ferret:Ai:DefaultEmbeddingModel"] = "ollama/nomic-embed-text"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
        Assert.Equal("ollama/nomic-embed-text", options.DefaultEmbeddingModel);
    }

    [Fact]
    public void ConfigureServices_EmptyConfig_UsesDefaults()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Configuration.AI.Tests/ -v n
```

Expected: compile errors — project and types not found.

- [ ] **Step 3: Create the csproj**

```xml
<!-- src/Ferret.Configuration.AI/Ferret.Configuration.AI.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Configuration.AI</AssemblyName>
    <RootNamespace>Ferret.Configuration.Ai</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

> **Note:** Check `Directory.Packages.props` for the version entries for `Microsoft.Extensions.Options.ConfigurationExtensions` and `Microsoft.Extensions.Configuration.Binder`. Add them if absent.

- [ ] **Step 4: Write the options classes**

```csharp
// src/Ferret.Configuration.AI/ProviderOptions.cs
namespace Ferret.Configuration.Ai;

/// <summary>Base configuration for any AI model provider.</summary>
public class ProviderOptions
{
    /// <summary>Whether this provider is active. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Provider API base URL.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Optional API key. Null means no key required (e.g. local Ollama).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Request timeout in seconds. Default: 60.</summary>
    public int TimeoutSeconds { get; set; } = 60;
}
```

```csharp
// src/Ferret.Configuration.AI/OllamaOptions.cs
namespace Ferret.Configuration.Ai;

/// <summary>Configuration for the Ollama provider.</summary>
public sealed class OllamaOptions : ProviderOptions
{
    /// <summary>Initializes a new instance of <see cref="OllamaOptions"/> with Ollama defaults.</summary>
    public OllamaOptions()
    {
        BaseUrl = "http://localhost:11434";
        TimeoutSeconds = 120;
    }
}
```

```csharp
// src/Ferret.Configuration.AI/OpenAiOptions.cs
namespace Ferret.Configuration.Ai;

/// <summary>Configuration for the OpenAI provider.</summary>
public sealed class OpenAiOptions : ProviderOptions
{
    /// <summary>Initializes a new instance of <see cref="OpenAiOptions"/> with OpenAI defaults.</summary>
    public OpenAiOptions()
    {
        BaseUrl = "https://api.openai.com/v1";
    }
}
```

```csharp
// src/Ferret.Configuration.AI/AiOptions.cs
namespace Ferret.Configuration.Ai;

/// <summary>Top-level AI platform configuration bound from <c>Ferret:Ai</c>.</summary>
public sealed class AiOptions
{
    /// <summary>Fully-qualified default chat model ID. Format: {provider}/{model}.</summary>
    public string DefaultChatModel { get; set; } = "ollama/llama3.2";

    /// <summary>Fully-qualified default embedding model ID. Format: {provider}/{model}.</summary>
    public string DefaultEmbeddingModel { get; set; } = "ollama/nomic-embed-text";

    /// <summary>Fully-qualified default reranker model ID. Null means no reranker configured.</summary>
    public string? DefaultReranker { get; set; }

    /// <summary>Per-provider configuration keyed by provider name (e.g. "Ollama", "OpenAi").</summary>
    public Dictionary<string, ProviderOptions> Providers { get; set; } = new();
}
```

- [ ] **Step 5: Write AiConfigurationModule**

```csharp
// src/Ferret.Configuration.AI/AiConfigurationModule.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Configuration.Ai;

/// <summary>Registers AI configuration options into the DI container.</summary>
public static class AiConfigurationModule
{
    /// <summary>Binds <see cref="AiOptions"/> from <c>Ferret:Ai</c> and registers it as <c>IOptions&lt;AiOptions&gt;</c>.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection("Ferret:Ai"));

        return services;
    }
}
```

- [ ] **Step 6: Create the test csproj**

```xml
<!-- tests/Ferret.Configuration.AI.Tests/Ferret.Configuration.AI.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Configuration.AI.Tests</AssemblyName>
    <RootNamespace>Ferret.Configuration.Ai.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Memory" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Configuration.AI\Ferret.Configuration.AI.csproj" />
  </ItemGroup>

</Project>
```

> **Note:** Check `Directory.Packages.props` for `Microsoft.Extensions.Configuration.Memory`. Add it if absent.

- [ ] **Step 7: Add projects to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Configuration.AI/Ferret.Configuration.AI.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Configuration.AI.Tests/Ferret.Configuration.AI.Tests.csproj
```

- [ ] **Step 8: Run tests to verify they pass**

```
dotnet test tests/Ferret.Configuration.AI.Tests/ -v n
```

Expected: 6 tests PASS.

- [ ] **Step 9: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 10: Commit**

```
git add src/Ferret.Configuration.AI/ tests/Ferret.Configuration.AI.Tests/ src/Ferret.sln
git commit -m "feat(sprint-12): Ferret.Configuration.AI — AiOptions, OllamaOptions, OpenAiOptions, AiConfigurationModule"
```

---

### Task 2: `IModelRegistry` + `ModelRegistry`

Immutable registry built from `IEnumerable<IModelProvider>` via a static async factory (`CreateAsync`). On creation, calls `ListModelsAsync` on each provider; if a provider is unreachable (throws), logs a warning and continues — its models are excluded. After construction the registry is read-only.

**Files:**
- Create: `src/Ferret.Models/Ferret.Models.csproj`
- Create: `src/Ferret.Models/IModelRegistry.cs`
- Create: `src/Ferret.Models/ModelRegistry.cs`
- Create: `tests/Ferret.Models.Tests/Ferret.Models.Tests.csproj`
- Create: `tests/Ferret.Models.Tests/ModelRegistryTests.cs`

**Interfaces:**
- Consumes: `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `ModelDescriptor`, `ProviderDescriptor`, `ModelId`, `ProviderId` from `Ferret.Core.Ai`
- Produces: `IModelRegistry`, `ModelRegistry` (sealed, created via `ModelRegistry.CreateAsync`)

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Models.Tests/ModelRegistryTests.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Models.Tests;

public sealed class ModelRegistryTests
{
    [Fact]
    public async Task CreateAsync_SingleProvider_ReturnsAllProviders()
    {
        var provider = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        var providers = registry.GetProviders();
        Assert.Single(providers);
        Assert.Equal("ollama", providers[0].Id.Value);
    }

    [Fact]
    public async Task CreateAsync_SingleProvider_ReturnsAggregatedModels()
    {
        var provider = new FakeModelProvider("ollama", [
            MakeDescriptor("ollama/llama3.2", "ollama"),
            MakeDescriptor("ollama/nomic-embed-text", "ollama")
        ]);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        Assert.Equal(2, registry.GetModels().Count);
    }

    [Fact]
    public async Task CreateAsync_MultipleProviders_AggregatesAllModels()
    {
        var ollamaProvider = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var openAiProvider = new FakeModelProvider("openai", [MakeDescriptor("openai/gpt-4o", "openai")]);
        var registry = await ModelRegistry.CreateAsync([ollamaProvider, openAiProvider], NullLogger<ModelRegistry>.Instance);

        Assert.Equal(2, registry.GetModels().Count);
        Assert.Equal(2, registry.GetProviders().Count);
    }

    [Fact]
    public async Task CreateAsync_UnreachableProvider_ExcludesItsModelsAndContinues()
    {
        var unreachable = new ThrowingModelProvider("broken");
        var working = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var registry = await ModelRegistry.CreateAsync([unreachable, working], NullLogger<ModelRegistry>.Instance);

        // Unreachable provider excluded; working provider's models present
        Assert.Equal(1, registry.GetModels().Count);
        Assert.Equal("ollama/llama3.2", registry.GetModels()[0].Id.Value);
    }

    [Fact]
    public async Task GetModel_ExistingModelId_ReturnsDescriptor()
    {
        var provider = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        var descriptor = registry.GetModel(new ModelId("ollama/llama3.2"));

        Assert.NotNull(descriptor);
        Assert.Equal("ollama/llama3.2", descriptor.Id.Value);
    }

    [Fact]
    public async Task GetModel_UnknownModelId_ReturnsNull()
    {
        var registry = await ModelRegistry.CreateAsync([], NullLogger<ModelRegistry>.Instance);

        Assert.Null(registry.GetModel(new ModelId("unknown/model")));
    }

    [Fact]
    public async Task GetProvider_ExistingProviderId_ReturnsProvider()
    {
        var provider = new FakeModelProvider("ollama", []);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        Assert.NotNull(registry.GetProvider(new ProviderId("ollama")));
    }

    [Fact]
    public async Task GetProvider_UnknownProviderId_ReturnsNull()
    {
        var registry = await ModelRegistry.CreateAsync([], NullLogger<ModelRegistry>.Instance);

        Assert.Null(registry.GetProvider(new ProviderId("unknown")));
    }

    private static ModelDescriptor MakeDescriptor(string modelId, string providerId) => new()
    {
        Id = new ModelId(modelId),
        ProviderId = new ProviderId(providerId),
        DisplayName = modelId,
        Capabilities = ModelCapabilities.Chat
    };

    private sealed class FakeModelProvider(string providerId, IReadOnlyList<ModelDescriptor> models) : IModelProvider
    {
        public ProviderDescriptor Descriptor { get; } = new()
        {
            Id = new ProviderId(providerId),
            DisplayName = providerId,
            Capabilities = ModelCapabilities.Chat,
            Version = "1.0"
        };

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct) =>
            Task.FromResult(models);

        public IChatModel? GetChatModel(string localModelId) => null;
        public IEmbeddingModel? GetEmbeddingModel(string localModelId) => null;
        public IReranker? GetReranker(string localModelId) => null;
    }

    private sealed class ThrowingModelProvider(string providerId) : IModelProvider
    {
        public ProviderDescriptor Descriptor { get; } = new()
        {
            Id = new ProviderId(providerId),
            DisplayName = providerId,
            Capabilities = ModelCapabilities.Chat,
            Version = "1.0"
        };

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct) =>
            throw new HttpRequestException("Connection refused");

        public IChatModel? GetChatModel(string localModelId) => null;
        public IEmbeddingModel? GetEmbeddingModel(string localModelId) => null;
        public IReranker? GetReranker(string localModelId) => null;
    }
}
```

> **Note:** Adjust `using` namespaces to match the exact namespace layout produced by s1. The types `IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `ModelDescriptor`, `ProviderDescriptor`, `ModelId`, `ProviderId`, `ModelCapabilities` must be available from `Ferret.Core`. Check s1 plan for exact namespaces.

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Models.Tests/ --filter "FullyQualifiedName~ModelRegistry" -v n
```

Expected: compile errors — project and types not found.

- [ ] **Step 3: Create the csproj**

```xml
<!-- src/Ferret.Models/Ferret.Models.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Models</AssemblyName>
    <RootNamespace>Ferret.Models</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Configuration.AI\Ferret.Configuration.AI.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Write IModelRegistry**

```csharp
// src/Ferret.Models/IModelRegistry.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Models;

/// <summary>Read-only view of all registered AI providers and their models.</summary>
public interface IModelRegistry
{
    /// <summary>Returns all registered provider descriptors.</summary>
    IReadOnlyList<ProviderDescriptor> GetProviders();

    /// <summary>Returns the provider instance for <paramref name="id"/>, or <c>null</c> if not registered.</summary>
    IModelProvider? GetProvider(ProviderId id);

    /// <summary>Returns all cached model descriptors across all providers.</summary>
    IReadOnlyList<ModelDescriptor> GetModels();

    /// <summary>Returns the descriptor for <paramref name="id"/>, or <c>null</c> if not found.</summary>
    ModelDescriptor? GetModel(ModelId id);
}
```

- [ ] **Step 5: Write ModelRegistry**

Implement `ModelRegistry : IModelRegistry` (public sealed) satisfying the tests:
- Private constructor; internal state is four immutable collections: provider descriptors list, provider-by-id dict, all-models list, model-by-id dict — use `OrdinalIgnoreCase` for dictionary keys
- `static async Task<ModelRegistry> CreateAsync(IEnumerable<IModelProvider> providers, ILogger<ModelRegistry> logger, CancellationToken ct = default)`: null-guard both params; for each provider call `ListModelsAsync` in a try/catch — on exception log warning and `continue`; on success add to all four collections; return new registry with `.AsReadOnly()` collections
- `GetProviders()`, `GetModels()`: return stored lists
- `GetProvider(ProviderId)`, `GetModel(ModelId)`: dict lookup by `.Value`, return null if missing

- [ ] **Step 6: Create the test csproj**

```xml
<!-- tests/Ferret.Models.Tests/Ferret.Models.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Models.Tests</AssemblyName>
    <RootNamespace>Ferret.Models.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Models\Ferret.Models.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Add projects to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Models/Ferret.Models.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Models.Tests/Ferret.Models.Tests.csproj
```

- [ ] **Step 8: Run tests to verify they pass**

```
dotnet test tests/Ferret.Models.Tests/ --filter "FullyQualifiedName~ModelRegistry" -v n
```

Expected: 8 tests PASS.

- [ ] **Step 9: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

- [ ] **Step 10: Commit**

```
git add src/Ferret.Models/ tests/Ferret.Models.Tests/ src/Ferret.sln
git commit -m "feat(sprint-12): IModelRegistry + ModelRegistry — immutable async factory, provider fault isolation"
```

---

### Task 3: `IModelRouter` + `ModelRouter` + `ModelNotFoundException`

Configuration-driven router that resolves the default chat and embedding models from `AiOptions` at construction. Delegates provider lookup to `IModelRegistry`. Throws `ModelNotFoundException` (with a user-friendly message) when the configured default is not available.

**Files:**
- Create: `src/Ferret.Models/IModelRouter.cs`
- Create: `src/Ferret.Models/ModelRouter.cs`
- Create: `src/Ferret.Models/Exceptions/ModelNotFoundException.cs`
- Create: `tests/Ferret.Models.Tests/ModelRouterTests.cs`

**Interfaces:**
- Consumes: `IModelRegistry`, `AiOptions` (from `Ferret.Configuration.AI`), `IChatModel`, `IEmbeddingModel`, `ModelId`, `IOptions<AiOptions>`
- Produces: `IModelRouter`, `ModelRouter` (sealed), `ModelNotFoundException` (sealed)

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Models.Tests/ModelRouterTests.cs
using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models;
using Ferret.Models.Exceptions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ferret.Models.Tests;

public sealed class ModelRouterTests
{
    [Fact]
    public void GetDefaultChatModel_WhenConfigured_ReturnsModel()
    {
        var chatModel = new FakeChatModel();
        var registry = new FakeModelRegistry(chatModel: chatModel);
        var options = Options.Create(new AiOptions { DefaultChatModel = "ollama/llama3.2" });
        var router = new ModelRouter(registry, options);

        var result = router.GetDefaultChatModel();

        Assert.Same(chatModel, result);
    }

    [Fact]
    public void GetDefaultChatModel_WhenModelNotFound_ThrowsModelNotFoundException()
    {
        var registry = new FakeModelRegistry(chatModel: null);
        var options = Options.Create(new AiOptions { DefaultChatModel = "ollama/missing" });
        var router = new ModelRouter(registry, options);

        var ex = Assert.Throws<ModelNotFoundException>(() => router.GetDefaultChatModel());
        Assert.Equal("ollama/missing", ex.ModelId.Value);
        Assert.Contains("ollama/missing", ex.Message);
        Assert.Contains("ferret models list", ex.Message);
    }

    [Fact]
    public void GetChatModel_ExistingModelId_ReturnsModel()
    {
        var chatModel = new FakeChatModel();
        var registry = new FakeModelRegistry(chatModel: chatModel);
        var options = Options.Create(new AiOptions());
        var router = new ModelRouter(registry, options);

        var result = router.GetChatModel(new ModelId("ollama/llama3.2"));

        Assert.Same(chatModel, result);
    }

    [Fact]
    public void GetChatModel_UnknownModelId_ReturnsNull()
    {
        var registry = new FakeModelRegistry(chatModel: null);
        var options = Options.Create(new AiOptions());
        var router = new ModelRouter(registry, options);

        Assert.Null(router.GetChatModel(new ModelId("unknown/model")));
    }

    [Fact]
    public void GetDefaultEmbeddingModel_WhenConfigured_ReturnsModel()
    {
        var embeddingModel = new FakeEmbeddingModel();
        var registry = new FakeModelRegistry(embeddingModel: embeddingModel);
        var options = Options.Create(new AiOptions { DefaultEmbeddingModel = "ollama/nomic-embed-text" });
        var router = new ModelRouter(registry, options);

        var result = router.GetDefaultEmbeddingModel();

        Assert.Same(embeddingModel, result);
    }

    [Fact]
    public void GetDefaultEmbeddingModel_WhenModelNotFound_ThrowsModelNotFoundException()
    {
        var registry = new FakeModelRegistry(embeddingModel: null);
        var options = Options.Create(new AiOptions { DefaultEmbeddingModel = "ollama/missing-embed" });
        var router = new ModelRouter(registry, options);

        var ex = Assert.Throws<ModelNotFoundException>(() => router.GetDefaultEmbeddingModel());
        Assert.Equal("ollama/missing-embed", ex.ModelId.Value);
    }

    [Fact]
    public void GetEmbeddingModel_UnknownModelId_ReturnsNull()
    {
        var registry = new FakeModelRegistry(embeddingModel: null);
        var options = Options.Create(new AiOptions());
        var router = new ModelRouter(registry, options);

        Assert.Null(router.GetEmbeddingModel(new ModelId("unknown/embed")));
    }

    // ---- Fakes ----

    private sealed class FakeModelRegistry(IChatModel? chatModel = null, IEmbeddingModel? embeddingModel = null)
        : IModelRegistry
    {
        public IReadOnlyList<ProviderDescriptor> GetProviders() => [];
        public IModelProvider? GetProvider(ProviderId id) => null;
        public IReadOnlyList<ModelDescriptor> GetModels() => [];
        public ModelDescriptor? GetModel(ModelId id) => null;

        // Router resolves via provider; fake delegates directly
        internal IChatModel? ChatModel => chatModel;
        internal IEmbeddingModel? EmbeddingModel => embeddingModel;
    }

    private sealed class FakeChatModel : IChatModel
    {
        public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");

        public IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(ChatRequest request, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");
    }

    private sealed class FakeEmbeddingModel : IEmbeddingModel
    {
        public Task<EmbeddingResult> EmbedAsync(EmbeddingRequest request, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");
    }
}
```

> **Note:** `ModelRouter` resolves models by calling `IModelRegistry.GetProvider(providerId).GetChatModel(localModelId)`. The `FakeModelRegistry` above does not wire this up; you will need to adjust `FakeModelRegistry` to also implement a fake `GetProvider` that returns a fake `IModelProvider` whose `GetChatModel`/`GetEmbeddingModel` return the test doubles. Adjust the fake hierarchy to match the actual `IModelProvider` contract from s1.

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Models.Tests/ --filter "FullyQualifiedName~ModelRouter" -v n
```

Expected: compile errors — types not found.

- [ ] **Step 3: Write ModelNotFoundException**

```csharp
// src/Ferret.Models/Exceptions/ModelNotFoundException.cs
using Ferret.Core.Ai.Models;

namespace Ferret.Models.Exceptions;

/// <summary>Thrown when a requested model ID is not available in the registry.</summary>
public sealed class ModelNotFoundException : InvalidOperationException
{
    /// <summary>The model ID that was not found.</summary>
    public ModelId ModelId { get; }

    /// <summary>Initializes a new instance of <see cref="ModelNotFoundException"/>.</summary>
    public ModelNotFoundException(ModelId modelId)
        : base($"Model '{modelId.Value}' is not available. Run `ferret models list` to see available models.")
    {
        ModelId = modelId;
    }
}
```

> **Note:** If Ferret has a shared `FerretException` base class in `Ferret.Core`, derive from that instead of `InvalidOperationException`. Check `src/Ferret.Core/` for any exception base type and match existing conventions.

- [ ] **Step 4: Write IModelRouter**

```csharp
// src/Ferret.Models/IModelRouter.cs
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models.Exceptions;

namespace Ferret.Models;

/// <summary>Resolves default and named AI models from the registry.</summary>
public interface IModelRouter
{
    /// <summary>Returns the default chat model.</summary>
    /// <exception cref="ModelNotFoundException">If the configured default is not available.</exception>
    IChatModel GetDefaultChatModel();

    /// <summary>Returns the chat model for <paramref name="id"/>, or <c>null</c> if not found.</summary>
    IChatModel? GetChatModel(ModelId id);

    /// <summary>Returns the default embedding model.</summary>
    /// <exception cref="ModelNotFoundException">If the configured default is not available.</exception>
    IEmbeddingModel GetDefaultEmbeddingModel();

    /// <summary>Returns the embedding model for <paramref name="id"/>, or <c>null</c> if not found.</summary>
    IEmbeddingModel? GetEmbeddingModel(ModelId id);
}
```

- [ ] **Step 5: Write ModelRouter**

Implement `ModelRouter : IModelRouter` (public sealed) satisfying the tests:
- Constructor: `(IModelRegistry registry, IOptions<AiOptions> options)` — null-guard both; capture default model IDs from `options.Value` at construction time
- `GetDefaultChatModel()`: call `GetChatModel(_defaultChatModelId)` — throw `ModelNotFoundException` if null
- `GetChatModel(ModelId)`: split `id.Value` on the first `/` to get provider prefix and local model name; call `registry.GetProvider(...)?.GetChatModel(...)`; return null if not found
- `GetDefaultEmbeddingModel()` / `GetEmbeddingModel(ModelId)`: same pattern for embedding

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Models.Tests/ --filter "FullyQualifiedName~ModelRouter" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 7: Run all Ferret.Models.Tests**

```
dotnet test tests/Ferret.Models.Tests/ -v n
```

Expected: all tests PASS.

- [ ] **Step 8: Commit**

```
git add src/Ferret.Models/IModelRouter.cs src/Ferret.Models/ModelRouter.cs src/Ferret.Models/Exceptions/
git add tests/Ferret.Models.Tests/ModelRouterTests.cs
git commit -m "feat(sprint-12): IModelRouter + ModelRouter + ModelNotFoundException"
```

---

### Task 4: `ModelPlatformModule` + `Ferret.AI` scaffold + Solution wiring

Composes the model platform into the DI container (`ModelPlatformModule`) and creates the empty `Ferret.AI` scaffold package. `ModelPlatformModule` registers `IModelRegistry` (singleton, built via `CreateAsync`) and `IModelRouter` (singleton). `AiModule` in `Ferret.AI` is a placeholder: it registers nothing and logs a startup trace.

**Files:**
- Create: `src/Ferret.Models/ModelPlatformModule.cs`
- Create: `src/Ferret.AI/Ferret.AI.csproj`
- Create: `src/Ferret.AI/AiModule.cs`
- Modify: `src/Ferret.sln` — add `Ferret.AI`

No new tests in this task (module wiring is covered by integration tests in s6).

**Interfaces:**
- Consumes: `IModelRegistry`, `IModelRouter`, `ModelRegistry.CreateAsync`, `IModelProvider` (keyed/enumerable), `AiOptions`, `IOptions<AiOptions>`
- Produces: `ModelPlatformModule`, `AiModule`

- [ ] **Step 1: Write ModelPlatformModule**

Implement `ModelPlatformModule` (public static class) with `ConfigureServices(IServiceCollection services)`:
- Register `IModelRegistry` as singleton: factory resolves `IEnumerable<IModelProvider>` and `ILogger<ModelRegistry>` from `sp`; calls `ModelRegistry.CreateAsync(...).GetAwaiter().GetResult()` — blocking at startup is acceptable here
- Register `IModelRouter` as singleton: `services.AddSingleton<IModelRouter, ModelRouter>()`

> Check how `IndexingModule` or `WorkspaceModule` initialise async singletons — match that pattern if it differs from the blocking approach above.

- [ ] **Step 2: Write Ferret.AI csproj**

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
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write AiModule scaffold**

Implement `AiModule` (public static class) with `ConfigureServices(IServiceCollection services)`:
- Null-guard `services`
- Sprint 12: register nothing; Sprint 13 will add context assembly and agent services
- Return `services`

> Do NOT call `services.BuildServiceProvider()` inside `ConfigureServices` — that creates a second container and is an anti-pattern. A simple no-op body with a `// Sprint 13: add orchestration services here` comment is sufficient.

- [ ] **Step 4: Add Ferret.AI to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.AI/Ferret.AI.csproj
```

- [ ] **Step 5: Full solution build check**

```
dotnet build src/Ferret.sln -v n
```

Expected: build succeeds with zero errors.

- [ ] **Step 6: Run all Ferret.Models.Tests and Ferret.Configuration.AI.Tests**

```
dotnet test tests/Ferret.Models.Tests/ tests/Ferret.Configuration.AI.Tests/ -v n
```

Expected: all tests PASS.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Models/ModelPlatformModule.cs src/Ferret.AI/ src/Ferret.sln
git commit -m "feat(sprint-12): ModelPlatformModule, Ferret.AI scaffold, solution wiring"
```

---

### Task 5: Full Solution Build and Test Pass

Final verification that all packages introduced in s2 compile and test cleanly alongside the full solution.

- [ ] **Step 1: Full solution build**

```
dotnet build src/Ferret.sln -v n
```

Expected: zero errors, zero warnings that were not present before s2.

- [ ] **Step 2: Full solution test**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. No new test failures introduced by s2.

- [ ] **Step 3: Architecture tests**

```
dotnet test tests/Ferret.Architecture.Tests/ -v n
```

Expected: PASS. Verify no architecture violations — `Ferret.Models`, `Ferret.Configuration.AI`, and `Ferret.AI` must not reference any vendor SDK (`OllamaSharp.*`, `OpenAI.*`).

- [ ] **Step 4: Verify solution file lists all new projects**

```
dotnet sln src/Ferret.sln list
```

Expected output includes:
- `src/Ferret.Configuration.AI/Ferret.Configuration.AI.csproj`
- `src/Ferret.Models/Ferret.Models.csproj`
- `src/Ferret.AI/Ferret.AI.csproj`
- `tests/Ferret.Configuration.AI.Tests/Ferret.Configuration.AI.Tests.csproj`
- `tests/Ferret.Models.Tests/Ferret.Models.Tests.csproj`

- [ ] **Step 5: Final commit**

```
git add src/Ferret.sln
git commit -m "chore(sprint-12): s2 model platform — full solution build and test pass"
```

---

## Summary

Sprint 12 s2 delivers:

| Package | Key Types |
|---|---|
| `Ferret.Configuration.AI` | `AiOptions`, `ProviderOptions`, `OllamaOptions`, `OpenAiOptions`, `AiConfigurationModule` |
| `Ferret.Models` | `IModelRegistry`, `ModelRegistry` (async factory), `IModelRouter`, `ModelRouter`, `ModelNotFoundException`, `ModelPlatformModule` |
| `Ferret.AI` | `AiModule` (scaffold) |

**Prerequisite for s3 (Ollama Provider) and s4 (OpenAI Provider):** both depend on `IModelProvider` (s1) and `IModelRegistry`/`IModelRouter` (s2). s3 and s4 may proceed in parallel once s2 is complete.
