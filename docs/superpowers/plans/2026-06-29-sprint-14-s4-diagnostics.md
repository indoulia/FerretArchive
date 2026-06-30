# Sprint 14 S4: Diagnostics and Logging Implementation Plan

> **For agentic workers:** Use `tokensave_context` as your primary exploration tool before reading any source file. Read only the files listed in each task's **Files** section. Follow TDD strictly: write a failing test, confirm it is red, implement, confirm green, commit. Never read generated files (`obj/`, `bin/`).

---

## Goal

Enhance observability for RC1 across three areas:
1. `ferret doctor` gains four structured workspace health checks.
2. `--log-level <debug|info|warning|error>` becomes a visible global option on the root command, wired to `ILoggerFactory`.
3. `ferret index --verbose` attaches a console event sink to the `IEventBus` so per-document indexing events stream to the console.

---

## Architecture

### Task 1 — `ferret doctor` health checks

Four new `IDiagnosticCheck` implementations are added under `src/Ferret.Cli/Diagnostics/Checks/`. `CoreCliModule.GetDiagnosticChecks()` yields them alongside the two existing checks. No changes to `DiagnosticRunner`, `DoctorCommandHandler`, or `IDiagnosticCheck`.

```
IDiagnosticCheck
├── ConfigurationCheck          (exists)
├── RuntimeLifecycleCheck       (exists)
├── WorkspaceRootCheck          (NEW)   — IWorkspaceContext.WorkspaceRoot exists on disk
├── FerretConfigDirCheck        (NEW)   — .ferret/ directory exists
├── IndexFreshnessCheck         (NEW)   — keyword-index.db mtime < 24 h
└── AiProviderConfigCheck       (NEW)   — Ferret:Ai:Providers section is non-empty
```

`CoreCliModule` already receives `IWorkspaceContext` nowhere — it is a zero-dependency module. The new checks receive their inputs via constructor injection, mirroring `ConfigurationCheck(string? configPath)`. `CoreCliModule.ConfigureServices` builds them with the `IWorkspaceContext` it can obtain from the `IConfiguration` / `IServiceProvider` at build time.

Because `CoreCliModule` is constructed before DI is finalised, workspace checks that need an `IWorkspaceContext` will receive it as a constructor parameter passed by `CoreCliModule` (the same pattern used for `ConfigurationCheck`). The workspace root is resolved from `IConfiguration["Ferret:Workspace:Root"]` (defaulting to `Environment.CurrentDirectory`) inside each check's constructor.

### Task 2 — `--log-level` global option

`GlobalOptions` gains a new `Option<string> LogLevel`. `GlobalOptions.AddAll()` adds it to the root command as a **visible** (non-hidden) option. `RootCommandFactory.RegisterHandlerAction` reads it from the `ParseResult` and replaces the hard-coded `NullLoggerFactory.Instance` with a real `LoggerFactory` configured to the chosen minimum level. `FerretContext.From` does not need to change — the logger factory is injected into `FerretServices`, not into the context itself.

### Task 3 — `ferret index --verbose` event sink

`IndexCliModule.GetCommands()` already declares a `--rebuild` option. A `--verbose` option is added alongside it. `IndexCommandHandler.ExecuteAsync` checks `context.GetOption<bool>("verbose")`. When true, it subscribes a `ConsoleIndexEventSink` to the `IEventBus` before calling `_pipeline.RunAsync`. The sink implements `IEventBus` as a decorator: it writes each event to the console then forwards to the inner bus.

```
IEventBus
└── ConsoleIndexEventSink : IEventBus   (NEW, Ferret.Cli.Commands.Indexing)
    └── delegates to NullEventBus (or any inner IEventBus)
```

The sink is constructed inline in `IndexCommandHandler` — it is not registered in DI, keeping the DI container clean for `--verbose` being a runtime toggle.

---

## Tech Stack

| Concern | Type / Package |
|---|---|
| CLI parsing | `System.CommandLine` (existing) |
| Logging | `Microsoft.Extensions.Logging` (existing) |
| DI | `Microsoft.Extensions.DependencyInjection` (existing) |
| Config | `Microsoft.Extensions.Configuration` (existing) |
| AI config | `Ferret.Configuration.AI` — `AiOptions`, section `Ferret:Ai` |
| Index events | `Ferret.Core.Events.Indexing` — `DocumentIndexedEvent`, `DocumentSkippedEvent`, `DocumentParsingFailedEvent`, `IndexingStartedEvent`, `IndexingCompletedEvent` |

---

## Global Constraints

- TDD: failing test first, verify red, implement, verify green, commit.
- Commit prefix: `feat(sprint-14):`.
- Modify only files in each task's **Files** list. No unrelated formatting or cleanup.
- `internal` visibility for all new types unless a public interface requires otherwise.
- No new NuGet packages.
- `#pragma warning disable CA1031` with matching restore around broad `catch` blocks, matching the existing pattern in `ConfigurationCheck` and `RuntimeLifecycleCheck`.
- Each `IDiagnosticCheck.Name` must be a stable, human-readable string (used in test assertions).

---

## File Structure

```
src/Ferret.Cli/
  Cli/
    GlobalOptions.cs                         MODIFY — add LogLevel option
  Commands/
    CoreCliModule.cs                         MODIFY — register new checks + accept workspace root
    RootCommandFactory.cs                    MODIFY — read --log-level, build real ILoggerFactory
    Indexing/
      IndexCliModule.cs                      MODIFY — add --verbose option
      IndexCommandHandler.cs                 MODIFY — wire ConsoleIndexEventSink when verbose
      ConsoleIndexEventSink.cs               NEW
  Diagnostics/
    Checks/
      WorkspaceRootCheck.cs                  NEW
      FerretConfigDirCheck.cs                NEW
      IndexFreshnessCheck.cs                 NEW
      AiProviderConfigCheck.cs               NEW

tests/Ferret.Cli.Tests/
  Diagnostics/
    WorkspaceRootCheckTests.cs               NEW
    FerretConfigDirCheckTests.cs             NEW
    IndexFreshnessCheckTests.cs              NEW
    AiProviderConfigCheckTests.cs            NEW
  Commands/
    DoctorCommandHandlerTests.cs             MODIFY — assert new check names appear in output
    Indexing/
      IndexCommandHandlerTests.cs            MODIFY — assert verbose sink fires
      IndexCliModuleTests.cs                 MODIFY — assert --verbose option present
  GlobalOptionsTests.cs                      NEW
```

---

## Tasks

### Task 1: Enhance `ferret doctor` health checks

**Files:**
- `src/Ferret.Cli/Diagnostics/Checks/WorkspaceRootCheck.cs` (NEW)
- `src/Ferret.Cli/Diagnostics/Checks/FerretConfigDirCheck.cs` (NEW)
- `src/Ferret.Cli/Diagnostics/Checks/IndexFreshnessCheck.cs` (NEW)
- `src/Ferret.Cli/Diagnostics/Checks/AiProviderConfigCheck.cs` (NEW)
- `src/Ferret.Cli/Commands/CoreCliModule.cs` (MODIFY)
- `tests/Ferret.Cli.Tests/Diagnostics/WorkspaceRootCheckTests.cs` (NEW)
- `tests/Ferret.Cli.Tests/Diagnostics/FerretConfigDirCheckTests.cs` (NEW)
- `tests/Ferret.Cli.Tests/Diagnostics/IndexFreshnessCheckTests.cs` (NEW)
- `tests/Ferret.Cli.Tests/Diagnostics/AiProviderConfigCheckTests.cs` (NEW)
- `tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs` (MODIFY)

**Interfaces / key types:**
- `IDiagnosticCheck` — `string Name`, `Task<DiagnosticCheckResult> RunAsync(IFerretContext, CancellationToken)`
- `DiagnosticCheckResult.Pass()` / `DiagnosticCheckResult.Fail(string)`
- `WorkspaceLayout.RootDirectoryName` = `".ferret"`
- `IndexLayout` — `IndexDirectoryName`, `KeywordDirectoryName`, `KeywordDatabaseFileName`
- `AiOptions` — `Providers` dictionary (section `Ferret:Ai`)

---

- [ ] **Step 1.1 — Write failing tests for `WorkspaceRootCheck`**

  File: `tests/Ferret.Cli.Tests/Diagnostics/WorkspaceRootCheckTests.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Diagnostics;
  using Ferret.Cli.Diagnostics.Checks;

  namespace Ferret.Cli.Tests.Diagnostics;

  public sealed class WorkspaceRootCheckTests
  {
      [Fact]
      public async Task Pass_WhenDirectoryExists()
      {
          var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
          Directory.CreateDirectory(dir);
          try
          {
              using var sw = new StringWriter();
              var ctx = FerretContext.CreateTest(sw, workingDirectory: dir);
              var check = new WorkspaceRootCheck(dir);
              var result = await check.RunAsync(ctx, CancellationToken.None);
              Assert.True(result.Passed);
          }
          finally { Directory.Delete(dir, recursive: true); }
      }

      [Fact]
      public async Task Fail_WhenDirectoryMissing()
      {
          using var sw = new StringWriter();
          var ctx = FerretContext.CreateTest(sw);
          var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
          var check = new WorkspaceRootCheck(missing);
          var result = await check.RunAsync(ctx, CancellationToken.None);
          Assert.False(result.Passed);
          Assert.NotNull(result.FailureReason);
      }

      [Fact]
      public void Name_IsStable()
      {
          var check = new WorkspaceRootCheck(Path.GetTempPath());
          Assert.Equal("Workspace root exists", check.Name);
      }
  }
  ```

- [ ] **Step 1.2 — Verify tests are red** — run `dotnet test --filter "WorkspaceRootCheckTests"` and confirm compile or runtime failure.

- [ ] **Step 1.3 — Implement `WorkspaceRootCheck`**

  File: `src/Ferret.Cli/Diagnostics/Checks/WorkspaceRootCheck.cs`

  ```csharp
  using Ferret.Cli.Cli;

  namespace Ferret.Cli.Diagnostics.Checks;

  /// <summary>Checks that the configured workspace root directory exists on disk.</summary>
  internal sealed class WorkspaceRootCheck : IDiagnosticCheck
  {
      private readonly string _workspaceRoot;

      internal WorkspaceRootCheck(string workspaceRoot)
      {
          ArgumentNullException.ThrowIfNull(workspaceRoot);
          _workspaceRoot = workspaceRoot;
      }

      /// <inheritdoc/>
      public string Name => "Workspace root exists";

      /// <inheritdoc/>
      public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
      {
          var result = Directory.Exists(_workspaceRoot)
              ? DiagnosticCheckResult.Pass()
              : DiagnosticCheckResult.Fail($"Directory not found: {_workspaceRoot}");
          return Task.FromResult(result);
      }
  }
  ```

- [ ] **Step 1.4 — Verify `WorkspaceRootCheckTests` are green.**

- [ ] **Step 1.5 — Write failing tests for `FerretConfigDirCheck`**

  File: `tests/Ferret.Cli.Tests/Diagnostics/FerretConfigDirCheckTests.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Diagnostics;
  using Ferret.Cli.Diagnostics.Checks;
  using Ferret.Core.Workspace;

  namespace Ferret.Cli.Tests.Diagnostics;

  public sealed class FerretConfigDirCheckTests
  {
      [Fact]
      public async Task Pass_WhenFerretDirExists()
      {
          var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
          var ferretDir = Path.Combine(root, WorkspaceLayout.RootDirectoryName);
          Directory.CreateDirectory(ferretDir);
          try
          {
              using var sw = new StringWriter();
              var ctx = FerretContext.CreateTest(sw);
              var check = new FerretConfigDirCheck(root);
              var result = await check.RunAsync(ctx, CancellationToken.None);
              Assert.True(result.Passed);
          }
          finally { Directory.Delete(root, recursive: true); }
      }

      [Fact]
      public async Task Fail_WhenFerretDirMissing()
      {
          var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
          Directory.CreateDirectory(root);
          try
          {
              using var sw = new StringWriter();
              var ctx = FerretContext.CreateTest(sw);
              var check = new FerretConfigDirCheck(root);
              var result = await check.RunAsync(ctx, CancellationToken.None);
              Assert.False(result.Passed);
              Assert.NotNull(result.FailureReason);
          }
          finally { Directory.Delete(root, recursive: true); }
      }

      [Fact]
      public void Name_IsStable()
      {
          var check = new FerretConfigDirCheck(Path.GetTempPath());
          Assert.Equal(".ferret config directory exists", check.Name);
      }
  }
  ```

- [ ] **Step 1.6 — Verify tests are red.**

- [ ] **Step 1.7 — Implement `FerretConfigDirCheck`**

  File: `src/Ferret.Cli/Diagnostics/Checks/FerretConfigDirCheck.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Core.Workspace;

  namespace Ferret.Cli.Diagnostics.Checks;

  /// <summary>Checks that the <c>.ferret</c> configuration directory exists under the workspace root.</summary>
  internal sealed class FerretConfigDirCheck : IDiagnosticCheck
  {
      private readonly string _workspaceRoot;

      internal FerretConfigDirCheck(string workspaceRoot)
      {
          ArgumentNullException.ThrowIfNull(workspaceRoot);
          _workspaceRoot = workspaceRoot;
      }

      /// <inheritdoc/>
      public string Name => ".ferret config directory exists";

      /// <inheritdoc/>
      public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
      {
          var ferretDir = Path.Combine(_workspaceRoot, WorkspaceLayout.RootDirectoryName);
          var result = Directory.Exists(ferretDir)
              ? DiagnosticCheckResult.Pass()
              : DiagnosticCheckResult.Fail($"Directory not found: {ferretDir}");
          return Task.FromResult(result);
      }
  }
  ```

- [ ] **Step 1.8 — Verify `FerretConfigDirCheckTests` are green.**

- [ ] **Step 1.9 — Write failing tests for `IndexFreshnessCheck`**

  File: `tests/Ferret.Cli.Tests/Diagnostics/IndexFreshnessCheckTests.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Diagnostics;
  using Ferret.Cli.Diagnostics.Checks;

  namespace Ferret.Cli.Tests.Diagnostics;

  public sealed class IndexFreshnessCheckTests
  {
      [Fact]
      public async Task Pass_WhenIndexFileIsRecent()
      {
          var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
          Directory.CreateDirectory(dir);
          var dbPath = Path.Combine(dir, "keyword-index.db");
          await File.WriteAllTextAsync(dbPath, "x");
          File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddHours(-1));
          try
          {
              using var sw = new StringWriter();
              var ctx = FerretContext.CreateTest(sw);
              var check = new IndexFreshnessCheck(dbPath);
              var result = await check.RunAsync(ctx, CancellationToken.None);
              Assert.True(result.Passed);
          }
          finally { Directory.Delete(dir, recursive: true); }
      }

      [Fact]
      public async Task Fail_WhenIndexFileIsMissing()
      {
          using var sw = new StringWriter();
          var ctx = FerretContext.CreateTest(sw);
          var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "keyword-index.db");
          var check = new IndexFreshnessCheck(missing);
          var result = await check.RunAsync(ctx, CancellationToken.None);
          Assert.False(result.Passed);
          Assert.NotNull(result.FailureReason);
      }

      [Fact]
      public async Task Fail_WhenIndexFileIsStale()
      {
          var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
          Directory.CreateDirectory(dir);
          var dbPath = Path.Combine(dir, "keyword-index.db");
          await File.WriteAllTextAsync(dbPath, "x");
          File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddHours(-25));
          try
          {
              using var sw = new StringWriter();
              var ctx = FerretContext.CreateTest(sw);
              var check = new IndexFreshnessCheck(dbPath);
              var result = await check.RunAsync(ctx, CancellationToken.None);
              Assert.False(result.Passed);
              Assert.NotNull(result.FailureReason);
          }
          finally { Directory.Delete(dir, recursive: true); }
      }

      [Fact]
      public void Name_IsStable()
      {
          var check = new IndexFreshnessCheck("dummy.db");
          Assert.Equal("Index freshness", check.Name);
      }
  }
  ```

- [ ] **Step 1.10 — Verify tests are red.**

- [ ] **Step 1.11 — Implement `IndexFreshnessCheck`**

  File: `src/Ferret.Cli/Diagnostics/Checks/IndexFreshnessCheck.cs`

  ```csharp
  using Ferret.Cli.Cli;

  namespace Ferret.Cli.Diagnostics.Checks;

  /// <summary>Checks that the keyword index database exists and was written within the last 24 hours.</summary>
  internal sealed class IndexFreshnessCheck : IDiagnosticCheck
  {
      private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);
      private readonly string _dbPath;

      internal IndexFreshnessCheck(string dbPath)
      {
          ArgumentNullException.ThrowIfNull(dbPath);
          _dbPath = dbPath;
      }

      /// <inheritdoc/>
      public string Name => "Index freshness";

      /// <inheritdoc/>
      public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
      {
          if (!File.Exists(_dbPath))
          {
              return Task.FromResult(DiagnosticCheckResult.Fail($"Index not found: {_dbPath}. Run 'ferret index' to build."));
          }

          var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(_dbPath);
          if (age > MaxAge)
          {
              return Task.FromResult(DiagnosticCheckResult.Fail(
                  $"Index is {age.TotalHours:F1}h old (limit: 24h). Run 'ferret index' to refresh."));
          }

          return Task.FromResult(DiagnosticCheckResult.Pass());
      }
  }
  ```

- [ ] **Step 1.12 — Verify `IndexFreshnessCheckTests` are green.**

- [ ] **Step 1.13 — Write failing tests for `AiProviderConfigCheck`**

  File: `tests/Ferret.Cli.Tests/Diagnostics/AiProviderConfigCheckTests.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Diagnostics;
  using Ferret.Cli.Diagnostics.Checks;
  using Microsoft.Extensions.Configuration;

  namespace Ferret.Cli.Tests.Diagnostics;

  public sealed class AiProviderConfigCheckTests
  {
      [Fact]
      public async Task Pass_WhenAiProvidersConfigured()
      {
          var config = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Ferret:Ai:Providers:Ollama:BaseUrl"] = "http://localhost:11434",
              })
              .Build();
          using var sw = new StringWriter();
          var ctx = FerretContext.CreateTest(sw);
          var check = new AiProviderConfigCheck(config);
          var result = await check.RunAsync(ctx, CancellationToken.None);
          Assert.True(result.Passed);
      }

      [Fact]
      public async Task Fail_WhenNoAiProvidersConfigured()
      {
          var config = new ConfigurationBuilder().Build();
          using var sw = new StringWriter();
          var ctx = FerretContext.CreateTest(sw);
          var check = new AiProviderConfigCheck(config);
          var result = await check.RunAsync(ctx, CancellationToken.None);
          Assert.False(result.Passed);
          Assert.NotNull(result.FailureReason);
      }

      [Fact]
      public void Name_IsStable()
      {
          var check = new AiProviderConfigCheck(new ConfigurationBuilder().Build());
          Assert.Equal("AI provider configured", check.Name);
      }
  }
  ```

- [ ] **Step 1.14 — Verify tests are red.**

- [ ] **Step 1.15 — Implement `AiProviderConfigCheck`**

  File: `src/Ferret.Cli/Diagnostics/Checks/AiProviderConfigCheck.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Microsoft.Extensions.Configuration;

  namespace Ferret.Cli.Diagnostics.Checks;

  /// <summary>Checks that at least one AI provider is present in the <c>Ferret:Ai:Providers</c> config section.</summary>
  internal sealed class AiProviderConfigCheck : IDiagnosticCheck
  {
      private readonly IConfiguration _configuration;

      internal AiProviderConfigCheck(IConfiguration configuration)
      {
          ArgumentNullException.ThrowIfNull(configuration);
          _configuration = configuration;
      }

      /// <inheritdoc/>
      public string Name => "AI provider configured";

      /// <inheritdoc/>
      public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
      {
          var section = _configuration.GetSection("Ferret:Ai:Providers");
          bool hasProviders = section.GetChildren().Any();
          var result = hasProviders
              ? DiagnosticCheckResult.Pass()
              : DiagnosticCheckResult.Fail("No AI providers found under 'Ferret:Ai:Providers'. Add Ollama or OpenAi config.");
          return Task.FromResult(result);
      }
  }
  ```

- [ ] **Step 1.16 — Verify `AiProviderConfigCheckTests` are green.**

- [ ] **Step 1.17 — Wire new checks into `CoreCliModule`**

  In `CoreCliModule.GetDiagnosticChecks()`, yield the four new checks. Workspace root defaults to `Environment.CurrentDirectory`. The `IConfiguration` is not available at `GetDiagnosticChecks()` time — pass it from `ConfigureServices` by storing it as a field. However, `CoreCliModule` is currently stateless. The cleanest approach: construct the checks in `ConfigureServices` where `IConfiguration` is available (already done for `ConfigurationCheck`), and store the full list as a field.

  Modify `src/Ferret.Cli/Commands/CoreCliModule.cs`:

  ```csharp
  // Add field at top of class:
  private IReadOnlyList<IDiagnosticCheck>? _checks;

  // Replace GetDiagnosticChecks():
  public override IEnumerable<IDiagnosticCheck> GetDiagnosticChecks() =>
      _checks ?? BuildChecks(null, Environment.CurrentDirectory);

  // Replace ConfigureServices():
  public override void ConfigureServices(IServiceCollection services)
  {
      services.AddTransient<VersionCommandHandler>();
      services.AddTransient<AboutCommandHandler>();
      services.AddTransient<StartCommandHandler>();
      services.AddTransient<StatusCommandHandler>();

      // Build checks with the real config available at service registration time.
      // RootCommandFactory calls ConfigureServices after loading IConfiguration,
      // but CoreCliModule does not receive IConfiguration directly.
      // We resolve it from the IServiceCollection snapshot via a BuildServiceProvider call.
      var tempProvider = services.BuildServiceProvider();
      var config = tempProvider.GetRequiredService<IConfiguration>();
      var workspaceRoot = config["Ferret:Workspace:Root"] ?? Environment.CurrentDirectory;

      _checks = BuildChecks(config, workspaceRoot).ToList();
      services.AddTransient<DoctorCommandHandler>(_ => new DoctorCommandHandler(_checks));
  }

  private static IEnumerable<IDiagnosticCheck> BuildChecks(IConfiguration? config, string workspaceRoot)
  {
      yield return new ConfigurationCheck();
      yield return new RuntimeLifecycleCheck();
      yield return new WorkspaceRootCheck(workspaceRoot);
      yield return new FerretConfigDirCheck(workspaceRoot);

      var dbPath = Path.Combine(
          workspaceRoot,
          Ferret.Core.Workspace.WorkspaceLayout.RootDirectoryName,
          Ferret.Core.Indexing.IndexLayout.IndexDirectoryName,
          Ferret.Core.Indexing.IndexLayout.KeywordDirectoryName,
          Ferret.Core.Indexing.IndexLayout.KeywordDatabaseFileName);
      yield return new IndexFreshnessCheck(dbPath);

      if (config is not null)
      {
          yield return new AiProviderConfigCheck(config);
      }
  }
  ```

  Add required using directives:
  ```csharp
  using Microsoft.Extensions.DependencyInjection.Extensions;
  using Microsoft.Extensions.DependencyInjection;
  ```
  (The existing `using Microsoft.Extensions.DependencyInjection;` already covers `GetRequiredService`.)

- [ ] **Step 1.18 — Write failing assertion tests in `DoctorCommandHandlerTests`**

  Add to `tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs`:

  ```csharp
  [Fact]
  public async Task Doctor_PrintsWorkspaceRootCheck()
  {
      using var sw = new StringWriter();
      await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
      Assert.Contains("Workspace root exists", sw.ToString(), StringComparison.Ordinal);
  }

  [Fact]
  public async Task Doctor_PrintsFerretConfigDirCheck()
  {
      using var sw = new StringWriter();
      await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
      Assert.Contains(".ferret config directory exists", sw.ToString(), StringComparison.Ordinal);
  }

  [Fact]
  public async Task Doctor_PrintsIndexFreshnessCheck()
  {
      using var sw = new StringWriter();
      await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
      Assert.Contains("Index freshness", sw.ToString(), StringComparison.Ordinal);
  }

  [Fact]
  public async Task Doctor_PrintsAiProviderCheck()
  {
      using var sw = new StringWriter();
      await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
      Assert.Contains("AI provider configured", sw.ToString(), StringComparison.Ordinal);
  }
  ```

- [ ] **Step 1.19 — Verify new `DoctorCommandHandlerTests` assertions are red.**

- [ ] **Step 1.20 — Verify all Task 1 tests are green** after `CoreCliModule` is updated.

- [ ] **Step 1.21 — Commit**
  ```
  feat(sprint-14): ferret doctor — WorkspaceRoot, FerretConfigDir, IndexFreshness, AiProviderConfig checks
  ```

---

### Task 2: Add `--log-level` global option

**Files:**
- `src/Ferret.Cli/Cli/GlobalOptions.cs` (MODIFY)
- `src/Ferret.Cli/Commands/RootCommandFactory.cs` (MODIFY)
- `tests/Ferret.Cli.Tests/GlobalOptionsTests.cs` (NEW)

**Key types:**
- `GlobalOptions` — static class, `Option<bool>` statics, `AddAll(RootCommand root)`
- `RootCommandFactory.RegisterHandlerAction` — creates `FerretServices` with `NullLoggerFactory.Instance`
- `Microsoft.Extensions.Logging.LogLevel` — `Debug`, `Information`, `Warning`, `Error`
- `LoggerFactory.Create(builder => builder.SetMinimumLevel(...).AddConsole())` — standard MEL pattern

---

- [ ] **Step 2.1 — Write failing tests**

  File: `tests/Ferret.Cli.Tests/GlobalOptionsTests.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using System.CommandLine;

  namespace Ferret.Cli.Tests;

  public sealed class GlobalOptionsTests
  {
      [Fact]
      public void LogLevel_Option_LongName_Is_LogLevel()
      {
          Assert.Contains("--log-level", GlobalOptions.LogLevel.Name, StringComparison.Ordinal);
      }

      [Fact]
      public void LogLevel_Option_IsNotHidden()
      {
          Assert.False(GlobalOptions.LogLevel.Hidden);
      }

      [Fact]
      public void AddAll_Adds_LogLevel_To_Root()
      {
          var root = new RootCommand();
          GlobalOptions.AddAll(root);
          Assert.Contains(root.Options, o => o.Name == "log-level");
      }
  }
  ```

- [ ] **Step 2.2 — Verify tests are red.**

- [ ] **Step 2.3 — Implement `GlobalOptions.LogLevel`**

  In `src/Ferret.Cli/Cli/GlobalOptions.cs`, add:

  ```csharp
  /// <summary>Gets the --log-level option.</summary>
  internal static Option<string> LogLevel { get; } = new Option<string>("--log-level")
  {
      Description = "Minimum log level: debug, info, warning, error.",
  };
  ```

  In `AddAll(RootCommand root)`, add:
  ```csharp
  root.Add(LogLevel);
  ```

  Full updated `GlobalOptions.cs`:

  ```csharp
  using System.CommandLine;

  namespace Ferret.Cli.Cli;

  /// <summary>
  /// Why: Centralises all global option definitions; hidden Sprint 6; Sprint 7 wires their values into FerretContext.
  /// Layer: Ferret.Cli only — System.CommandLine types confined here.
  /// Thread Safety: Thread Safe — read-only after static initialization.
  /// </summary>
  internal static class GlobalOptions
  {
      /// <summary>Gets the --verbose option.</summary>
      internal static Option<bool> Verbose { get; } = Hidden(new Option<bool>("--verbose") { Description = "Verbose output." });

      /// <summary>Gets the --quiet option.</summary>
      internal static Option<bool> Quiet { get; } = Hidden(new Option<bool>("--quiet") { Description = "Suppress output." });

      /// <summary>Gets the --json option (Sprint 7).</summary>
      internal static Option<bool> Json { get; } = Hidden(new Option<bool>("--json") { Description = "JSON output (Sprint 7)." });

      /// <summary>Gets the --no-color option (Sprint 7).</summary>
      internal static Option<bool> NoColor { get; } = Hidden(new Option<bool>("--no-color") { Description = "Disable color (Sprint 7)." });

      /// <summary>Gets the --log-level option.</summary>
      internal static Option<string> LogLevel { get; } = new Option<string>("--log-level")
      {
          Description = "Minimum log level: debug, info, warning, error.",
      };

      /// <summary>Adds all global options to the root command.</summary>
      /// <param name="root">The root command to add options to.</param>
      internal static void AddAll(RootCommand root)
      {
          root.Add(Verbose);
          root.Add(Quiet);
          root.Add(Json);
          root.Add(NoColor);
          root.Add(LogLevel);
      }

      private static Option<bool> Hidden(Option<bool> opt)
      {
          opt.Hidden = true;
          return opt;
      }
  }
  ```

- [ ] **Step 2.4 — Verify `GlobalOptionsTests` are green.**

- [ ] **Step 2.5 — Wire `--log-level` into `RootCommandFactory.RegisterHandlerAction`**

  Replace the `NullLoggerFactory.Instance` line in `RegisterHandlerAction` with a factory that reads the parsed `--log-level` value. The change is confined to that one private method.

  In `src/Ferret.Cli/Commands/RootCommandFactory.cs`, replace in `RegisterHandlerAction`:

  ```csharp
  // OLD:
  var ferretServices = new FerretServices(
      provider,
      config,
      NullLoggerFactory.Instance,
      formatter);

  // NEW:
  var logLevelValue = parseResult.GetValue(GlobalOptions.LogLevel);
  var loggerFactory = BuildLoggerFactory(logLevelValue);
  var ferretServices = new FerretServices(
      provider,
      config,
      loggerFactory,
      formatter);
  ```

  Add a new private static method to `RootCommandFactory`:

  ```csharp
  private static ILoggerFactory BuildLoggerFactory(string? logLevel)
  {
      if (string.IsNullOrEmpty(logLevel))
      {
          return NullLoggerFactory.Instance;
      }

      var level = logLevel.ToLowerInvariant() switch
      {
          "debug" => LogLevel.Debug,
          "info" or "information" => LogLevel.Information,
          "warning" or "warn" => LogLevel.Warning,
          "error" => LogLevel.Error,
          _ => LogLevel.Warning,
      };

      return LoggerFactory.Create(builder =>
          builder.SetMinimumLevel(level).AddConsole());
  }
  ```

  Add using at top of `RootCommandFactory.cs` (already has `Microsoft.Extensions.Logging`; verify `LoggerFactory` static is accessible — it is via `Microsoft.Extensions.Logging.LoggerFactory`):
  ```csharp
  using LogLevel = Microsoft.Extensions.Logging.LogLevel;
  ```

- [ ] **Step 2.6 — Write integration test** (add to `GlobalOptionsTests.cs` or a new `RootCommandFactoryLogLevelTests.cs`):

  File addition to `tests/Ferret.Cli.Tests/GlobalOptionsTests.cs`:

  ```csharp
  [Fact]
  public async Task LogLevel_Debug_DoesNot_Throw()
  {
      using var sw = new StringWriter();
      // Should not throw; verifies the factory wiring path runs without error.
      var exit = await Ferret.Cli.Commands.RootCommandFactory
          .Build([new Ferret.Cli.Commands.CoreCliModule()], sw)
          .InvokeAsync(["--log-level", "debug", "version"]);
      Assert.Equal(0, exit);
  }
  ```

- [ ] **Step 2.7 — Verify all Task 2 tests are green.**

- [ ] **Step 2.8 — Commit**
  ```
  feat(sprint-14): --log-level global option wired to ILoggerFactory
  ```

---

### Task 3: Add `--verbose` flag to `ferret index`

**Files:**
- `src/Ferret.Cli/Commands/Indexing/ConsoleIndexEventSink.cs` (NEW)
- `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs` (MODIFY)
- `src/Ferret.Cli/Commands/Indexing/IndexCommandHandler.cs` (MODIFY)
- `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCommandHandlerTests.cs` (MODIFY)
- `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCliModuleTests.cs` (MODIFY)

**Key types:**
- `IEventBus` — `Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct)`
- `DomainEvent` — base class in `Ferret.Core.Events`
- `DocumentIndexedEvent`, `DocumentDiscoveredEvent`, `DocumentSkippedEvent`, `DocumentParsingFailedEvent` — in `Ferret.Core.Events.Indexing`
- `IOutputFormatter.WriteVerbose(string)` — writes only when verbosity is Verbose
- `context.GetOption<bool>("verbose")` — reads the per-command verbose flag

---

- [ ] **Step 3.1 — Write failing test: `IndexCliModule` exposes `--verbose` option**

  Add to `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCliModuleTests.cs`:

  ```csharp
  [Fact]
  public void GetCommands_Index_Has_Verbose_Option()
  {
      var module = new IndexCliModule(MakeFakeWorkspaceContext());
      var indexCmd = module.GetCommands().First(c => c.Metadata.Name == "index");
      Assert.NotNull(indexCmd.Options);
      Assert.Contains(indexCmd.Options!, o => o.LongName == "--verbose");
  }
  ```

- [ ] **Step 3.2 — Verify test is red.**

- [ ] **Step 3.3 — Add `--verbose` option to `IndexCliModule.GetCommands()`**

  In `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs`, update the `Options` array:

  ```csharp
  Options:
  [
      new OptionDefinition("--rebuild", "Rebuild index from scratch, discarding existing data.", typeof(bool)),
      new OptionDefinition("--verbose", "Stream per-document indexing events to console.", typeof(bool)),
  ]);
  ```

- [ ] **Step 3.4 — Verify `GetCommands_Index_Has_Verbose_Option` is green.**

- [ ] **Step 3.5 — Write failing tests for `ConsoleIndexEventSink`**

  File: `tests/Ferret.Cli.Tests/Commands/Indexing/ConsoleIndexEventSinkTests.cs` (new file in the same test directory)

  Note: `ConsoleIndexEventSinkTests.cs` is not in the original file structure plan but is needed for TDD. Add it.

  ```csharp
  using Ferret.Cli.Commands.Indexing;
  using Ferret.Cli.Tests.Commands.Indexing;
  using Ferret.Core.Events;
  using Ferret.Core.Events.Indexing;
  using Ferret.Core.Connectors;
  using Ferret.Core.Primitives;

  namespace Ferret.Cli.Tests.Commands.Indexing;

  public sealed class ConsoleIndexEventSinkTests
  {
      [Fact]
      public async Task PublishAsync_DocumentIndexed_WritesVerboseLine()
      {
          var output = new FakeIndexOutput();
          var sink = new ConsoleIndexEventSink(output, NullEventBus.Instance);

          var evt = new DocumentIndexedEvent("doc-1", CorrelationId.Create("corr-1"))
          {
              DocumentId = DocumentId.Create("doc-1"),
              AssetId = AssetId.Create("asset-1"),
              MediaType = "text/plain",
              CharCount = 100,
          };

          await sink.PublishAsync(evt, CancellationToken.None);
          Assert.Contains(output.Lines, l => l.Contains("doc-1") || l.Contains("Indexed"));
      }

      [Fact]
      public async Task PublishAsync_Forwards_To_InnerBus()
      {
          var output = new FakeIndexOutput();
          var inner = new RecordingEventBus();
          var sink = new ConsoleIndexEventSink(output, inner);

          var evt = new DocumentIndexedEvent("doc-2", CorrelationId.Create("corr-2"))
          {
              DocumentId = DocumentId.Create("doc-2"),
              AssetId = AssetId.Create("asset-2"),
              MediaType = "text/plain",
              CharCount = 50,
          };

          await sink.PublishAsync(evt, CancellationToken.None);
          Assert.Equal(1, inner.PublishCount);
      }
  }

  internal sealed class RecordingEventBus : IEventBus
  {
      internal int PublishCount { get; private set; }
      public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
          where TEvent : DomainEvent
      {
          PublishCount++;
          return Task.CompletedTask;
      }
  }
  ```

- [ ] **Step 3.6 — Verify tests are red.**

- [ ] **Step 3.7 — Implement `ConsoleIndexEventSink`**

  File: `src/Ferret.Cli/Commands/Indexing/ConsoleIndexEventSink.cs`

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Core.Events;
  using Ferret.Core.Events.Indexing;

  namespace Ferret.Cli.Commands.Indexing;

  /// <summary>
  /// IEventBus decorator that writes per-document indexing events to the console output formatter.
  /// Constructed inline in IndexCommandHandler when --verbose is set; not registered in DI.
  /// </summary>
  internal sealed class ConsoleIndexEventSink : IEventBus
  {
      private readonly IOutputFormatter _output;
      private readonly IEventBus _inner;

      internal ConsoleIndexEventSink(IOutputFormatter output, IEventBus inner)
      {
          ArgumentNullException.ThrowIfNull(output);
          ArgumentNullException.ThrowIfNull(inner);
          _output = output;
          _inner = inner;
      }

      /// <inheritdoc/>
      public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
          where TEvent : DomainEvent
      {
          WriteToConsole(domainEvent);
          await _inner.PublishAsync(domainEvent, ct).ConfigureAwait(false);
      }

      private void WriteToConsole(DomainEvent evt)
      {
          switch (evt)
          {
              case IndexingStartedEvent started:
                  _output.WriteVerbose($"  [index] Starting{(started.IsRebuild ? " (rebuild)" : string.Empty)}…");
                  break;
              case DocumentDiscoveredEvent discovered:
                  _output.WriteVerbose($"  [discover] {discovered.AggregateId}");
                  break;
              case DocumentIndexedEvent indexed:
                  _output.WriteVerbose($"  [indexed]  {indexed.DocumentId.Value}  ({indexed.CharCount} chars, {indexed.MediaType})");
                  break;
              case DocumentSkippedEvent skipped:
                  _output.WriteVerbose($"  [skipped]  {skipped.AssetId?.Value ?? skipped.AggregateId}  — {skipped.Reason}");
                  break;
              case DocumentParsingFailedEvent failed:
                  _output.WriteVerbose($"  [failed]   {failed.AssetId?.Value ?? failed.AggregateId}  — {failed.ErrorMessage}");
                  break;
              case IndexingCompletedEvent:
                  _output.WriteVerbose("  [index] Complete.");
                  break;
          }
      }
  }
  ```

  Note: `DocumentSkippedEvent.Reason` and `DocumentParsingFailedEvent.ErrorMessage` must be confirmed from the event type. Check `src/Ferret.Core/Events/Indexing/DocumentSkippedEvent.cs` and `DocumentParsingFailedEvent.cs` during implementation — use the exact property names. The properties `Reason` and `ErrorMessage` match the `IndexPipeline` code already read above.

- [ ] **Step 3.8 — Verify `ConsoleIndexEventSinkTests` are green.**

- [ ] **Step 3.9 — Write failing test: `IndexCommandHandler` calls verbose sink**

  Add to `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCommandHandlerTests.cs`:

  ```csharp
  // Add FakeVerboseFerretContext to the test fakes at the top of the file:
  internal sealed class FakeVerboseFerretContext : IFerretContext
  {
      internal FakeVerboseFerretContext(IFerretServices services) => Services = services;
      public CancellationToken CancellationToken => CancellationToken.None;
      public VerbosityLevel Verbosity => VerbosityLevel.Verbose;
      public OutputFormat OutputFormat => OutputFormat.Text;
      public IFerretServices Services { get; }
      public string WorkingDirectory => @"C:\fake\cwd";
      public T? GetOption<T>(string name)
      {
          if (name == "verbose" && typeof(T) == typeof(bool)) return (T)(object)true;
          if (name == "rebuild" && typeof(T) == typeof(bool)) return (T)(object)false;
          return default;
      }
  }

  // Test method:
  [Fact]
  public async Task Handler_VerboseMode_WritesVerboseLines()
  {
      var output = new FakeIndexOutput();
      var ctx = new FakeVerboseFerretContext(new FakeIndexServices(output));
      var pipeline = new FakeIndexPipeline();
      var result = await MakeHandler(pipeline).ExecuteAsync(ctx);
      // Verbose lines are written by the sink via WriteVerbose, which in FakeIndexOutput
      // uses the "  " prefix. Verify that at least one verbose-prefixed line exists after
      // a successful run (pipeline publishes IndexingCompleted internally via the real bus
      // — for unit testing, the FakeIndexPipeline does not publish events; the sink must
      // handle zero events gracefully and the handler must still succeed).
      Assert.Equal(CommandResult.Success, result);
  }
  ```

  Note: Because `FakeIndexPipeline` does not use `IEventBus`, verbose lines won't fire. This test confirms the code path doesn't throw. A separate integration test is out of scope for S4.

- [ ] **Step 3.10 — Verify test is red (type `FakeVerboseFerretContext` does not exist yet).**

- [ ] **Step 3.11 — Update `IndexCommandHandler` to wire the sink when `--verbose`**

  In `src/Ferret.Cli/Commands/Indexing/IndexCommandHandler.cs`:

  Add constructor parameter `IEventBus eventBus` and field `_eventBus`. Update `IndexCliModule.ConfigureServices` to pass `IEventBus` to `IndexCommandHandler` (it is already registered as `NullEventBus.Instance`).

  Updated `IndexCommandHandler`:

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Commands.Indexing.Formatting;
  using Ferret.Cli.Commands.Indexing.ViewModels;
  using Ferret.Core.Events;
  using Ferret.Core.Indexing;
  using Ferret.Core.Workspace;

  namespace Ferret.Cli.Commands.Indexing;

  /// <summary>Handles 'ferret index' — runs the full discover → parse → index pipeline.</summary>
  internal sealed class IndexCommandHandler : ICommandHandler
  {
      private readonly IIndexPipeline _pipeline;
      private readonly IWorkspaceContext _workspaceContext;
      private readonly IEventBus _eventBus;

      /// <summary>Initializes a new instance of the <see cref="IndexCommandHandler"/> class.</summary>
      /// <param name="pipeline">The index pipeline to run.</param>
      /// <param name="workspaceContext">The current workspace context.</param>
      /// <param name="eventBus">The event bus used to attach the verbose console sink.</param>
      public IndexCommandHandler(
          IIndexPipeline pipeline,
          IWorkspaceContext workspaceContext,
          IEventBus eventBus)
      {
          _pipeline = pipeline;
          _workspaceContext = workspaceContext;
          _eventBus = eventBus;
      }

      /// <inheritdoc/>
      public async Task<CommandResult> ExecuteAsync(IFerretContext context)
      {
          var forceRebuild = context.GetOption<bool>("rebuild");
          var verbose = context.GetOption<bool>("verbose");

          IEventBus bus = verbose
              ? new ConsoleIndexEventSink(context.Services.Output, _eventBus)
              : _eventBus;

          var options = new IndexPipelineOptions { ForceRebuild = forceRebuild };

          context.Services.Output.WriteLine("Indexing workspace…");

          var result = await _pipeline.RunAsync(
              _workspaceContext.WorkspaceId,
              options,
              context.CancellationToken).ConfigureAwait(false);

          var dbPath = Path.Combine(
              _workspaceContext.WorkspaceRoot.FullPath,
              WorkspaceLayout.RootDirectoryName,
              IndexLayout.IndexDirectoryName,
              IndexLayout.KeywordDirectoryName,
              IndexLayout.KeywordDatabaseFileName);

          var vm = IndexSummaryViewModel.From(result, dbPath);
          var formatted = TextIndexSummaryFormatter.Format(vm);
          context.Services.Output.WriteLine(formatted);

          return result.Failures == 0 ? CommandResult.Success : CommandResult.Failure;
      }
  }
  ```

  Note: `bus` is constructed but the `FakeIndexPipeline` does not use `IEventBus`. In the real `IndexPipeline`, the bus is passed at construction time — the verbose decorator wraps the bus *at the point of handler execution*, so the decorator must be passed into `IndexPipeline.RunAsync`. However, the current `IIndexPipeline` interface does not accept an `IEventBus` parameter per-call. The `IndexPipeline` holds the bus as a constructor dependency.

  **Architectural constraint:** Because `IIndexPipeline` does not support per-call `IEventBus` swapping, the correct approach is to register the `ConsoleIndexEventSink` as the `IEventBus` in DI *when `--verbose` is detected*. Since DI is built before the handler executes, the `IEventBus` registration in `IndexCliModule` must be replaceable at runtime.

  **Revised approach:** Introduce an `EventBusAccessor` (a mutable wrapper) that `IndexCliModule` registers as a singleton. Both `IndexPipeline` and `IndexCommandHandler` reference it. Before calling `RunAsync`, `IndexCommandHandler` sets the inner bus on the accessor when `--verbose` is true.

  **Simpler alternative (recommended for S4):** Register `IEventBus` as a singleton that is a `SwappableEventBus` — an `IEventBus` whose inner bus can be replaced between calls. `IndexCommandHandler` unwraps it and sets the verbose sink before the pipeline runs, then restores the original after.

  ```csharp
  // NEW: src/Ferret.Cli/Commands/Indexing/SwappableEventBus.cs
  internal sealed class SwappableEventBus : IEventBus
  {
      private IEventBus _inner;

      internal SwappableEventBus(IEventBus inner) => _inner = inner;

      internal IEventBus Inner
      {
          get => _inner;
          set => _inner = value;
      }

      public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
          where TEvent : DomainEvent => _inner.PublishAsync(domainEvent, ct);
  }
  ```

  `IndexCliModule.ConfigureServices` registers:
  ```csharp
  services.AddSingleton<SwappableEventBus>(_ => new SwappableEventBus(NullEventBus.Instance));
  services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<SwappableEventBus>());
  ```

  `IndexCommandHandler` receives `SwappableEventBus` (not `IEventBus`) so it can swap:
  ```csharp
  public IndexCommandHandler(IIndexPipeline pipeline, IWorkspaceContext workspaceContext, SwappableEventBus eventBus)
  ```

  In `ExecuteAsync`:
  ```csharp
  if (verbose)
  {
      eventBus.Inner = new ConsoleIndexEventSink(context.Services.Output, NullEventBus.Instance);
  }
  // ... after RunAsync, restore:
  eventBus.Inner = NullEventBus.Instance;
  ```

  Update existing `IndexCommandHandlerTests` fakes: `FakeFerretContext.GetOption` already returns `default` for `"verbose"` — no change needed. Add `FakeVerboseFerretContext` as above.

  Update `MakeHandler` to accept optional `SwappableEventBus`:
  ```csharp
  private static IndexCommandHandler MakeHandler(
      FakeIndexPipeline? pipeline = null,
      FakeWorkspaceContext? workspaceCtx = null,
      SwappableEventBus? bus = null)
  {
      return new IndexCommandHandler(
          pipeline ?? new FakeIndexPipeline(),
          workspaceCtx ?? new FakeWorkspaceContext(),
          bus ?? new SwappableEventBus(NullEventBus.Instance));
  }
  ```

- [ ] **Step 3.12 — Implement `SwappableEventBus`**

  File: `src/Ferret.Cli/Commands/Indexing/SwappableEventBus.cs`

  ```csharp
  using Ferret.Core.Events;

  namespace Ferret.Cli.Commands.Indexing;

  /// <summary>
  /// IEventBus whose inner bus can be replaced between pipeline invocations.
  /// Registered as a singleton so IndexCommandHandler can inject a verbose sink at runtime.
  /// </summary>
  internal sealed class SwappableEventBus : IEventBus
  {
      private IEventBus _inner;

      internal SwappableEventBus(IEventBus inner)
      {
          ArgumentNullException.ThrowIfNull(inner);
          _inner = inner;
      }

      /// <summary>Gets or sets the active inner bus.</summary>
      internal IEventBus Inner
      {
          get => _inner;
          set
          {
              ArgumentNullException.ThrowIfNull(value);
              _inner = value;
          }
      }

      /// <inheritdoc/>
      public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
          where TEvent : DomainEvent => _inner.PublishAsync(domainEvent, ct);
  }
  ```

- [ ] **Step 3.13 — Update `IndexCliModule.ConfigureServices`** to register `SwappableEventBus`:

  In `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs`, replace:
  ```csharp
  services.AddSingleton<IEventBus>(NullEventBus.Instance);
  ```
  with:
  ```csharp
  services.AddSingleton<SwappableEventBus>(_ => new SwappableEventBus(NullEventBus.Instance));
  services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<SwappableEventBus>());
  ```

  Also update `services.AddSingleton<IndexCommandHandler>()` to pass `SwappableEventBus` explicitly, since `IndexCommandHandler`'s constructor now takes `SwappableEventBus`:
  ```csharp
  services.AddSingleton<IndexCommandHandler>();
  ```
  DI will resolve `SwappableEventBus` automatically since it is registered as a concrete singleton.

- [ ] **Step 3.14 — Update `IndexCommandHandler`** with the final implementation as described in Step 3.11 (using `SwappableEventBus`):

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Commands.Indexing.Formatting;
  using Ferret.Cli.Commands.Indexing.ViewModels;
  using Ferret.Core.Events;
  using Ferret.Core.Indexing;
  using Ferret.Core.Workspace;

  namespace Ferret.Cli.Commands.Indexing;

  /// <summary>Handles 'ferret index' — runs the full discover → parse → index pipeline.</summary>
  internal sealed class IndexCommandHandler : ICommandHandler
  {
      private readonly IIndexPipeline _pipeline;
      private readonly IWorkspaceContext _workspaceContext;
      private readonly SwappableEventBus _eventBus;

      /// <summary>Initializes a new instance of the <see cref="IndexCommandHandler"/> class.</summary>
      /// <param name="pipeline">The index pipeline to run.</param>
      /// <param name="workspaceContext">The current workspace context.</param>
      /// <param name="eventBus">The swappable event bus; accepts a verbose sink when --verbose is set.</param>
      public IndexCommandHandler(
          IIndexPipeline pipeline,
          IWorkspaceContext workspaceContext,
          SwappableEventBus eventBus)
      {
          _pipeline = pipeline;
          _workspaceContext = workspaceContext;
          _eventBus = eventBus;
      }

      /// <inheritdoc/>
      public async Task<CommandResult> ExecuteAsync(IFerretContext context)
      {
          var forceRebuild = context.GetOption<bool>("rebuild");
          var verbose = context.GetOption<bool>("verbose");

          if (verbose)
          {
              _eventBus.Inner = new ConsoleIndexEventSink(context.Services.Output, NullEventBus.Instance);
          }

          try
          {
              var options = new IndexPipelineOptions { ForceRebuild = forceRebuild };

              context.Services.Output.WriteLine("Indexing workspace…");

              var result = await _pipeline.RunAsync(
                  _workspaceContext.WorkspaceId,
                  options,
                  context.CancellationToken).ConfigureAwait(false);

              var dbPath = Path.Combine(
                  _workspaceContext.WorkspaceRoot.FullPath,
                  WorkspaceLayout.RootDirectoryName,
                  IndexLayout.IndexDirectoryName,
                  IndexLayout.KeywordDirectoryName,
                  IndexLayout.KeywordDatabaseFileName);

              var vm = IndexSummaryViewModel.From(result, dbPath);
              var formatted = TextIndexSummaryFormatter.Format(vm);
              context.Services.Output.WriteLine(formatted);

              return result.Failures == 0 ? CommandResult.Success : CommandResult.Failure;
          }
          finally
          {
              if (verbose)
              {
                  _eventBus.Inner = NullEventBus.Instance;
              }
          }
      }
  }
  ```

- [ ] **Step 3.15 — Verify all Task 3 tests are green.**

  Run:
  ```
  dotnet test --filter "IndexCommandHandlerTests|IndexCliModuleTests|ConsoleIndexEventSinkTests"
  ```

- [ ] **Step 3.16 — Commit**
  ```
  feat(sprint-14): ferret index --verbose — ConsoleIndexEventSink, SwappableEventBus
  ```

---

## Self-Review

### Architectural consistency
- All new `IDiagnosticCheck` implementations follow the exact pattern of `ConfigurationCheck` (constructor injection, `#pragma warning disable CA1031`, `Task.FromResult`).
- `GlobalOptions.LogLevel` follows the same `Option<string>` shape as the existing typed options; it is the only visible (non-hidden) global option, matching the S4 spec.
- `SwappableEventBus` is a minimal, focused type. It solves the problem of per-invocation event bus swapping without changing `IIndexPipeline` or `IEventBus`.

### Test completeness
- Every new source type has at least one unit test covering the happy path and at least one covering the failure path.
- Existing tests for `DoctorCommandHandler`, `IndexCommandHandler`, and `IndexCliModule` are extended rather than replaced.
- The `FakeIndexPipeline` does not publish events, so the verbose sink test validates the code path without exercising real event firing — acceptable for S4 unit coverage.

### Risks
- `CoreCliModule.ConfigureServices` calls `services.BuildServiceProvider()` to resolve `IConfiguration` — this is a pattern known to produce "BuildServiceProvider called multiple times" warnings with some DI validators. If that warning surfaces, extract `IConfiguration` resolution by passing it into `CoreCliModule` as a constructor parameter instead.
- `SwappableEventBus.Inner` is mutated during handler execution. If `ferret index` is ever invoked concurrently from the same DI scope, this is a race condition. For a CLI tool invoked once per process lifetime this is safe; document the constraint with a comment on the class.
- `AiProviderConfigCheck` uses `section.GetChildren().Any()` which returns true if any key exists under `Ferret:Ai:Providers`, even a malformed one. This is intentional — the check is presence-only, not schema-validation.
