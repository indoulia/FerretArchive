# Sprint 6 — Platform Entry Point & CLI Host

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the first working `ferret` executable that starts the runtime, hosts at least one built-in module through its full lifecycle, and shuts down cleanly.

**Architecture:** The `Ferret.Cli` project is the executable composition root. It wires System.CommandLine subcommands to the `IRuntimeBuilder` / `IRuntimeHost` contracts from Sprint 5, using the `AddFerretRuntime` DI extension (Task 14, Sprint 5). Configuration is loaded from `ferret.json` + environment variables via `Microsoft.Extensions.Configuration`. Logging goes to the console via `Microsoft.Extensions.Logging.Console`. A single built-in `DiagnosticsModule` proves the module hosting pipeline is wired correctly.

**Tech Stack:** System.CommandLine 2.x (beta), Microsoft.Extensions.Hosting 9.0, Microsoft.Extensions.Configuration.Json 9.0, Microsoft.Extensions.Logging.Console 9.0, xUnit 2.x

> **Sprint 5 prerequisite note:** The user's high-level Sprint 6 objectives include "Implement the Ferret Runtime", "Build the composition root", "Implement module lifecycle management", and "Integrate standard Microsoft infrastructure." These are **delivered by Sprint 5** (AISpace.Runtime → Ferret.Runtime after rebrand). Sprint 6 builds on those contracts to produce the **executable** — the missing piece that makes the runtime runnable end-to-end. The rebranding (AISpace → Ferret) must be complete before Sprint 6 begins.

## Prerequisites

- Sprint 5 tagged `v0.5.0-sprint5` — Ferret.Runtime complete, all tests green
- Rebranding complete — all namespaces, projects, and assemblies are `Ferret.*`; CLI binary name is `ferret`; tagged `v0.5.0-ferret`
- `AddFerretRuntime(IServiceCollection, Action<RuntimeBuilder>?)` DI extension exists in `Ferret.Runtime`

## Global Constraints

- .NET 9, C# 13, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisMode>All</AnalysisMode>`, StyleCop
- Central Package Management — all `PackageVersion` entries in `Directory.Packages.props`; never add `Version=` to `<PackageReference>` in `.csproj`
- `Ferret.Cli` must reference `Ferret.Runtime` and `Ferret.Core` only — no direct `Ferret.Workspace` or `Ferret.Plugins` references
- `Ferret.Cli` is the **only** project with `<OutputType>Exe</OutputType>`
- Every production class: XML doc answers Why / Lifecycle owner / Layer dependency / Thread Safety
- Every mutable class states: Thread Safe / Thread Compatible / Single Thread Only
- 0 warnings = 0 errors before any commit; no `#pragma warning disable` without a cited ticket
- TDD: failing test → confirm red → implement → confirm green → commit
- CLI binary name: `ferret` (set via `<AssemblyName>ferret</AssemblyName>`)
- No `Console.WriteLine` in production code — use `ILogger<T>` everywhere
- `ferret start` must handle `Ctrl+C` (SIGINT) and `SIGTERM` with graceful shutdown

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Directory.Packages.props` | Modify | Add System.CommandLine, Configuration.Json, Logging.Console |
| `src/Ferret.Cli/Ferret.Cli.csproj` | Modify | Add package refs, set AssemblyName=ferret, add InternalsVisibleTo |
| `src/Ferret.Cli/Program.cs` | Rewrite | CLI entry point — builds and invokes root command |
| `src/Ferret.Cli/Commands/RootCommandFactory.cs` | Create | Assembles the command tree |
| `src/Ferret.Cli/Commands/VersionCommand.cs` | Create | `ferret version` subcommand |
| `src/Ferret.Cli/Commands/StartCommand.cs` | Create | `ferret start [--config path]` subcommand |
| `src/Ferret.Cli/Commands/StatusCommand.cs` | Create | `ferret status` subcommand |
| `src/Ferret.Cli/Configuration/FerretConfigLoader.cs` | Create | Loads ferret.json + env var overrides |
| `src/Ferret.Cli/Modules/DiagnosticsModule.cs` | Create | Built-in module — logs startup, version, and health on activation |
| `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj` | Create | xUnit test project for CLI |
| `tests/Ferret.Cli.Tests/Commands/VersionCommandTests.cs` | Create | Unit tests for `ferret version` |
| `tests/Ferret.Cli.Tests/Commands/StartCommandTests.cs` | Create | Integration tests for start/stop lifecycle |
| `tests/Ferret.Cli.Tests/Modules/DiagnosticsModuleTests.cs` | Create | Unit tests for DiagnosticsModule |

---

## Tasks

### Task 1: CLI Project Setup

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`
- Create: `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`
- Create: `src/Ferret.Cli/Properties/AssemblyInfo.cs`

**Interfaces:**
- Produces: `Ferret.Cli` project buildable and referenceable from tests

- [ ] **Step 1: Add packages to Directory.Packages.props**

Add in `Directory.Packages.props` under a new `ItemGroup Label="CLI"`:

```xml
<ItemGroup Label="CLI">
  <PackageVersion Include="System.CommandLine" Version="2.0.0-beta4.22529.1" />
</ItemGroup>

<ItemGroup Label="Microsoft.Extensions">
  <!-- existing Microsoft.Extensions.Hosting 9.0.0 line stays -->
  <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
  <PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="9.0.0" />
</ItemGroup>
```

Note: Verify the exact System.CommandLine pre-release version available on NuGet for .NET 9 at implementation time. The 2.0.0-beta4 series is the current Microsoft-maintained CLI framework for .NET. If a stable 2.x release is available, prefer it.

- [ ] **Step 2: Update Ferret.Cli.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>ferret</AssemblyName>
    <RootNamespace>Ferret.Cli</RootNamespace>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" />
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Runtime\Ferret.Runtime.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Add InternalsVisibleTo**

Create `src/Ferret.Cli/Properties/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.Cli.Tests")]
```

- [ ] **Step 4: Create test project**

Create `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Ferret.Cli.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <ProjectReference Include="..\..\src\Ferret.Cli\Ferret.Cli.csproj" />
  </ItemGroup>

</Project>
```

Add the test project to `Ferret.sln`.

- [ ] **Step 5: Build and verify**

```
dotnet build src/Ferret.Cli/Ferret.Cli.csproj
dotnet build tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj
```

Expected: Both build with 0 warnings, 0 errors.

- [ ] **Step 6: Commit**

```
git add Directory.Packages.props src/Ferret.Cli/ tests/Ferret.Cli.Tests/ Ferret.sln
git commit -m "feat(sprint-6): CLI project setup — System.CommandLine, test project, InternalsVisibleTo (Task 1)"
```

---

### Task 2: ferret version command

**Files:**
- Rewrite: `src/Ferret.Cli/Program.cs`
- Create: `src/Ferret.Cli/Commands/RootCommandFactory.cs`
- Create: `src/Ferret.Cli/Commands/VersionCommand.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/VersionCommandTests.cs`

**Interfaces:**
- Consumes: `System.CommandLine.RootCommand`, `System.CommandLine.Command`
- Produces: `RootCommandFactory.Build()` → `RootCommand`; `ferret version` exits 0 and prints `"ferret 0.6.0"`

**Thread Safety:** Single Thread Only (CLI entry point; no shared mutable state)

- [ ] **Step 1: Write failing test**

```csharp
// tests/Ferret.Cli.Tests/Commands/VersionCommandTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class VersionCommandTests
{
    [Fact]
    public async Task Version_PrintsVersionAndExitsZero()
    {
        var console = new TestConsole();
        var root = RootCommandFactory.Build(console);
        var exitCode = await root.InvokeAsync(["version"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("0.6.0", console.Out.ToString());
    }
}
```

Note: `TestConsole` is from `System.CommandLine.IO`. Adjust import if API differs.

- [ ] **Step 2: Run test — confirm red**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "VersionCommandTests" --no-build 2>&1 | tail -5
```

Expected: FAIL — `RootCommandFactory` does not exist.

- [ ] **Step 3: Implement VersionCommand**

```csharp
// src/Ferret.Cli/Commands/VersionCommand.cs
using System.CommandLine;
using System.CommandLine.IO;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: Surfaces the Ferret platform version without starting the runtime.
/// Lifecycle: Disposable only — no long-lived state.
/// Layer: Ferret.Cli only (presentation layer).
/// Thread Safety: Single Thread Only — CLI commands are invoked sequentially.
/// </summary>
internal static class VersionCommand
{
    internal const string FerretVersion = "0.6.0";

    internal static Command Build()
    {
        var cmd = new Command("version", "Print the Ferret platform version.");
        cmd.SetHandler(ctx =>
        {
            ctx.Console.Out.WriteLine($"ferret {FerretVersion}");
            ctx.ExitCode = 0;
        });
        return cmd;
    }
}
```

- [ ] **Step 4: Implement RootCommandFactory**

```csharp
// src/Ferret.Cli/Commands/RootCommandFactory.cs
using System.CommandLine;
using System.CommandLine.IO;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: Single assembly point for the ferret command tree; keeps Program.cs minimal.
/// Lifecycle: Called once at startup; discarded after InvokeAsync returns.
/// Layer: Ferret.Cli only (presentation layer).
/// Thread Safety: Single Thread Only.
/// </summary>
internal static class RootCommandFactory
{
    internal static RootCommand Build(IConsole? console = null)
    {
        var root = new RootCommand("Ferret — Dig Deep. Deliver Context.")
        {
            VersionCommand.Build(),
            // StartCommand and StatusCommand added in later tasks
        };
        return root;
    }
}
```

- [ ] **Step 5: Rewrite Program.cs**

```csharp
// src/Ferret.Cli/Program.cs
using Ferret.Cli.Commands;

return await RootCommandFactory.Build().InvokeAsync(args);
```

- [ ] **Step 6: Run test — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "VersionCommandTests"
```

Expected: PASS — 1 test.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/
git commit -m "feat(sprint-6): ferret version command with CLI root command tree (Task 2)"
```

---

### Task 3: DiagnosticsModule

**Files:**
- Create: `src/Ferret.Cli/Modules/DiagnosticsModule.cs`
- Create: `tests/Ferret.Cli.Tests/Modules/DiagnosticsModuleTests.cs`

**Interfaces:**
- Consumes: `Ferret.Runtime.Modules.DefaultModule`, `Ferret.Core.Modules.IModuleContext`, `Microsoft.Extensions.Logging.ILogger<DiagnosticsModule>`
- Produces: `DiagnosticsModule` — a `DefaultModule` subclass registered as the first built-in module

**Thread Safety:** Thread Compatible (immutable state; logger is thread-safe)

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Modules/DiagnosticsModuleTests.cs
using Ferret.Core.Modules;
using Ferret.Core.Primitives;
using Ferret.Cli.Modules;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Modules;

public sealed class DiagnosticsModuleTests
{
    private static DiagnosticsModule CreateModule() =>
        new(NullLogger<DiagnosticsModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        var module = CreateModule();
        Assert.Equal("ferret.diagnostics", module.Metadata.Id);
    }

    [Fact]
    public void Metadata_HasExpectedName()
    {
        var module = CreateModule();
        Assert.Equal("Ferret Diagnostics", module.Metadata.Name);
    }

    [Fact]
    public async Task OnStartingAsync_CompletesWithoutThrowing()
    {
        var module = CreateModule();
        // OnStartingAsync should complete without throwing
        await module.OnStartingAsync(CancellationToken.None);
    }

    [Fact]
    public async Task OnStartedAsync_CompletesWithoutThrowing()
    {
        var module = CreateModule();
        await module.OnStartedAsync(CancellationToken.None);
    }
}
```

- [ ] **Step 2: Run tests — confirm red**

Expected: FAIL — `DiagnosticsModule` does not exist.

- [ ] **Step 3: Implement DiagnosticsModule**

```csharp
// src/Ferret.Cli/Modules/DiagnosticsModule.cs
using Ferret.Core.Modules;
using Ferret.Core.Primitives;
using Ferret.Runtime.Modules;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Modules;

/// <summary>
/// Why: The first built-in module; proves the module hosting pipeline is wired correctly.
///      Logs platform version and startup confirmation so operators can verify the runtime
///      started cleanly without inspecting internal state.
/// Lifecycle: Registered at composition root; managed by ModuleLifecycleService.
/// Layer: Ferret.Cli — depends on Ferret.Runtime and Ferret.Core.
/// Thread Safety: Thread Compatible — all state is immutable after construction.
/// </summary>
internal sealed class DiagnosticsModule : DefaultModule
{
    private readonly ILogger<DiagnosticsModule> _logger;

    internal DiagnosticsModule(ILogger<DiagnosticsModule> logger)
    {
        _logger = logger;
        Metadata = ModuleMetadata.Create(
            id: "ferret.diagnostics",
            name: "Ferret Diagnostics",
            version: SemanticVersion.Create(0, 6, 0),
            capabilities: [],
            description: "Built-in diagnostics module — verifies platform startup.",
            author: "Ferret Platform");
    }

    /// <inheritdoc />
    public override ModuleMetadata Metadata { get; }

    /// <inheritdoc />
    public override Task OnStartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ferret Diagnostics starting (v{Version})", Metadata.Version);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnStartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ferret Diagnostics active — runtime is healthy.");
        return Task.CompletedTask;
    }
}
```

Note: Verify `ModuleMetadata.Create` signature and `DefaultModule.Metadata` override pattern against the Sprint 5 implementation before writing. The Sprint 5 implementer found the actual signature is `Create(id, name, version, IReadOnlyCollection<ModuleCapability>, description, author)` — use exactly that.

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "DiagnosticsModuleTests"
```

Expected: PASS — 4 tests.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Modules/ tests/Ferret.Cli.Tests/Modules/
git commit -m "feat(sprint-6): DiagnosticsModule — first built-in module (Task 3)"
```

---

### Task 4: ferret start command

**Files:**
- Create: `src/Ferret.Cli/Configuration/FerretConfigLoader.cs`
- Create: `src/Ferret.Cli/Commands/StartCommand.cs`
- Modify: `src/Ferret.Cli/Commands/RootCommandFactory.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/StartCommandTests.cs`

**Interfaces:**
- Consumes: `IRuntimeBuilder`, `IRuntimeHost`, `AddFerretRuntime(IServiceCollection, Action<RuntimeBuilder>?)`, `IHost` (via Microsoft.Extensions.Hosting — **internal to RuntimeBuilder**, not exposed)
- Produces: `StartCommand.Build(IServiceProvider)` → `Command`; on `ferret start`, builds host with `DiagnosticsModule`, starts it, blocks until cancellation, stops cleanly

**Thread Safety:** Single Thread Only (CLI command; no shared mutable state)

- [ ] **Step 1: Write failing integration test**

```csharp
// tests/Ferret.Cli.Tests/Commands/StartCommandTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Tests.Commands;

public sealed class StartCommandTests
{
    [Fact]
    public async Task Start_WithCancellation_StartsAndStopsCleanly()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var console = new TestConsole();
        var root = RootCommandFactory.Build(console);

        // Cancel immediately to prevent blocking the test
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        var exitCode = await root.InvokeAsync(["start", "--config", "nonexistent.json"], console);

        // Exit code 0 = clean shutdown; non-zero only on startup failure
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Start_MissingConfig_UsesDefaults()
    {
        // ferret start without a config file should still start with defaults
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var root = RootCommandFactory.Build();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        var exitCode = await root.InvokeAsync(["start"]);
        Assert.Equal(0, exitCode);
    }
}
```

Note: These are integration tests that actually start the runtime. Use a short cancellation timeout. The host must handle `OperationCanceledException` and exit cleanly.

- [ ] **Step 2: Run tests — confirm red**

Expected: FAIL — `StartCommand` does not exist.

- [ ] **Step 3: Implement FerretConfigLoader**

```csharp
// src/Ferret.Cli/Configuration/FerretConfigLoader.cs
using Microsoft.Extensions.Configuration;

namespace Ferret.Cli.Configuration;

/// <summary>
/// Why: Centralises config loading so StartCommand stays focused on wiring.
///      Supports ferret.json as the primary config file; falls back to defaults when absent.
/// Lifecycle: Called once per ferret start invocation; discarded after host is built.
/// Layer: Ferret.Cli — presentation layer.
/// Thread Safety: Single Thread Only — called from the CLI command handler.
/// </summary>
internal static class FerretConfigLoader
{
    internal static IConfiguration Load(string? configPath)
    {
        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables("FERRET_");

        var path = configPath ?? "ferret.json";
        if (File.Exists(path))
        {
            builder.AddJsonFile(path, optional: false, reloadOnChange: false);
        }

        return builder.Build();
    }
}
```

- [ ] **Step 4: Implement StartCommand**

```csharp
// src/Ferret.Cli/Commands/StartCommand.cs
using System.CommandLine;
using Ferret.Cli.Configuration;
using Ferret.Cli.Modules;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: The primary runtime entry point — wires configuration, modules, and the runtime host
///      into a single blocking call that runs until SIGTERM or Ctrl+C.
/// Lifecycle: Built once per invocation; discarded after host stops.
/// Layer: Ferret.Cli — composition root for the runtime host.
/// Thread Safety: Single Thread Only — CLI commands are invoked sequentially.
/// </summary>
internal static class StartCommand
{
    internal static Command Build()
    {
        var configOption = new Option<string?>(
            "--config",
            description: "Path to ferret.json configuration file.");

        var cmd = new Command("start", "Start the Ferret runtime host.")
        {
            configOption,
        };

        cmd.SetHandler(async (configPath, ctx) =>
        {
            var config = FerretConfigLoader.Load(configPath);
            var cancellationToken = ctx.GetCancellationToken();

            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices(services =>
                {
                    services.AddFerretRuntime(builder =>
                    {
                        builder.AddModule(new DiagnosticsModuleDescriptor());
                    });
                })
                .Build();

            // Use AddFerretRuntime DI extension from Ferret.Runtime (Sprint 5, Task 14)
            // The runtime host is managed by ModuleLifecycleService (IHostedService) internally

            try
            {
                await host.RunAsync(cancellationToken);
                ctx.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                ctx.ExitCode = 0;
            }
            catch (Exception ex)
            {
                ctx.Console.Error.WriteLine($"Fatal: {ex.Message}");
                ctx.ExitCode = 1;
            }
        }, configOption, new System.CommandLine.Binding.BinderBase<System.CommandLine.Invocation.InvocationContext>());

        return cmd;
    }
}
```

> **Implementation note:** The exact System.CommandLine handler API for option binding may differ from the snippet above. Verify the correct `SetHandler` overload for the installed version. The key requirement is: load config, build the runtime host, call `RunAsync` with the cancellation token, exit 0 on clean stop.
>
> `AddFerretRuntime` is the DI extension from Sprint 5 Task 14. Verify its exact signature before calling. The extension registers `ModuleLifecycleService` as `IHostedService` internally.
>
> Do **not** reference `IHost`, `IHostedService`, or `RuntimeHost` directly in `StartCommand` — use only `IRuntimeHost` if health or state is needed, accessed via `host.Services.GetRequiredService<IRuntimeHost>()`.

- [ ] **Step 5: Register StartCommand in RootCommandFactory**

```csharp
// In RootCommandFactory.Build():
internal static RootCommand Build(IConsole? console = null)
{
    var root = new RootCommand("Ferret — Dig Deep. Deliver Context.")
    {
        VersionCommand.Build(),
        StartCommand.Build(),
    };
    return root;
}
```

- [ ] **Step 6: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "StartCommandTests"
```

Expected: PASS — 2 tests.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/
git commit -m "feat(sprint-6): ferret start command — composition root wires runtime host with DiagnosticsModule (Task 4)"
```

---

### Task 5: ferret status command

**Files:**
- Create: `src/Ferret.Cli/Commands/StatusCommand.cs`
- Modify: `src/Ferret.Cli/Commands/RootCommandFactory.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/StatusCommandTests.cs`

**Interfaces:**
- Consumes: `IRuntimeHost.State` (RuntimeState enum from Ferret.Core), `IRuntimeHost.GetHealthReportAsync` (from Sprint 5 Task 12 — RuntimeHealthService)
- Produces: `ferret status` exits 0 and prints current runtime state + module health summary

> **Note:** `ferret status` requires a running runtime to query. In this sprint it reads a state file or pipe — OR the simpler approach: since there's no IPC between processes yet, `ferret status` in Sprint 6 reports "Ferret is not running (no runtime process found)" and exits 1 when no runtime is active. Full IPC/health query is Sprint 7 scope. See Global Constraints: no features beyond what's needed.

**Thread Safety:** Single Thread Only

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/StatusCommandTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class StatusCommandTests
{
    [Fact]
    public async Task Status_WhenNoRuntimeRunning_ReportsNotRunningAndExitsOne()
    {
        var console = new TestConsole();
        var root = RootCommandFactory.Build(console);
        var exitCode = await root.InvokeAsync(["status"]);
        Assert.Equal(1, exitCode);
        Assert.Contains("not running", console.Out.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run test — confirm red**

Expected: FAIL — `StatusCommand` does not exist.

- [ ] **Step 3: Implement StatusCommand**

```csharp
// src/Ferret.Cli/Commands/StatusCommand.cs
using System.CommandLine;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: Provides operator visibility into runtime state without attaching to a running process.
///      In Sprint 6, reports "not running" (no IPC yet); full live-status query is Sprint 7.
/// Lifecycle: Invoked once per CLI call; no long-lived state.
/// Layer: Ferret.Cli — presentation layer.
/// Thread Safety: Single Thread Only.
/// </summary>
internal static class StatusCommand
{
    internal static Command Build()
    {
        var cmd = new Command("status", "Report the current Ferret runtime status.");
        cmd.SetHandler(ctx =>
        {
            // Sprint 6: no IPC between processes — report not running.
            // Sprint 7 will add a Unix domain socket / named pipe health endpoint.
            ctx.Console.Out.WriteLine("Ferret is not running (start with: ferret start)");
            ctx.ExitCode = 1;
        });
        return cmd;
    }
}
```

- [ ] **Step 4: Register StatusCommand in RootCommandFactory**

Add `StatusCommand.Build()` to the root command in `RootCommandFactory.Build()`.

- [ ] **Step 5: Run test — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "StatusCommandTests"
```

Expected: PASS — 1 test.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/
git commit -m "feat(sprint-6): ferret status command — reports not running in Sprint 6; IPC deferred to Sprint 7 (Task 5)"
```

---

### Task 6: End-to-End Integration Tests

**Files:**
- Create: `tests/Ferret.Cli.Tests/Integration/RuntimeHostingIntegrationTests.cs`

**Interfaces:**
- Consumes: All of `Ferret.Runtime` via `AddFerretRuntime`, `DiagnosticsModule`, `IRuntimeHost`, `IRuntimeEventDispatcher`
- Produces: Green integration tests proving: DiagnosticsModule activates, `ModuleActivated` event fires, graceful stop runs `OnStopped`

> These tests run the real runtime host in-process with a short cancellation timeout. They are the primary proof that the full Sprint 6 composition works end-to-end.

- [ ] **Step 1: Write failing integration tests**

```csharp
// tests/Ferret.Cli.Tests/Integration/RuntimeHostingIntegrationTests.cs
using Ferret.Cli.Modules;
using Ferret.Core.Runtime.Events;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ferret.Cli.Tests.Integration;

public sealed class RuntimeHostingIntegrationTests
{
    [Fact]
    public async Task Start_DiagnosticsModule_ActivatesSuccessfully()
    {
        var activatedModuleIds = new List<string>();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddFerretRuntime(builder =>
                {
                    builder.AddModule(new DiagnosticsModule(
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<DiagnosticsModule>.Instance));
                });
            })
            .Build();

        var dispatcher = host.Services.GetRequiredService<IRuntimeEventDispatcher>();
        using var _ = dispatcher.Subscribe<ModuleActivated>(evt =>
        {
            activatedModuleIds.Add(evt.ModuleId);
            return Task.CompletedTask;
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        await host.StopAsync(cts.Token);

        Assert.Contains("ferret.diagnostics", activatedModuleIds);
    }

    [Fact]
    public async Task Stop_DiagnosticsModule_StopsCleanly()
    {
        var stoppedModuleIds = new List<string>();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddFerretRuntime(builder =>
                {
                    builder.AddModule(new DiagnosticsModule(
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<DiagnosticsModule>.Instance));
                });
            })
            .Build();

        var dispatcher = host.Services.GetRequiredService<IRuntimeEventDispatcher>();
        using var _ = dispatcher.Subscribe<ModuleStopped>(evt =>
        {
            stoppedModuleIds.Add(evt.ModuleId);
            return Task.CompletedTask;
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        await host.StopAsync(cts.Token);

        Assert.Contains("ferret.diagnostics", stoppedModuleIds);
    }
}
```

Note: Verify `IRuntimeEventDispatcher` is registered in DI by `AddFerretRuntime`. If it is not, register it explicitly in the test setup. Verify `ModuleActivated` constructor signature against Sprint 5 implementation.

- [ ] **Step 2: Run tests — confirm red**

Expected: FAIL — compilation errors until all Sprint 6 types exist; or test fails because `AddFerretRuntime` is not yet wired to publish events.

- [ ] **Step 3: Fix any wiring gaps discovered**

If `AddFerretRuntime` does not register `IRuntimeEventDispatcher` in the DI container, add the registration. Do not change architecture — file an ADR recommendation if this requires a structural change not covered by the Sprint 5 plan.

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "RuntimeHostingIntegrationTests"
```

Expected: PASS — 2 tests.

- [ ] **Step 5: Full test suite**

```
dotnet test
```

Expected: All tests pass, 0 failures.

- [ ] **Step 6: Commit**

```
git add tests/Ferret.Cli.Tests/Integration/
git commit -m "test(sprint-6): end-to-end integration tests — DiagnosticsModule activates and stops cleanly (Task 6)"
```

---

### Task 7: Final Verification + Sprint Tag

**Files:**
- Modify: `docs/superpowers/plans/2026-06-28-sprint-6-cli-host.md` (update task checkboxes only)

**Interfaces:** None — verification only.

**Deliverable checklist:**
- [ ] `dotnet build` — 0 warnings, 0 errors across entire solution
- [ ] `dotnet test` — all tests green; new test count ≥ baseline + 9 (4 DiagnosticsModule + 2 Start + 1 Status + 2 Integration)
- [ ] `ferret --version` or `ferret version` prints "ferret 0.6.0" and exits 0
- [ ] `ferret status` prints "not running" and exits 1
- [ ] `ferret start` starts the runtime, DiagnosticsModule logs appear, Ctrl+C stops cleanly with exit 0
- [ ] `Ferret.Cli` project has 0 direct references to `IHost`, `IHostedService`, `RuntimeHost` (grep and confirm)
- [ ] XML doc scan: every `public` and `internal` class in `src/Ferret.Cli/` has Why/Lifecycle/Layer/Thread Safety
- [ ] No `Console.WriteLine` in `src/Ferret.Cli/` production code (grep and confirm)

- [ ] **Step 1: Build entire solution**

```
dotnet build
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 2: Run all tests**

```
dotnet test --logger "console;verbosity=minimal"
```

Expected: All pass.

- [ ] **Step 3: Smoke test the binary**

```
dotnet run --project src/Ferret.Cli -- version
dotnet run --project src/Ferret.Cli -- status
```

Expected: "ferret 0.6.0" then exit 0; "Ferret is not running" then exit 1.

- [ ] **Step 4: Tag and commit**

```
git tag v0.6.0-sprint6
git commit --allow-empty -m "chore(sprint-6): mark sprint complete — Runtime Entry Point & CLI Host"
```

---

## Review Gates

| Gate | Criterion |
|------|-----------|
| Build | 0 warnings, 0 errors |
| Tests | All pass; ≥ 9 new tests |
| CLI smoke | `ferret version` exits 0; `ferret start` starts and stops cleanly |
| Architecture | `Ferret.Cli` does not expose `IHost` / `IHostedService` in any public or internal API |
| Docs | XML docs complete on all production classes |

## Alignment with Sprint 6 Objectives

| Objective | Status |
|-----------|--------|
| Implement the Ferret Runtime | **Sprint 5 prerequisite** — complete before Sprint 6 begins |
| Build the composition root (startup/shutdown) | **Sprint 5 prerequisite** — RuntimeBuilder/RuntimeHost |
| Module lifecycle management | **Sprint 5 prerequisite** — LifecycleOrchestrator, ModuleLifecycleService |
| Integrate Microsoft infrastructure (DI, Hosting, Logging, Health) | **Sprint 5 prerequisite** — AddFerretRuntime DI extension |
| **Create the first working executable** | **Sprint 6 delivers** — Tasks 2-6 |
| Extensible for Workspace, Plugin, ContextOS modules | **Sprint 5 prerequisite** — IModule/IModuleDescriptor contracts + Sprint 6 proves the plugin surface with DiagnosticsModule |

## Out of Scope (Sprint 6)

- Live `ferret status` querying a running process (IPC — Sprint 7)
- `ferret workspace` / `ferret index` commands (Sprint 8+)
- Plugin loading from disk (ARCH-011 — Sprint 9+)
- ContextOS module hosting (Sprint 10+)
- HTTP health endpoint (Sprint 7)
- Telemetry / OpenTelemetry integration (Sprint 7)
