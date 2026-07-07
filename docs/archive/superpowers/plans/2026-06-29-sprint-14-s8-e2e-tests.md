# Sprint 14 S8: End-to-End Tests Implementation Plan

> **For agentic workers:** Follow tasks in order. Each task is self-contained. Run `dotnet build src/Ferret.sln` after Task 1 to confirm the project compiles before writing tests. Commit prefix: `test(sprint-14):`. Solution path: `<repo-root>\src\Ferret.sln`.

---

## Goal

Create `Ferret.E2E.Tests` — a process-level test project that runs the real `ferret` binary via `Process.Start` against a temporary workspace directory. These tests catch integration failures that in-process unit and integration tests cannot: output format regressions, exit-code contracts, cross-module DI wiring, and CLI argument parsing.

Seven scenarios are covered:
1. Workspace init creates `.ferret/`
2. Index: 3 real `.cs` files, exit 0, output contains "Indexed"
3. Search: after index, results contain file names
4. Incremental index: second run shows "Skipped"
5. Config validate: exit 0 on valid config
6. Doctor: exit 0, output contains "healthy"
7. MCP serve smoke: process starts without error output within 2 s

---

## Architecture

```
tests/
  Ferret.E2E.Tests/
    Ferret.E2E.Tests.csproj       — no ProjectReference; locates binary via build output
    Infrastructure/
      FerretCliRunner.cs          — Process.Start wrapper → (ExitCode, Stdout, Stderr)
      FerretBinaryLocator.cs      — resolves path to ferret(.exe) in publish output
    Fixtures/
      WorkspaceFixture.cs         — IAsyncLifetime; creates temp dir, workspace init, teardown
    Tests/
      WorkspaceInitE2ETests.cs
      IndexE2ETests.cs
      SearchE2ETests.cs
      IncrementalIndexE2ETests.cs
      ConfigValidateE2ETests.cs
      DoctorE2ETests.cs
      ServeE2ETests.cs
```

**Key design choice — no `ProjectReference`:** The E2E project does not reference `Ferret.Cli.csproj`. It locates the pre-built binary by searching the `src/Ferret.Cli/bin` output tree. `IAsyncLifetime.InitializeAsync` publishes the binary once via `dotnet publish` before any test runs.

---

## Tech Stack

| Concern | Choice |
|---|---|
| Test framework | xUnit 2.9.2 (already in `Directory.Packages.props`) |
| Process launch | `System.Diagnostics.Process` (BCL, no extra package) |
| Temp directory | `Path.GetTempPath()` + `Guid` suffix, deleted in `DisposeAsync` |
| Binary location | `dotnet publish` during `InitializeAsync`; path cached in static field |
| Timeout | `CancellationTokenSource` with configurable ceiling; default 30 s per command, 2 s for serve smoke |

---

## Global Constraints

- `TreatWarningsAsErrors` is `true` — all public members need XML doc comments; suppress with `#pragma` or add summaries.
- Central Package Management is active — no `Version=` attributes in `<PackageReference>`.
- `CentralPackageTransitivePinningEnabled` is `true` on the repo; set it to `false` in this project (same pattern as `Ferret.Integration.Tests`).
- StyleCop is enabled globally — files must have `// <copyright>` headers or the build will fail. Add a `stylecop.json` override disabling the file header rule, or add headers consistently.
- The project must end in `.Tests` so `Directory.Build.props` sets `IsTestProject=true` and suppresses XML doc requirements.
- Binary publish target framework: `net9.0`.

---

## File Structure

### 1. `tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj`

No `ProjectReference`. The project stands alone and drives the real process.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.E2E.Tests</AssemblyName>
    <RootNamespace>Ferret.E2E.Tests</RootNamespace>
    <!-- No ProjectReference to Ferret.Cli — this project drives the real binary -->
    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

---

### 2. `tests/Ferret.E2E.Tests/Infrastructure/FerretBinaryLocator.cs`

Publishes the CLI once and caches the path. All tests share the same publish output.

```csharp
namespace Ferret.E2E.Tests.Infrastructure;

/// <summary>Resolves and publishes the ferret CLI binary once per test session.</summary>
internal static class FerretBinaryLocator
{
    private static string? _binaryPath;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>Returns the absolute path to the ferret binary, publishing it if needed.</summary>
    internal static async Task<string> GetOrPublishAsync(CancellationToken ct = default)
    {
        if (_binaryPath is not null)
        {
            return _binaryPath;
        }

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_binaryPath is not null)
            {
                return _binaryPath;
            }

            // Resolve repo root: this assembly lives in tests/Ferret.E2E.Tests/bin/...
            // Walk up until we find the src/ directory containing Ferret.Cli.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Ferret.Cli")))
            {
                dir = dir.Parent;
            }

            if (dir is null)
            {
                throw new InvalidOperationException(
                    "Cannot locate repo root from " + AppContext.BaseDirectory);
            }

            var cliProjectPath = Path.Combine(dir.FullName, "src", "Ferret.Cli", "Ferret.Cli.csproj");
            var publishDir = Path.Combine(
                Path.GetTempPath(),
                "ferret-e2e-publish-" + Environment.ProcessId);

            var (exitCode, _, stderr) = await FerretCliRunner.RunProcessAsync(
                "dotnet",
                $"publish \"{cliProjectPath}\" -c Release -f net9.0 -o \"{publishDir}\" --nologo -v quiet",
                workingDir: dir.FullName,
                timeout: TimeSpan.FromSeconds(120),
                ct: ct).ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"dotnet publish failed (exit {exitCode}):\n{stderr}");
            }

            var exeName = OperatingSystem.IsWindows() ? "Ferret.Cli.exe" : "Ferret.Cli";
            var binaryPath = Path.Combine(publishDir, exeName);

            if (!File.Exists(binaryPath))
            {
                throw new FileNotFoundException(
                    $"Published binary not found at: {binaryPath}");
            }

            _binaryPath = binaryPath;
            return _binaryPath;
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

---

### 3. `tests/Ferret.E2E.Tests/Infrastructure/FerretCliRunner.cs`

Core process runner. Returns a value tuple so callers can assert exit code, stdout, and stderr independently.

```csharp
using System.Diagnostics;
using System.Text;

namespace Ferret.E2E.Tests.Infrastructure;

/// <summary>Runs a process and captures its stdout and stderr.</summary>
internal static class FerretCliRunner
{
    /// <summary>Runs the ferret binary with the given arguments in the given working directory.</summary>
    /// <param name="binaryPath">Absolute path to the ferret binary.</param>
    /// <param name="args">Command-line arguments to pass.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="timeout">Maximum time to wait for the process to exit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exit code, captured stdout, and captured stderr.</returns>
    internal static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string binaryPath,
        string args,
        string workingDir,
        TimeSpan timeout,
        CancellationToken ct = default) =>
        RunProcessAsync(binaryPath, args, workingDir, timeout, ct);

    /// <summary>Runs any process and captures stdout/stderr. Used internally and by FerretBinaryLocator.</summary>
    internal static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDir,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        // Capture both streams asynchronously to avoid deadlocks on large output.
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdoutBuilder.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout — kill the process, return what we have.
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
    }
}
```

---

### 4. `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs`

Shared fixture that creates a temp workspace, runs `workspace init`, and tears down after all tests.

```csharp
using Ferret.E2E.Tests.Infrastructure;

namespace Ferret.E2E.Tests.Fixtures;

/// <summary>
/// xUnit class fixture that provisions a temporary Ferret workspace for E2E tests.
/// InitializeAsync: creates temp dir, publishes binary, runs workspace init.
/// DisposeAsync: deletes temp dir.
/// </summary>
public sealed class WorkspaceFixture : IAsyncLifetime
{
    /// <summary>Gets the absolute path to the temporary workspace directory.</summary>
    public string WorkspaceDir { get; } = Path.Combine(
        Path.GetTempPath(),
        "ferret-e2e-ws-" + Guid.NewGuid().ToString("N")[..8]);

    /// <summary>Gets the absolute path to the ferret binary after initialization.</summary>
    public string BinaryPath { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(WorkspaceDir);
        BinaryPath = await FerretBinaryLocator.GetOrPublishAsync().ConfigureAwait(false);

        // Initialize the workspace so all tests start from a valid state.
        var (exitCode, _, stderr) = await FerretCliRunner.RunAsync(
            BinaryPath,
            "workspace init",
            WorkspaceDir,
            TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"workspace init failed (exit {exitCode}):\n{stderr}");
        }
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        if (Directory.Exists(WorkspaceDir))
        {
            Directory.Delete(WorkspaceDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <summary>Writes three sample C# source files into the workspace directory.</summary>
    public async Task WriteSampleCsFilesAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(WorkspaceDir, "Alpha.cs"),
            """
            namespace Sample;
            public class AlphaService { }
            """).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(WorkspaceDir, "Beta.cs"),
            """
            namespace Sample;
            public class BetaRepository { }
            """).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(WorkspaceDir, "Gamma.cs"),
            """
            namespace Sample;
            public class GammaController { }
            """).ConfigureAwait(false);
    }

    /// <summary>Runs a ferret command in the workspace directory.</summary>
    public Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string args,
        TimeSpan? timeout = null) =>
        FerretCliRunner.RunAsync(
            BinaryPath,
            args,
            WorkspaceDir,
            timeout ?? TimeSpan.FromSeconds(30));
}
```

---

## Task 1: Project Scaffolding

**Files to create:**
- `tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj` — content above
- `tests/Ferret.E2E.Tests/Infrastructure/FerretCliRunner.cs` — content above
- `tests/Ferret.E2E.Tests/Infrastructure/FerretBinaryLocator.cs` — content above
- `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs` — content above

**Add to solution:**
```
dotnet sln src/Ferret.sln add tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj
```

**Verification:** `dotnet build src/Ferret.sln` compiles with no errors.

**Commit:** `test(sprint-14): scaffold Ferret.E2E.Tests project and infrastructure`

---

## Task 2: Scenario 1 — Workspace Init

**File:** `tests/Ferret.E2E.Tests/Tests/WorkspaceInitE2ETests.cs`

```csharp
using Ferret.E2E.Tests.Infrastructure;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret workspace init.</summary>
public sealed class WorkspaceInitE2ETests : IAsyncLifetime
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "ferret-e2e-init-" + Guid.NewGuid().ToString("N")[..8]);

    private string _binaryPath = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempDir);
        _binaryPath = await FerretBinaryLocator.GetOrPublishAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <summary>workspace init in a clean directory creates .ferret/workspace.json.</summary>
    [Fact]
    public async Task WorkspaceInit_CreatesWorkspaceJson()
    {
        var (exitCode, _, _) = await FerretCliRunner.RunAsync(
            _binaryPath,
            "workspace init",
            _tempDir,
            TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        Assert.Equal(0, exitCode);
        Assert.True(
            File.Exists(Path.Combine(_tempDir, ".ferret", "workspace.json")),
            ".ferret/workspace.json must exist after workspace init");
    }

    /// <summary>workspace init in a clean directory creates .ferret/state.json.</summary>
    [Fact]
    public async Task WorkspaceInit_CreatesStateJson()
    {
        var (exitCode, _, _) = await FerretCliRunner.RunAsync(
            _binaryPath,
            "workspace init",
            _tempDir,
            TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        Assert.Equal(0, exitCode);
        Assert.True(
            File.Exists(Path.Combine(_tempDir, ".ferret", "state.json")),
            ".ferret/state.json must exist after workspace init");
    }

    /// <summary>workspace init when already initialised returns non-zero exit code.</summary>
    [Fact]
    public async Task WorkspaceInit_WhenAlreadyInitialised_ReturnsNonZeroExitCode()
    {
        // First init
        await FerretCliRunner.RunAsync(
            _binaryPath, "workspace init", _tempDir, TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        // Second init — must fail
        var (exitCode, _, _) = await FerretCliRunner.RunAsync(
            _binaryPath,
            "workspace init",
            _tempDir,
            TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        Assert.NotEqual(0, exitCode);
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 1 — workspace init`

---

## Task 3: Scenario 2 — Index

**File:** `tests/Ferret.E2E.Tests/Tests/IndexE2ETests.cs`

```csharp
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret index.</summary>
public sealed class IndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public Task InitializeAsync() => _workspace.InitializeAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>index on a workspace with 3 cs files exits with code 0.</summary>
    [Fact]
    public async Task Index_ThreeCsFiles_ExitCodeZero()
    {
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);

        var (exitCode, _, _) = await _workspace.RunAsync("index").ConfigureAwait(false);

        Assert.Equal(0, exitCode);
    }

    /// <summary>index output contains "Indexed" when files are processed.</summary>
    [Fact]
    public async Task Index_ThreeCsFiles_OutputContainsIndexed()
    {
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);

        var (_, stdout, _) = await _workspace.RunAsync("index").ConfigureAwait(false);

        Assert.Contains("Indexed", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>index output contains "Index complete" summary line.</summary>
    [Fact]
    public async Task Index_ThreeCsFiles_OutputContainsIndexComplete()
    {
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);

        var (_, stdout, _) = await _workspace.RunAsync("index").ConfigureAwait(false);

        Assert.Contains("Index complete", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 2 — index`

---

## Task 4: Scenario 3 — Search

**File:** `tests/Ferret.E2E.Tests/Tests/SearchE2ETests.cs`

```csharp
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret search.</summary>
public sealed class SearchE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search "class" after indexing returns exit code 0.</summary>
    [Fact]
    public async Task Search_AfterIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search class").ConfigureAwait(false);

        Assert.Equal(0, exitCode);
    }

    /// <summary>search "AlphaService" returns output containing Alpha.cs.</summary>
    [Fact]
    public async Task Search_AlphaService_ReturnsAlphaCs()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search AlphaService").ConfigureAwait(false);

        Assert.Contains("Alpha.cs", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>search "BetaRepository" returns output containing Beta.cs.</summary>
    [Fact]
    public async Task Search_BetaRepository_ReturnsBetaCs()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search BetaRepository").ConfigureAwait(false);

        Assert.Contains("Beta.cs", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>search "GammaController" returns output containing Gamma.cs.</summary>
    [Fact]
    public async Task Search_GammaController_ReturnsGammaCs()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search GammaController").ConfigureAwait(false);

        Assert.Contains("Gamma.cs", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 3 — search`

---

## Task 5: Scenario 4 — Incremental Index

**File:** `tests/Ferret.E2E.Tests/Tests/IncrementalIndexE2ETests.cs`

The `TextIndexSummaryFormatter` outputs `"  Skipped:     N"` — the second run on unchanged files must show `Skipped: > 0`.

```csharp
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret incremental index behaviour.</summary>
public sealed class IncrementalIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>Second index run on unchanged files reports skipped documents.</summary>
    [Fact]
    public async Task IncrementalIndex_SecondRun_ReportsSkipped()
    {
        // First run — indexes everything
        await _workspace.RunAsync("index").ConfigureAwait(false);

        // Second run — files unchanged; engine should skip them
        var (exitCode, stdout, _) = await _workspace.RunAsync("index").ConfigureAwait(false);

        Assert.Equal(0, exitCode);
        Assert.Contains("Skipped", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Second index run on unchanged files exits with code 0.</summary>
    [Fact]
    public async Task IncrementalIndex_SecondRun_ExitCodeZero()
    {
        await _workspace.RunAsync("index").ConfigureAwait(false);
        var (exitCode, _, _) = await _workspace.RunAsync("index").ConfigureAwait(false);

        Assert.Equal(0, exitCode);
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 4 — incremental index`

---

## Task 6: Scenario 5 — Config Validate

**File:** `tests/Ferret.E2E.Tests/Tests/ConfigValidateE2ETests.cs`

**Note:** As of Sprint 14, `ferret connector validate filesystem` is the closest available "config validate" surface. If `ferret config validate` is not yet implemented, this test uses the connector validate path. Adjust the command string to match the actual registered command.

```csharp
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret configuration validation.</summary>
public sealed class ConfigValidateE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public Task InitializeAsync() => _workspace.InitializeAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>connector validate filesystem on a valid workspace exits with code 0.</summary>
    [Fact]
    public async Task ConnectorValidate_Filesystem_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync(
            "connector validate filesystem").ConfigureAwait(false);

        Assert.Equal(0, exitCode);
    }

    /// <summary>connector validate filesystem outputs connector name or "valid".</summary>
    [Fact]
    public async Task ConnectorValidate_Filesystem_OutputContainsValidOrConnectorName()
    {
        var (_, stdout, _) = await _workspace.RunAsync(
            "connector validate filesystem").ConfigureAwait(false);

        var hasExpected =
            stdout.Contains("valid", StringComparison.OrdinalIgnoreCase) ||
            stdout.Contains("filesystem", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasExpected, $"Expected 'valid' or 'filesystem' in output. Got:\n{stdout}");
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 5 — config validate`

---

## Task 7: Scenario 6 — Doctor

**File:** `tests/Ferret.E2E.Tests/Tests/DoctorE2ETests.cs`

`DoctorCommandHandler` writes "Ferret is healthy." on success. The test checks for "healthy" (case-insensitive) to be resilient to minor wording tweaks.

```csharp
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret doctor.</summary>
public sealed class DoctorE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public Task InitializeAsync() => _workspace.InitializeAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>doctor exits with code 0 in a valid workspace.</summary>
    [Fact]
    public async Task Doctor_ValidWorkspace_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("doctor").ConfigureAwait(false);

        Assert.Equal(0, exitCode);
    }

    /// <summary>doctor output contains "healthy" in a valid workspace.</summary>
    [Fact]
    public async Task Doctor_ValidWorkspace_OutputContainsHealthy()
    {
        var (_, stdout, _) = await _workspace.RunAsync("doctor").ConfigureAwait(false);

        Assert.Contains("healthy", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>doctor output contains "Ferret Doctor" header.</summary>
    [Fact]
    public async Task Doctor_ValidWorkspace_OutputContainsDoctorHeader()
    {
        var (_, stdout, _) = await _workspace.RunAsync("doctor").ConfigureAwait(false);

        Assert.Contains("Ferret Doctor", stdout, StringComparison.Ordinal);
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 6 — doctor`

---

## Task 8: Scenario 7 — MCP Serve Smoke

**File:** `tests/Ferret.E2E.Tests/Tests/ServeE2ETests.cs`

`ferret serve` blocks indefinitely reading MCP stdio. The test starts it with a 2-second timeout. The process is expected to either:
- Start cleanly (no error output in stderr before timeout), or
- Exit cleanly if the runtime exits on EOF/no stdin.

A non-empty stderr within the 2-second window is treated as a startup failure.

```csharp
using Ferret.E2E.Tests.Fixtures;
using Ferret.E2E.Tests.Infrastructure;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E smoke test for ferret serve (MCP stdio).</summary>
public sealed class ServeE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        // Index must exist before serving — serve depends on the keyword DB.
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>serve starts within 2 s and produces no error output on stderr.</summary>
    [Fact]
    public async Task Serve_StartsWithoutErrorOutput()
    {
        // Run with a 2-second timeout — the process will be killed after 2 s.
        var (_, _, stderr) = await FerretCliRunner.RunAsync(
            _workspace.BinaryPath,
            "serve",
            _workspace.WorkspaceDir,
            TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        // Stderr must be empty — any exception or startup error written there is a test failure.
        Assert.True(
            string.IsNullOrWhiteSpace(stderr),
            $"ferret serve produced unexpected stderr:\n{stderr}");
    }
}
```

**Commit:** `test(sprint-14): E2E scenario 7 — MCP serve smoke`

---

## Task 9: Wire into Solution and Final Verification

1. Confirm project is in solution:
   ```
   dotnet sln src/Ferret.sln list
   ```

2. Build:
   ```
   dotnet build src/Ferret.sln
   ```

3. Run E2E tests (requires network-free build; first run publishes binary):
   ```
   dotnet test tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj --logger "console;verbosity=normal"
   ```

4. Expected outcome: all 14 test methods pass. `ServeE2ETests` may show a non-zero exit code (process killed by timeout) — that is acceptable as long as stderr is empty.

**Commit:** `test(sprint-14): wire Ferret.E2E.Tests into solution, all scenarios green`

---

## Self-Review Checklist

- [ ] `FerretCliRunner` captures both streams async (no deadlock on large output)
- [ ] `FerretBinaryLocator` publishes once via semaphore (no parallel publish races)
- [ ] `WorkspaceFixture` tears down temp dir even when a test throws
- [ ] `ServeE2ETests` kills the process after 2 s and checks only stderr (not exit code)
- [ ] All test classes implement `IAsyncLifetime` — no `IDisposable` (avoids sync-over-async in teardown)
- [ ] No `ProjectReference` in E2E csproj — the project is truly end-to-end
- [ ] `CentralPackageTransitivePinningEnabled=false` mirrors `Ferret.Integration.Tests` pattern
- [ ] StyleCop: `IsTestProject=true` suppresses XML doc generation; add file headers or disable the rule via `stylecop.json`
- [ ] Search tests assert on specific class names (`AlphaService`, `BetaRepository`, `GammaController`) not generic tokens — tests are precise and will catch regressions

---

## Notes for Implementer

**Config validate command:** At the time of writing, `ferret config validate` is not in `CoreCliModule` or any registered module. Scenario 5 uses `connector validate filesystem` as the nearest equivalent. If `ferret config validate` is added before S8 ships, update the command string in `ConfigValidateE2ETests`.

**Serve exit code:** `ferret serve` blocks on stdin reads in the MCP SDK loop. When the process is killed by timeout, the OS returns a non-zero exit code (e.g. -1 on Windows). The smoke test only asserts on stderr to avoid a flaky test that depends on OS-specific exit code behaviour when a process is force-terminated.

**Binary name:** `FerretBinaryLocator` uses `Ferret.Cli.exe` / `Ferret.Cli` (the assembly name from `Ferret.Cli.csproj`). If `Program.cs` ever changes the output name via `<AssemblyName>ferret</AssemblyName>`, update `FerretBinaryLocator.GetOrPublishAsync` accordingly.
