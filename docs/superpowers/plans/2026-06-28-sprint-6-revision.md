# Sprint 6 — First User Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the first polished Ferret executable with a CLI architecture that survives to Sprint 12 — `ferret version`, `ferret about`, `ferret start`, `ferret doctor`, 13 reserved command groups, and a module extensibility system (`ICliModule`) ready for Sprint 7 plugins.

**Architecture:** Commands are `ICommandHandler` implementations resolved via DI — no delegate lambdas. `ICliModule` is the extensibility contract: each module contributes commands, diagnostic checks, and service registrations. `RootCommandFactory` discovers all modules, builds a ServiceCollection from their registrations, and constructs the System.CommandLine command tree. `IFerretContext` (per-invocation: CancellationToken, Verbosity, OutputFormat, Services) and `IFerretServices` (platform services: Output, Configuration, LoggerFactory, Runtime?, Workspace?) decouple command logic from the CLI framework. System.CommandLine types are confined to `RootCommandFactory` and `ConsoleFormatter`. Empty command groups are real `Command` objects that show Sprint roadmap info — like Git/Docker/kubectl.

**Tech Stack:** System.CommandLine 2.x, Microsoft.Extensions.DependencyInjection 9.0.0, Microsoft.Extensions.Logging.Console 9.0.0, Microsoft.Extensions.Configuration.Json 9.0.0, xUnit 2.x, .NET 9

## Global Constraints

- .NET 9, C# 13, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisMode>All</AnalysisMode>`, StyleCop
- Central Package Management — all `PackageVersion` entries in `Directory.Packages.props`; never `Version=` in `<PackageReference>` in `.csproj`
- `Ferret.Cli` references `Ferret.Runtime` and `Ferret.Core` only
- `Ferret.Cli` is the **only** project with `<OutputType>Exe</OutputType>`
- Every production class: XML doc with Why / Lifecycle / Layer / Thread Safety
- 0 warnings, 0 errors before any commit; no `#pragma warning disable` without a cited ticket
- TDD: failing test → confirm red → implement → confirm green → commit
- CLI binary name: `ferret` (`<AssemblyName>ferret</AssemblyName>`)
- **No `Console.WriteLine` in production code** — use `IOutputFormatter` for CLI output, `ILogger<T>` for diagnostics
- **No ANSI escape codes, no color, no rich formatting** — `ConsoleFormatter` writes plain text with `✓`/`✗` markers only
- System.CommandLine types (`InvocationContext`, `IConsole`, `Option<T>`, `Command`, etc.) must not appear outside `RootCommandFactory.cs`, `GlobalOptions.cs`, `ConsoleFormatter.cs`, and `FerretContext.cs` (From() method only)
- Version string is NEVER hardcoded — always read from `FerretPlatform.Version` (assembly attribute)
- `<Version>0.6.0</Version>` in `Ferret.Cli.csproj` is the single source of truth
- Sprint tag: `v0.6.0-sprint6`

---

## Critical Correctness Notes

1. **`DefaultModule` constructor:** `protected DefaultModule(ModuleMetadata metadata)` — pass metadata to `base(...)`, do NOT override the `Metadata` property.
2. **Lifecycle signatures:** `Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default)` — all four lifecycle methods take `IModuleContext` first.
3. **`RuntimeBuilder.AddModule` takes `IModuleDescriptor`** — `DefaultModule` implements it.
4. **`AddFerretRuntime` does NOT start the runtime** — `StartCommandHandler` must call `StartAsync`/`StopAsync` explicitly.
5. **`RuntimeEventDispatcher` and `RuntimeHealthService` are `internal`** — not accessible from `Ferret.Cli`.
6. **`RuntimeHost` implements `IAsyncDisposable`** but `IRuntimeHost` does not — dispose via: `if (host is IAsyncDisposable d) await d.DisposeAsync();`
7. **`ModuleMetadata.Create` capabilities is `IEnumerable<ModuleCapability>`** — pass `[]` for no capabilities.
8. **System.CommandLine version check** — Before implementing Task 1, run: `dotnet package search "System.CommandLine" --prerelease false`. If stable 2.x exists, use it. Otherwise use `2.0.0-beta4.22529.1`.

---

## Architecture

```
Ferret.Cli
│
├── RootCommandFactory          ← ONLY file using SC types (+ GlobalOptions, ConsoleFormatter, FerretContext.From)
│                                 Discovers ICliModule instances, builds DI, constructs command tree
├── ICliModule / CliModuleBase  ← Module extensibility: commands + checks + service registrations
│      └── CoreCliModule        ← Sprint 6: version, about, start, doctor, status + 13 group stubs
│                                 Sprint 7+: WorkspaceCliModule, GitCliModule, JiraCliModule, ...
│
├── ICommandHandler             ← Command execution; resolved from DI (constructor injection enabled)
│      └── [VersionCommandHandler, AboutCommandHandler, StartCommandHandler, DoctorCommandHandler, StatusCommandHandler]
│
├── IFerretContext              ← Per-invocation: CancellationToken, Verbosity, OutputFormat, Services, GetOption<T>
├── IFerretServices             ← Platform services: Output, Config, LoggerFactory, Runtime?, Workspace?
│
├── IOutputFormatter            ← Plain text abstraction; no ANSI; Sprint 7 adds JsonFormatter
│      └── ConsoleFormatter     ← ✓/✗ markers, verbose gating
│
└── IDiagnosticCheck            ← Doctor extensibility; registered by ICliModule.GetDiagnosticChecks()
       └── DiagnosticRunner     ← Runs checks sequentially, reports via IOutputFormatter
```

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Directory.Packages.props` | Modify | Add System.CommandLine, DI, Config, Logging packages |
| `src/Ferret.Cli/Ferret.Cli.csproj` | Modify | AssemblyName=ferret, Version=0.6.0, package refs |
| `src/Ferret.Cli/Properties/AssemblyInfo.cs` | Create | InternalsVisibleTo test project |
| `src/Ferret.Cli/Infrastructure/FerretPlatform.cs` | Create | Version + RuntimeInfo from assembly / RuntimeInformation |
| `src/Ferret.Cli/Cli/CommandResult.cs` | Create | Typed exit code enum |
| `src/Ferret.Cli/Cli/VerbosityLevel.cs` | Create | Quiet/Normal/Verbose enum |
| `src/Ferret.Cli/Cli/OutputFormat.cs` | Create | Text/Json enum (Json reserved Sprint 7) |
| `src/Ferret.Cli/Cli/GlobalOptions.cs` | Create | Reserved global options (--verbose, --quiet, --json, --no-color); all hidden Sprint 6 |
| `src/Ferret.Cli/Cli/IOutputFormatter.cs` | Create | Plain text output abstraction |
| `src/Ferret.Cli/Cli/ConsoleFormatter.cs` | Create | IOutputFormatter over IConsole; ✓/✗, verbose gating, no ANSI |
| `src/Ferret.Cli/Cli/IFerretServices.cs` | Create | Platform service bag interface |
| `src/Ferret.Cli/Cli/FerretServices.cs` | Create | IFerretServices implementation |
| `src/Ferret.Cli/Cli/IFerretContext.cs` | Create | Per-invocation context interface |
| `src/Ferret.Cli/Cli/FerretContext.cs` | Create | IFerretContext impl; From() (SC types); CreateTest() (no SC) |
| `src/Ferret.Cli/Cli/CommandMetadata.cs` | Create | Name, Description, Category, Hidden, Experimental, Aliases, Examples |
| `src/Ferret.Cli/Cli/OptionDefinition.cs` | Create | Per-command option descriptor (no SC types) |
| `src/Ferret.Cli/Cli/CommandDefinition.cs` | Create | Metadata + HandlerType + Group + PlannedSubcommands; EmptyGroup factory |
| `src/Ferret.Cli/Cli/ICommandHandler.cs` | Create | `Task<CommandResult> ExecuteAsync(IFerretContext)` |
| `src/Ferret.Cli/Cli/ICliModule.cs` | Create | Module extensibility contract |
| `src/Ferret.Cli/Cli/CliModuleBase.cs` | Create | Default no-op base for ICliModule |
| `src/Ferret.Cli/Diagnostics/IDiagnosticCheck.cs` | Create | Doctor extensibility point |
| `src/Ferret.Cli/Diagnostics/DiagnosticCheckResult.cs` | Create | Pass/fail result record |
| `src/Ferret.Cli/Diagnostics/DiagnosticRunner.cs` | Create | Runs checks, reports via IOutputFormatter |
| `src/Ferret.Cli/Diagnostics/Checks/ConfigurationCheck.cs` | Create | Verifies config loads |
| `src/Ferret.Cli/Diagnostics/Checks/RuntimeLifecycleCheck.cs` | Create | Full start/stop cycle verification |
| `src/Ferret.Cli/Modules/DiagnosticsModule.cs` | Create | Built-in Ferret.Runtime module |
| `src/Ferret.Cli/Configuration/FerretConfigLoader.cs` | Create | ferret.json + FERRET_ env vars |
| `src/Ferret.Cli/Commands/Handlers/VersionCommandHandler.cs` | Create | `ferret version` |
| `src/Ferret.Cli/Commands/Handlers/AboutCommandHandler.cs` | Create | `ferret about` |
| `src/Ferret.Cli/Commands/Handlers/StartCommandHandler.cs` | Create | `ferret start` |
| `src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs` | Create | `ferret doctor` |
| `src/Ferret.Cli/Commands/Handlers/StatusCommandHandler.cs` | Create | `ferret status` |
| `src/Ferret.Cli/Commands/CoreCliModule.cs` | Create | ICliModule: all working commands + 13 group stubs + built-in checks |
| `src/Ferret.Cli/Commands/RootCommandFactory.cs` | Create | SC wiring only; discovers modules, builds DI, builds command tree |
| `src/Ferret.Cli/Program.cs` | Rewrite | `return await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(args);` |
| `src/Ferret.Runtime/Bootstrap/RuntimeBuilder.cs` | Modify | Add `ConfigureLogging(Action<ILoggingBuilder>)` |
| `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj` | Modify | Add project ref + package refs |
| `tests/Ferret.Cli.Tests/CliModuleTests.cs` | Rewrite | Remove placeholder |
| `tests/Ferret.Cli.Tests/Infrastructure/FerretPlatformTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Cli/ConsoleFormatterTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Cli/FerretContextTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Diagnostics/DiagnosticRunnerTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Modules/DiagnosticsModuleTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Commands/VersionCommandHandlerTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Commands/AboutCommandHandlerTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Commands/StartCommandHandlerTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Commands/StatusCommandHandlerTests.cs` | Create | |
| `tests/Ferret.Cli.Tests/Integration/RuntimeLifecycleIntegrationTests.cs` | Create | |
| `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeBuilderLoggingTests.cs` | Create | |

---

## Tasks

### Task 1: CLI Project Setup

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`
- Modify: `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`
- Create: `src/Ferret.Cli/Properties/AssemblyInfo.cs`

**Interfaces:**
- Produces: `Ferret.Cli` buildable as `ferret.exe` with assembly version `0.6.0`

- [ ] **Step 1: Check System.CommandLine stable version**

```
dotnet package search "System.CommandLine" --prerelease false 2>&1 | head -20
```

If stable 2.x exists, use it. Otherwise use `2.0.0-beta4.22529.1`. Record the version.

- [ ] **Step 2: Add packages to Directory.Packages.props**

Read the file first. Add to the existing `Microsoft.Extensions` ItemGroup:
```xml
<PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
<PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="9.0.0" />
```

Add a new CLI ItemGroup:
```xml
<ItemGroup Label="CLI">
  <PackageVersion Include="System.CommandLine" Version="[CHOSEN VERSION]" />
</ItemGroup>
```

- [ ] **Step 3: Rewrite Ferret.Cli.csproj**

Read the existing file first, then replace:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>ferret</AssemblyName>
    <RootNamespace>Ferret.Cli</RootNamespace>
    <OutputType>Exe</OutputType>
    <Version>0.6.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" />
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\Ferret.Runtime\Ferret.Runtime.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create Properties/AssemblyInfo.cs**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.Cli.Tests")]
```

- [ ] **Step 5: Update Ferret.Cli.Tests.csproj**

Read file first. Add to the package `<ItemGroup>`:
```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
<ProjectReference Include="..\..\src\Ferret.Cli\Ferret.Cli.csproj" />
```

- [ ] **Step 6: Build and verify — 0 warnings, 0 errors**

```
dotnet build src/Ferret.Cli/Ferret.Cli.csproj
dotnet build tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj
```

- [ ] **Step 7: Commit**

```
git add Directory.Packages.props src/Ferret.Cli/ tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj .claude/
git commit -m "feat(sprint-6): CLI project setup — AssemblyName=ferret, Version=0.6.0 (Task 1)"
```

---

### Task 2: RuntimeBuilder.ConfigureLogging

**Files:**
- Modify: `src/Ferret.Runtime/Bootstrap/RuntimeBuilder.cs`
- Create: `tests/Ferret.Runtime.Tests/Bootstrap/RuntimeBuilderLoggingTests.cs`

**Interfaces:**
- Produces: `RuntimeBuilder.ConfigureLogging(Action<ILoggingBuilder>)` → `RuntimeBuilder` (fluent)

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Runtime.Tests/Bootstrap/RuntimeBuilderLoggingTests.cs
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging;

namespace Ferret.Runtime.Tests.Bootstrap;

public sealed class RuntimeBuilderLoggingTests
{
    [Fact]
    public void ConfigureLogging_ReturnsSameBuilder()
    {
        var builder = new RuntimeBuilder();
        var returned = builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Debug));
        Assert.Same(builder, returned);
    }

    [Fact]
    public async Task Build_WithConfigureLogging_StartsAndStopsWithoutError()
    {
        IRuntimeHost host = new RuntimeBuilder()
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning))
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        await host.StopAsync(cts.Token);
        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }
}
```

- [ ] **Step 2: Run tests — confirm red**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeBuilderLoggingTests" 2>&1 | tail -5
```

Expected: FAIL — `ConfigureLogging` does not exist.

- [ ] **Step 3: Add ConfigureLogging to RuntimeBuilder**

Read `src/Ferret.Runtime/Bootstrap/RuntimeBuilder.cs` first.

Add `using Microsoft.Extensions.Logging;` to usings.

Add private field:
```csharp
private Action<ILoggingBuilder>? _loggingConfigure;
```

Add method after existing public methods:
```csharp
/// <summary>Configures the logging pipeline for the internal runtime host.</summary>
/// <param name="configure">Delegate that configures the <see cref="ILoggingBuilder"/>.</param>
/// <returns>The same builder instance, to allow call chaining.</returns>
public RuntimeBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
{
    _loggingConfigure = configure ?? throw new ArgumentNullException(nameof(configure));
    return this;
}
```

In `Build()`, find the `HostBuilder` construction and add `.ConfigureLogging(...)` before `.ConfigureServices(...)`:
```csharp
.ConfigureLogging(logging => { _loggingConfigure?.Invoke(logging); })
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Runtime.Tests/ --filter "RuntimeBuilderLoggingTests"
dotnet test tests/Ferret.Runtime.Tests/
```

Expected: All pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Runtime/Bootstrap/RuntimeBuilder.cs tests/Ferret.Runtime.Tests/ .claude/
git commit -m "feat(sprint-6): RuntimeBuilder.ConfigureLogging — additive logging hook (Task 2)"
```

---

### Task 3: CLI Foundation

**Files:** (create all in `src/Ferret.Cli/Cli/` and `src/Ferret.Cli/Infrastructure/`)
- `FerretPlatform.cs`, `CommandResult.cs`, `VerbosityLevel.cs`, `OutputFormat.cs`, `GlobalOptions.cs`
- `IOutputFormatter.cs`, `ConsoleFormatter.cs`
- `IFerretServices.cs`, `FerretServices.cs`, `IFerretContext.cs`, `FerretContext.cs`
- `CommandMetadata.cs`, `OptionDefinition.cs`, `CommandDefinition.cs`, `ICommandHandler.cs`
- `ICliModule.cs`, `CliModuleBase.cs`

**Test files:** `FerretPlatformTests.cs`, `ConsoleFormatterTests.cs`, `FerretContextTests.cs`

**Interfaces:**
- Produces: All foundational abstractions used by every later task.

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Infrastructure/FerretPlatformTests.cs
using Ferret.Cli.Infrastructure;
namespace Ferret.Cli.Tests.Infrastructure;

public sealed class FerretPlatformTests
{
    [Fact] public void Version_IsNotEmpty() => Assert.False(string.IsNullOrWhiteSpace(FerretPlatform.Version));
    [Fact] public void Version_MatchesSemVer() => Assert.Matches(@"^\d+\.\d+\.\d+", FerretPlatform.Version);
    [Fact] public void RuntimeInfo_ContainsDotNet() => Assert.Contains(".NET", FerretPlatform.RuntimeInfo, StringComparison.OrdinalIgnoreCase);
}
```

```csharp
// tests/Ferret.Cli.Tests/Cli/ConsoleFormatterTests.cs
using System.CommandLine;
using Ferret.Cli.Cli;
namespace Ferret.Cli.Tests.Cli;

public sealed class ConsoleFormatterTests
{
    [Fact]
    public void WriteSuccess_PrependsTick()
    {
        var c = new TestConsole();
        new ConsoleFormatter(c).WriteSuccess("All good");
        Assert.Contains("✓ All good", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public void WriteError_PrependsX()
    {
        var c = new TestConsole();
        new ConsoleFormatter(c).WriteError("Something wrong");
        Assert.Contains("✗ Something wrong", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public void WriteVerbose_WhenNormal_WritesNothing()
    {
        var c = new TestConsole();
        new ConsoleFormatter(c, VerbosityLevel.Normal).WriteVerbose("secret");
        Assert.Empty((c.Out.ToString() ?? string.Empty).Trim());
    }

    [Fact]
    public void WriteVerbose_WhenVerbose_WritesMessage()
    {
        var c = new TestConsole();
        new ConsoleFormatter(c, VerbosityLevel.Verbose).WriteVerbose("secret");
        Assert.Contains("secret", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public void WriteLine_WritesMessage()
    {
        var c = new TestConsole();
        new ConsoleFormatter(c).WriteLine("hello");
        Assert.Contains("hello", c.Out.ToString() ?? string.Empty);
    }
}
```

```csharp
// tests/Ferret.Cli.Tests/Cli/FerretContextTests.cs
using System.CommandLine;
using Ferret.Cli.Cli;
namespace Ferret.Cli.Tests.Cli;

public sealed class FerretContextTests
{
    [Fact]
    public void CreateTest_ReturnsValidContext()
    {
        var ctx = FerretContext.CreateTest(new TestConsole());
        Assert.NotNull(ctx.Services.Output);
        Assert.Equal(VerbosityLevel.Normal, ctx.Verbosity);
        Assert.Equal(OutputFormat.Text, ctx.OutputFormat);
    }

    [Fact]
    public void CreateTest_VerbosePropagates()
    {
        var ctx = FerretContext.CreateTest(new TestConsole(), VerbosityLevel.Verbose);
        Assert.Equal(VerbosityLevel.Verbose, ctx.Verbosity);
    }

    [Fact]
    public void GetOption_UnknownKey_ReturnsDefault()
    {
        var ctx = FerretContext.CreateTest(new TestConsole());
        Assert.Null(ctx.GetOption<string>("nonexistent"));
    }
}
```

- [ ] **Step 2: Run tests — confirm red**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FerretPlatformTests|ConsoleFormatterTests|FerretContextTests" 2>&1 | tail -5
```

Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement FerretPlatform**

```csharp
// src/Ferret.Cli/Infrastructure/FerretPlatform.cs
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ferret.Cli.Infrastructure;

/// <summary>
/// Why: Single source of truth for CLI version and runtime metadata; prevents version drift between assembly and output.
/// Lifecycle: Static; read once at process start.
/// Layer: Ferret.Cli only.
/// Thread Safety: Thread Safe — read-only after static initialization.
/// </summary>
internal static class FerretPlatform
{
    internal static string Version { get; } =
        typeof(FerretPlatform).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "0.0.0";

    internal static string RuntimeInfo { get; } =
        $".NET {Environment.Version} / {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
}
```

- [ ] **Step 4: Implement enums and CommandResult**

```csharp
// src/Ferret.Cli/Cli/CommandResult.cs
namespace Ferret.Cli.Cli;
/// <summary>Why: Typed command exit; maps to process exit codes. Thread Safety: Thread Safe — value type.</summary>
internal enum CommandResult { Success = 0, Failure = 1, Cancelled = 130 }
```

```csharp
// src/Ferret.Cli/Cli/VerbosityLevel.cs
namespace Ferret.Cli.Cli;
internal enum VerbosityLevel { Quiet, Normal, Verbose }
```

```csharp
// src/Ferret.Cli/Cli/OutputFormat.cs
namespace Ferret.Cli.Cli;
/// <summary>Json reserved for Sprint 7 (--json global option).</summary>
internal enum OutputFormat { Text, Json }
```

- [ ] **Step 5: Implement GlobalOptions**

```csharp
// src/Ferret.Cli/Cli/GlobalOptions.cs
using System.CommandLine;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Centralises all global option definitions; hidden Sprint 6; Sprint 7 wires their values into FerretContext.
/// Layer: Ferret.Cli only — System.CommandLine types confined here.
/// Thread Safety: Thread Safe — read-only after static initialization.
/// </summary>
internal static class GlobalOptions
{
    internal static Option<bool> Verbose { get; } = Hidden(new Option<bool>("--verbose", "Verbose output."));
    internal static Option<bool> Quiet { get; } = Hidden(new Option<bool>("--quiet", "Suppress output."));
    internal static Option<bool> Json { get; } = Hidden(new Option<bool>("--json", "JSON output (Sprint 7)."));
    internal static Option<bool> NoColor { get; } = Hidden(new Option<bool>("--no-color", "Disable color (Sprint 7)."));

    internal static void AddAll(RootCommand root)
    {
        root.AddGlobalOption(Verbose);
        root.AddGlobalOption(Quiet);
        root.AddGlobalOption(Json);
        root.AddGlobalOption(NoColor);
    }

    private static Option<bool> Hidden(Option<bool> opt) { opt.IsHidden = true; return opt; }
}
```

- [ ] **Step 6: Implement IOutputFormatter + ConsoleFormatter**

```csharp
// src/Ferret.Cli/Cli/IOutputFormatter.cs
namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Abstracts output medium; Sprint 7 adds JsonFormatter without touching commands.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface IOutputFormatter
{
    void WriteLine(string text = "");
    void WriteSuccess(string message);   // ✓ prefix
    void WriteError(string message);     // ✗ prefix
    void WriteVerbose(string message);   // no-op unless Verbose
}
```

```csharp
// src/Ferret.Cli/Cli/ConsoleFormatter.cs
using System.CommandLine;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: The only class referencing IConsole directly. Plain text only — no ANSI, no color.
/// Layer: Ferret.Cli only.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class ConsoleFormatter : IOutputFormatter
{
    private const string CheckMark = "✓";
    private const string CrossMark = "✗";

    private readonly IConsole _console;
    private readonly bool _verbose;

    internal ConsoleFormatter(IConsole console, VerbosityLevel verbosity = VerbosityLevel.Normal)
    {
        _console = console;
        _verbose = verbosity == VerbosityLevel.Verbose;
    }

    public void WriteLine(string text = "") => _console.Out.WriteLine(text);
    public void WriteSuccess(string message) => _console.Out.WriteLine($"{CheckMark} {message}");
    public void WriteError(string message) => _console.Out.WriteLine($"{CrossMark} {message}");
    public void WriteVerbose(string message) { if (_verbose) _console.Out.WriteLine(message); }
}
```

- [ ] **Step 7: Implement IFerretServices + FerretServices**

```csharp
// src/Ferret.Cli/Cli/IFerretServices.cs
using Ferret.Core.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Platform service bag — commands access all services through one stable interface.
///      Runtime is nullable Sprint 6 (no daemon); non-null Sprint 7+ when daemon is introduced.
/// Thread Safety: Thread Safe — services are singletons.
/// </summary>
internal interface IFerretServices
{
    IServiceProvider Services { get; }
    IConfiguration Configuration { get; }
    ILoggerFactory LoggerFactory { get; }
    IOutputFormatter Output { get; }
    IRuntimeHost? Runtime { get; }    // null Sprint 6; non-null Sprint 7+ daemon
    // IWorkspace? Workspace { get; }  // Sprint 7
}
```

```csharp
// src/Ferret.Cli/Cli/FerretServices.cs
using Ferret.Core.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Concrete IFerretServices built once by RootCommandFactory from the DI container.
/// Lifecycle: Singleton per CLI invocation.
/// Thread Safety: Thread Safe — all members read-only after construction.
/// </summary>
internal sealed class FerretServices : IFerretServices
{
    internal FerretServices(
        IServiceProvider services,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IOutputFormatter output)
    {
        Services = services;
        Configuration = configuration;
        LoggerFactory = loggerFactory;
        Output = output;
    }

    public IServiceProvider Services { get; }
    public IConfiguration Configuration { get; }
    public ILoggerFactory LoggerFactory { get; }
    public IOutputFormatter Output { get; }
    public IRuntimeHost? Runtime => null; // Sprint 7: resolve from Services
}
```

- [ ] **Step 8: Implement IFerretContext + FerretContext**

```csharp
// src/Ferret.Cli/Cli/IFerretContext.cs
namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Per-invocation context — every ICommandHandler receives exactly this.
///      Adding options, trace IDs, or user identity is a one-property change here.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface IFerretContext
{
    CancellationToken CancellationToken { get; }
    VerbosityLevel Verbosity { get; }
    OutputFormat OutputFormat { get; }
    IFerretServices Services { get; }
    T? GetOption<T>(string name);
}
```

```csharp
// src/Ferret.Cli/Cli/FerretContext.cs
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: IFerretContext implementation. From() builds from SC InvocationContext (SC types stay in this method).
///      CreateTest() builds without SC for unit tests.
/// Layer: Ferret.Cli only.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class FerretContext : IFerretContext
{
    private readonly IReadOnlyDictionary<string, object?> _options;

    private FerretContext(
        CancellationToken cancellationToken,
        VerbosityLevel verbosity,
        OutputFormat outputFormat,
        IFerretServices services,
        IReadOnlyDictionary<string, object?> options)
    {
        CancellationToken = cancellationToken;
        Verbosity = verbosity;
        OutputFormat = outputFormat;
        Services = services;
        _options = options;
    }

    public CancellationToken CancellationToken { get; }
    public VerbosityLevel Verbosity { get; }
    public OutputFormat OutputFormat { get; }
    public IFerretServices Services { get; }

    public T? GetOption<T>(string name) =>
        _options.TryGetValue(name, out var v) && v is T typed ? typed : default;

    /// <summary>Builds from InvocationContext — called only from RootCommandFactory.</summary>
    internal static FerretContext From(
        InvocationContext ctx,
        IFerretServices services,
        IReadOnlyDictionary<string, object?> parsedOptions)
    {
        bool verbose = ctx.ParseResult.GetValueForOption(GlobalOptions.Verbose);
        bool quiet = ctx.ParseResult.GetValueForOption(GlobalOptions.Quiet);
        var verbosity = verbose ? VerbosityLevel.Verbose : quiet ? VerbosityLevel.Quiet : VerbosityLevel.Normal;
        bool jsonFlag = ctx.ParseResult.GetValueForOption(GlobalOptions.Json);
        return new FerretContext(
            ctx.GetCancellationToken(),
            verbosity,
            jsonFlag ? OutputFormat.Json : OutputFormat.Text,
            services,
            parsedOptions);
    }

    /// <summary>Builds without System.CommandLine — for unit tests.</summary>
    internal static FerretContext CreateTest(
        IConsole console,
        VerbosityLevel verbosity = VerbosityLevel.Normal,
        IReadOnlyDictionary<string, object?>? options = null)
    {
        var formatter = new ConsoleFormatter(console, verbosity);
        var services = new FerretServices(
            new EmptyServiceProvider(),
            new ConfigurationBuilder().Build(),
            NullLoggerFactory.Instance,
            formatter);
        return new FerretContext(CancellationToken.None, verbosity, OutputFormat.Text, services,
            options ?? new Dictionary<string, object?>());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
```

- [ ] **Step 9: Implement CommandMetadata, OptionDefinition, CommandDefinition, ICommandHandler**

```csharp
// src/Ferret.Cli/Cli/CommandMetadata.cs
namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Rich command descriptor — Hidden, Experimental, Aliases, Examples reserved for Sprint 7 tooling.
/// Thread Safety: Thread Safe — immutable record.
/// </summary>
internal sealed record CommandMetadata(
    string Name,
    string Description,
    string? Category = null,
    bool Hidden = false,
    bool Experimental = false,
    IReadOnlyList<string>? Aliases = null,
    IReadOnlyList<string>? Examples = null);
```

```csharp
// src/Ferret.Cli/Cli/OptionDefinition.cs
namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Describes a per-command option without System.CommandLine types; RootCommandFactory converts these.
/// Thread Safety: Thread Safe — immutable record.
/// </summary>
internal sealed record OptionDefinition(
    string LongName,
    string Description,
    Type ValueType,
    bool IsHidden = false,
    object? DefaultValue = null);
```

```csharp
// src/Ferret.Cli/Cli/CommandDefinition.cs
namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Pure metadata + HandlerType; DI resolves the handler. No delegate lambdas —
///      enables constructor injection, telemetry, middleware, and decorators in Sprint 7+.
/// Thread Safety: Thread Safe — immutable record.
/// </summary>
internal sealed record CommandDefinition(
    CommandMetadata Metadata,
    Type? HandlerType,
    string? Group = null,
    IReadOnlyList<OptionDefinition>? Options = null,
    IReadOnlyList<string>? PlannedSubcommands = null,
    string? PlannedSprint = null)
{
    internal static CommandDefinition EmptyGroup(
        string name, string description, string plannedSprint, string[] plannedSubcommands) =>
        new(new CommandMetadata(name, description), HandlerType: null,
            PlannedSubcommands: plannedSubcommands, PlannedSprint: plannedSprint);
}
```

```csharp
// src/Ferret.Cli/Cli/ICommandHandler.cs
namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Command execution contract; resolved from DI so commands get constructor injection.
///      Enables telemetry, middleware, and authorization decorators without changing commands.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface ICommandHandler
{
    Task<CommandResult> ExecuteAsync(IFerretContext context);
}
```

- [ ] **Step 10: Implement ICliModule + CliModuleBase**

```csharp
// src/Ferret.Cli/Cli/ICliModule.cs
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Full extensibility contract — one module contributes commands, checks, and service registrations.
///      Sprint 7 WorkspaceCliModule, GitCliModule etc. implement this without changing RootCommandFactory.
/// Thread Safety: Thread Safe — called once during startup.
/// </summary>
internal interface ICliModule
{
    string Name { get; }
    string Description { get; }
    IEnumerable<CommandDefinition> GetCommands();
    IEnumerable<Diagnostics.IDiagnosticCheck> GetDiagnosticChecks();
    void ConfigureServices(IServiceCollection services);
}
```

```csharp
// src/Ferret.Cli/Cli/CliModuleBase.cs
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Cli;

/// <summary>Why: No-op base so concrete modules only override what they contribute.</summary>
internal abstract class CliModuleBase : ICliModule
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual IEnumerable<CommandDefinition> GetCommands() => [];
    public virtual IEnumerable<Diagnostics.IDiagnosticCheck> GetDiagnosticChecks() => [];
    public virtual void ConfigureServices(IServiceCollection services) { }
}
```

- [ ] **Step 11: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FerretPlatformTests|ConsoleFormatterTests|FerretContextTests"
```

Expected: PASS — 11 tests.

- [ ] **Step 12: Commit**

```
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/Infrastructure/ tests/Ferret.Cli.Tests/Cli/ .claude/
git commit -m "feat(sprint-6): CLI foundation — ICliModule, ICommandHandler, IFerretContext, IFerretServices, CommandDefinition, IOutputFormatter (Task 3)"
```

---

### Task 4: IDiagnosticCheck framework

**Files:**
- Create: `src/Ferret.Cli/Diagnostics/IDiagnosticCheck.cs`
- Create: `src/Ferret.Cli/Diagnostics/DiagnosticCheckResult.cs`
- Create: `src/Ferret.Cli/Diagnostics/DiagnosticRunner.cs`
- Create: `src/Ferret.Cli/Diagnostics/Checks/ConfigurationCheck.cs`
- Create: `src/Ferret.Cli/Diagnostics/Checks/RuntimeLifecycleCheck.cs`
- Create: `tests/Ferret.Cli.Tests/Diagnostics/DiagnosticRunnerTests.cs`

**Interfaces:**
- Consumes: `IFerretContext`, `FerretContext.CreateTest`
- Produces: `IDiagnosticCheck`, `DiagnosticRunner.RunAsync`, two built-in checks

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Diagnostics/DiagnosticRunnerTests.cs
using System.CommandLine;
using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
namespace Ferret.Cli.Tests.Diagnostics;

public sealed class DiagnosticRunnerTests
{
    private static IFerretContext Ctx() => FerretContext.CreateTest(new TestConsole());

    [Fact]
    public async Task RunAsync_AllPass_ReturnsTrue() =>
        Assert.True(await DiagnosticRunner.RunAsync([new PassCheck()], Ctx()));

    [Fact]
    public async Task RunAsync_OneFails_ReturnsFalse() =>
        Assert.False(await DiagnosticRunner.RunAsync([new PassCheck(), new FailCheck()], Ctx()));

    [Fact]
    public async Task RunAsync_PassingCheck_PrintsSuccessLine()
    {
        var c = new TestConsole();
        await DiagnosticRunner.RunAsync([new PassCheck()], FerretContext.CreateTest(c));
        Assert.Contains("✓ Always passes", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task RunAsync_FailingCheck_PrintsErrorLine()
    {
        var c = new TestConsole();
        await DiagnosticRunner.RunAsync([new FailCheck()], FerretContext.CreateTest(c));
        Assert.Contains("✗ Always fails", c.Out.ToString() ?? string.Empty);
    }
}

internal sealed class PassCheck : IDiagnosticCheck
{
    public string Name => "Always passes";
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext ctx, CancellationToken ct) =>
        Task.FromResult(DiagnosticCheckResult.Pass());
}

internal sealed class FailCheck : IDiagnosticCheck
{
    public string Name => "Always fails";
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext ctx, CancellationToken ct) =>
        Task.FromResult(DiagnosticCheckResult.Fail("intentional"));
}
```

Note: If StyleCop rejects `internal` helper classes in a test file, move them to a `TestHelpers/` subdirectory.

- [ ] **Step 2: Run tests — confirm red**

Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement**

```csharp
// src/Ferret.Cli/Diagnostics/IDiagnosticCheck.cs
using Ferret.Cli.Cli;
namespace Ferret.Cli.Diagnostics;

/// <summary>
/// Why: ASP.NET Health Check pattern for CLI — modules register checks; doctor discovers and runs them.
///      Sprint 7: WorkspaceCliModule, GitCliModule contribute their own checks automatically.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface IDiagnosticCheck
{
    string Name { get; }
    Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken);
}
```

```csharp
// src/Ferret.Cli/Diagnostics/DiagnosticCheckResult.cs
namespace Ferret.Cli.Diagnostics;
/// <summary>Typed check outcome. Thread Safety: Thread Safe — immutable record.</summary>
internal sealed record DiagnosticCheckResult(bool Passed, string? FailureReason = null)
{
    internal static DiagnosticCheckResult Pass() => new(true);
    internal static DiagnosticCheckResult Fail(string reason) => new(false, reason);
}
```

```csharp
// src/Ferret.Cli/Diagnostics/DiagnosticRunner.cs
using Ferret.Cli.Cli;
namespace Ferret.Cli.Diagnostics;

/// <summary>
/// Why: Runs an ordered check list, reports via IOutputFormatter, returns overall pass/fail.
///      Isolated from DoctorCommandHandler so the check list is injectable in tests.
/// Thread Safety: Single Thread Only.
/// </summary>
internal static class DiagnosticRunner
{
    internal static async Task<bool> RunAsync(IReadOnlyList<IDiagnosticCheck> checks, IFerretContext context)
    {
        bool allPassed = true;
        foreach (IDiagnosticCheck check in checks)
        {
            DiagnosticCheckResult result;
            try
            {
                result = await check.RunAsync(context, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                result = DiagnosticCheckResult.Fail(ex.Message);
            }

            if (result.Passed)
                context.Services.Output.WriteSuccess(check.Name);
            else
            {
                string detail = result.FailureReason is not null ? $": {result.FailureReason}" : string.Empty;
                context.Services.Output.WriteError($"{check.Name}{detail}");
                allPassed = false;
            }
        }
        return allPassed;
    }
}
```

```csharp
// src/Ferret.Cli/Diagnostics/Checks/ConfigurationCheck.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;
namespace Ferret.Cli.Diagnostics.Checks;

internal sealed class ConfigurationCheck : IDiagnosticCheck
{
    private readonly string? _configPath;
    internal ConfigurationCheck(string? configPath = null) { _configPath = configPath; }
    public string Name => "Configuration loaded";
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        try { FerretConfigLoader.Load(_configPath); return Task.FromResult(DiagnosticCheckResult.Pass()); }
        catch (Exception ex) { return Task.FromResult(DiagnosticCheckResult.Fail(ex.Message)); }
    }
}
```

```csharp
// src/Ferret.Cli/Diagnostics/Checks/RuntimeLifecycleCheck.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Modules;
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging.Abstractions;
namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>
/// Why: Proves runtime init + module registry + event dispatcher + health in one check via a full
///      build-start-verify-stop cycle. Bundled because these share the host instance.
/// </summary>
internal sealed class RuntimeLifecycleCheck : IDiagnosticCheck
{
    public string Name => "Runtime lifecycle";

    public async Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        IRuntimeHost host;
        try
        {
            host = new RuntimeBuilder()
                .AddModule(new DiagnosticsModule(NullLogger<DiagnosticsModule>.Instance))
                .Build();
        }
        catch (Exception ex) { return DiagnosticCheckResult.Fail($"Init failed: {ex.Message}"); }

        if (host.Modules.Modules.Count == 0)
            return DiagnosticCheckResult.Fail("Module registry empty after initialization.");

        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return host.State == RuntimeState.Running
                ? DiagnosticCheckResult.Pass()
                : DiagnosticCheckResult.Fail($"State is '{host.State}' instead of Running.");
        }
        catch (Exception ex) { return DiagnosticCheckResult.Fail($"Start failed: {ex.Message}"); }
        finally
        {
            try
            {
                if (host.State == RuntimeState.Running)
                    await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException) { }
            if (host is IAsyncDisposable d) await d.DisposeAsync().ConfigureAwait(false);
        }
    }
}
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "DiagnosticRunnerTests"
```

Expected: PASS — 4 tests.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Diagnostics/ tests/Ferret.Cli.Tests/Diagnostics/ .claude/
git commit -m "feat(sprint-6): IDiagnosticCheck framework — DiagnosticRunner, ConfigurationCheck, RuntimeLifecycleCheck (Task 4)"
```

---

### Task 5: DiagnosticsModule + FerretConfigLoader

**Files:**
- Create: `src/Ferret.Cli/Configuration/FerretConfigLoader.cs`
- Create: `src/Ferret.Cli/Modules/DiagnosticsModule.cs`
- Create: `tests/Ferret.Cli.Tests/Modules/DiagnosticsModuleTests.cs`

**Interfaces:**
- Produces: `DiagnosticsModule` (DefaultModule, id=ferret.diagnostics), `FerretConfigLoader.Load(string?)`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Modules/DiagnosticsModuleTests.cs
using Ferret.Cli.Infrastructure;
using Ferret.Cli.Modules;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
namespace Ferret.Cli.Tests.Modules;

public sealed class DiagnosticsModuleTests
{
    private static DiagnosticsModule Create() => new(NullLogger<DiagnosticsModule>.Instance);

    [Fact] public void Metadata_Id() => Assert.Equal("ferret.diagnostics", Create().Metadata.Id);
    [Fact] public void Metadata_Name() => Assert.Equal("Ferret Diagnostics", Create().Metadata.Name);
    [Fact] public void Metadata_Version_MatchesPlatform() =>
        Assert.Equal(SemanticVersion.Parse(FerretPlatform.Version), Create().Metadata.Version);
    [Fact] public async Task OnStartingAsync_Completes() =>
        await Create().OnStartingAsync(new FakeModuleCtx(), CancellationToken.None);
    [Fact] public async Task OnStartedAsync_Completes() =>
        await Create().OnStartedAsync(new FakeModuleCtx(), CancellationToken.None);
    [Fact] public async Task OnStoppedAsync_Completes() =>
        await Create().OnStoppedAsync(new FakeModuleCtx(), CancellationToken.None);
}

internal sealed class FakeModuleCtx : IModuleContext
{
    public string ModuleId => "ferret.diagnostics";
    public IExecutionContext ExecutionContext => throw new NotImplementedException();
    public IModuleRegistry Registry => throw new NotImplementedException();
}
```

- [ ] **Step 2: Run tests — confirm red**

Expected: FAIL.

- [ ] **Step 3: Implement FerretConfigLoader**

```csharp
// src/Ferret.Cli/Configuration/FerretConfigLoader.cs
using Microsoft.Extensions.Configuration;
namespace Ferret.Cli.Configuration;

/// <summary>
/// Why: Centralises config loading — ferret.json primary, FERRET_ env vars override, silent defaults when no file.
/// Thread Safety: Single Thread Only.
/// </summary>
internal static class FerretConfigLoader
{
    internal static IConfiguration Load(string? configPath)
    {
        var builder = new ConfigurationBuilder().AddEnvironmentVariables("FERRET_");
        var path = configPath ?? "ferret.json";
        if (File.Exists(path))
            builder.AddJsonFile(path, optional: false, reloadOnChange: false);
        return builder.Build();
    }
}
```

- [ ] **Step 4: Implement DiagnosticsModule**

```csharp
// src/Ferret.Cli/Modules/DiagnosticsModule.cs
using Ferret.Cli.Infrastructure;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Microsoft.Extensions.Logging;
namespace Ferret.Cli.Modules;

/// <summary>
/// Why: First built-in module — proves the hosting pipeline works end-to-end.
///      Version derived from FerretPlatform.Version so module and CLI versions stay in sync.
/// Lifecycle: Instantiated by StartCommandHandler and RuntimeLifecycleCheck.
/// Thread Safety: Thread Compatible.
/// </summary>
internal sealed class DiagnosticsModule : DefaultModule
{
    private readonly ILogger<DiagnosticsModule> _logger;

    internal DiagnosticsModule(ILogger<DiagnosticsModule> logger)
        : base(ModuleMetadata.Create(
            id: "ferret.diagnostics",
            name: "Ferret Diagnostics",
            version: SemanticVersion.Parse(FerretPlatform.Version),
            capabilities: [],
            description: "Built-in diagnostics module — verifies platform startup.",
            author: "Ferret Platform"))
    {
        _logger = logger;
    }

    public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DiagnosticsModule starting (v{Version})", Metadata.Version);
        return Task.CompletedTask;
    }

    public override Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DiagnosticsModule activated.");
        return Task.CompletedTask;
    }

    public override Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DiagnosticsModule stopped.");
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "DiagnosticsModuleTests"
```

Expected: PASS — 6 tests.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Cli/Configuration/ src/Ferret.Cli/Modules/ tests/Ferret.Cli.Tests/Modules/ .claude/
git commit -m "feat(sprint-6): DiagnosticsModule + FerretConfigLoader (Task 5)"
```

---

### Task 6: CoreCliModule + RootCommandFactory + ferret version + ferret about

**Files:**
- Create: `src/Ferret.Cli/Commands/Handlers/VersionCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Handlers/AboutCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/CoreCliModule.cs`
- Create: `src/Ferret.Cli/Commands/RootCommandFactory.cs`
- Rewrite: `src/Ferret.Cli/Program.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/VersionCommandHandlerTests.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/AboutCommandHandlerTests.cs`

**Interfaces:**
- Produces: `RootCommandFactory.Build(IEnumerable<ICliModule>, IConsole?)` → `RootCommand`; working `ferret version` + `ferret about`; 13 empty group stubs

Note: `StartCommandHandler`, `DoctorCommandHandler`, `StatusCommandHandler` are referenced in `CoreCliModule.GetCommands()` and `ConfigureServices()` but implemented in Tasks 7–9. Implement them as stubs (return `CommandResult.Failure` with "not yet implemented") in this task so `CoreCliModule` compiles. Replace with real implementations in Tasks 7–9.

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/VersionCommandHandlerTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;
using Ferret.Cli.Infrastructure;
namespace Ferret.Cli.Tests.Commands;

public sealed class VersionCommandHandlerTests
{
    private static RootCommand Root(IConsole c) => RootCommandFactory.Build([new CoreCliModule()], c);

    [Fact] public async Task Version_ExitsZero() =>
        Assert.Equal(0, await Root(new TestConsole()).InvokeAsync(["version"]));

    [Fact]
    public async Task Version_PrintsAssemblyVersion()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["version"]);
        Assert.Contains($"Ferret {FerretPlatform.Version}", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Version_PrintsPoweredBy()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["version"]);
        Assert.Contains("Powered by ContextOS", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Version_PrintsRuntimeInfo()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["version"]);
        Assert.Contains(".NET", c.Out.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Workspace_GroupStub_ExitsZero() =>
        Assert.Equal(0, await Root(new TestConsole()).InvokeAsync(["workspace"]));

    [Fact]
    public async Task Workspace_GroupStub_PrintsSprintInfo()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["workspace"]);
        Assert.Contains("Sprint 7", c.Out.ToString() ?? string.Empty);
    }
}
```

```csharp
// tests/Ferret.Cli.Tests/Commands/AboutCommandHandlerTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;
namespace Ferret.Cli.Tests.Commands;

public sealed class AboutCommandHandlerTests
{
    private static RootCommand Root(IConsole c) => RootCommandFactory.Build([new CoreCliModule()], c);

    [Fact] public async Task About_ExitsZero() =>
        Assert.Equal(0, await Root(new TestConsole()).InvokeAsync(["about"]));

    [Fact]
    public async Task About_PrintsProductName()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["about"]);
        Assert.Contains("Ferret", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task About_PrintsTagline()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["about"]);
        Assert.Contains("Dig Deep", c.Out.ToString() ?? string.Empty);
    }
}
```

- [ ] **Step 2: Run tests — confirm red**

Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement VersionCommandHandler**

```csharp
// src/Ferret.Cli/Commands/Handlers/VersionCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Infrastructure;
namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: First command a new user runs; surfaces version + runtime info for bug reports.
///      Version comes from FerretPlatform so it cannot drift from the git tag.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class VersionCommandHandler : ICommandHandler
{
    internal const string PoweredBy = "Powered by ContextOS";

    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine($"Ferret {FerretPlatform.Version}");
        context.Services.Output.WriteLine(PoweredBy);
        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine($"Runtime: {FerretPlatform.RuntimeInfo}");
        return Task.FromResult(CommandResult.Success);
    }
}
```

- [ ] **Step 4: Implement AboutCommandHandler**

```csharp
// src/Ferret.Cli/Commands/Handlers/AboutCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Infrastructure;
namespace Ferret.Cli.Commands.Handlers;

internal sealed class AboutCommandHandler : ICommandHandler
{
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine("Ferret");
        context.Services.Output.WriteLine("Dig Deep. Deliver Context.");
        context.Services.Output.WriteLine(VersionCommandHandler.PoweredBy);
        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine($"Version: {FerretPlatform.Version}");
        context.Services.Output.WriteLine($"Runtime: {FerretPlatform.RuntimeInfo}");
        return Task.FromResult(CommandResult.Success);
    }
}
```

- [ ] **Step 5: Create stub handlers for Tasks 7–9 (so CoreCliModule compiles)**

```csharp
// src/Ferret.Cli/Commands/Handlers/StartCommandHandler.cs — STUB; replaced in Task 7
using Ferret.Cli.Cli;
namespace Ferret.Cli.Commands.Handlers;
internal sealed class StartCommandHandler : ICommandHandler
{
    internal static CancellationToken TestCancellationToken { get; set; } = CancellationToken.None;
    public Task<CommandResult> ExecuteAsync(IFerretContext context) =>
        Task.FromResult(CommandResult.Failure); // replaced Task 7
}
```

```csharp
// src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs — STUB; replaced in Task 8
using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
namespace Ferret.Cli.Commands.Handlers;
internal sealed class DoctorCommandHandler : ICommandHandler
{
    internal DoctorCommandHandler(IEnumerable<IDiagnosticCheck> checks) { }
    public Task<CommandResult> ExecuteAsync(IFerretContext context) =>
        Task.FromResult(CommandResult.Failure); // replaced Task 8
}
```

```csharp
// src/Ferret.Cli/Commands/Handlers/StatusCommandHandler.cs — STUB; replaced in Task 9
using Ferret.Cli.Cli;
namespace Ferret.Cli.Commands.Handlers;
internal sealed class StatusCommandHandler : ICommandHandler
{
    public Task<CommandResult> ExecuteAsync(IFerretContext context) =>
        Task.FromResult(CommandResult.Failure); // replaced Task 9
}
```

- [ ] **Step 6: Implement CoreCliModule**

```csharp
// src/Ferret.Cli/Commands/CoreCliModule.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Handlers;
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Diagnostics.Checks;
using Microsoft.Extensions.DependencyInjection;
namespace Ferret.Cli.Commands;

/// <summary>
/// Why: The built-in ICliModule — contributes all Sprint 6 working commands plus 13 reserved group stubs.
///      Sprint 7 modules add their own ICliModule without touching RootCommandFactory.
/// Thread Safety: Thread Safe — called once during startup.
/// </summary>
internal sealed class CoreCliModule : CliModuleBase
{
    public override string Name => "ferret.core";
    public override string Description => "Core Ferret CLI commands.";

    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return Cmd("version", "Print the Ferret platform version.", typeof(VersionCommandHandler));
        yield return Cmd("about", "About Ferret and ContextOS.", typeof(AboutCommandHandler));
        yield return Cmd("start", "Start the Ferret runtime host.", typeof(StartCommandHandler),
            new OptionDefinition("--config", "Path to ferret.json.", typeof(string)));
        yield return Cmd("doctor", "Validate the local Ferret installation.", typeof(DoctorCommandHandler));
        yield return Cmd("status", "Report the current Ferret runtime status.", typeof(StatusCommandHandler));

        // Reserved command groups — real empty Command objects; show Sprint roadmap when invoked
        yield return CommandDefinition.EmptyGroup("workspace", "Workspace management.",
            "Sprint 7", ["workspace init", "workspace status", "workspace open"]);
        yield return CommandDefinition.EmptyGroup("index", "Content indexing.",
            "Sprint 8", ["index build", "index status", "index clear"]);
        yield return CommandDefinition.EmptyGroup("search", "Search indexed content.",
            "Sprint 8", ["search query", "search files"]);
        yield return CommandDefinition.EmptyGroup("memory", "Semantic memory management.",
            "Sprint 9", ["memory store", "memory recall"]);
        yield return CommandDefinition.EmptyGroup("context", "ContextOS integration.",
            "Sprint 9", ["context switch", "context list"]);
        yield return CommandDefinition.EmptyGroup("review", "AI-assisted code review.", "Sprint 10", []);
        yield return CommandDefinition.EmptyGroup("git", "Git integration.",
            "Sprint 10", ["git sync", "git status"]);
        yield return CommandDefinition.EmptyGroup("jira", "JIRA integration.",
            "Sprint 10", ["jira search", "jira create"]);
        yield return CommandDefinition.EmptyGroup("docs", "Documentation management.", "Sprint 11", []);
        yield return CommandDefinition.EmptyGroup("plugin", "Plugin management.",
            "Sprint 11", ["plugin install", "plugin list"]);
        yield return CommandDefinition.EmptyGroup("model", "AI model management.", "Sprint 12", []);
        yield return CommandDefinition.EmptyGroup("logs", "Runtime log access.",
            "Sprint 7", ["logs tail", "logs clear"]);
        yield return CommandDefinition.EmptyGroup("telemetry", "Usage telemetry.", "Sprint 12", []);
    }

    public override IEnumerable<IDiagnosticCheck> GetDiagnosticChecks()
    {
        yield return new ConfigurationCheck();
        yield return new RuntimeLifecycleCheck();
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<VersionCommandHandler>();
        services.AddTransient<AboutCommandHandler>();
        services.AddTransient<StartCommandHandler>();
        services.AddTransient<StatusCommandHandler>();
        // DoctorCommandHandler registered separately — needs check list injected
        var checks = GetDiagnosticChecks().ToList();
        services.AddTransient<DoctorCommandHandler>(_ => new DoctorCommandHandler(checks));
    }

    private static CommandDefinition Cmd(string name, string description, Type handlerType,
        params OptionDefinition[] options) =>
        new(new CommandMetadata(name, description), handlerType,
            Options: options.Length > 0 ? options : null);
}
```

- [ ] **Step 7: Implement RootCommandFactory**

```csharp
// src/Ferret.Cli/Commands/RootCommandFactory.cs
using System.CommandLine;
using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
namespace Ferret.Cli.Commands;

/// <summary>
/// Why: The ONLY file importing System.CommandLine types (alongside GlobalOptions, ConsoleFormatter, FerretContext.From).
///      Swapping CLI frameworks = only this file changes.
///      Discovers ICliModule instances, builds DI container, constructs command tree.
/// Thread Safety: Single Thread Only — called once at startup.
/// </summary>
internal static class RootCommandFactory
{
    internal static RootCommand Build(IEnumerable<ICliModule> modules, IConsole? console = null)
    {
        var moduleList = modules.ToList();
        var resolvedConsole = console ?? new SystemConsole();
        var formatter = new ConsoleFormatter(resolvedConsole);

        IConfiguration config = FerretConfigLoader.Load(null);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IOutputFormatter>(formatter);
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        foreach (var module in moduleList)
            module.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var ferretServices = new FerretServices(provider, config, NullLoggerFactory.Instance, formatter);

        var root = new RootCommand("Ferret — Dig Deep. Deliver Context.");
        GlobalOptions.AddAll(root);

        var allDefs = moduleList.SelectMany(m => m.GetCommands()).ToList();

        foreach (var def in allDefs.Where(d => d.Group == null))
            root.AddCommand(BuildCommand(def, provider, ferretServices));

        foreach (var grp in allDefs.Where(d => d.Group != null).GroupBy(d => d.Group!))
        {
            var groupCmd = root.Subcommands.OfType<Command>().FirstOrDefault(c => c.Name == grp.Key)
                           ?? new Command(grp.Key);
            foreach (var def in grp)
                groupCmd.AddCommand(BuildCommand(def, provider, ferretServices));
            if (!root.Subcommands.Contains(groupCmd))
                root.AddCommand(groupCmd);
        }

        return root;
    }

    private static Command BuildCommand(CommandDefinition def, IServiceProvider provider, IFerretServices ferretServices)
    {
        var cmd = new Command(def.Metadata.Name, def.Metadata.Description);
        if (def.Metadata.Hidden) cmd.IsHidden = true;

        var optMap = new Dictionary<string, Option>(StringComparer.Ordinal);
        foreach (var optDef in def.Options ?? [])
        {
            var opt = MakeOption(optDef);
            cmd.AddOption(opt);
            optMap[optDef.LongName.TrimStart('-')] = opt;
        }

        if (def.HandlerType is null)
        {
            var planned = def.PlannedSubcommands ?? [];
            var sprint = def.PlannedSprint ?? "A future sprint";
            cmd.SetHandler((InvocationContext ctx) =>
            {
                ctx.Console.Out.WriteLine(def.Metadata.Description);
                ctx.Console.Out.WriteLine();
                ctx.Console.Out.WriteLine("No commands are currently installed.");
                if (planned.Count > 0)
                {
                    ctx.Console.Out.WriteLine($"\n{sprint} will introduce:");
                    foreach (var sub in planned)
                        ctx.Console.Out.WriteLine($"  {sub}");
                }
                ctx.ExitCode = 0;
            });
        }
        else
        {
            var handlerType = def.HandlerType;
            cmd.SetHandler(async (InvocationContext ctx) =>
            {
                bool verbose = ctx.ParseResult.GetValueForOption(GlobalOptions.Verbose);
                var verbosity = verbose ? VerbosityLevel.Verbose : VerbosityLevel.Normal;
                var scopedFormatter = new ConsoleFormatter(ctx.Console, verbosity);
                var scopedServices = new FerretServices(provider, ferretServices.Configuration,
                    ferretServices.LoggerFactory, scopedFormatter);
                var parsedOpts = ParseOptions(ctx, optMap);
                var context = FerretContext.From(ctx, scopedServices, parsedOpts);
                var handler = (ICommandHandler)provider.GetRequiredService(handlerType);
                ctx.ExitCode = (int)await handler.ExecuteAsync(context).ConfigureAwait(false);
            });
        }

        return cmd;
    }

    private static Option MakeOption(OptionDefinition def)
    {
        Option opt = def.ValueType == typeof(bool)
            ? new Option<bool>(def.LongName, def.Description)
            : new Option<string>(def.LongName, def.Description);
        opt.IsHidden = def.IsHidden;
        return opt;
    }

    private static IReadOnlyDictionary<string, object?> ParseOptions(
        InvocationContext ctx, Dictionary<string, Option> optMap)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (name, opt) in optMap)
            result[name] = ctx.ParseResult.GetValueForOption(opt);
        return result;
    }
}
```

Note on `SystemConsole`: In System.CommandLine beta4 this is `System.CommandLine.IO.SystemConsole`. In newer versions it may differ. If it doesn't exist, fall back to `new TestConsole()` in tests and skip `SystemConsole` in production (let `console` parameter be null and handle it by checking SC's default console). Adjust the null-fallback line in `Build()` as needed.

- [ ] **Step 8: Rewrite Program.cs**

```csharp
// src/Ferret.Cli/Program.cs
using Ferret.Cli.Commands;

return await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(args);
```

- [ ] **Step 9: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "VersionCommandHandlerTests|AboutCommandHandlerTests"
```

Expected: PASS — 9 tests.

- [ ] **Step 10: Smoke test**

```
dotnet build && dotnet run --project src/Ferret.Cli -- version
dotnet run --project src/Ferret.Cli -- workspace
```

Expected: version output; workspace prints "Sprint 7 will introduce:..."

- [ ] **Step 11: Commit**

```
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/Commands/VersionCommandHandlerTests.cs tests/Ferret.Cli.Tests/Commands/AboutCommandHandlerTests.cs .claude/
git commit -m "feat(sprint-6): CoreCliModule, RootCommandFactory, ferret version + about + 13 group stubs (Task 6)"
```

---

### Task 7: ferret start command

**Files:**
- Rewrite: `src/Ferret.Cli/Commands/Handlers/StartCommandHandler.cs` (was stub from Task 6)
- Create: `tests/Ferret.Cli.Tests/Commands/StartCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ICommandHandler`, `IFerretContext.GetOption<string>("config")`, `RuntimeBuilder`, `DiagnosticsModule`
- Produces: `StartCommandHandler` — banner → build → start → block → Ctrl+C → stop → `CommandResult.Success`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/StartCommandHandlerTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Handlers;
using Ferret.Cli.Infrastructure;
namespace Ferret.Cli.Tests.Commands;

public sealed class StartCommandHandlerTests : IDisposable
{
    public void Dispose() => StartCommandHandler.TestCancellationToken = CancellationToken.None;

    private static void Arm(out CancellationTokenSource cts)
    {
        cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(600));
        StartCommandHandler.TestCancellationToken = cts.Token;
    }

    [Fact]
    public async Task Start_CancelsCleanly_ExitsZero()
    {
        Arm(out var cts); using (cts)
            Assert.Equal(0, await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(["start"]));
    }

    [Fact]
    public async Task Start_PrintsBanner()
    {
        Arm(out var cts); using (cts)
        {
            var c = new TestConsole();
            await RootCommandFactory.Build([new CoreCliModule()], c).InvokeAsync(["start"]);
            Assert.Contains($"Ferret {FerretPlatform.Version}", c.Out.ToString() ?? string.Empty);
        }
    }

    [Fact]
    public async Task Start_PrintsRuntimeReady()
    {
        Arm(out var cts); using (cts)
        {
            var c = new TestConsole();
            await RootCommandFactory.Build([new CoreCliModule()], c).InvokeAsync(["start"]);
            Assert.Contains("Runtime ready", c.Out.ToString() ?? string.Empty);
        }
    }
}
```

- [ ] **Step 2: Run tests — confirm red** (stub returns Failure, so Exit=1 ≠ 0)

```
dotnet test tests/Ferret.Cli.Tests/ --filter "StartCommandHandlerTests" 2>&1 | tail -5
```

- [ ] **Step 3: Replace stub with full implementation**

```csharp
// src/Ferret.Cli/Commands/Handlers/StartCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;
using Ferret.Cli.Modules;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Builds the runtime, starts it, blocks until cancellation, then shuts down cleanly.
///      TestCancellationToken allows tests to cancel without blocking indefinitely.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class StartCommandHandler : ICommandHandler
{
    internal static CancellationToken TestCancellationToken { get; set; } = CancellationToken.None;

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var cancellationToken = TestCancellationToken.CanBeCanceled
            ? TestCancellationToken
            : context.CancellationToken;

        var output = context.Services.Output;
        output.WriteLine($"Ferret {FerretPlatform.Version}");
        output.WriteLine(VersionCommandHandler.PoweredBy);
        output.WriteLine();
        output.WriteLine("Starting runtime...");
        output.WriteLine("Loading modules...");

        string? configPath = context.GetOption<string>("config");
        FerretConfigLoader.Load(configPath);

        var runtimeHost = new RuntimeBuilder()
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning))
            .AddModule(new DiagnosticsModule(NullLogger<DiagnosticsModule>.Instance))
            .Build();

        try
        {
            await runtimeHost.StartAsync(cancellationToken).ConfigureAwait(false);
            output.WriteLine("DiagnosticsModule activated.");
            output.WriteLine("Runtime ready.");
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await runtimeHost.StopAsync(CancellationToken.None).ConfigureAwait(false);
            if (runtimeHost is IAsyncDisposable d)
                await d.DisposeAsync().ConfigureAwait(false);
        }

        return CommandResult.Success;
    }
}
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "StartCommandHandlerTests"
```

Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Commands/Handlers/StartCommandHandler.cs tests/Ferret.Cli.Tests/Commands/StartCommandHandlerTests.cs .claude/
git commit -m "feat(sprint-6): ferret start — banner, runtime lifecycle, Ctrl+C (Task 7)"
```

---

### Task 8: ferret doctor command

**Files:**
- Rewrite: `src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs` (was stub)
- Create: `tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `DiagnosticRunner`, `IEnumerable<IDiagnosticCheck>` injected via constructor
- Produces: `DoctorCommandHandler` — check output, summary, `CommandResult.Success` or `Failure`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;
namespace Ferret.Cli.Tests.Commands;

public sealed class DoctorCommandHandlerTests
{
    private static RootCommand Root(IConsole c) => RootCommandFactory.Build([new CoreCliModule()], c);

    [Fact] public async Task Doctor_ExitsZero() =>
        Assert.Equal(0, await Root(new TestConsole()).InvokeAsync(["doctor"]));

    [Fact]
    public async Task Doctor_PrintsHeader()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["doctor"]);
        Assert.Contains("Ferret Doctor", c.Out.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Doctor_PrintsChecks()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["doctor"]);
        string output = c.Out.ToString() ?? string.Empty;
        Assert.Contains("Configuration loaded", output);
        Assert.Contains("Runtime lifecycle", output);
    }

    [Fact]
    public async Task Doctor_PrintsHealthyConclusion()
    {
        var c = new TestConsole();
        await Root(c).InvokeAsync(["doctor"]);
        Assert.Contains("Ferret is healthy", c.Out.ToString() ?? string.Empty);
    }
}
```

- [ ] **Step 2: Run tests — confirm red** (stub returns Failure → exit 1)

- [ ] **Step 3: Replace stub with full implementation**

```csharp
// src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Discovers all IDiagnosticCheck instances from registered modules and runs them.
///      Adding a new module automatically extends doctor — this handler never changes.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class DoctorCommandHandler : ICommandHandler
{
    private readonly IReadOnlyList<IDiagnosticCheck> _checks;

    internal DoctorCommandHandler(IEnumerable<IDiagnosticCheck> checks)
    {
        _checks = checks.ToList();
    }

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine("Ferret Doctor");
        context.Services.Output.WriteLine();

        bool healthy = await DiagnosticRunner.RunAsync(_checks, context).ConfigureAwait(false);

        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine(healthy
            ? "Ferret is healthy."
            : "Ferret has issues. Review the checks above.");

        return healthy ? CommandResult.Success : CommandResult.Failure;
    }
}
```

- [ ] **Step 4: Run tests — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "DoctorCommandHandlerTests"
```

Expected: PASS — 4 tests.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs .claude/
git commit -m "feat(sprint-6): ferret doctor — module-contributed IDiagnosticCheck, DiagnosticRunner (Task 8)"
```

---

### Task 9: ferret status command

**Files:**
- Rewrite: `src/Ferret.Cli/Commands/Handlers/StatusCommandHandler.cs` (was stub)
- Create: `tests/Ferret.Cli.Tests/Commands/StatusCommandHandlerTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// tests/Ferret.Cli.Tests/Commands/StatusCommandHandlerTests.cs
using System.CommandLine;
using Ferret.Cli.Commands;
namespace Ferret.Cli.Tests.Commands;

public sealed class StatusCommandHandlerTests
{
    [Fact]
    public async Task Status_ReportsNotRunning_ExitsOne()
    {
        var c = new TestConsole();
        int code = await RootCommandFactory.Build([new CoreCliModule()], c).InvokeAsync(["status"]);
        Assert.Equal(1, code);
        Assert.Contains("not running", c.Out.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run test — confirm red** (stub currently exits 1 — test might pass. Verify "not running" string is NOT in output with the stub)

If stub test passes for the wrong reason (exits 1 but no "not running" text), the Assert.Contains will fail correctly.

- [ ] **Step 3: Replace stub**

```csharp
// src/Ferret.Cli/Commands/Handlers/StatusCommandHandler.cs
using Ferret.Cli.Cli;
namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Operator visibility into runtime state. Sprint 7: named-pipe IPC health query.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class StatusCommandHandler : ICommandHandler
{
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        // Sprint 6: no IPC. Sprint 7 adds named-pipe health endpoint.
        context.Services.Output.WriteLine("Ferret is not running (start with: ferret start)");
        return Task.FromResult(CommandResult.Failure);
    }
}
```

- [ ] **Step 4: Run test — confirm green**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "StatusCommandHandlerTests"
```

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Commands/Handlers/StatusCommandHandler.cs tests/Ferret.Cli.Tests/Commands/StatusCommandHandlerTests.cs .claude/
git commit -m "feat(sprint-6): ferret status — not running stub; IPC in Sprint 7 (Task 9)"
```

---

### Task 10: Integration tests + placeholder cleanup

**Files:**
- Create: `tests/Ferret.Cli.Tests/Integration/RuntimeLifecycleIntegrationTests.cs`
- Rewrite: `tests/Ferret.Cli.Tests/CliModuleTests.cs`

- [ ] **Step 1: Write integration tests**

```csharp
// tests/Ferret.Cli.Tests/Integration/RuntimeLifecycleIntegrationTests.cs
using Ferret.Cli.Modules;
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging.Abstractions;
namespace Ferret.Cli.Tests.Integration;

public sealed class RuntimeLifecycleIntegrationTests
{
    private static IRuntimeHost Build() => new RuntimeBuilder()
        .AddModule(new DiagnosticsModule(NullLogger<DiagnosticsModule>.Instance))
        .Build();

    [Fact]
    public async Task Start_ReachesRunning()
    {
        var host = Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        Assert.Equal(RuntimeState.Running, host.State);
        await host.StopAsync(cts.Token);
        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }

    [Fact]
    public async Task Stop_AfterStart_ReachesStopped()
    {
        var host = Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        await host.StopAsync(cts.Token);
        Assert.Equal(RuntimeState.Stopped, host.State);
        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }

    [Fact]
    public async Task Modules_ContainsDiagnosticsModule()
    {
        var host = Build();
        IModule? module = host.Modules.GetById("ferret.diagnostics");
        Assert.NotNull(module);
        Assert.Equal("Ferret Diagnostics", module!.Name);
        if (host is IAsyncDisposable d) await d.DisposeAsync();
    }
}
```

- [ ] **Step 2: Replace CliModuleTests.cs placeholder**

Read the existing file first, then write:
```csharp
// tests/Ferret.Cli.Tests/CliModuleTests.cs
namespace Ferret.Cli.Tests;

// Replaced in Sprint 6 — see Commands/, Modules/, Diagnostics/, Integration/ subdirectories.
public sealed class CliModuleTests
{
    [Fact]
    public void ProjectStructure_IsValid() => Assert.True(true);
}
```

- [ ] **Step 3: Run integration tests + full suite**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "RuntimeLifecycleIntegrationTests"
dotnet test --logger "console;verbosity=minimal"
```

Expected: All pass, 0 failures.

- [ ] **Step 4: Commit**

```
git add tests/Ferret.Cli.Tests/ .claude/
git commit -m "test(sprint-6): E2E integration tests — runtime lifecycle with DiagnosticsModule (Task 10)"
```

---

### Task 11: Final verification + sprint demo + tag

- [ ] **Step 1: Full build — 0 warnings, 0 errors**

```
dotnet build
```

- [ ] **Step 2: No Console.WriteLine in production code**

```
grep -r "Console.WriteLine" src/Ferret.Cli/ --include="*.cs"
```

Expected: no output.

- [ ] **Step 3: No ANSI escape codes**

```
grep -r "\\\\u001b\|\\\\e\[" src/Ferret.Cli/ --include="*.cs"
```

Expected: no output.

- [ ] **Step 4: System.CommandLine confined to designated files**

```
grep -r "using System.CommandLine" src/Ferret.Cli/ --include="*.cs" -l
```

Expected: only `RootCommandFactory.cs`, `GlobalOptions.cs`, `ConsoleFormatter.cs`, `FerretContext.cs`.

- [ ] **Step 5: Full test suite — ≥ 40 new tests**

```
dotnet test --logger "console;verbosity=minimal"
```

- [ ] **Step 6: Demo — ferret version**

```
dotnet run --project src/Ferret.Cli -- version
```

Expected:
```
Ferret 0.6.0
Powered by ContextOS

Runtime: .NET 9.0.x / Windows 11 Pro (X64)
```

- [ ] **Step 7: Demo — ferret about**

```
dotnet run --project src/Ferret.Cli -- about
```

Expected: "Ferret" + "Dig Deep. Deliver Context." + "Powered by ContextOS" + version + runtime.

- [ ] **Step 8: Demo — ferret doctor**

```
dotnet run --project src/Ferret.Cli -- doctor
```

Expected:
```
Ferret Doctor

✓ Configuration loaded
✓ Runtime lifecycle

Ferret is healthy.
```

- [ ] **Step 9: Demo — reserved group stubs**

```
dotnet run --project src/Ferret.Cli -- workspace
dotnet run --project src/Ferret.Cli -- git
```

Expected: description + "Sprint 7/10 will introduce: ..." + planned subcommands. Exit 0.

- [ ] **Step 10: Demo — ferret start + Ctrl+C**

```
dotnet run --project src/Ferret.Cli -- start
```

Expected: banner + "DiagnosticsModule activated." + "Runtime ready." → Ctrl+C exits 0.

- [ ] **Step 11: Version sync regression test**

Temporarily set `<Version>0.6.1-test</Version>` in `Ferret.Cli.csproj`, rebuild, run `ferret version` — output must show `0.6.1-test` with 0 code changes. Restore to `0.6.0`.

- [ ] **Step 12: Tag**

```
git tag v0.6.0-sprint6
```

---

## Review Gates

| Gate | Criterion |
|------|-----------|
| Build | 0 warnings, 0 errors |
| Tests | All pass; ≥ 40 new tests |
| `ferret version` | Assembly version + runtime info; ContextOS branding |
| `ferret about` | "Dig Deep. Deliver Context." + ContextOS |
| `ferret doctor` | All checks ✓; "Ferret is healthy." |
| `ferret start` | Banner + "Runtime ready."; Ctrl+C exits 0 |
| Reserved groups | `ferret workspace` prints Sprint 7 roadmap; exits 0 |
| No Console.WriteLine | grep returns nothing |
| No ANSI codes | grep returns nothing |
| SC isolation | SC types in ≤ 4 files |
| ICliModule | CoreCliModule is the only module; Sprint 7 adds more without touching RootCommandFactory |
| ICommandHandler | All commands resolved via DI constructor injection; no delegate lambdas |
| Version sync | Bumping `<Version>` changes output with 0 code changes |

## Out of Scope (Sprint 7+)

| Item | Sprint |
|------|--------|
| WorkspaceCliModule, GitCliModule, JiraCliModule, IndexCliModule | 7–10 |
| Named-pipe IPC for `ferret status` | 7 |
| `--verbose`, `--quiet`, `--json` wired into FerretContext | 7 |
| JsonFormatter, MarkdownFormatter implementations | 7 |
| Git SHA + build date in `ferret version` | 7 |
| `IWorkspace` in IFerretServices | 7 |
| Module-contributed IDiagnosticCheck via ICliModule.GetDiagnosticChecks | 7 |
| Middleware pipeline for ICommandHandler | 8 |
| Authorization on commands | 9 |
| Plugin loading from disk | 9 |
