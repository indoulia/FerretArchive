# Sprint 14 S5: Configuration Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Use `tokensave_context` as primary exploration tool before reading any source file.

**Goal:** Harden Ferret for RC1 by adding three configuration features: a `ferret config validate` CLI subcommand that validates `ferret.config.json` and reports field errors; glob pattern filtering via `.ferretignore` (the `FerretIgnoreProvider` infrastructure already exists — this task enhances its pattern matching to handle `**` and leading `/` anchoring and wires it into the connector discovery pipeline by default); and environment variable overrides for AI provider settings so `FERRET_AI_PROVIDER`, `FERRET_OPENAI_API_KEY`, and `FERRET_OLLAMA_BASE_URL` can override config file values without modifying the file.

**Architecture:**

- Task 1 adds a new `ConfigCliModule` in `src/Ferret.Cli/Commands/Config/` following the exact pattern of `WorkspaceCliModule` and `ConnectorCliModule`. The `ConfigValidateCommandHandler` reads `ferret.config.json` via `FerretConfigLoader`, runs field-level validation, and returns exit code 1 if any required field is absent or invalid.
- Task 2 enhances the existing `GitIgnoreProvider.MatchesPattern` static helper (already shared by `FerretIgnoreProvider`) to correctly handle `**` multi-segment wildcards and leading `/` root-anchoring. Then `FilesystemConnectorFactory` (or the connector's own construction site) instantiates a `FerretIgnoreProvider` and injects it via `AssetDiscoveryOptions` so every `DiscoverAsync` call respects `.ferretignore` automatically.
- Task 3 adds env-var override logic to `AiConfigurationModule` using `Microsoft.Extensions.Configuration` environment variable provider scoped to the `FERRET_AI_` prefix.

**Tech Stack:** .NET 9, C# 13, xUnit, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`

## Global Constraints

- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-14):`, `test(sprint-14):`.
- No third-party gitignore library. Pattern matching is implemented in `GitIgnoreProvider.MatchesPattern` only — no new helper class.
- `FerretIgnoreProvider` already exists in `src/Ferret.Connectors.Filesystem/Ignore/FerretIgnoreProvider.cs`. Do not recreate it; enhance and test it.
- Architecture tests must pass: `dotnet test tests/Ferret.Architecture.Tests/ -v n`.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.
- Build: `dotnet build src/Ferret.sln -v n`.

---

## File Structure Map

```
src/Ferret.Cli/Commands/Config/
  ConfigCliModule.cs                       [NEW — Task 1]
  ConfigValidateCommandHandler.cs          [NEW — Task 1]

tests/Ferret.Cli.Tests/
  ConfigValidateCommandHandlerTests.cs     [NEW — Task 1]

src/Ferret.Connectors.Filesystem/Ignore/
  GitIgnoreProvider.cs                     [MODIFY — Task 2] fix MatchesPattern for ** and /
  FerretIgnoreProvider.cs                  [NO CHANGE — already correct callers]

tests/Ferret.Connectors.Filesystem.Tests/
  GitIgnoreProviderPatternTests.cs         [NEW — Task 2] ** and / anchor cases
  FerretIgnoreProviderTests.cs             [NEW — Task 2] end-to-end .ferretignore file tests

src/Ferret.Connectors.Filesystem/
  FilesystemConnectorFactory.cs            [MODIFY — Task 2] wire FerretIgnoreProvider into DiscoverAsync

src/Ferret.Configuration.AI/
  AiConfigurationModule.cs                 [MODIFY — Task 3] add env-var overrides

tests/Ferret.Configuration.AI.Tests/
  AiOptionsEnvVarTests.cs                  [NEW — Task 3]
```

---

### Task 1: `ferret config validate` subcommand

Adds a `config` group and a `validate` subcommand. The handler reads `ferret.config.json` (via `FerretConfigLoader`), checks required fields, and prints per-field errors. Returns `CommandResult.Failure` (exit code 1) on any error.

**Files:**
- Create: `src/Ferret.Cli/Commands/Config/ConfigCliModule.cs`
- Create: `src/Ferret.Cli/Commands/Config/ConfigValidateCommandHandler.cs`
- Modify: `src/Ferret.Cli/Commands/RootCommandFactory.cs` — register the new module
- Create: `tests/Ferret.Cli.Tests/ConfigValidateCommandHandlerTests.cs`

**Interfaces / types used:**
- `CliModuleBase` — `src/Ferret.Cli/Cli/CliModuleBase.cs`
- `CommandDefinition`, `CommandMetadata` — `src/Ferret.Cli/Cli/CommandDefinition.cs`
- `ICommandHandler`, `IFerretContext`, `CommandResult` — `src/Ferret.Cli/Cli/`
- `FerretConfigLoader.Load(string? configPath)` — `src/Ferret.Cli/Configuration/FerretConfigLoader.cs`
- `ValidationResult`, `ValidationFailure`, `ValidationSeverity` — `src/Ferret.Core/Results/`

**Steps:**

- [ ] **1.1 — Failing test: valid config returns success**

  Create `tests/Ferret.Cli.Tests/ConfigValidateCommandHandlerTests.cs`:

  ```csharp
  using Ferret.Cli.Commands.Config;
  using Ferret.Cli.Cli;
  using Xunit;

  namespace Ferret.Cli.Tests;

  public sealed class ConfigValidateCommandHandlerTests
  {
      [Fact]
      public async Task ExecuteAsync_ValidConfig_ReturnsSuccess()
      {
          using var dir = new TempDirectory();
          var json = """
              {
                "Ferret": {
                  "Workspace": { "Name": "test-ws", "Root": "." }
                }
              }
              """;
          File.WriteAllText(Path.Combine(dir.Path, "ferret.config.json"), json);

          var handler = new ConfigValidateCommandHandler();
          var ctx = FakeContext.Create(dir.Path, configPath: Path.Combine(dir.Path, "ferret.config.json"));

          var result = await handler.ExecuteAsync(ctx);

          Assert.Equal(CommandResult.Success, result);
      }

      [Fact]
      public async Task ExecuteAsync_MissingWorkspaceName_ReturnsFailure()
      {
          using var dir = new TempDirectory();
          var json = """{ "Ferret": { "Workspace": { "Root": "." } } }""";
          File.WriteAllText(Path.Combine(dir.Path, "ferret.config.json"), json);

          var handler = new ConfigValidateCommandHandler();
          var ctx = FakeContext.Create(dir.Path, configPath: Path.Combine(dir.Path, "ferret.config.json"));

          var result = await handler.ExecuteAsync(ctx);

          Assert.Equal(CommandResult.Failure, result);
      }

      [Fact]
      public async Task ExecuteAsync_MissingConfigFile_ReturnsFailure()
      {
          using var dir = new TempDirectory();
          var handler = new ConfigValidateCommandHandler();
          var ctx = FakeContext.Create(dir.Path, configPath: Path.Combine(dir.Path, "ferret.config.json"));

          var result = await handler.ExecuteAsync(ctx);

          Assert.Equal(CommandResult.Failure, result);
      }
  }
  ```

  Run `dotnet test tests/Ferret.Cli.Tests/ -v n` — confirm it fails to compile (type not found).

- [ ] **1.2 — Create `ConfigValidateCommandHandler`**

  Create `src/Ferret.Cli/Commands/Config/ConfigValidateCommandHandler.cs`:

  ```csharp
  using Ferret.Cli.Cli;
  using Ferret.Cli.Configuration;
  using Ferret.Core.Results;

  namespace Ferret.Cli.Commands.Config;

  /// <summary>Handles 'ferret config validate' — validates ferret.config.json and reports field errors.</summary>
  internal sealed class ConfigValidateCommandHandler : ICommandHandler
  {
      /// <inheritdoc/>
      public Task<CommandResult> ExecuteAsync(IFerretContext context)
      {
          ArgumentNullException.ThrowIfNull(context);

          var configPath = context.GetOption<string>("--config") ?? "ferret.config.json";

          if (!File.Exists(configPath))
          {
              context.Services.Output.WriteError($"Config file not found: {configPath}");
              return Task.FromResult(CommandResult.Failure);
          }

          var failures = Validate(configPath);

          if (failures.Count == 0)
          {
              context.Services.Output.WriteSuccess("ferret.config.json is valid.");
              return Task.FromResult(CommandResult.Success);
          }

          context.Services.Output.WriteError($"ferret.config.json has {failures.Count} error(s):");
          foreach (var f in failures)
          {
              context.Services.Output.WriteLine($"  [{f.Field}] {f.Constraint} — {f.Guidance}");
          }

          return Task.FromResult(CommandResult.Failure);
      }

      private static IReadOnlyList<ValidationFailure> Validate(string configPath)
      {
          var failures = new List<ValidationFailure>();
          var configuration = FerretConfigLoader.Load(configPath);

          var workspaceName = configuration["Ferret:Workspace:Name"];
          if (string.IsNullOrWhiteSpace(workspaceName))
          {
              failures.Add(new ValidationFailure(
                  "Ferret:Workspace:Name",
                  "required",
                  "Set a non-empty workspace name in ferret.config.json under Ferret.Workspace.Name.",
                  Ferret.Core.Enumerations.ValidationSeverity.Error));
          }

          var workspaceRoot = configuration["Ferret:Workspace:Root"];
          if (string.IsNullOrWhiteSpace(workspaceRoot))
          {
              failures.Add(new ValidationFailure(
                  "Ferret:Workspace:Root",
                  "required",
                  "Set the workspace root directory in ferret.config.json under Ferret.Workspace.Root.",
                  Ferret.Core.Enumerations.ValidationSeverity.Error));
          }
          else if (!Directory.Exists(workspaceRoot) && !workspaceRoot.Equals(".", StringComparison.OrdinalIgnoreCase))
          {
              failures.Add(new ValidationFailure(
                  "Ferret:Workspace:Root",
                  "path-exists",
                  $"Workspace root directory '{workspaceRoot}' does not exist.",
                  Ferret.Core.Enumerations.ValidationSeverity.Error));
          }

          return failures;
      }
  }
  ```

- [ ] **1.3 — Create `ConfigCliModule`**

  Create `src/Ferret.Cli/Commands/Config/ConfigCliModule.cs`:

  ```csharp
  using Ferret.Cli.Cli;
  using Microsoft.Extensions.DependencyInjection;

  namespace Ferret.Cli.Commands.Config;

  /// <summary>Contributes config subcommands to the Ferret CLI.</summary>
  internal sealed class ConfigCliModule : CliModuleBase
  {
      /// <inheritdoc/>
      public override string Name => "ferret.config";

      /// <inheritdoc/>
      public override string Description => "Configuration management.";

      /// <inheritdoc/>
      public override IEnumerable<CommandDefinition> GetCommands()
      {
          yield return new CommandDefinition(
              new CommandMetadata("config", "Manage Ferret configuration."),
              HandlerType: null);

          yield return new CommandDefinition(
              new CommandMetadata("validate", "Validate ferret.config.json and report errors."),
              typeof(ConfigValidateCommandHandler),
              Group: "config",
              Options:
              [
                  new OptionDefinition("--config", "Path to ferret.config.json.", typeof(string)),
              ]);
      }

      /// <inheritdoc/>
      public override void ConfigureServices(IServiceCollection services)
      {
          services.AddTransient<ConfigValidateCommandHandler>();
      }
  }
  ```

- [ ] **1.4 — Wire `ConfigCliModule` into `RootCommandFactory`**

  Read `src/Ferret.Cli/Commands/RootCommandFactory.cs` to find the module registration list, then add `ConfigCliModule`:

  ```csharp
  // In the modules list (follow existing pattern)
  new ConfigCliModule(),
  ```

  Ensure `using Ferret.Cli.Commands.Config;` is present.

- [ ] **1.5 — Add `FakeContext` test helper if missing**

  Check `tests/Ferret.Cli.Tests/` for an existing `FakeContext`. If absent, create `tests/Ferret.Cli.Tests/FakeContext.cs` following the same pattern as any existing test helpers in that project. The minimal contract needed:

  ```csharp
  using Ferret.Cli.Cli;
  using NSubstitute;

  namespace Ferret.Cli.Tests;

  internal static class FakeContext
  {
      internal static IFerretContext Create(string workingDirectory, string? configPath = null)
      {
          var output = Substitute.For<IOutputFormatter>();
          var services = Substitute.For<IFerretServices>();
          services.Output.Returns(output);

          var ctx = Substitute.For<IFerretContext>();
          ctx.WorkingDirectory.Returns(workingDirectory);
          ctx.Services.Returns(services);
          ctx.GetOption<string>("--config").Returns(configPath);
          ctx.CancellationToken.Returns(CancellationToken.None);
          return ctx;
      }
  }
  ```

  Verify the test project already references `NSubstitute` in its `.csproj`; add it if absent.

- [ ] **1.6 — Verify green**

  ```
  dotnet test tests/Ferret.Cli.Tests/ -v n
  dotnet test src/Ferret.sln -v n
  ```

- [ ] **1.7 — Commit**

  ```
  git add src/Ferret.Cli/Commands/Config/ tests/Ferret.Cli.Tests/ConfigValidateCommandHandlerTests.cs
  git commit -m "feat(sprint-14): ferret config validate subcommand"
  ```

---

### Task 2: `.ferretignore` pattern matching enhancements

The `FerretIgnoreProvider` and `GitIgnoreProvider` classes already exist. `GitIgnoreProvider.MatchesPattern` is the shared static helper. It currently handles `*` but not `**` (match across path segments) or leading `/` (root-anchoring). This task adds those two cases and wires `FerretIgnoreProvider` into `FilesystemConnectorFactory` so it is applied automatically on every `DiscoverAsync` call.

**Files:**
- Modify: `src/Ferret.Connectors.Filesystem/Ignore/GitIgnoreProvider.cs` — extend `MatchesPattern`
- Modify: `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs` — inject `FerretIgnoreProvider`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/GitIgnoreProviderPatternTests.cs`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/FerretIgnoreProviderTests.cs`

**Steps:**

- [ ] **2.1 — Failing tests: `**` and leading `/` patterns**

  Create `tests/Ferret.Connectors.Filesystem.Tests/GitIgnoreProviderPatternTests.cs`:

  ```csharp
  using Ferret.Connectors.Filesystem.Ignore;
  using Xunit;

  namespace Ferret.Connectors.Filesystem.Tests;

  public sealed class GitIgnoreProviderPatternTests
  {
      // ** — matches across path segments

      [Theory]
      [InlineData("**/bin", "src/MyLib/bin")]
      [InlineData("**/bin", "bin")]
      [InlineData("**/obj/**", "src/MyLib/obj/Debug/net9.0/out.dll")]
      [InlineData("**/*.log", "logs/debug/server.log")]
      public void MatchesPattern_DoubleGlob_Matches(string pattern, string input)
          => Assert.True(GitIgnoreProvider.MatchesPattern(pattern, input));

      [Theory]
      [InlineData("**/bin", "src/bin_backup")]
      [InlineData("**/bin", "src/bingo")]
      public void MatchesPattern_DoubleGlob_NoMatch(string pattern, string input)
          => Assert.False(GitIgnoreProvider.MatchesPattern(pattern, input));

      // Leading / — anchors to root (path must start with the remainder)

      [Theory]
      [InlineData("/dist", "dist")]
      [InlineData("/dist", "dist/bundle.js")]
      public void MatchesPattern_LeadingSlash_Matches(string pattern, string input)
          => Assert.True(GitIgnoreProvider.MatchesPattern(pattern, input));

      [Theory]
      [InlineData("/dist", "src/dist")]
      [InlineData("/dist", "build/dist/app")]
      public void MatchesPattern_LeadingSlash_NoMatch(string pattern, string input)
          => Assert.False(GitIgnoreProvider.MatchesPattern(pattern, input));
  }
  ```

  Run `dotnet test tests/Ferret.Connectors.Filesystem.Tests/ -v n` — confirm failures for `**` and `/` cases.

- [ ] **2.2 — Extend `GitIgnoreProvider.MatchesPattern`**

  Replace the body of `MatchesPattern` in `src/Ferret.Connectors.Filesystem/Ignore/GitIgnoreProvider.cs`:

  ```csharp
  internal static bool MatchesPattern(string pattern, string input)
  {
      // Leading / means anchored to root: strip it and require input starts with the remainder.
      if (pattern.StartsWith('/'))
      {
          var anchored = pattern[1..];
          return input.Equals(anchored, StringComparison.OrdinalIgnoreCase)
              || input.StartsWith(anchored + "/", StringComparison.OrdinalIgnoreCase);
      }

      // ** — replace with a sentinel, then do segment-aware matching.
      if (pattern.Contains("**", StringComparison.Ordinal))
      {
          return MatchesDoubleGlob(pattern, input);
      }

      if (!pattern.Contains('*', StringComparison.Ordinal))
      {
          return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase)
              || input.EndsWith("/" + pattern, StringComparison.OrdinalIgnoreCase)
              || input.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase);
      }

      var parts = pattern.Split('*');
      var pos = 0;
      for (var i = 0; i < parts.Length; i++)
      {
          if (parts[i].Length == 0)
          {
              continue;
          }

          var idx = input.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
          if (idx < 0)
          {
              return false;
          }

          if (i == 0 && idx > 0 && !pattern.StartsWith('*'))
          {
              return false;
          }

          pos = idx + parts[i].Length;
      }

      return !pattern.EndsWith('*') ? pos == input.Length || input[pos..].All(c => c == '/') : true;
  }

  private static bool MatchesDoubleGlob(string pattern, string input)
  {
      // Split pattern on ** boundaries; each segment between ** is matched in order.
      // A ** matches zero or more path segments (including none).
      var segments = pattern.Split(new[] { "**" }, StringSplitOptions.None);

      // First segment (before first **) must match start of input unless empty.
      var pos = 0;
      for (var i = 0; i < segments.Length; i++)
      {
          var seg = segments[i].Trim('/');

          if (seg.Length == 0)
          {
              if (i == segments.Length - 1)
              {
                  // Trailing ** — matches everything remaining.
                  return true;
              }
              // Empty between ** — continue.
              continue;
          }

          if (i == 0)
          {
              // Must match at start.
              if (!MatchesPattern(seg, input[..Math.Min(seg.Length + 1, input.Length)].TrimEnd('/')))
              {
                  // Recheck: prefix must equal seg or input starts with seg + /
                  if (!input.StartsWith(seg + "/", StringComparison.OrdinalIgnoreCase)
                      && !input.Equals(seg, StringComparison.OrdinalIgnoreCase))
                  {
                      return false;
                  }
                  pos = seg.Length;
              }
              continue;
          }

          // For subsequent segments after **, find the segment anywhere from pos onward.
          var found = false;
          while (pos <= input.Length)
          {
              var remaining = input[pos..];
              if (MatchesSingleGlob(seg, remaining) || remaining.StartsWith(seg + "/", StringComparison.OrdinalIgnoreCase))
              {
                  pos += seg.Length;
                  found = true;
                  break;
              }

              var next = input.IndexOf('/', pos, StringComparison.Ordinal);
              if (next < 0)
              {
                  break;
              }

              pos = next + 1;
          }

          if (!found)
          {
              return false;
          }
      }

      return true;
  }

  private static bool MatchesSingleGlob(string pattern, string input)
  {
      // Single * does not cross /
      if (!pattern.Contains('*', StringComparison.Ordinal))
      {
          return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase)
              || input.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase);
      }

      var parts = pattern.Split('*');
      var segment = input.Contains('/', StringComparison.Ordinal)
          ? input[..input.IndexOf('/', StringComparison.Ordinal)]
          : input;

      var pos = 0;
      for (var i = 0; i < parts.Length; i++)
      {
          if (parts[i].Length == 0)
          {
              continue;
          }

          var idx = segment.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
          if (idx < 0)
          {
              return false;
          }

          if (i == 0 && idx > 0 && !pattern.StartsWith('*'))
          {
              return false;
          }

          pos = idx + parts[i].Length;
      }

      return !pattern.EndsWith('*') ? pos == segment.Length : true;
  }
  ```

  Run `dotnet test tests/Ferret.Connectors.Filesystem.Tests/ -v n` — confirm all pattern tests pass.

- [ ] **2.3 — Failing test: FerretIgnoreProvider reads `.ferretignore` and skips files**

  Create `tests/Ferret.Connectors.Filesystem.Tests/FerretIgnoreProviderTests.cs`:

  ```csharp
  using Ferret.Connectors.Filesystem.Ignore;
  using Ferret.Core.Connectors;
  using Xunit;

  namespace Ferret.Connectors.Filesystem.Tests;

  public sealed class FerretIgnoreProviderTests
  {
      [Fact]
      public void ShouldIgnore_Returns_False_When_No_FerretIgnore_File()
      {
          using var dir = new TempDirectory();
          var provider = new FerretIgnoreProvider(dir.Path);
          var asset = MakeAsset(new Uri("filesystem:///src/Program.cs"));

          Assert.False(provider.ShouldIgnore(asset));
      }

      [Fact]
      public void ShouldIgnore_Returns_False_For_Non_Filesystem_Uri()
      {
          using var dir = new TempDirectory();
          File.WriteAllText(Path.Combine(dir.Path, ".ferretignore"), "*.log\n");
          var provider = new FerretIgnoreProvider(dir.Path);
          var asset = MakeAsset(new Uri("jira:///PROJ-1"));

          Assert.False(provider.ShouldIgnore(asset));
      }

      [Fact]
      public void ShouldIgnore_Returns_True_For_Matching_Pattern()
      {
          using var dir = new TempDirectory();
          File.WriteAllText(Path.Combine(dir.Path, ".ferretignore"), "*.log\n");
          var provider = new FerretIgnoreProvider(dir.Path);

          Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///debug.log"))));
          Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///src/Program.cs"))));
      }

      [Fact]
      public void ShouldIgnore_Ignores_Comment_Lines()
      {
          using var dir = new TempDirectory();
          File.WriteAllText(Path.Combine(dir.Path, ".ferretignore"), "# comment\n*.tmp\n");
          var provider = new FerretIgnoreProvider(dir.Path);

          Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///readme.md"))));
          Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///temp.tmp"))));
      }

      [Fact]
      public void ShouldIgnore_DoubleGlob_Pattern_Matches_Nested_Path()
      {
          using var dir = new TempDirectory();
          File.WriteAllText(Path.Combine(dir.Path, ".ferretignore"), "**/bin\n");
          var provider = new FerretIgnoreProvider(dir.Path);

          Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///src/MyLib/bin"))));
          Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///src/MyLib/src"))));
      }

      private static AssetDescriptor MakeAsset(Uri uri) => new()
      {
          Id = AssetId.From(uri),
          ConnectorId = new ConnectorId("filesystem"),
          InstanceId = new ConnectorInstanceId("src-root"),
          Kind = AssetKind.File,
          CanonicalUri = uri,
          DisplayName = Path.GetFileName(uri.AbsolutePath),
          LastModified = DateTimeOffset.UtcNow,
      };
  }
  ```

  Run — confirm red (double-glob test expected to fail until 2.2 is complete; others should pass).

- [ ] **2.4 — Wire `FerretIgnoreProvider` into discovery**

  Read `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs` to understand how `FilesystemConnector` is constructed. Locate where `AssetDiscoveryOptions` is created (or where `DiscoverAsync` is called). Inject a `FerretIgnoreProvider` using the workspace root path:

  The goal is that when `DiscoverAsync` is called without an explicit `IgnoreProvider`, a default `FerretIgnoreProvider` is used. The cleanest approach is to update `FilesystemConnector.DiscoverAsync` to fall back to a `FerretIgnoreProvider` when `options.IgnoreProvider` is null:

  In `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs`, change the `DiscoverAsync` body:

  ```csharp
  public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
      AssetDiscoveryOptions options,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
  {
      ArgumentNullException.ThrowIfNull(options);

      var root = new DirectoryInfo(_config.RootPath);
      if (!root.Exists)
      {
          yield break;
      }

      // Default ignore provider: .ferretignore in workspace root (if not already supplied).
      var ignoreProvider = options.IgnoreProvider ?? new FerretIgnoreProvider(_config.RootPath);
      var effectiveOptions = options.IgnoreProvider is null
          ? new AssetDiscoveryOptions { IgnoreProvider = ignoreProvider }
          : options;

      await foreach (var descriptor in WalkDirectoryAsync(root, root, effectiveOptions, _mimeTypeResolver, ct).ConfigureAwait(false))
      {
          yield return descriptor;
      }
  }
  ```

  Add `using Ferret.Connectors.Filesystem.Ignore;` to the top of `FilesystemConnector.cs`.

- [ ] **2.5 — Verify green**

  ```
  dotnet test tests/Ferret.Connectors.Filesystem.Tests/ -v n
  dotnet test src/Ferret.sln -v n
  ```

- [ ] **2.6 — Commit**

  ```
  git add src/Ferret.Connectors.Filesystem/ tests/Ferret.Connectors.Filesystem.Tests/
  git commit -m "feat(sprint-14): .ferretignore ** and / pattern support, auto-wire into DiscoverAsync"
  ```

---

### Task 3: Environment variable overrides for AI options

`FERRET_AI_PROVIDER`, `FERRET_OPENAI_API_KEY`, and `FERRET_OLLAMA_BASE_URL` must override the equivalent config file values without changing `ferret.config.json`. This is done by enhancing `AiConfigurationModule.ConfigureServices` to call `.PostConfigure<AiOptions>` and map the three specific env vars.

**Files:**
- Modify: `src/Ferret.Configuration.AI/AiConfigurationModule.cs`
- Create: `tests/Ferret.Configuration.AI.Tests/AiOptionsEnvVarTests.cs`

**Steps:**

- [ ] **3.1 — Failing tests: env vars override config**

  Create `tests/Ferret.Configuration.AI.Tests/AiOptionsEnvVarTests.cs`:

  ```csharp
  using Ferret.Configuration.Ai;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Options;
  using Xunit;

  namespace Ferret.Configuration.Ai.Tests;

  public sealed class AiOptionsEnvVarTests
  {
      [Fact]
      public void FERRET_AI_PROVIDER_Overrides_DefaultChatModel()
      {
          var config = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Ferret:Ai:DefaultChatModel"] = "ollama/llama3.2",
                  ["FERRET_AI_PROVIDER"] = "openai",
              })
              .Build();

          var services = new ServiceCollection();
          AiConfigurationModule.ConfigureServices(services, config);

          var provider = services.BuildServiceProvider();
          var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

          // When FERRET_AI_PROVIDER=openai, DefaultChatModel prefix switches to openai/
          Assert.StartsWith("openai/", options.DefaultChatModel, StringComparison.OrdinalIgnoreCase);
      }

      [Fact]
      public void FERRET_OPENAI_API_KEY_Overrides_OpenAi_Provider_ApiKey()
      {
          var config = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "file-key",
                  ["FERRET_OPENAI_API_KEY"] = "env-key-123",
              })
              .Build();

          var services = new ServiceCollection();
          AiConfigurationModule.ConfigureServices(services, config);

          var provider = services.BuildServiceProvider();
          var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

          Assert.Equal("env-key-123", options.Providers.GetValueOrDefault("OpenAi")?.ApiKey);
      }

      [Fact]
      public void FERRET_OLLAMA_BASE_URL_Overrides_Ollama_Provider_BaseUrl()
      {
          var config = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["FERRET_OLLAMA_BASE_URL"] = "http://remote-host:11434",
              })
              .Build();

          var services = new ServiceCollection();
          AiConfigurationModule.ConfigureServices(services, config);

          var provider = services.BuildServiceProvider();
          var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

          Assert.Equal("http://remote-host:11434", options.Providers.GetValueOrDefault("Ollama")?.BaseUrl);
      }

      [Fact]
      public void No_Env_Vars_Leaves_Config_Values_Unchanged()
      {
          var config = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Ferret:Ai:DefaultChatModel"] = "ollama/llama3.2",
              })
              .Build();

          var services = new ServiceCollection();
          AiConfigurationModule.ConfigureServices(services, config);

          var provider = services.BuildServiceProvider();
          var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

          Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
      }
  }
  ```

  Run `dotnet test tests/Ferret.Configuration.AI.Tests/ -v n` — confirm failures (env vars not yet applied).

- [ ] **3.2 — Extend `AiConfigurationModule`**

  Read `src/Ferret.Configuration.AI/AiConfigurationModule.cs` then replace the `ConfigureServices` body:

  ```csharp
  public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
  {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(configuration);

      services.AddOptions<AiOptions>()
          .Bind(configuration.GetSection("Ferret:Ai"))
          .PostConfigure(options =>
          {
              // FERRET_AI_PROVIDER — rewrite DefaultChatModel / DefaultEmbeddingModel prefix.
              var aiProvider = configuration["FERRET_AI_PROVIDER"];
              if (!string.IsNullOrWhiteSpace(aiProvider))
              {
                  var chatModel = options.DefaultChatModel;
                  var slashIndex = chatModel.IndexOf('/', StringComparison.Ordinal);
                  var modelName = slashIndex >= 0 ? chatModel[(slashIndex + 1)..] : chatModel;
                  options.DefaultChatModel = $"{aiProvider}/{modelName}";

                  var embModel = options.DefaultEmbeddingModel;
                  var embSlash = embModel.IndexOf('/', StringComparison.Ordinal);
                  var embName = embSlash >= 0 ? embModel[(embSlash + 1)..] : embModel;
                  options.DefaultEmbeddingModel = $"{aiProvider}/{embName}";
              }

              // FERRET_OPENAI_API_KEY — override OpenAi provider ApiKey.
              var openAiKey = configuration["FERRET_OPENAI_API_KEY"];
              if (!string.IsNullOrWhiteSpace(openAiKey))
              {
                  if (!options.Providers.TryGetValue("OpenAi", out var openAiOpts))
                  {
                      openAiOpts = new OpenAiOptions();
                      options.Providers["OpenAi"] = openAiOpts;
                  }

                  openAiOpts.ApiKey = openAiKey;
              }

              // FERRET_OLLAMA_BASE_URL — override Ollama provider BaseUrl.
              var ollamaUrl = configuration["FERRET_OLLAMA_BASE_URL"];
              if (!string.IsNullOrWhiteSpace(ollamaUrl))
              {
                  if (!options.Providers.TryGetValue("Ollama", out var ollamaOpts))
                  {
                      ollamaOpts = new OllamaOptions();
                      options.Providers["Ollama"] = ollamaOpts;
                  }

                  ollamaOpts.BaseUrl = ollamaUrl;
              }
          });

      return services;
  }
  ```

- [ ] **3.3 — Verify green**

  ```
  dotnet test tests/Ferret.Configuration.AI.Tests/ -v n
  dotnet test src/Ferret.sln -v n
  ```

- [ ] **3.4 — Commit**

  ```
  git add src/Ferret.Configuration.AI/AiConfigurationModule.cs tests/Ferret.Configuration.AI.Tests/AiOptionsEnvVarTests.cs
  git commit -m "feat(sprint-14): FERRET_AI_PROVIDER, FERRET_OPENAI_API_KEY, FERRET_OLLAMA_BASE_URL env var overrides"
  ```

---

## Self-Review

- **Task 1** registers `ConfigCliModule` following the exact pattern of `WorkspaceCliModule`. `ConfigValidateCommandHandler` uses `FerretConfigLoader` (not a new loader), validates `Ferret:Workspace:Name` and `Ferret:Workspace:Root`, and returns `CommandResult.Failure` with per-field messages. Exit code 1 maps to `CommandResult.Failure = 1` per `CommandResult.cs`.
- **Task 2** does not introduce a new class. `GitIgnoreProvider.MatchesPattern` gains `**` via `MatchesDoubleGlob` and `/` root-anchoring. `FerretIgnoreProvider` continues to call `GitIgnoreProvider.MatchesPattern` unchanged. The connector wiring adds a one-liner fallback in `FilesystemConnector.DiscoverAsync` — callers that already supply `options.IgnoreProvider` are unaffected.
- **Task 3** uses `.PostConfigure<AiOptions>` which is the idiomatic `Microsoft.Extensions.Options` extension point. It reads the three env vars directly from the supplied `IConfiguration` (not from `IHostEnvironment`) so it works identically in tests (in-memory dictionary) and production (real env vars via `AddEnvironmentVariables`).
- All three tasks follow the TDD cycle: failing test first → confirm red → implement → verify green → commit.
- No third-party libraries added. No files created outside the listed paths.
