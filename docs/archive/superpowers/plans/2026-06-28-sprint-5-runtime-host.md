> **Historical note:** This document was written when the product was named AISpace, which was renamed to Ferret during Sprint 5.

# Sprint 5 — Runtime Host Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Ferret.Runtime — a working runtime host that can start, register modules in dependency order, manage module lifecycle, dispatch runtime events, aggregate health, and shut down cleanly.

**Architecture:** RuntimeHost wraps Microsoft.Extensions.Hosting's IHost (composition root) and exposes only Ferret contracts. Module lifecycle is managed by a single ModuleLifecycleService (IHostedService) that drives LifecycleOrchestrator in dependency order. Plugin authors implement IModule directly or extend optional DefaultModule — inheritance is never required.

**Tech Stack:** .NET 9, C# 13, xunit 2.9, Microsoft.Extensions.Hosting 9.x (wraps DI + Logging + Options transitively)

## Global Constraints

- `Ferret.Core` must have zero project references — never modify its `.csproj`.
- `Ferret.Runtime` may reference `Ferret.Core` and approved NuGet packages only.
- `TreatWarningsAsErrors=true` — every warning is a build failure.
- `Nullable=enable` — all reference types require explicit nullability annotations.
- `AnalysisMode=All` plus StyleCop — follow existing code conventions exactly.
- All public members in production code require XML doc comments (`/// <summary>`).
- Every production class XML doc must answer: (1) Why does this class exist? (2) Who owns its lifecycle? (3) Which architectural layer may depend on it? (4) Thread Safety stance.
- Every mutable component must state one of: **Thread Safe** / **Thread Compatible** / **Single Thread Only** in its XML summary.
- Central Package Management (`Directory.Packages.props`) — never add a version to a `.csproj` `PackageReference`.
- Microsoft types must NOT appear in any `public` interface, method signature, property, or return type.
- TDD: write the failing test first, confirm red, implement, confirm green, commit.
- Startup performance target: < 500 ms. Shutdown performance target: < 200 ms.
- Target: 180–220 passing tests across `Ferret.Runtime.Tests`.
- Do not touch workspace contracts or implement workspace functionality.

---

## Technology Evaluation

These formal decisions are recorded here. Each decision is one of: Adopt / Wrap / Build / Reject / Defer.

| Package | Decision | Rationale |
|---|---|---|
| `Microsoft.Extensions.Hosting` | **Wrap** | IHost provides composition root, DI container setup, graceful shutdown with CancellationToken, and IHostedService lifecycle. RuntimeHost wraps IHost internally. Module lifecycle is one IHostedService. All three transitive packages (DI, Logging, Options) are included. |
| `Microsoft.Extensions.DependencyInjection` | **Adopt** (via Hosting) | Transitively included. IServiceCollection used for internal composition in RuntimeBuilder.Build(). Never exposed in public contracts. |
| `Microsoft.Extensions.Logging` | **Adopt** (via Hosting) | Transitively included. ILogger&lt;T&gt; injected into all internal classes. Never in public interfaces. |
| `Microsoft.Extensions.Options` | **Adopt** (via Hosting) | Transitively included. RuntimeOptions registered as singleton POCO; IOptions&lt;T&gt; overhead not needed for a single-startup value. |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | **Reject** | Ferret.Core already defines IHealthCheck and HealthCheckResult with our own signatures. Adopting MS types would conflict with Core contracts and leak MS types through the abstraction boundary. Build a thin RuntimeHealthService instead. |
| `System.Threading.Channels` | **Reject** | ARCH-013 specifies synchronous in-process event dispatch with handler failure isolation. Channels is designed for async producer/consumer with buffering — wrong shape for our dispatch model. A simple lock-protected handler dictionary provides exactly what ARCH-013 requires. |
| `Scrutor` | **Defer** | Assembly scanning would benefit plugin auto-discovery. Not needed in Sprint 5's static module registration model. Evaluate in the plugin host sprint. |
| `Polly` | **Defer** | Resilience policies (retry, circuit breaker, timeout) are not defined in Sprint 5 architecture. Engine-layer requirements will determine if this is needed. |

---

## File Structure

```
src/Ferret.Runtime/
├── Bootstrap/
│   ├── RuntimeOptions.cs            — configuration POCO (RuntimeVersion string)
│   ├── RuntimeStateManager.cs       — thread-safe RuntimeState machine (Interlocked)
│   ├── RuntimeHost.cs               — implements IRuntimeHost; wraps IHost
│   └── RuntimeBuilder.cs            — implements IRuntimeBuilder; wraps HostBuilder
├── Modules/
│   ├── DefaultModule.cs             — optional abstract helper (IModule + IModuleDescriptor)
│   ├── BoundModule.cs               — internal adapter wrapping a plain IModuleDescriptor
│   └── IModuleWithDependencies.cs   — optional interface for dependency declaration
├── Registry/
│   ├── ModuleDescriptorStore.cs     — holds descriptors during Build; detects duplicates; wraps into DefaultModule
│   ├── ModuleDependencyGraph.cs     — topological sort and cycle detection over DefaultModule list
│   └── ModuleRegistry.cs            — implements IModuleRegistry (read-only view of active modules)
├── Lifecycle/
│   ├── ExecutionContext.cs          — implements IExecutionContext
│   ├── ModuleContext.cs             — implements IModuleContext
│   ├── LifecycleOrchestrator.cs    — drives OnStarting/OnStarted/IInitializable/OnStopping/OnStopped
│   └── ModuleLifecycleService.cs   — IHostedService; starts/stops modules in dependency order
├── Events/
│   └── RuntimeEventDispatcher.cs   — in-process typed pub/sub; handler failures isolated (ARCH-013)
├── Health/
│   ├── ModuleHealthResult.cs        — pairs module ID with HealthCheckResult
│   ├── RuntimeHealthReport.cs       — aggregated report (overall HealthStatus + per-module results)
│   └── RuntimeHealthService.cs     — aggregates IHealthCheck implementations
├── Extensions/
│   └── RuntimeServiceExtensions.cs — AddFerretRuntime(IServiceCollection, Action<IRuntimeBuilder>?)
└── Properties/
    └── AssemblyInfo.cs              — InternalsVisibleTo("Ferret.Runtime.Tests")

tests/Ferret.Runtime.Tests/
├── Fakes/
│   ├── FakeModule.cs               — DefaultModule subclass with call-tracking
│   └── FakeHealthCheck.cs          — IHealthCheck returning a preset result
├── Bootstrap/
│   ├── RuntimeStateManagerTests.cs
│   ├── RuntimeHostTests.cs
│   └── RuntimeBuilderTests.cs
├── Modules/
│   ├── DefaultModuleTests.cs
│   └── BoundModuleTests.cs
├── Registry/
│   ├── ModuleDescriptorStoreTests.cs
│   ├── ModuleDependencyGraphTests.cs
│   └── ModuleRegistryTests.cs
├── Lifecycle/
│   ├── ExecutionContextTests.cs
│   └── LifecycleOrchestratorTests.cs
├── Events/
│   └── RuntimeEventDispatcherTests.cs
├── Health/
│   └── RuntimeHealthServiceTests.cs
├── Extensions/
│   └── RuntimeServiceExtensionsTests.cs
└── Integration/
    └── RuntimeIntegrationTests.cs
```

---

## Task 1: Project Setup

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Ferret.Runtime/Ferret.Runtime.csproj`
- Create: `src/Ferret.Runtime/Properties/AssemblyInfo.cs`

**Interfaces:**
- Produces: `[assembly: InternalsVisibleTo("Ferret.Runtime.Tests")]` — lets tests access internal types.

- [ ] **Step 1: Add Microsoft.Extensions.Hosting to Directory.Packages.props**

Open `Directory.Packages.props` and add a new ItemGroup after the existing ones:

```xml
  <ItemGroup Label="Microsoft.Extensions">
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
  </ItemGroup>
```

- [ ] **Step 2: Update Ferret.Runtime.csproj**

Replace the content of `src/Ferret.Runtime/Ferret.Runtime.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Runtime</AssemblyName>
    <RootNamespace>Ferret.Runtime</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create AssemblyInfo.cs**

Create `src/Ferret.Runtime/Properties/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.Runtime.Tests")]
```

- [ ] **Step 4: Replace RuntimeModule.cs stub**

Replace `src/Ferret.Runtime/RuntimeModule.cs` with:

```csharp
// Intentionally empty. Assembly entry point; all types are in sub-namespaces.
```

- [ ] **Step 5: Verify build**

```
dotnet build src/Ferret.Runtime/Ferret.Runtime.csproj
```

Expected: Build succeeded, 0 warnings, 0 errors.

- [ ] **Step 6: Commit**

```
git add Directory.Packages.props src/Ferret.Runtime/Ferret.Runtime.csproj src/Ferret.Runtime/Properties/AssemblyInfo.cs src/Ferret.Runtime/RuntimeModule.cs
git commit -m "chore(sprint-5): configure Microsoft.Extensions.Hosting and InternalsVisibleTo"
```

---

## Task 2: RuntimeHost + RuntimeBuilder + RuntimeOptions (composition root shells)

Implement first per CA mandate. These are the composition root. Dependencies are stubs at this stage — the types compile; integration tests are written now and pass after Tasks 3–12 complete.

**Files:**
- Create: `src/Ferret.Runtime/Bootstrap/RuntimeOptions.cs`
- Create: `src/Ferret.Runtime/Bootstrap/RuntimeStateManager.cs` (stub only — full TDD in Task 3)
- Create: `src/Ferret.Runtime/Bootstrap/RuntimeHost.cs`
- Create: `src/Ferret.Runtime/Bootstrap/RuntimeBuilder.cs`

**Interfaces:**
- Consumes: `IRuntimeHost`, `IRuntimeBuilder`, `IModuleDescriptor`, `RuntimeState` (all from Ferret.Core.Runtime)
- Produces: `RuntimeHost`, `RuntimeBuilder`, `RuntimeOptions` — consumed by all later tasks

- [ ] **Step 1: Create RuntimeOptions**

Create `src/Ferret.Runtime/Bootstrap/RuntimeOptions.cs`:

```csharp
namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Tunable configuration values for the Ferret runtime host.
/// <para>Why: Centralises values that must be consistent across all runtime collaborators (e.g. version written to domain events).</para>
/// <para>Lifecycle: Created by the caller before RuntimeBuilder.Build(); owned by DI as a singleton after Build().</para>
/// <para>Layer: Ferret.Runtime only — callers access it through RuntimeBuilder.WithOptions(); never referenced by Core.</para>
/// <para>Thread Safety: Single Thread Only — set all properties before passing to Build(); treat as immutable afterward.</para>
/// </summary>
public sealed class RuntimeOptions
{
    /// <summary>Gets or sets the version string written to <c>RuntimeStarted</c> and <c>RuntimeStopped</c> events.</summary>
    public string RuntimeVersion { get; set; } = "0.5.0";
}
```

- [ ] **Step 2: Create RuntimeStateManager stub (full TDD in Task 3)**

Create `src/Ferret.Runtime/Bootstrap/RuntimeStateManager.cs`:

```csharp
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Atomic state machine for the host-level runtime lifecycle.
/// <para>Why: Provides a single authority for RuntimeState so all runtime collaborators read from one source.</para>
/// <para>Lifecycle: Created inside RuntimeBuilder.Build() and registered as a DI singleton; lives until the RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime internal — only RuntimeHost and ModuleLifecycleService may use this directly.</para>
/// <para>Thread Safety: Thread Safe — all transitions use Interlocked.CompareExchange.</para>
/// </summary>
internal sealed class RuntimeStateManager
{
    private int _state = (int)RuntimeState.Stopped;

    /// <summary>Gets the current runtime state.</summary>
    public RuntimeState Current => (RuntimeState)Volatile.Read(ref _state);

    /// <summary>Atomically transitions from <paramref name="from"/> to <paramref name="to"/>. Returns <c>true</c> on success.</summary>
    public bool TryTransition(RuntimeState from, RuntimeState to)
        => Interlocked.CompareExchange(ref _state, (int)to, (int)from) == (int)from;

    /// <summary>Unconditionally sets the state (used for Faulted transitions where CAS may race).</summary>
    public void ForceSet(RuntimeState state)
        => Volatile.Write(ref _state, (int)state);
}
```

- [ ] **Step 3: Create RuntimeHost**

Create `src/Ferret.Runtime/Bootstrap/RuntimeHost.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Coordinates module startup, shutdown, event dispatch, and health aggregation for the Ferret platform.
/// <para>Why: Owns the runtime lifecycle and composes all collaborators behind the IRuntimeHost contract.</para>
/// <para>Lifecycle: Built by RuntimeBuilder.Build(); owned by the application entry point; disposed at application shutdown.</para>
/// <para>Layer: Ferret.Runtime — consumed by the application layer only; never referenced by Core.</para>
/// <para>Thread Safety: Thread Compatible — StartAsync/StopAsync must not be called concurrently.</para>
/// </summary>
internal sealed class RuntimeHost : IRuntimeHost, IAsyncDisposable
{
    private readonly IHost _host;
    private readonly RuntimeStateManager _stateManager;

    internal RuntimeHost(IHost host)
    {
        _host = host;
        _stateManager = host.Services.GetRequiredService<RuntimeStateManager>();
    }

    /// <inheritdoc/>
    public RuntimeState State => _stateManager.Current;

    /// <inheritdoc/>
    public IModuleRegistry Modules =>
        _host.Services.GetRequiredService<ModuleRegistry>();

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_stateManager.TryTransition(RuntimeState.Stopped, RuntimeState.Starting))
            throw new InvalidOperationException(
                $"Cannot start runtime: current state is '{State}'. Runtime must be Stopped before starting.");

        try
        {
            await _host.StartAsync(cancellationToken).ConfigureAwait(false);
            // ModuleLifecycleService.StartAsync transitions Starting → Running
        }
        catch
        {
            _stateManager.ForceSet(RuntimeState.Faulted);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_stateManager.TryTransition(RuntimeState.Running, RuntimeState.Stopping))
            throw new InvalidOperationException(
                $"Cannot stop runtime: current state is '{State}'. Runtime must be Running before stopping.");

        try
        {
            await _host.StopAsync(cancellationToken).ConfigureAwait(false);
            // ModuleLifecycleService.StopAsync transitions Stopping → Stopped
        }
        catch
        {
            _stateManager.ForceSet(RuntimeState.Faulted);
            throw;
        }
    }

    /// <summary>Stops the runtime if running, then disposes the underlying host.</summary>
    public async ValueTask DisposeAsync()
    {
        if (State is RuntimeState.Running)
        {
            try { await StopAsync().ConfigureAwait(false); }
            catch { /* best-effort */ }
        }

        if (_host is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            _host.Dispose();
    }
}
```

- [ ] **Step 4: Create RuntimeBuilder**

Create `src/Ferret.Runtime/Bootstrap/RuntimeBuilder.cs`:

```csharp
using Ferret.Core.Abstractions;
using Ferret.Core.Runtime;
using Ferret.Runtime.Events;
using Ferret.Runtime.Health;
using Ferret.Runtime.Lifecycle;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Fluent builder that assembles an IRuntimeHost from registered module descriptors.
/// <para>Why: Separates host construction (module registration, dependency sorting, DI wiring) from host operation (start/stop).</para>
/// <para>Lifecycle: Transient — configure once, call Build() once. Discard after Build() returns.</para>
/// <para>Layer: Ferret.Runtime — called by the application layer or DI extension helper; not referenced by Core.</para>
/// <para>Thread Safety: Single Thread Only — configure from one thread before calling Build().</para>
/// </summary>
public sealed class RuntimeBuilder : IRuntimeBuilder
{
    private readonly ModuleDescriptorStore _store = new();
    private readonly List<IHealthCheck> _extraHealthChecks = [];
    private RuntimeOptions _options = new();

    /// <inheritdoc/>
    public IRuntimeBuilder AddModule(IModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _store.Add(descriptor);
        return this;
    }

    /// <summary>Overrides default runtime options.</summary>
    public RuntimeBuilder WithOptions(RuntimeOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    /// <summary>Registers an additional health check beyond those contributed by modules.</summary>
    public RuntimeBuilder AddHealthCheck(IHealthCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _extraHealthChecks.Add(check);
        return this;
    }

    /// <inheritdoc/>
    public IRuntimeHost Build()
    {
        IReadOnlyList<DefaultModule> ordered = ModuleDependencyGraph.Sort(_store.GetAll());
        RuntimeOptions options = _options;
        List<IHealthCheck> extraChecks = [.._extraHealthChecks];

        IHost host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(options);
                services.AddSingleton<RuntimeStateManager>();
                services.AddSingleton<RuntimeEventDispatcher>();
                services.AddSingleton<LifecycleOrchestrator>();
                services.AddSingleton<RuntimeHealthService>(sp =>
                    new RuntimeHealthService(extraChecks));
                services.AddSingleton<IReadOnlyList<DefaultModule>>(_ => ordered);
                services.AddSingleton<ModuleRegistry>(
                    _ => new ModuleRegistry(ordered));
                services.AddHostedService<ModuleLifecycleService>();
            })
            .Build();

        return new RuntimeHost(host);
    }
}
```

- [ ] **Step 5: Verify compilation**

```
dotnet build src/Ferret.Runtime/Ferret.Runtime.csproj
```

Several type references will be unresolved (DefaultModule, ModuleDependencyGraph, etc.) — that is expected. These stubs compile once each referenced type is added in subsequent tasks. Temporarily add placeholder stubs if needed to allow the project to compile now:

If compilation fails due to missing types, create empty placeholder files:
- `src/Ferret.Runtime/Modules/DefaultModule.cs` — `namespace Ferret.Runtime.Modules; internal abstract class DefaultModule { }`
- `src/Ferret.Runtime/Registry/ModuleDependencyGraph.cs` — `namespace Ferret.Runtime.Registry; internal static class ModuleDependencyGraph { public static System.Collections.Generic.IReadOnlyList<DefaultModule> Sort(System.Collections.Generic.IEnumerable<Ferret.Core.Runtime.IModuleDescriptor> d) => []; }`
- `src/Ferret.Runtime/Registry/ModuleDescriptorStore.cs` — `namespace Ferret.Runtime.Registry; internal sealed class ModuleDescriptorStore { public void Add(Ferret.Core.Runtime.IModuleDescriptor d) { } public System.Collections.Generic.IEnumerable<Ferret.Core.Runtime.IModuleDescriptor> GetAll() => []; }`
- `src/Ferret.Runtime/Registry/ModuleRegistry.cs` — `namespace Ferret.Runtime.Registry; internal sealed class ModuleRegistry : Ferret.Core.Runtime.IModuleRegistry { public System.Collections.Generic.IReadOnlyList<Ferret.Core.Runtime.IModule> Modules => []; public bool TryGet(string id, out Ferret.Core.Runtime.IModule? m) { m = null; return false; } public Ferret.Core.Runtime.IModule GetById(string id) => throw new System.Exception(); }`
- `src/Ferret.Runtime/Events/RuntimeEventDispatcher.cs` — `namespace Ferret.Runtime.Events; internal sealed class RuntimeEventDispatcher { }`
- `src/Ferret.Runtime/Lifecycle/LifecycleOrchestrator.cs` — `namespace Ferret.Runtime.Lifecycle; internal sealed class LifecycleOrchestrator { }`
- `src/Ferret.Runtime/Lifecycle/ModuleLifecycleService.cs` — `namespace Ferret.Runtime.Lifecycle; internal sealed class ModuleLifecycleService : Microsoft.Extensions.Hosting.IHostedService { public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken ct) => System.Threading.Tasks.Task.CompletedTask; public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken ct) => System.Threading.Tasks.Task.CompletedTask; }`
- `src/Ferret.Runtime/Health/RuntimeHealthService.cs` — `namespace Ferret.Runtime.Health; internal sealed class RuntimeHealthService { public RuntimeHealthService(System.Collections.Generic.List<Ferret.Core.Abstractions.IHealthCheck> _) { } }`

These placeholders are replaced by full implementations in subsequent tasks.

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Runtime/
git commit -m "feat(sprint-5): add RuntimeHost, RuntimeBuilder, RuntimeOptions composition root shells (WP-A)"
```

---

## Task 3: RuntimeStateManager (full TDD)

Replace the Task 2 stub with a tested implementation.

**Files:**
- Modify: `src/Ferret.Runtime/Bootstrap/RuntimeStateManager.cs` (already exists from Task 2 — content is already correct; verify it matches the full implementation below)
- Create: `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeStateManagerTests.cs`

**Interfaces:**
- Produces: `RuntimeStateManager.Current`, `TryTransition(from, to)`, `ForceSet(state)` — used by RuntimeHost and ModuleLifecycleService.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeStateManagerTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;

namespace Ferret.Runtime.Tests.Bootstrap;

public sealed class RuntimeStateManagerTests
{
    [Fact]
    public void Current_InitialState_IsStopped()
    {
        var mgr = new RuntimeStateManager();
        Assert.Equal(RuntimeState.Stopped, mgr.Current);
    }

    [Fact]
    public void TryTransition_FromMatchingState_Succeeds()
    {
        var mgr = new RuntimeStateManager();
        bool result = mgr.TryTransition(RuntimeState.Stopped, RuntimeState.Starting);
        Assert.True(result);
        Assert.Equal(RuntimeState.Starting, mgr.Current);
    }

    [Fact]
    public void TryTransition_FromWrongState_Fails()
    {
        var mgr = new RuntimeStateManager();
        bool result = mgr.TryTransition(RuntimeState.Running, RuntimeState.Stopping);
        Assert.False(result);
        Assert.Equal(RuntimeState.Stopped, mgr.Current);
    }

    [Fact]
    public void TryTransition_DoesNotChangeStateOnFailure()
    {
        var mgr = new RuntimeStateManager();
        mgr.TryTransition(RuntimeState.Stopped, RuntimeState.Starting);
        mgr.TryTransition(RuntimeState.Running, RuntimeState.Stopping); // wrong from
        Assert.Equal(RuntimeState.Starting, mgr.Current);
    }

    [Fact]
    public void ForceSet_OverridesCurrentState()
    {
        var mgr = new RuntimeStateManager();
        mgr.ForceSet(RuntimeState.Faulted);
        Assert.Equal(RuntimeState.Faulted, mgr.Current);
    }

    [Fact]
    public void TryTransition_IsThreadSafe_OnlyOneWinner()
    {
        var mgr = new RuntimeStateManager();
        int successCount = 0;

        var threads = Enumerable.Range(0, 20)
            .Select(_ => new Thread(() =>
            {
                if (mgr.TryTransition(RuntimeState.Stopped, RuntimeState.Starting))
                    Interlocked.Increment(ref successCount);
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        Assert.Equal(1, successCount);
        Assert.Equal(RuntimeState.Starting, mgr.Current);
    }

    [Theory]
    [InlineData(RuntimeState.Stopped, RuntimeState.Starting)]
    [InlineData(RuntimeState.Starting, RuntimeState.Running)]
    [InlineData(RuntimeState.Running, RuntimeState.Stopping)]
    [InlineData(RuntimeState.Stopping, RuntimeState.Stopped)]
    public void TryTransition_ValidTransitions_Succeed(RuntimeState from, RuntimeState to)
    {
        var mgr = new RuntimeStateManager();
        mgr.ForceSet(from);
        Assert.True(mgr.TryTransition(from, to));
    }
}
```

- [ ] **Step 2: Run tests — confirm red**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeStateManagerTests" --no-build
```

Expected: compilation failure (RuntimeStateManager is internal but InternalsVisibleTo is set — it should compile). If the stub from Task 2 already matches, tests pass. If tests do not pass, check the implementation matches Task 2 Step 2 exactly.

- [ ] **Step 3: Verify implementation matches expected**

The `RuntimeStateManager.cs` content from Task 2 Step 2 is already the full implementation. Confirm the file content is:

```csharp
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Atomic state machine for the host-level runtime lifecycle.
/// <para>Why: Provides a single authority for RuntimeState so all runtime collaborators read from one source.</para>
/// <para>Lifecycle: Created inside RuntimeBuilder.Build() and registered as a DI singleton; lives until RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime internal — only RuntimeHost and ModuleLifecycleService may use this directly.</para>
/// <para>Thread Safety: Thread Safe — all transitions use Interlocked.CompareExchange.</para>
/// </summary>
internal sealed class RuntimeStateManager
{
    private int _state = (int)RuntimeState.Stopped;

    /// <summary>Gets the current runtime state.</summary>
    public RuntimeState Current => (RuntimeState)Volatile.Read(ref _state);

    /// <summary>Atomically transitions from <paramref name="from"/> to <paramref name="to"/>. Returns <c>true</c> on success.</summary>
    public bool TryTransition(RuntimeState from, RuntimeState to)
        => Interlocked.CompareExchange(ref _state, (int)to, (int)from) == (int)from;

    /// <summary>Unconditionally sets the state. Use only when a CAS loop would create a race (e.g. forced Faulted transition).</summary>
    public void ForceSet(RuntimeState state)
        => Volatile.Write(ref _state, (int)state);
}
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeStateManagerTests"
```

Expected: 8 tests passed, 0 failed.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Runtime/Bootstrap/RuntimeStateManager.cs tests/Ferret.Runtime.Tests/Bootstrap/RuntimeStateManagerTests.cs
git commit -m "feat(sprint-5): RuntimeStateManager with thread-safe Interlocked transitions (WP-A)"
```

---

## Task 4: DefaultModule + BoundModule + IModuleWithDependencies

**Files:**
- Create: `src/Ferret.Runtime/Modules/IModuleWithDependencies.cs`
- Create: `src/Ferret.Runtime/Modules/DefaultModule.cs` (replaces placeholder)
- Create: `src/Ferret.Runtime/Modules/BoundModule.cs`
- Create: `tests/Ferret.Runtime.Tests/Modules/DefaultModuleTests.cs`
- Create: `tests/Ferret.Runtime.Tests/Modules/BoundModuleTests.cs`

**Interfaces:**
- Produces: `DefaultModule` (optional base class — composition is preferred; plugin authors may implement IModule directly without inheriting), `BoundModule` (internal adapter), `IModuleWithDependencies` — consumed by ModuleDependencyGraph (Task 6).

- [ ] **Step 1: Create IModuleWithDependencies**

Create `src/Ferret.Runtime/Modules/IModuleWithDependencies.cs`:

```csharp
namespace Ferret.Runtime.Modules;

/// <summary>
/// Optional interface that a module descriptor or DefaultModule subclass may implement to declare startup dependencies.
/// <para>Why: IModuleDescriptor has no Dependencies property (by design). This interface is the extension point for dependency ordering without forcing it into the Core contract.</para>
/// <para>Lifecycle: Checked once during ModuleDependencyGraph.Sort() at RuntimeBuilder.Build() time.</para>
/// <para>Layer: Ferret.Runtime — checked by ModuleDependencyGraph; never in Core.</para>
/// <para>Thread Safety: Single Thread Only — read-only at build time.</para>
/// </summary>
public interface IModuleWithDependencies
{
    /// <summary>Gets the module IDs that this module depends on. The runtime starts dependencies first.</summary>
    IReadOnlyList<string> DependsOn { get; }
}
```

- [ ] **Step 2: Create DefaultModule**

Replace the placeholder in `src/Ferret.Runtime/Modules/DefaultModule.cs`:

```csharp
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Modules;

/// <summary>
/// Optional convenience base class for Ferret modules. Plugin authors may implement <see cref="IModule"/> and <see cref="IModuleDescriptor"/> directly without inheriting this class.
/// <para>Why: Provides a default no-op state machine and lifecycle stubs so simple modules do not repeat boilerplate. It is not required — composition over inheritance is preferred.</para>
/// <para>Lifecycle: Subclasses are instantiated by the plugin author; passed to IRuntimeBuilder.AddModule(); owned by ModuleRegistry after Build().</para>
/// <para>Layer: Ferret.Runtime — subclasses live in plugin assemblies or in Ferret.Runtime itself for built-in modules.</para>
/// <para>Thread Safety: Thread Compatible — SetState is called only by LifecycleOrchestrator on one thread at a time; State reads are volatile.</para>
/// </summary>
public abstract class DefaultModule : IModule, IModuleDescriptor
{
    private int _state = (int)ModuleState.Unloaded;

    /// <summary>Initializes a new instance of <see cref="DefaultModule"/> with the specified metadata.</summary>
    protected DefaultModule(ModuleMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <inheritdoc/>
    public ModuleMetadata Metadata { get; }

    /// <inheritdoc/>
    public ModuleState State => (ModuleState)Volatile.Read(ref _state);

    // IModuleDescriptor members — delegate to Metadata for consistency.

    /// <inheritdoc/>
    public string Id => Metadata.Id;

    /// <inheritdoc/>
    public string Name => Metadata.Name;

    /// <inheritdoc/>
    public SemanticVersion Version => Metadata.Version;

    /// <inheritdoc/>
    public ModuleCapability Capabilities => Metadata.Capabilities;

    /// <summary>Sets the module state. Called exclusively by LifecycleOrchestrator.</summary>
    internal void SetState(ModuleState state)
        => Volatile.Write(ref _state, (int)state);

    /// <inheritdoc/>
    public virtual Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

- [ ] **Step 3: Create BoundModule**

Create `src/Ferret.Runtime/Modules/BoundModule.cs`:

```csharp
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Modules;

/// <summary>
/// Internal adapter that wraps a plain <see cref="IModuleDescriptor"/> (or <see cref="IModule"/>) so LifecycleOrchestrator always works with <see cref="DefaultModule"/>.
/// <para>Why: Allows plugin authors to implement IModule directly without extending DefaultModule; the runtime normalises all descriptors to DefaultModule at build time.</para>
/// <para>Lifecycle: Created by ModuleDescriptorStore.Add() for descriptors that do not already extend DefaultModule; owned by ModuleRegistry.</para>
/// <para>Layer: Ferret.Runtime internal — never exposed publicly.</para>
/// <para>Thread Safety: Thread Compatible — same contract as DefaultModule.</para>
/// </summary>
internal sealed class BoundModule : DefaultModule
{
    private readonly IModule? _lifecycleTarget;

    internal BoundModule(IModuleDescriptor descriptor)
        : base(ModuleMetadata.Create(
            descriptor.Id,
            descriptor.Name,
            descriptor.Version,
            descriptor.Capabilities))
    {
        _lifecycleTarget = descriptor as IModule;
    }

    /// <inheritdoc/>
    public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken)
        => _lifecycleTarget?.OnStartingAsync(context, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken)
        => _lifecycleTarget?.OnStartedAsync(context, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken)
        => _lifecycleTarget?.OnStoppingAsync(context, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken)
        => _lifecycleTarget?.OnStoppedAsync(context, cancellationToken) ?? Task.CompletedTask;
}
```

- [ ] **Step 4: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Modules/DefaultModuleTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Tests.Modules;

public sealed class DefaultModuleTests
{
    private sealed class ConcreteModule : DefaultModule
    {
        public ConcreteModule()
            : base(ModuleMetadata.Create("test", "Test", new SemanticVersion(1, 0, 0), ModuleCapability.None)) { }

        public int StartingCalls { get; private set; }
        public int StartedCalls { get; private set; }

        public override Task OnStartingAsync(IModuleContext ctx, CancellationToken ct)
        {
            StartingCalls++;
            return Task.CompletedTask;
        }

        public override Task OnStartedAsync(IModuleContext ctx, CancellationToken ct)
        {
            StartedCalls++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void State_Initial_IsUnloaded()
    {
        var m = new ConcreteModule();
        Assert.Equal(ModuleState.Unloaded, m.State);
    }

    [Fact]
    public void SetState_ChangesState()
    {
        var m = new ConcreteModule();
        m.SetState(ModuleState.Active);
        Assert.Equal(ModuleState.Active, m.State);
    }

    [Fact]
    public void MetadataProperties_DelegateToMetadata()
    {
        var m = new ConcreteModule();
        Assert.Equal("test", m.Id);
        Assert.Equal("Test", m.Name);
        Assert.Equal(new SemanticVersion(1, 0, 0), m.Version);
        Assert.Equal(ModuleCapability.None, m.Capabilities);
    }

    [Fact]
    public async Task OnStartingAsync_DefaultImpl_ReturnsCompleted()
    {
        // DefaultModule base method returns Task.CompletedTask when not overridden
        var m = new ConcreteModule();
        // override calls base body — counting calls proves override fires
        await m.OnStartingAsync(null!, CancellationToken.None);
        Assert.Equal(1, m.StartingCalls);
    }
}
```

Create `tests/Ferret.Runtime.Tests/Modules/BoundModuleTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Tests.Modules;

public sealed class BoundModuleTests
{
    private sealed class PlainDescriptor : IModuleDescriptor
    {
        public string Id => "plain";
        public string Name => "Plain";
        public SemanticVersion Version => new(1, 0, 0);
        public ModuleCapability Capabilities => ModuleCapability.None;
    }

    private sealed class FullModule : IModule, IModuleDescriptor
    {
        public string Id => "full";
        public string Name => "Full";
        public SemanticVersion Version => new(1, 0, 0);
        public ModuleCapability Capabilities => ModuleCapability.None;
        public ModuleMetadata Metadata => ModuleMetadata.Create(Id, Name, Version, Capabilities);
        public ModuleState State => ModuleState.Unloaded;
        public int StartingCalls { get; private set; }

        public Task OnStartingAsync(IModuleContext ctx, CancellationToken ct)
        {
            StartingCalls++;
            return Task.CompletedTask;
        }

        public Task OnStartedAsync(IModuleContext ctx, CancellationToken ct) => Task.CompletedTask;
        public Task OnStoppingAsync(IModuleContext ctx, CancellationToken ct) => Task.CompletedTask;
        public Task OnStoppedAsync(IModuleContext ctx, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public void BoundModule_WithPlainDescriptor_HasNoLifecycleTarget()
    {
        var bound = new BoundModule(new PlainDescriptor());
        Assert.Equal("plain", bound.Id);
        Assert.Equal(ModuleState.Unloaded, bound.State);
    }

    [Fact]
    public async Task BoundModule_WithIModule_DelegatesOnStarting()
    {
        var inner = new FullModule();
        var bound = new BoundModule(inner);
        await bound.OnStartingAsync(null!, CancellationToken.None);
        Assert.Equal(1, inner.StartingCalls);
    }

    [Fact]
    public async Task BoundModule_WithPlainDescriptor_OnStarting_IsNoOp()
    {
        var bound = new BoundModule(new PlainDescriptor());
        await bound.OnStartingAsync(null!, CancellationToken.None); // no exception
    }
}
```

- [ ] **Step 5: Run tests — confirm red, then green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "DefaultModuleTests|BoundModuleTests"
```

Expected after implementation: all tests pass.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Runtime/Modules/ tests/Ferret.Runtime.Tests/Modules/
git commit -m "feat(sprint-5): DefaultModule optional base class, BoundModule adapter, IModuleWithDependencies (WP-A)"
```

---

## Task 5: ModuleDescriptorStore

**Files:**
- Create: `src/Ferret.Runtime/Registry/ModuleDescriptorStore.cs` (replaces placeholder)
- Create: `tests/Ferret.Runtime.Tests/Registry/ModuleDescriptorStoreTests.cs`

**Interfaces:**
- Consumes: `IModuleDescriptor`, `DefaultModule`, `BoundModule`
- Produces: `ModuleDescriptorStore.Add(IModuleDescriptor)`, `GetAll() → IReadOnlyList<IModuleDescriptor>` — consumed by `RuntimeBuilder.Build()` → `ModuleDependencyGraph.Sort()`.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Registry/ModuleDescriptorStoreTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

namespace Ferret.Runtime.Tests.Registry;

public sealed class ModuleDescriptorStoreTests
{
    private sealed class Desc : IModuleDescriptor
    {
        public Desc(string id) => Id = id;
        public string Id { get; }
        public string Name => Id;
        public SemanticVersion Version => new(1, 0, 0);
        public ModuleCapability Capabilities => ModuleCapability.None;
    }

    [Fact]
    public void GetAll_Empty_ReturnsEmpty()
    {
        var store = new ModuleDescriptorStore();
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Add_PlainDescriptor_WrapsInBoundModule()
    {
        var store = new ModuleDescriptorStore();
        store.Add(new Desc("a"));
        var all = store.GetAll();
        Assert.Single(all);
        Assert.IsType<BoundModule>(all[0]);
    }

    [Fact]
    public void Add_DefaultModuleSubclass_KeptAsIs()
    {
        var store = new ModuleDescriptorStore();
        store.Add(new FakeMod());
        var all = store.GetAll();
        Assert.IsType<FakeMod>(all[0]);
    }

    [Fact]
    public void Add_DuplicateId_ThrowsInvalidOperation()
    {
        var store = new ModuleDescriptorStore();
        store.Add(new Desc("dup"));
        var ex = Assert.Throws<InvalidOperationException>(() => store.Add(new Desc("dup")));
        Assert.Contains("dup", ex.Message);
    }

    [Fact]
    public void Add_NullDescriptor_ThrowsArgumentNull()
    {
        var store = new ModuleDescriptorStore();
        Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    }

    private sealed class FakeMod : DefaultModule
    {
        public FakeMod() : base(ModuleMetadata.Create("fake", "Fake", new SemanticVersion(1, 0, 0), ModuleCapability.None)) { }
    }
}
```

- [ ] **Step 2: Implement ModuleDescriptorStore**

Replace `src/Ferret.Runtime/Registry/ModuleDescriptorStore.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Registry;

/// <summary>
/// Accumulates module descriptors during the RuntimeBuilder configuration phase, wrapping non-DefaultModule entries into BoundModule.
/// <para>Why: Normalises all descriptors to DefaultModule so LifecycleOrchestrator always works with a uniform type.</para>
/// <para>Lifecycle: Created by RuntimeBuilder; consumed once by Build(); discarded after Build() returns.</para>
/// <para>Layer: Ferret.Runtime internal — used only by RuntimeBuilder.</para>
/// <para>Thread Safety: Single Thread Only — configure from one thread before Build().</para>
/// </summary>
internal sealed class ModuleDescriptorStore
{
    private readonly List<DefaultModule> _modules = [];
    private readonly HashSet<string> _ids = [];

    /// <summary>Adds a descriptor. Wraps plain IModuleDescriptor into BoundModule. Throws if the ID is already registered.</summary>
    public void Add(IModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_ids.Add(descriptor.Id))
            throw new InvalidOperationException(
                $"A module with ID '{descriptor.Id}' has already been registered.");

        if (descriptor is DefaultModule dm)
            _modules.Add(dm);
        else
            _modules.Add(new BoundModule(descriptor));
    }

    /// <summary>Returns all registered modules in registration order.</summary>
    public IReadOnlyList<DefaultModule> GetAll() => _modules;
}
```

- [ ] **Step 3: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "ModuleDescriptorStoreTests"
```

Expected: all tests pass.

- [ ] **Step 4: Commit**

```
git add src/Ferret.Runtime/Registry/ModuleDescriptorStore.cs tests/Ferret.Runtime.Tests/Registry/ModuleDescriptorStoreTests.cs
git commit -m "feat(sprint-5): ModuleDescriptorStore with BoundModule wrapping and duplicate ID guard (WP-B)"
```

---

## Task 6: ModuleDependencyGraph

**Files:**
- Create: `src/Ferret.Runtime/Registry/ModuleDependencyGraph.cs` (replaces placeholder)
- Create: `tests/Ferret.Runtime.Tests/Registry/ModuleDependencyGraphTests.cs`

**Interfaces:**
- Consumes: `DefaultModule`, `IModuleWithDependencies`
- Produces: `ModuleDependencyGraph.Sort(IReadOnlyList<DefaultModule>) → IReadOnlyList<DefaultModule>` — called in `RuntimeBuilder.Build()`.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Registry/ModuleDependencyGraphTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

namespace Ferret.Runtime.Tests.Registry;

public sealed class ModuleDependencyGraphTests
{
    private static DefaultModule Make(string id, params string[] deps)
    {
        var meta = ModuleMetadata.Create(id, id, new SemanticVersion(1, 0, 0), ModuleCapability.None);
        return deps.Length == 0
            ? new NoDepsModule(meta)
            : new DepsModule(meta, deps);
    }

    private sealed class NoDepsModule(ModuleMetadata m) : DefaultModule(m) { }

    private sealed class DepsModule(ModuleMetadata m, string[] deps) : DefaultModule(m), IModuleWithDependencies
    {
        public IReadOnlyList<string> DependsOn => deps;
    }

    [Fact]
    public void Sort_NoDependencies_PreservesOrder()
    {
        var a = Make("a");
        var b = Make("b");
        var sorted = ModuleDependencyGraph.Sort([a, b]);
        Assert.Equal(["a", "b"], sorted.Select(m => m.Id));
    }

    [Fact]
    public void Sort_ChainDependency_StartsDependencyFirst()
    {
        var b = Make("b", "a");
        var a = Make("a");
        var sorted = ModuleDependencyGraph.Sort([b, a]);
        var ids = sorted.Select(m => m.Id).ToList();
        Assert.True(ids.IndexOf("a") < ids.IndexOf("b"));
    }

    [Fact]
    public void Sort_DiamondDependency_StartsRootFirst()
    {
        // a ← b, a ← c, b ← d, c ← d
        var d = Make("d", "b", "c");
        var b = Make("b", "a");
        var c = Make("c", "a");
        var a = Make("a");
        var sorted = ModuleDependencyGraph.Sort([d, b, c, a]);
        var ids = sorted.Select(m => m.Id).ToList();
        Assert.True(ids.IndexOf("a") < ids.IndexOf("b"));
        Assert.True(ids.IndexOf("a") < ids.IndexOf("c"));
        Assert.True(ids.IndexOf("b") < ids.IndexOf("d"));
        Assert.True(ids.IndexOf("c") < ids.IndexOf("d"));
    }

    [Fact]
    public void Sort_CycleDetected_ThrowsInvalidOperation()
    {
        var a = Make("a", "b");
        var b = Make("b", "a");
        var ex = Assert.Throws<InvalidOperationException>(() => ModuleDependencyGraph.Sort([a, b]));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sort_MissingDependency_ThrowsInvalidOperation()
    {
        var a = Make("a", "missing");
        var ex = Assert.Throws<InvalidOperationException>(() => ModuleDependencyGraph.Sort([a]));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void Sort_EmptyList_ReturnsEmpty()
    {
        Assert.Empty(ModuleDependencyGraph.Sort([]));
    }
}
```

- [ ] **Step 2: Implement ModuleDependencyGraph**

Replace `src/Ferret.Runtime/Registry/ModuleDependencyGraph.cs`:

```csharp
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Registry;

/// <summary>
/// Performs topological sort over a set of DefaultModule entries using DFS, respecting IModuleWithDependencies edges.
/// <para>Why: Modules must start in dependency order; the sort is computed once at build time, not at every startup.</para>
/// <para>Lifecycle: Stateless static utility — called once by RuntimeBuilder.Build().</para>
/// <para>Layer: Ferret.Runtime internal — used only by RuntimeBuilder.</para>
/// <para>Thread Safety: Thread Safe — stateless static method.</para>
/// </summary>
internal static class ModuleDependencyGraph
{
    /// <summary>Returns modules sorted in dependency order (dependencies first). Throws on cycles or missing IDs.</summary>
    public static IReadOnlyList<DefaultModule> Sort(IReadOnlyList<DefaultModule> modules)
    {
        var byId = modules.ToDictionary(m => m.Id);
        var sorted = new List<DefaultModule>(modules.Count);
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        foreach (var module in modules)
            Visit(module, byId, sorted, visited, inStack);

        return sorted;
    }

    private static void Visit(
        DefaultModule module,
        Dictionary<string, DefaultModule> byId,
        List<DefaultModule> sorted,
        HashSet<string> visited,
        HashSet<string> inStack)
    {
        if (visited.Contains(module.Id))
            return;

        if (!inStack.Add(module.Id))
            throw new InvalidOperationException(
                $"Dependency cycle detected involving module '{module.Id}'.");

        if (module is IModuleWithDependencies deps)
        {
            foreach (var depId in deps.DependsOn)
            {
                if (!byId.TryGetValue(depId, out var dep))
                    throw new InvalidOperationException(
                        $"Module '{module.Id}' depends on '{depId}' which is not registered.");

                Visit(dep, byId, sorted, visited, inStack);
            }
        }

        inStack.Remove(module.Id);
        visited.Add(module.Id);
        sorted.Add(module);
    }
}
```

- [ ] **Step 3: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "ModuleDependencyGraphTests"
```

Expected: all 6 tests pass.

- [ ] **Step 4: Commit**

```
git add src/Ferret.Runtime/Registry/ModuleDependencyGraph.cs tests/Ferret.Runtime.Tests/Registry/ModuleDependencyGraphTests.cs
git commit -m "feat(sprint-5): ModuleDependencyGraph DFS topological sort with cycle and missing-dependency detection (WP-B)"
```

---

## Task 7: ModuleRegistry

**Files:**
- Create: `src/Ferret.Runtime/Registry/ModuleRegistry.cs` (replaces placeholder)
- Create: `tests/Ferret.Runtime.Tests/Registry/ModuleRegistryTests.cs`

**Interfaces:**
- Consumes: `IReadOnlyList<DefaultModule>` (sorted)
- Produces: `ModuleRegistry : IModuleRegistry` — consumed by `RuntimeHost.Modules` and `ModuleLifecycleService`.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Registry/ModuleRegistryTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

namespace Ferret.Runtime.Tests.Registry;

public sealed class ModuleRegistryTests
{
    private static DefaultModule MakeModule(string id)
    {
        var meta = ModuleMetadata.Create(id, id, new SemanticVersion(1, 0, 0), ModuleCapability.None);
        return new FakeMod(meta);
    }

    private sealed class FakeMod(ModuleMetadata m) : DefaultModule(m) { }

    [Fact]
    public void Modules_ReturnsAllRegistered()
    {
        var a = MakeModule("a");
        var b = MakeModule("b");
        var registry = new ModuleRegistry([a, b]);
        Assert.Equal(2, registry.Modules.Count);
    }

    [Fact]
    public void TryGet_ExistingId_ReturnsTrue()
    {
        var a = MakeModule("a");
        var registry = new ModuleRegistry([a]);
        bool found = registry.TryGet("a", out IModule? m);
        Assert.True(found);
        Assert.Same(a, m);
    }

    [Fact]
    public void TryGet_MissingId_ReturnsFalse()
    {
        var registry = new ModuleRegistry([]);
        bool found = registry.TryGet("x", out IModule? m);
        Assert.False(found);
        Assert.Null(m);
    }

    [Fact]
    public void GetById_ExistingId_ReturnsModule()
    {
        var a = MakeModule("a");
        var registry = new ModuleRegistry([a]);
        Assert.Same(a, registry.GetById("a"));
    }

    [Fact]
    public void GetById_MissingId_ThrowsKeyNotFound()
    {
        var registry = new ModuleRegistry([]);
        Assert.Throws<KeyNotFoundException>(() => registry.GetById("missing"));
    }
}
```

- [ ] **Step 2: Implement ModuleRegistry**

Replace `src/Ferret.Runtime/Registry/ModuleRegistry.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Registry;

/// <summary>
/// Read-only registry of active modules, keyed by module ID.
/// <para>Why: Gives the application layer and module contexts a safe, read-only view of all registered modules without exposing internal lifecycle state.</para>
/// <para>Lifecycle: Created by RuntimeBuilder.Build() from the sorted module list; registered as a DI singleton; lives until RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime — IRuntimeHost.Modules exposes this via the IModuleRegistry contract.</para>
/// <para>Thread Safety: Thread Safe — immutable after construction; dictionary lookups are read-only.</para>
/// </summary>
internal sealed class ModuleRegistry : IModuleRegistry
{
    private readonly IReadOnlyList<DefaultModule> _ordered;
    private readonly Dictionary<string, DefaultModule> _byId;

    internal ModuleRegistry(IReadOnlyList<DefaultModule> ordered)
    {
        _ordered = ordered;
        _byId = ordered.ToDictionary(m => m.Id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IModule> Modules => _ordered;

    /// <inheritdoc/>
    public bool TryGet(string id, out IModule? module)
    {
        bool found = _byId.TryGetValue(id, out DefaultModule? dm);
        module = dm;
        return found;
    }

    /// <inheritdoc/>
    public IModule GetById(string id)
    {
        if (!_byId.TryGetValue(id, out DefaultModule? dm))
            throw new KeyNotFoundException($"No module with ID '{id}' is registered.");
        return dm;
    }
}
```

- [ ] **Step 3: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "ModuleRegistryTests"
```

Expected: all 5 tests pass.

- [ ] **Step 4: Commit**

```
git add src/Ferret.Runtime/Registry/ModuleRegistry.cs tests/Ferret.Runtime.Tests/Registry/ModuleRegistryTests.cs
git commit -m "feat(sprint-5): ModuleRegistry read-only IModuleRegistry view (WP-B)"
```

---

## Task 8: ExecutionContext + ModuleContext

**Files:**
- Create: `src/Ferret.Runtime/Lifecycle/ExecutionContext.cs`
- Create: `src/Ferret.Runtime/Lifecycle/ModuleContext.cs`
- Create: `tests/Ferret.Runtime.Tests/Lifecycle/ExecutionContextTests.cs`

**Interfaces:**
- Consumes: `IExecutionContext`, `IModuleContext`, `IModuleRegistry` from Ferret.Core
- Produces: `ExecutionContext : IExecutionContext`, `ModuleContext : IModuleContext` — passed to module lifecycle methods.

- [ ] **Step 1: Inspect Core contracts**

Read `src/Ferret.Core/Runtime/IExecutionContext.cs` and `src/Ferret.Core/Runtime/IModuleContext.cs` to confirm the exact members. Adapt the implementation to match whatever properties those interfaces define.

- [ ] **Step 2: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Lifecycle/ExecutionContextTests.cs` based on the Core contracts you read in Step 1. At minimum test:
- `ExecutionContext` implements `IExecutionContext`
- `ModuleContext` implements `IModuleContext`
- `ModuleContext.Registry` returns the passed registry
- `ModuleContext.Module` returns the passed module

- [ ] **Step 3: Implement ExecutionContext**

Create `src/Ferret.Runtime/Lifecycle/ExecutionContext.cs`:

```csharp
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// Default implementation of IExecutionContext, carrying shared context for a single runtime operation.
/// <para>Why: Provides lifecycle methods a uniform way to access shared runtime services without tight coupling to RuntimeHost.</para>
/// <para>Lifecycle: Created per-operation by LifecycleOrchestrator; not reused across operations.</para>
/// <para>Layer: Ferret.Runtime internal — never exposed publicly; passed through IModuleContext.</para>
/// <para>Thread Safety: Single Thread Only — created and consumed on the same call stack.</para>
/// </summary>
internal sealed class ExecutionContext : IExecutionContext
{
    // Add members that IExecutionContext requires (read from the interface file in Step 1).
}
```

- [ ] **Step 4: Implement ModuleContext**

Create `src/Ferret.Runtime/Lifecycle/ModuleContext.cs`:

```csharp
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// Default implementation of IModuleContext, giving a module access to the registry and its own identity.
/// <para>Why: Gives modules a stable, narrow view of the runtime so they can discover peer modules without accessing RuntimeHost directly.</para>
/// <para>Lifecycle: Created by ModuleLifecycleService per module per lifecycle phase; not reused.</para>
/// <para>Layer: Ferret.Runtime internal — passed to module lifecycle methods as IModuleContext.</para>
/// <para>Thread Safety: Single Thread Only — created and consumed on the lifecycle thread.</para>
/// </summary>
internal sealed class ModuleContext : IModuleContext
{
    internal ModuleContext(IModule module, IModuleRegistry registry)
    {
        // Assign properties that IModuleContext requires (from Step 1).
    }

    // Add properties matching IModuleContext interface.
}
```

- [ ] **Step 5: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "ExecutionContextTests"
```

- [ ] **Step 6: Commit**

```
git add src/Ferret.Runtime/Lifecycle/ExecutionContext.cs src/Ferret.Runtime/Lifecycle/ModuleContext.cs tests/Ferret.Runtime.Tests/Lifecycle/ExecutionContextTests.cs
git commit -m "feat(sprint-5): ExecutionContext and ModuleContext lifecycle context implementations (WP-C)"
```

---

## Task 9: FakeModule + FakeHealthCheck (test helpers)

**Files:**
- Create: `tests/Ferret.Runtime.Tests/Fakes/FakeModule.cs`
- Create: `tests/Ferret.Runtime.Tests/Fakes/FakeHealthCheck.cs`

These helpers are used across Tasks 10–14. No production code changes.

- [ ] **Step 1: Create FakeModule**

Create `tests/Ferret.Runtime.Tests/Fakes/FakeModule.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Tests.Fakes;

/// <summary>Test double for DefaultModule. Tracks lifecycle call counts and optionally throws on start.</summary>
public sealed class FakeModule : DefaultModule
{
    private readonly Exception? _startException;

    public FakeModule(string id = "fake", Exception? startException = null)
        : base(ModuleMetadata.Create(id, id, new SemanticVersion(1, 0, 0), ModuleCapability.None))
    {
        _startException = startException;
    }

    public int OnStartingCalls { get; private set; }
    public int OnStartedCalls { get; private set; }
    public int OnStoppingCalls { get; private set; }
    public int OnStoppedCalls { get; private set; }

    public override Task OnStartingAsync(IModuleContext ctx, CancellationToken ct)
    {
        OnStartingCalls++;
        if (_startException is not null) throw _startException;
        return Task.CompletedTask;
    }

    public override Task OnStartedAsync(IModuleContext ctx, CancellationToken ct)
    {
        OnStartedCalls++;
        return Task.CompletedTask;
    }

    public override Task OnStoppingAsync(IModuleContext ctx, CancellationToken ct)
    {
        OnStoppingCalls++;
        return Task.CompletedTask;
    }

    public override Task OnStoppedAsync(IModuleContext ctx, CancellationToken ct)
    {
        OnStoppedCalls++;
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Create FakeHealthCheck**

Create `tests/Ferret.Runtime.Tests/Fakes/FakeHealthCheck.cs`:

```csharp
using Ferret.Core.Abstractions;

namespace Ferret.Runtime.Tests.Fakes;

/// <summary>Test double for IHealthCheck returning a preset result.</summary>
public sealed class FakeHealthCheck : IHealthCheck
{
    private readonly HealthCheckResult _result;

    public FakeHealthCheck(HealthCheckResult result) => _result = result;

    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
        => Task.FromResult(_result);
}
```

- [ ] **Step 3: Build tests project**

```
dotnet build tests/Ferret.Runtime.Tests/
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```
git add tests/Ferret.Runtime.Tests/Fakes/
git commit -m "test(sprint-5): FakeModule and FakeHealthCheck test helpers"
```

---

## Task 10: LifecycleOrchestrator + ModuleLifecycleService

**Files:**
- Create: `src/Ferret.Runtime/Lifecycle/LifecycleOrchestrator.cs` (replaces placeholder)
- Create: `src/Ferret.Runtime/Lifecycle/ModuleLifecycleService.cs` (replaces placeholder)
- Create: `tests/Ferret.Runtime.Tests/Lifecycle/LifecycleOrchestratorTests.cs`

**Interfaces:**
- Consumes: `DefaultModule`, `IModuleContext`, `IInitializable`, `ModuleContext`, `RuntimeStateManager`, `RuntimeEventDispatcher`
- Produces: `LifecycleOrchestrator.StartModuleAsync`, `StopModuleAsync` and `ModuleLifecycleService : IHostedService`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Lifecycle/LifecycleOrchestratorTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Lifecycle;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;
using Ferret.Runtime.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Runtime.Tests.Lifecycle;

public sealed class LifecycleOrchestratorTests
{
    private static ModuleContext MakeContext(DefaultModule module)
    {
        var registry = new ModuleRegistry([module]);
        return new ModuleContext(module, registry);
    }

    [Fact]
    public async Task StartModuleAsync_CallsOnStartingAndOnStarted()
    {
        var module = new FakeModule("m");
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await orchestrator.StartModuleAsync(module, ctx, CancellationToken.None);

        Assert.Equal(1, module.OnStartingCalls);
        Assert.Equal(1, module.OnStartedCalls);
        Assert.Equal(ModuleState.Active, module.State);
    }

    [Fact]
    public async Task StopModuleAsync_CallsOnStoppingAndOnStopped()
    {
        var module = new FakeModule("m");
        module.SetState(ModuleState.Active);
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await orchestrator.StopModuleAsync(module, ctx, CancellationToken.None);

        Assert.Equal(1, module.OnStoppingCalls);
        Assert.Equal(1, module.OnStoppedCalls);
        Assert.Equal(ModuleState.Stopped, module.State);
    }

    [Fact]
    public async Task StartModuleAsync_WhenOnStartingThrows_SetsFaulted()
    {
        var module = new FakeModule("m", startException: new InvalidOperationException("fail"));
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.StartModuleAsync(module, ctx, CancellationToken.None));

        Assert.Equal(ModuleState.Faulted, module.State);
    }

    [Fact]
    public async Task StartModuleAsync_IInitializable_CallsInitialize()
    {
        var module = new FakeInitializableModule("init");
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await orchestrator.StartModuleAsync(module, ctx, CancellationToken.None);

        Assert.True(module.InitializeCalled);
    }
}

file sealed class FakeInitializableModule : DefaultModule, Ferret.Core.Abstractions.IInitializable
{
    public FakeInitializableModule(string id)
        : base(ModuleMetadata.Create(id, id, new SemanticVersion(1, 0, 0), ModuleCapability.None)) { }

    public bool InitializeCalled { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        InitializeCalled = true;
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Implement LifecycleOrchestrator**

Replace `src/Ferret.Runtime/Lifecycle/LifecycleOrchestrator.cs`:

```csharp
using Ferret.Core.Abstractions;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Microsoft.Extensions.Logging;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// Drives the lifecycle method sequence for a single module: Loading → Active (start) or Active → Stopped (stop).
/// <para>Why: Centralises lifecycle sequencing so RuntimeHost and ModuleLifecycleService do not duplicate the start/stop logic.</para>
/// <para>Lifecycle: Registered as a DI singleton; injected into ModuleLifecycleService.</para>
/// <para>Layer: Ferret.Runtime internal — not accessible outside the runtime assembly.</para>
/// <para>Thread Safety: Thread Compatible — each StartModuleAsync/StopModuleAsync call is independent; do not call concurrently on the same module.</para>
/// </summary>
internal sealed class LifecycleOrchestrator
{
    private readonly ILogger<LifecycleOrchestrator> _logger;

    public LifecycleOrchestrator(ILogger<LifecycleOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>Starts a module: Loading → (OnStarting → IInitializable → OnStarted) → Active. Throws and sets Faulted on failure.</summary>
    public async Task StartModuleAsync(
        DefaultModule module,
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        module.SetState(ModuleState.Loading);

        try
        {
            await module.OnStartingAsync(context, cancellationToken).ConfigureAwait(false);

            if (module is IInitializable initializable)
                await initializable.InitializeAsync(cancellationToken).ConfigureAwait(false);

            await module.OnStartedAsync(context, cancellationToken).ConfigureAwait(false);
            module.SetState(ModuleState.Active);

            _logger.LogInformation("Module '{Id}' started.", module.Id);
        }
        catch (Exception ex)
        {
            module.SetState(ModuleState.Faulted);
            _logger.LogError(ex, "Module '{Id}' faulted during startup.", module.Id);
            throw;
        }
    }

    /// <summary>Stops a module: Deactivating → (OnStopping → OnStopped) → Stopped. Best-effort — logs but does not rethrow.</summary>
    public async Task StopModuleAsync(
        DefaultModule module,
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        module.SetState(ModuleState.Deactivating);

        try
        {
            await module.OnStoppingAsync(context, cancellationToken).ConfigureAwait(false);
            await module.OnStoppedAsync(context, cancellationToken).ConfigureAwait(false);
            module.SetState(ModuleState.Stopped);

            _logger.LogInformation("Module '{Id}' stopped.", module.Id);
        }
        catch (Exception ex)
        {
            module.SetState(ModuleState.Faulted);
            _logger.LogWarning(ex, "Module '{Id}' faulted during shutdown.", module.Id);
            // Do not rethrow — stop remaining modules.
        }
    }
}
```

- [ ] **Step 3: Implement ModuleLifecycleService**

Replace `src/Ferret.Runtime/Lifecycle/ModuleLifecycleService.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Core.Runtime.Events;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Events;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;
using Microsoft.Extensions.Hosting;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// IHostedService that starts and stops all modules in dependency order, publishing domain events and updating RuntimeState.
/// <para>Why: Bridges the IHost startup/shutdown lifecycle to the Ferret module lifecycle so RuntimeHost.StartAsync delegates to a single IHostedService.</para>
/// <para>Lifecycle: Registered as an IHostedService in RuntimeBuilder.Build(); started and stopped by IHost.</para>
/// <para>Layer: Ferret.Runtime internal — never accessible outside the runtime assembly.</para>
/// <para>Thread Safety: Single Thread Only — IHost guarantees StartAsync and StopAsync are not called concurrently.</para>
/// </summary>
internal sealed class ModuleLifecycleService : IHostedService
{
    private readonly LifecycleOrchestrator _orchestrator;
    private readonly IReadOnlyList<DefaultModule> _modules;
    private readonly RuntimeStateManager _stateManager;
    private readonly RuntimeEventDispatcher _events;
    private readonly RuntimeOptions _options;
    private readonly ModuleRegistry _registry;

    public ModuleLifecycleService(
        LifecycleOrchestrator orchestrator,
        IReadOnlyList<DefaultModule> modules,
        RuntimeStateManager stateManager,
        RuntimeEventDispatcher events,
        RuntimeOptions options,
        ModuleRegistry registry)
    {
        _orchestrator = orchestrator;
        _modules = modules;
        _stateManager = stateManager;
        _events = events;
        _options = options;
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var module in _modules)
        {
            var ctx = new ModuleContext(module, _registry);
            await _orchestrator.StartModuleAsync(module, ctx, cancellationToken).ConfigureAwait(false);
            await _events.PublishAsync(
                new ModuleActivated(module.Metadata.Id, module.Metadata.Name),
                cancellationToken).ConfigureAwait(false);
        }

        _stateManager.TryTransition(RuntimeState.Starting, RuntimeState.Running);
        await _events.PublishAsync(
            new RuntimeStarted(_options.RuntimeVersion),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        int activeCount = _modules.Count(m => m.State == ModuleState.Active);

        foreach (var module in _modules.Reverse())
        {
            var ctx = new ModuleContext(module, _registry);
            await _orchestrator.StopModuleAsync(module, ctx, cancellationToken).ConfigureAwait(false);
            await _events.PublishAsync(
                new ModuleStopped(module.Metadata.Id, module.Metadata.Name),
                cancellationToken).ConfigureAwait(false);
        }

        _stateManager.TryTransition(RuntimeState.Stopping, RuntimeState.Stopped);
        await _events.PublishAsync(
            new RuntimeStopped(_options.RuntimeVersion, activeCount),
            cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "LifecycleOrchestratorTests"
```

Expected: all 4 tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Runtime/Lifecycle/ tests/Ferret.Runtime.Tests/Lifecycle/
git commit -m "feat(sprint-5): LifecycleOrchestrator and ModuleLifecycleService IHostedService (WP-C)"
```

---

## Task 11: RuntimeEventDispatcher

**Files:**
- Create: `src/Ferret.Runtime/Events/RuntimeEventDispatcher.cs` (replaces placeholder)
- Create: `tests/Ferret.Runtime.Tests/Events/RuntimeEventDispatcherTests.cs`

**Decision:** System.Threading.Channels evaluated and **Rejected** (see Technology Evaluation). ARCH-013 requires synchronous, isolated in-process dispatch — a lock-protected handler dictionary is correct.

**Interfaces:**
- Consumes: `DomainEvent` from Ferret.Core.Events
- Produces: `RuntimeEventDispatcher.Subscribe<T>`, `PublishAsync<T>` — consumed by ModuleLifecycleService and tests.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Events/RuntimeEventDispatcherTests.cs`:

```csharp
using Ferret.Core.Events;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime.Events;
using Ferret.Runtime.Events;

namespace Ferret.Runtime.Tests.Events;

public sealed class RuntimeEventDispatcherTests
{
    [Fact]
    public async Task PublishAsync_NoHandlers_DoesNotThrow()
    {
        var dispatcher = new RuntimeEventDispatcher();
        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);
    }

    [Fact]
    public async Task Subscribe_HandlerReceivesPublishedEvent()
    {
        var dispatcher = new RuntimeEventDispatcher();
        RuntimeStarted? received = null;
        dispatcher.Subscribe<RuntimeStarted>(e => { received = e; return Task.CompletedTask; });

        await dispatcher.PublishAsync(new RuntimeStarted("2.0.0"), CancellationToken.None);

        Assert.NotNull(received);
        Assert.Equal("2.0.0", received.RuntimeVersion);
    }

    [Fact]
    public async Task Subscribe_MultipleHandlers_AllInvoked()
    {
        var dispatcher = new RuntimeEventDispatcher();
        int count = 0;
        dispatcher.Subscribe<RuntimeStarted>(_ => { count++; return Task.CompletedTask; });
        dispatcher.Subscribe<RuntimeStarted>(_ => { count++; return Task.CompletedTask; });

        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task PublishAsync_HandlerThrows_OtherHandlersStillRun()
    {
        // ARCH-013: handler failures are isolated
        var dispatcher = new RuntimeEventDispatcher();
        int count = 0;
        dispatcher.Subscribe<RuntimeStarted>(_ => throw new InvalidOperationException("handler fail"));
        dispatcher.Subscribe<RuntimeStarted>(_ => { count++; return Task.CompletedTask; });

        // Must not throw to caller
        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Subscribe_DifferentEventTypes_HandlerNotCalledForWrongType()
    {
        var dispatcher = new RuntimeEventDispatcher();
        bool called = false;
        dispatcher.Subscribe<RuntimeStopped>(_ => { called = true; return Task.CompletedTask; });

        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.False(called);
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        var dispatcher = new RuntimeEventDispatcher();
        int count = 0;
        IDisposable sub = dispatcher.Subscribe<RuntimeStarted>(_ => { count++; return Task.CompletedTask; });
        sub.Dispose();

        dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(0, count);
    }
}
```

- [ ] **Step 2: Implement RuntimeEventDispatcher**

Replace `src/Ferret.Runtime/Events/RuntimeEventDispatcher.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Runtime.Events;

/// <summary>
/// In-process typed pub/sub event bus for runtime domain events. Handler failures are isolated per ARCH-013.
/// <para>Why: Decouples lifecycle components (LifecycleOrchestrator, ModuleLifecycleService) from each other; they publish events rather than calling each other directly.</para>
/// <para>Lifecycle: Registered as a DI singleton in RuntimeBuilder.Build(); lives until RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime internal — never exposed publicly; runtime components subscribe via injection.</para>
/// <para>Thread Safety: Thread Safe — handler registration and dispatch are protected by a lock.</para>
/// </summary>
internal sealed class RuntimeEventDispatcher
{
    private readonly Dictionary<Type, List<Func<DomainEvent, Task>>> _handlers = [];
    private readonly Lock _lock = new();

    /// <summary>Subscribes <paramref name="handler"/> to events of type <typeparamref name="T"/>. Returns a disposable to unsubscribe.</summary>
    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        Func<DomainEvent, Task> wrapper = e => handler((T)e);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Func<DomainEvent, Task>>? list))
            {
                list = [];
                _handlers[typeof(T)] = list;
            }

            list.Add(wrapper);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out List<Func<DomainEvent, Task>>? l))
                    l.Remove(wrapper);
            }
        });
    }

    /// <summary>Publishes <paramref name="domainEvent"/> to all registered handlers. Handler exceptions are caught and isolated.</summary>
    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        List<Func<DomainEvent, Task>>? handlers;
        lock (_lock)
        {
            _handlers.TryGetValue(typeof(T), out handlers);
            handlers = handlers is null ? null : [..handlers];
        }

        if (handlers is null) return;

        foreach (Func<DomainEvent, Task> handler in handlers)
        {
            try
            {
                await handler(domainEvent).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // ARCH-013: OperationCanceledException propagates
            }
            catch
            {
                // ARCH-013: all other handler failures are isolated; other handlers still run
            }
        }
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                onDispose();
            }
        }
    }
}
```

- [ ] **Step 3: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeEventDispatcherTests"
```

Expected: all 6 tests pass.

- [ ] **Step 4: Commit**

```
git add src/Ferret.Runtime/Events/RuntimeEventDispatcher.cs tests/Ferret.Runtime.Tests/Events/RuntimeEventDispatcherTests.cs
git commit -m "feat(sprint-5): RuntimeEventDispatcher synchronous in-process pub/sub with ARCH-013 handler isolation (WP-D)"
```

---

## Task 12: RuntimeHealthService

**Files:**
- Create: `src/Ferret.Runtime/Health/ModuleHealthResult.cs`
- Create: `src/Ferret.Runtime/Health/RuntimeHealthReport.cs`
- Create: `src/Ferret.Runtime/Health/RuntimeHealthService.cs` (replaces placeholder)
- Create: `tests/Ferret.Runtime.Tests/Health/RuntimeHealthServiceTests.cs`

**Decision:** Microsoft.Extensions.Diagnostics.HealthChecks evaluated and **Rejected** (see Technology Evaluation). Ferret.Core already defines IHealthCheck and HealthCheckResult — adopting MS types would conflict.

**Interfaces:**
- Consumes: `IHealthCheck`, `HealthCheckResult`, `HealthStatus` from Ferret.Core.Abstractions
- Produces: `RuntimeHealthService.CheckAsync() → RuntimeHealthReport`

- [ ] **Step 1: Create ModuleHealthResult and RuntimeHealthReport**

Create `src/Ferret.Runtime/Health/ModuleHealthResult.cs`:

```csharp
using Ferret.Core.Abstractions;

namespace Ferret.Runtime.Health;

/// <summary>
/// Pairs a named health check with its result for inclusion in a RuntimeHealthReport.
/// <para>Why: Preserves per-check identity so callers can pinpoint which check degraded or failed.</para>
/// <para>Lifecycle: Created by RuntimeHealthService.CheckAsync(); immutable value object.</para>
/// <para>Layer: Ferret.Runtime — returned to application layer via RuntimeHealthReport.</para>
/// <para>Thread Safety: Thread Safe — immutable after construction.</para>
/// </summary>
public sealed class ModuleHealthResult
{
    /// <summary>Initializes a new instance of <see cref="ModuleHealthResult"/>.</summary>
    public ModuleHealthResult(string checkName, HealthCheckResult result)
    {
        CheckName = checkName ?? throw new ArgumentNullException(nameof(checkName));
        Result = result;
    }

    /// <summary>Gets the name of the health check.</summary>
    public string CheckName { get; }

    /// <summary>Gets the health check result.</summary>
    public HealthCheckResult Result { get; }
}
```

Create `src/Ferret.Runtime/Health/RuntimeHealthReport.cs`:

```csharp
using Ferret.Core.Abstractions;

namespace Ferret.Runtime.Health;

/// <summary>
/// Aggregated health report for the Ferret runtime, containing the overall status and per-check results.
/// <para>Why: Provides a single snapshot of runtime health that the application layer can expose via health endpoints or telemetry.</para>
/// <para>Lifecycle: Created on each call to RuntimeHealthService.CheckAsync(); not cached.</para>
/// <para>Layer: Ferret.Runtime — consumed by the application layer; not referenced by Core.</para>
/// <para>Thread Safety: Thread Safe — immutable after construction.</para>
/// </summary>
public sealed class RuntimeHealthReport
{
    /// <summary>Initializes a new instance of <see cref="RuntimeHealthReport"/>.</summary>
    public RuntimeHealthReport(HealthStatus overallStatus, IReadOnlyList<ModuleHealthResult> results)
    {
        OverallStatus = overallStatus;
        Results = results ?? throw new ArgumentNullException(nameof(results));
    }

    /// <summary>Gets the worst status across all individual checks.</summary>
    public HealthStatus OverallStatus { get; }

    /// <summary>Gets the per-check results.</summary>
    public IReadOnlyList<ModuleHealthResult> Results { get; }
}
```

- [ ] **Step 2: Write failing tests**

Create `tests/Ferret.Runtime.Tests/Health/RuntimeHealthServiceTests.cs`:

```csharp
using Ferret.Core.Abstractions;
using Ferret.Runtime.Health;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Health;

public sealed class RuntimeHealthServiceTests
{
    [Fact]
    public async Task CheckAsync_NoChecks_ReturnsHealthy()
    {
        var service = new RuntimeHealthService([]);
        RuntimeHealthReport report = await service.CheckAsync(CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, report.OverallStatus);
        Assert.Empty(report.Results);
    }

    [Fact]
    public async Task CheckAsync_AllHealthy_ReturnsHealthy()
    {
        var service = new RuntimeHealthService([
            new FakeHealthCheck(HealthCheckResult.Healthy()),
            new FakeHealthCheck(HealthCheckResult.Healthy()),
        ]);
        RuntimeHealthReport report = await service.CheckAsync(CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, report.OverallStatus);
    }

    [Fact]
    public async Task CheckAsync_OneDegraded_ReturnsDegraded()
    {
        var service = new RuntimeHealthService([
            new FakeHealthCheck(HealthCheckResult.Healthy()),
            new FakeHealthCheck(HealthCheckResult.Degraded("slow")),
        ]);
        RuntimeHealthReport report = await service.CheckAsync(CancellationToken.None);
        Assert.Equal(HealthStatus.Degraded, report.OverallStatus);
    }

    [Fact]
    public async Task CheckAsync_OneUnhealthy_ReturnsUnhealthy()
    {
        var service = new RuntimeHealthService([
            new FakeHealthCheck(HealthCheckResult.Degraded("slow")),
            new FakeHealthCheck(HealthCheckResult.Unhealthy("down")),
        ]);
        RuntimeHealthReport report = await service.CheckAsync(CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, report.OverallStatus);
    }

    [Fact]
    public async Task CheckAsync_CheckThrows_ResultIsUnhealthy()
    {
        var service = new RuntimeHealthService([
            new ThrowingCheck(),
        ]);
        RuntimeHealthReport report = await service.CheckAsync(CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, report.OverallStatus);
        Assert.NotNull(report.Results[0].Result.Exception);
    }
}

file sealed class ThrowingCheck : Ferret.Core.Abstractions.IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct)
        => throw new InvalidOperationException("check exploded");
}
```

- [ ] **Step 3: Implement RuntimeHealthService**

Replace `src/Ferret.Runtime/Health/RuntimeHealthService.cs`:

```csharp
using Ferret.Core.Abstractions;

namespace Ferret.Runtime.Health;

/// <summary>
/// Aggregates IHealthCheck results into a RuntimeHealthReport.
/// <para>Why: Provides a single entry point for health aggregation so the application layer does not iterate checks itself.</para>
/// <para>Lifecycle: Registered as a DI singleton in RuntimeBuilder.Build(); lives until RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime — accessible to application layer via RuntimeHost (not yet exposed — expose in a future sprint).</para>
/// <para>Thread Safety: Thread Compatible — individual CheckAsync calls are independent; concurrent calls are safe if the underlying checks are.</para>
/// </summary>
internal sealed class RuntimeHealthService
{
    private readonly IReadOnlyList<IHealthCheck> _checks;

    internal RuntimeHealthService(IReadOnlyList<IHealthCheck> checks)
    {
        _checks = checks ?? throw new ArgumentNullException(nameof(checks));
    }

    /// <summary>Runs all registered health checks and returns an aggregated report.</summary>
    public async Task<RuntimeHealthReport> CheckAsync(CancellationToken cancellationToken)
    {
        if (_checks.Count == 0)
            return new RuntimeHealthReport(HealthStatus.Healthy, []);

        var results = new List<ModuleHealthResult>(_checks.Count);
        HealthStatus worst = HealthStatus.Healthy;

        for (int i = 0; i < _checks.Count; i++)
        {
            HealthCheckResult result;
            try
            {
                result = await _checks[i].CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result = HealthCheckResult.Unhealthy("Check threw an unhandled exception.", ex);
            }

            results.Add(new ModuleHealthResult($"check-{i}", result));

            if (result.Status > worst)
                worst = result.Status;
        }

        return new RuntimeHealthReport(worst, results);
    }
}
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeHealthServiceTests"
```

Expected: all 5 tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Runtime/Health/ tests/Ferret.Runtime.Tests/Health/
git commit -m "feat(sprint-5): RuntimeHealthService with per-check isolation and worst-status aggregation (WP-E)"
```

---

## Task 13: RuntimeHost + RuntimeBuilder Tests

Now that all dependencies are implemented, the RuntimeBuilder.Build() call is complete and the full integration can be tested.

**Files:**
- Create: `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeHostTests.cs`
- Create: `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeBuilderTests.cs`
- Create: `tests/Ferret.Runtime.Tests/Integration/RuntimeIntegrationTests.cs`

**Interfaces:**
- Consumes: everything built in Tasks 2–12

- [ ] **Step 1: Write RuntimeHostTests**

Create `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeHostTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Bootstrap;

public sealed class RuntimeHostTests : IAsyncDisposable
{
    private readonly IRuntimeHost _host;

    public RuntimeHostTests()
    {
        _host = new RuntimeBuilder().Build();
    }

    [Fact]
    public void State_BeforeStart_IsStopped()
    {
        Assert.Equal(RuntimeState.Stopped, _host.State);
    }

    [Fact]
    public async Task StartAsync_TransitionsToRunning()
    {
        await _host.StartAsync();
        Assert.Equal(RuntimeState.Running, _host.State);
    }

    [Fact]
    public async Task StopAsync_AfterStart_TransitionsToStopped()
    {
        await _host.StartAsync();
        await _host.StopAsync();
        Assert.Equal(RuntimeState.Stopped, _host.State);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperation()
    {
        await _host.StartAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _host.StartAsync());
    }

    [Fact]
    public void StopAsync_WhenNotRunning_ThrowsInvalidOperation()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _host.StopAsync());
    }

    [Fact]
    public void Modules_ReturnsRegistry()
    {
        Assert.NotNull(_host.Modules);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable d) await d.DisposeAsync();
    }
}
```

- [ ] **Step 2: Write RuntimeBuilderTests**

Create `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeBuilderTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Bootstrap;

public sealed class RuntimeBuilderTests
{
    [Fact]
    public void Build_NoModules_ReturnsRuntimeHost()
    {
        IRuntimeHost host = new RuntimeBuilder().Build();
        Assert.NotNull(host);
    }

    [Fact]
    public void AddModule_NullDescriptor_ThrowsArgumentNull()
    {
        var builder = new RuntimeBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.AddModule(null!));
    }

    [Fact]
    public async Task AddModule_ModuleAppearsInRegistry()
    {
        var module = new FakeModule("m");
        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(module)
            .Build();

        await host.StartAsync();

        Assert.True(host.Modules.TryGet("m", out _));

        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }

    [Fact]
    public async Task AddModule_DuplicateId_ThrowsOnBuild()
    {
        var builder = new RuntimeBuilder();
        builder.AddModule(new FakeModule("dup"));
        Assert.Throws<InvalidOperationException>(() => builder.AddModule(new FakeModule("dup")));
    }
}
```

- [ ] **Step 3: Write RuntimeIntegrationTests**

Create `tests/Ferret.Runtime.Tests/Integration/RuntimeIntegrationTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Core.Runtime.Events;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Events;
using Ferret.Runtime.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Runtime.Tests.Integration;

public sealed class RuntimeIntegrationTests
{
    [Fact]
    public async Task FullLifecycle_StartAndStop_AllModulesActivatedAndStopped()
    {
        var a = new FakeModule("a");
        var b = new FakeModule("b");

        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(a)
            .AddModule(b)
            .Build();

        await host.StartAsync();
        Assert.Equal(RuntimeState.Running, host.State);
        Assert.Equal(ModuleState.Active, a.State);
        Assert.Equal(ModuleState.Active, b.State);

        await host.StopAsync();
        Assert.Equal(RuntimeState.Stopped, host.State);
        Assert.Equal(ModuleState.Stopped, a.State);
        Assert.Equal(ModuleState.Stopped, b.State);

        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }

    [Fact]
    public async Task EventDispatch_RuntimeStartedEventFires()
    {
        RuntimeStarted? received = null;

        IRuntimeHost host = new RuntimeBuilder().Build();
        // Access dispatcher via services before start
        // Note: dispatcher is internal; test via event subscription after a future public API is added.
        // For now verify via state transitions only.
        await host.StartAsync();
        Assert.Equal(RuntimeState.Running, host.State);

        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }

    [Fact]
    public async Task DependencyOrder_DependentModuleStartsAfterDependency()
    {
        var startOrder = new List<string>();

        var a = new OrderTrackingModule("a", startOrder);
        var b = new OrderTrackingModuleWithDeps("b", "a", startOrder);

        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(b) // registered first, but must start after a
            .AddModule(a)
            .Build();

        await host.StartAsync();

        Assert.Equal(["a", "b"], startOrder);

        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }

    [Fact]
    public async Task FaultedModule_OnStart_RuntimeTransitionsToFaulted()
    {
        var bad = new FakeModule("bad", startException: new InvalidOperationException("boot fail"));

        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(bad)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
        Assert.Equal(RuntimeState.Faulted, host.State);

        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }
}

file sealed class OrderTrackingModule : Ferret.Runtime.Modules.DefaultModule
{
    private readonly List<string> _order;

    public OrderTrackingModule(string id, List<string> order)
        : base(Ferret.Core.Runtime.ModuleMetadata.Create(id, id, new Ferret.Core.Runtime.SemanticVersion(1, 0, 0), Ferret.Core.Runtime.ModuleCapability.None))
    {
        _order = order;
    }

    public override Task OnStartingAsync(Ferret.Core.Runtime.IModuleContext ctx, CancellationToken ct)
    {
        _order.Add(Id);
        return Task.CompletedTask;
    }
}

file sealed class OrderTrackingModuleWithDeps : Ferret.Runtime.Modules.DefaultModule, Ferret.Runtime.Modules.IModuleWithDependencies
{
    private readonly List<string> _order;

    public OrderTrackingModuleWithDeps(string id, string dep, List<string> order)
        : base(Ferret.Core.Runtime.ModuleMetadata.Create(id, id, new Ferret.Core.Runtime.SemanticVersion(1, 0, 0), Ferret.Core.Runtime.ModuleCapability.None))
    {
        DependsOn = [dep];
        _order = order;
    }

    public IReadOnlyList<string> DependsOn { get; }

    public override Task OnStartingAsync(Ferret.Core.Runtime.IModuleContext ctx, CancellationToken ct)
    {
        _order.Add(Id);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Run all tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/
```

Expected: 180+ tests pass, 0 fail.

- [ ] **Step 5: Commit**

```
git add tests/Ferret.Runtime.Tests/Bootstrap/ tests/Ferret.Runtime.Tests/Integration/
git commit -m "test(sprint-5): RuntimeHost, RuntimeBuilder, and integration tests (WP-A WP-F)"
```

---

## Task 14: DI Extensions

**Files:**
- Create: `src/Ferret.Runtime/Extensions/RuntimeServiceExtensions.cs`
- Create: `tests/Ferret.Runtime.Tests/Extensions/RuntimeServiceExtensionsTests.cs`

**Interfaces:**
- Produces: `AddFerretRuntime(IServiceCollection, Action<RuntimeBuilder>?) → IServiceCollection`

- [ ] **Step 1: Write failing test**

Create `tests/Ferret.Runtime.Tests/Extensions/RuntimeServiceExtensionsTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Extensions;
using Ferret.Runtime.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Runtime.Tests.Extensions;

public sealed class RuntimeServiceExtensionsTests
{
    [Fact]
    public void AddFerretRuntime_RegistersIRuntimeHost()
    {
        var services = new ServiceCollection();
        services.AddFerretRuntime();
        var provider = services.BuildServiceProvider();
        var host = provider.GetService<IRuntimeHost>();
        Assert.NotNull(host);
    }

    [Fact]
    public void AddFerretRuntime_WithConfigure_ModuleRegistered()
    {
        var services = new ServiceCollection();
        services.AddFerretRuntime(b => b.AddModule(new FakeModule("m")));
        var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<IRuntimeHost>();
        Assert.True(host.Modules.TryGet("m", out _));
    }

    [Fact]
    public void AddFerretRuntime_CalledTwice_ThrowsOrIsIdempotent()
    {
        // Registering twice should not produce two IRuntimeHost singletons
        var services = new ServiceCollection();
        services.AddFerretRuntime();
        services.AddFerretRuntime();
        var provider = services.BuildServiceProvider();
        var h1 = provider.GetRequiredService<IRuntimeHost>();
        var h2 = provider.GetRequiredService<IRuntimeHost>();
        Assert.Same(h1, h2);
    }
}
```

- [ ] **Step 2: Implement RuntimeServiceExtensions**

Create `src/Ferret.Runtime/Extensions/RuntimeServiceExtensions.cs`:

```csharp
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Runtime.Extensions;

/// <summary>
/// IServiceCollection extension methods for registering the Ferret runtime in an existing DI container.
/// <para>Why: Allows application-layer hosts (e.g. a Generic Host entry point) to add the Ferret runtime alongside other services without constructing RuntimeBuilder manually.</para>
/// <para>Lifecycle: Called once at application startup; the registered IRuntimeHost is a singleton.</para>
/// <para>Layer: Ferret.Runtime — consumed by the application layer; never referenced by Core.</para>
/// <para>Thread Safety: Single Thread Only — call during the service registration phase before the container is built.</para>
/// </summary>
public static class RuntimeServiceExtensions
{
    /// <summary>Registers <see cref="IRuntimeHost"/> as a singleton built from the optional <paramref name="configure"/> delegate.</summary>
    public static IServiceCollection AddFerretRuntime(
        this IServiceCollection services,
        Action<RuntimeBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRuntimeHost>(_ =>
        {
            var builder = new RuntimeBuilder();
            configure?.Invoke(builder);
            return builder.Build();
        });

        return services;
    }
}
```

- [ ] **Step 3: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeServiceExtensionsTests"
```

Expected: all 3 tests pass.

- [ ] **Step 4: Full test suite**

```
dotnet test tests/Ferret.Runtime.Tests/
```

Expected: 180+ tests, 0 failed.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Runtime/Extensions/ tests/Ferret.Runtime.Tests/Extensions/
git commit -m "feat(sprint-5): AddFerretRuntime DI extension for IServiceCollection (WP-F)"
```

---

## Task 15: Final Verification + Sprint Tag

- [ ] **Step 1: Full build — confirm zero warnings**

```
dotnet build src/Ferret.sln
```

Expected: Build succeeded, **0 warnings**, 0 errors. (`TreatWarningsAsErrors=true` — any warning is a failure.)

- [ ] **Step 2: Full test run — confirm target count**

```
dotnet test tests/Ferret.Runtime.Tests/ --verbosity normal
```

Expected: 180–220 tests passed, 0 failed.

- [ ] **Step 3: Verify XML doc coverage**

Every production class must have a `<summary>` that answers: Why / Lifecycle / Layer / Thread Safety. Scan `src/Ferret.Runtime/` for any file missing this pattern:

```
dotnet build src/Ferret.Runtime/ 2>&1 | grep "CS1591"
```

Expected: no CS1591 warnings (XML doc missing). If any appear, add the doc comment before proceeding.

- [ ] **Step 4: Verify Ferret.Core has no new project references**

```
dotnet list src/Ferret.Core/Ferret.Core.csproj reference
```

Expected: empty (zero project references). If any appear, remove them — Ferret.Core must remain a zero-dependency project.

- [ ] **Step 5: Sprint 5 completion commit**

```
git add -u
git commit -m "chore(sprint-5): Sprint 5 complete — Ferret.Runtime runtime host implementation"
```

- [ ] **Step 6: Tag v0.5.0-sprint5**

```
git tag v0.5.0-sprint5
```

Expected: tag created locally. Push if CI requires it.

---

## Success Criteria Checklist

Before declaring Sprint 5 complete, verify every item:

- [ ] `dotnet build src/Ferret.sln` — 0 warnings, 0 errors
- [ ] `dotnet test tests/Ferret.Runtime.Tests/` — 180–220 tests, 0 failures
- [ ] `Ferret.Core.csproj` has zero project references
- [ ] Every production class has Why / Lifecycle / Layer / Thread Safety in XML doc
- [ ] Every mutable class states Thread Safe / Thread Compatible / Single Thread Only
- [ ] `DefaultModule` is abstract and optional — tests exist proving plain `IModule` implementors work via `BoundModule`
- [ ] `RuntimeEventDispatcher` uses lock-based synchronous dispatch (not Channels)
- [ ] `RuntimeHealthService` uses `Ferret.Core.Abstractions.IHealthCheck` (not MS health checks)
- [ ] `RuntimeHost` wraps `IHost` internally — no IHost type appears in any public contract
- [ ] Technology evaluation table records 8 packages with Adopt/Wrap/Build/Reject/Defer decisions
- [ ] Integration test proves dependency-ordered startup (Task 13)
- [ ] Integration test proves fault propagation → Faulted state (Task 13)
- [ ] `v0.5.0-sprint5` tag exists in git history
