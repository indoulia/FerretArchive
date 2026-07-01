# Doctor Parser Platform Report — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an informative, human-readable **Parser Platform** report to `ferret doctor` (installed parsers, extension coverage, parseable/opaque extensions, parser packages) with a `--verbose` mode, so "my file isn't indexing" is answerable in one command.

**Architecture:** Additive. `MimeTypeResolver` gains category-grouped extension access; a new `ParserPlatformReport` renderer turns the composed parser set + resolver data into the report text; `DoctorCommandHandler` renders it after the existing health checks, using the **already-global** `--verbose` flag (`IFerretContext.Verbosity`). No parsing/registry/behavior changes.

**Tech Stack:** .NET 9, C#, xUnit, `Microsoft.Extensions.DependencyInjection`.

**Spec:** `docs/superpowers/specs/2026-07-01-doctor-parser-report-design.md`
**Branch:** `feat/enterprise-content-pack-1` (follow-up to the Enterprise Content Pack 1 RC).

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props`. `TreatWarningsAsErrors=true`, `AnalysisMode=All`, StyleCop enforced — code must be analyzer-clean (braces on all control bodies; `using` directives ordered `System.*` then alphabetical; static members before instance; one public type per file (SA1402); public members in non-test projects need XML doc comments).
- **Human-readable only** — no JSON/machine output this change.
- **No new dependencies.**
- **`--verbose` is global already** (`GlobalOptions.Verbose` → `IFerretContext.Verbosity == VerbosityLevel.Verbose`). Do NOT add a per-command option.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | `MimeTypeResolver.ExtensionsInCategory` + `ExtensionMediaType` | `Ferret.ParserPlatform` |
| 2 | `ParserPlatformReport` renderer + capturing formatter + unit tests | `Ferret.Cli`, `Ferret.Cli.Tests` |
| 3 | Wire report into `ferret doctor`; simplify `InstalledParsersCheck` line | `Ferret.Cli` |
| 4 | E2E assertion of the report through the published binary | `Ferret.E2E.Tests` |
| 5 | Manual updates (cli, parsers, troubleshooting decision tree) | `Ferret.Manual` |

Task 1 stands alone. Task 2 depends on Task 1 (report reads the resolver). Task 3 depends on Task 2 (wires the report). Task 4 depends on Task 3 (report must be live in the binary). Task 5 is documentation.

---

### Task 1: `MimeTypeResolver.ExtensionsInCategory` + `ExtensionMediaType`

**Files:**
- Create: `src/Ferret.ParserPlatform/ExtensionMediaType.cs`
- Modify: `src/Ferret.ParserPlatform/MimeTypeResolver.cs` (add `ExtensionsInCategory`, after `KnownExtensionCount`)
- Test: `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs`

**Interfaces:**
- Consumes: `MediaCategory` (`Ferret.Core.Documents`), the private `Map` and `MediaTypeInfo.Category`/`.MediaType`.
- Produces: `public sealed record ExtensionMediaType(string Extension, string MediaType)`; `public static IReadOnlyList<ExtensionMediaType> MimeTypeResolver.ExtensionsInCategory(MediaCategory category)` — ordinal-sorted by extension.

- [ ] **Step 1: Write the failing tests**

Add to `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs` (same class, append):

```csharp
[Fact]
public void ExtensionsInCategory_ParseableBinary_IsExactlyTheThreeOfficeFormats()
{
    var parseable = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable)
        .Select(e => e.Extension).ToList();
    Assert.Equal([".docx", ".pdf", ".xlsx"], parseable); // ordinal-sorted
}

[Fact]
public void ExtensionsInCategory_Parseable_CarriesMediaType()
{
    var docx = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable)
        .Single(e => e.Extension == ".docx");
    Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", docx.MediaType);
}

[Fact]
public void ExtensionsInCategory_Opaque_ContainsKnownBinaries_AndIsOrdinalSorted()
{
    var opaque = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryOpaque)
        .Select(e => e.Extension).ToList();
    Assert.Contains(".dll", opaque);
    Assert.Contains(".exe", opaque);
    Assert.Contains(".zip", opaque);
    Assert.Equal(opaque.OrderBy(x => x, StringComparer.Ordinal), opaque);
}

[Fact]
public void ExtensionsInCategory_Counts_SumToKnownPlusOpaque()
{
    var text = MimeTypeResolver.ExtensionsInCategory(MediaCategory.Text).Count;
    var parseable = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable).Count;
    // Text + Parseable is exactly the "known/indexable" count.
    Assert.Equal(MimeTypeResolver.KnownExtensionCount, text + parseable);
}
```

Add `using Ferret.Core.Documents;` to the test file if not already present (for `MediaCategory`).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter ExtensionsInCategory`
Expected: FAIL — `ExtensionsInCategory` / `ExtensionMediaType` do not exist.

- [ ] **Step 3: Create the `ExtensionMediaType` record**

```csharp
// src/Ferret.ParserPlatform/ExtensionMediaType.cs
namespace Ferret.ParserPlatform;

/// <summary>An extension mapped to the media type it resolves to (a view over the resolver map).</summary>
/// <param name="Extension">The file extension, including the leading dot (e.g. <c>.pdf</c>).</param>
/// <param name="MediaType">The media type the extension resolves to.</param>
public sealed record ExtensionMediaType(string Extension, string MediaType);
```

- [ ] **Step 4: Add `ExtensionsInCategory` to `MimeTypeResolver`**

In `src/Ferret.ParserPlatform/MimeTypeResolver.cs`, immediately **after** the `KnownExtensionCount` property and **before** the `Resolve` method (static members before instance):

```csharp
    /// <summary>Gets the mapped extensions in the given category, ordered by extension (ordinal).</summary>
    /// <param name="category">The media category to filter by.</param>
    /// <returns>The matching extensions and their media types.</returns>
    public static IReadOnlyList<ExtensionMediaType> ExtensionsInCategory(MediaCategory category) =>
        Map.Where(kv => kv.Value.Category == category)
            .Select(kv => new ExtensionMediaType(kv.Key, kv.Value.MediaType))
            .OrderBy(e => e.Extension, StringComparer.Ordinal)
            .ToList();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter ExtensionsInCategory`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Ferret.ParserPlatform/ExtensionMediaType.cs src/Ferret.ParserPlatform/MimeTypeResolver.cs tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs
git commit -m "feat(parsers): expose extensions grouped by MediaCategory on MimeTypeResolver"
```

---

### Task 2: `ParserPlatformReport` renderer + capturing formatter + unit tests

**Files:**
- Create: `src/Ferret.Cli/Diagnostics/ParserPlatformReport.cs`
- Create: `tests/Ferret.Cli.Tests/Infrastructure/CapturingOutputFormatter.cs`
- Test: `tests/Ferret.Cli.Tests/Diagnostics/ParserPlatformReportTests.cs`

**Interfaces:**
- Consumes: `IContentParser` + `ParserDescriptor` (`Name`/`Priority`/`SupportedMediaTypes`) (`Ferret.Core.Documents`); `MimeTypeResolver.ExtensionsInCategory` + `ExtensionMediaType` (Task 1); `IOutputFormatter` (`Ferret.Cli.Cli`); `MediaCategory` (`Ferret.Core.Documents`).
- Produces: `internal sealed class ParserPlatformReport { internal ParserPlatformReport(IReadOnlyList<IContentParser> parsers); internal void Render(IOutputFormatter output, bool verbose); }`.

- [ ] **Step 1: Add a capturing `IOutputFormatter` test double**

```csharp
// tests/Ferret.Cli.Tests/Infrastructure/CapturingOutputFormatter.cs
using Ferret.Cli.Cli;

namespace Ferret.Cli.Tests.Infrastructure;

/// <summary>Captures formatter output for assertions. Records the raw text of every write.</summary>
internal sealed class CapturingOutputFormatter : IOutputFormatter
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public string Text => string.Join("\n", _lines);

    public void WriteLine(string text = "") => _lines.Add(text);

    public void WriteSuccess(string message) => _lines.Add("✓ " + message);

    public void WriteError(string message) => _lines.Add("✗ " + message);

    public void WriteVerbose(string message) => _lines.Add(message);
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/Ferret.Cli.Tests/Diagnostics/ParserPlatformReportTests.cs
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Tests.Infrastructure;
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Parsers;
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class ParserPlatformReportTests
{
    // Registration order: platform text parsers, then PDF, then Office (as ParserPackModule composes them).
    private static IReadOnlyList<IContentParser> AllParsers() =>
    [
        new PlainTextParser(),
        new MarkdownParser(),
        new JsonParser(),
        new CsvParser(new ParserOptions()),
        new PdfParser(new ParserOptions()),
        new WordParser(new ParserOptions()),
        new ExcelParser(new ParserOptions()),
    ];

    private static string Render(IReadOnlyList<IContentParser> parsers, bool verbose)
    {
        var formatter = new CapturingOutputFormatter();
        new ParserPlatformReport(parsers).Render(formatter, verbose);
        return formatter.Text;
    }

    [Fact]
    public void Default_HasAllSectionsAndCoverageTotals()
    {
        var text = Render(AllParsers(), verbose: false);

        Assert.Contains("Parser Platform", text, StringComparison.Ordinal);
        Assert.Contains("Installed Parsers (7)", text, StringComparison.Ordinal);
        Assert.Contains("Extension Coverage", text, StringComparison.Ordinal);
        Assert.Contains("Parseable Binary: 3", text, StringComparison.Ordinal);
        Assert.Contains("Known Extensions:", text, StringComparison.Ordinal);
        Assert.Contains(".docx", text, StringComparison.Ordinal);
        Assert.Contains("currently treated as opaque binary", text, StringComparison.Ordinal);
        Assert.Contains("Parser Packages", text, StringComparison.Ordinal);
        Assert.Contains("Ferret.Parsers.Pdf", text, StringComparison.Ordinal);
        Assert.Contains("Ferret.Parsers.Office", text, StringComparison.Ordinal);
        Assert.Contains("Ferret.ParserPlatform", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrationOrder_IsStable_NotSorted()
    {
        var text = Render(AllParsers(), verbose: false);
        int[] positions =
        [
            text.IndexOf("Plain Text Parser", StringComparison.Ordinal),
            text.IndexOf("Markdown Parser", StringComparison.Ordinal),
            text.IndexOf("JSON Parser", StringComparison.Ordinal),
            text.IndexOf("CSV Parser", StringComparison.Ordinal),
            text.IndexOf("PDF Parser", StringComparison.Ordinal),
            text.IndexOf("Word (DOCX) Parser", StringComparison.Ordinal),
            text.IndexOf("Excel (XLSX) Parser", StringComparison.Ordinal),
        ];
        Assert.All(positions, p => Assert.True(p >= 0));
        var sorted = positions.OrderBy(x => x).ToArray();
        Assert.Equal(sorted, positions); // appear in registration order, not alphabetical
    }

    [Fact]
    public void Default_SummarizesOpaque_WithHint()
    {
        var text = Render(AllParsers(), verbose: false);
        Assert.Contains("run `ferret doctor --verbose` for the full list", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".zip", text, StringComparison.Ordinal); // sorts late, beyond the sample
    }

    [Fact]
    public void Verbose_ShowsAllOpaque_PriorityMediaType_AndParseableMime()
    {
        var text = Render(AllParsers(), verbose: true);
        Assert.DoesNotContain("run `ferret doctor --verbose`", text, StringComparison.Ordinal);
        Assert.Contains(".zip", text, StringComparison.Ordinal);           // full opaque list
        Assert.Contains("Priority: 200", text, StringComparison.Ordinal);  // per-parser detail
        Assert.Contains("Media Type: application/pdf", text, StringComparison.Ordinal);
        Assert.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document", text, StringComparison.Ordinal); // parseable MIME
    }

    [Fact]
    public void EmptyParsers_RendersSafeguard_NoException()
    {
        var text = Render([], verbose: false);
        Assert.Contains("Installed Parsers (0)", text, StringComparison.Ordinal);
        Assert.Contains("No parsers are registered.", text, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.Cli.Tests --filter ParserPlatformReportTests`
Expected: FAIL — `ParserPlatformReport` does not exist.

> The test project already references the parser packages transitively via `Ferret.Cli` (which references `Ferret.Parsers`). If the parser types are not resolvable, add `ProjectReference`s to `Ferret.Parsers.Pdf` and `Ferret.Parsers.Office` in `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`.

- [ ] **Step 4: Implement `ParserPlatformReport`**

```csharp
// src/Ferret.Cli/Diagnostics/ParserPlatformReport.cs
using System.Globalization;

using Ferret.Cli.Cli;
using Ferret.Core.Documents;
using Ferret.ParserPlatform;

namespace Ferret.Cli.Diagnostics;

/// <summary>Renders the informational "Parser Platform" section of `ferret doctor`: installed
/// parsers, extension coverage, parseable/opaque extensions, and loaded parser packages.</summary>
internal sealed class ParserPlatformReport
{
    private const int OpaqueSampleSize = 8;

    private readonly IReadOnlyList<IContentParser> _parsers;

    /// <summary>Initializes a new instance of the <see cref="ParserPlatformReport"/> class.</summary>
    /// <param name="parsers">The composed content parsers, in registration order.</param>
    internal ParserPlatformReport(IReadOnlyList<IContentParser> parsers)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        _parsers = parsers;
    }

    /// <summary>Renders the report to the output.</summary>
    /// <param name="output">The output formatter.</param>
    /// <param name="verbose">When true, shows all opaque extensions plus per-parser priority/media
    /// type and parseable-extension MIME mappings.</param>
    internal void Render(IOutputFormatter output, bool verbose)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.WriteLine();
        output.WriteLine("Parser Platform");
        output.WriteLine();

        RenderInstalledParsers(output, _parsers, verbose);
        output.WriteLine();
        RenderExtensionCoverage(output);
        output.WriteLine();
        RenderParseableBinary(output, verbose);
        output.WriteLine();
        RenderOpaqueBinary(output, verbose);
        output.WriteLine();
        RenderPackages(output, _parsers);
    }

    private static void RenderInstalledParsers(IOutputFormatter output, IReadOnlyList<IContentParser> parsers, bool verbose)
    {
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Installed Parsers ({parsers.Count})"));
        if (parsers.Count == 0)
        {
            output.WriteLine("  No parsers are registered.");
            return;
        }

        foreach (var parser in parsers)
        {
            var descriptor = parser.Descriptor;
            output.WriteLine("  ✓ " + descriptor.Name);
            if (verbose)
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"      Priority: {descriptor.Priority}"));
                output.WriteLine("      Media Type: " + string.Join(", ", descriptor.SupportedMediaTypes));
            }
        }
    }

    private static void RenderExtensionCoverage(IOutputFormatter output)
    {
        var text = MimeTypeResolver.ExtensionsInCategory(MediaCategory.Text).Count;
        var parseable = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable).Count;
        var opaque = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryOpaque).Count;

        output.WriteLine("Extension Coverage");
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Text: {text}"));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Parseable Binary: {parseable}"));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Opaque Binary: {opaque}"));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Known Extensions: {text + parseable + opaque}"));
    }

    private static void RenderParseableBinary(IOutputFormatter output, bool verbose)
    {
        var entries = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable);
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Parseable Binary ({entries.Count})"));
        if (verbose)
        {
            foreach (var entry in entries)
            {
                output.WriteLine("  " + entry.Extension + " → " + entry.MediaType);
            }
        }
        else
        {
            output.WriteLine("  " + string.Join("  ", entries.Select(e => e.Extension)));
        }
    }

    private static void RenderOpaqueBinary(IOutputFormatter output, bool verbose)
    {
        var extensions = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryOpaque)
            .Select(e => e.Extension).ToList();
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Opaque Binary ({extensions.Count}) — currently treated as opaque binary"));

        if (verbose || extensions.Count <= OpaqueSampleSize)
        {
            output.WriteLine("  " + string.Join(" ", extensions));
        }
        else
        {
            output.WriteLine("  " + string.Join(" ", extensions.Take(OpaqueSampleSize)) + " ...");
            output.WriteLine("  run `ferret doctor --verbose` for the full list");
        }
    }

    private static void RenderPackages(IOutputFormatter output, IReadOnlyList<IContentParser> parsers)
    {
        var packages = parsers
            .Select(p => p.GetType().Assembly.GetName().Name ?? "(unknown)")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Parser Packages ({packages.Count})"));
        if (packages.Count == 0)
        {
            output.WriteLine("  No parser packages loaded.");
            return;
        }

        foreach (var package in packages)
        {
            output.WriteLine("  " + package);
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.Cli.Tests --filter ParserPlatformReportTests`
Expected: PASS (5 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Ferret.Cli/Diagnostics/ParserPlatformReport.cs tests/Ferret.Cli.Tests/Infrastructure/CapturingOutputFormatter.cs tests/Ferret.Cli.Tests/Diagnostics/ParserPlatformReportTests.cs
git commit -m "feat(cli): add ParserPlatformReport renderer for doctor parser diagnostics"
```

---

### Task 3: Wire the report into `ferret doctor`; simplify the health-check line

**Files:**
- Modify: `src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs`
- Modify: `src/Ferret.Cli/Commands/CoreCliModule.cs`
- Modify: `src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs`
- Test: `tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ParserPlatformReport` (Task 2); `IFerretContext.Verbosity`, `VerbosityLevel` (`Ferret.Cli.Cli`).
- Produces: `DoctorCommandHandler(IEnumerable<IDiagnosticCheck> checks, ParserPlatformReport parserReport)`.

- [ ] **Step 1: Write the failing tests**

Append to `tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs`:

```csharp
[Fact]
public async Task Doctor_PrintsParserPlatformSection()
{
    using var sw = new StringWriter();
    await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
    string output = sw.ToString();
    Assert.Contains("Parser Platform", output, StringComparison.Ordinal);
    Assert.Contains("Installed Parsers", output, StringComparison.Ordinal);
    Assert.Contains("Excel (XLSX) Parser", output, StringComparison.Ordinal);
    Assert.Contains("Extension Coverage", output, StringComparison.Ordinal);
    Assert.Contains("Parser Packages", output, StringComparison.Ordinal);
}

[Fact]
public async Task Doctor_Verbose_ShowsParserPriorityAndFullOpaqueList()
{
    using var sw = new StringWriter();
    await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor", "--verbose"]);
    string output = sw.ToString();
    Assert.Contains("Priority:", output, StringComparison.Ordinal);
    Assert.Contains("Media Type:", output, StringComparison.Ordinal);
    Assert.DoesNotContain("run `ferret doctor --verbose`", output, StringComparison.Ordinal);
}

[Fact]
public async Task Doctor_Default_SummarizesOpaqueExtensions()
{
    using var sw = new StringWriter();
    await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
    Assert.Contains("run `ferret doctor --verbose` for the full list", sw.ToString(), StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.Cli.Tests --filter DoctorCommandHandlerTests`
Expected: FAIL — the report section is not rendered yet.

- [ ] **Step 3: Add the report to `DoctorCommandHandler`**

Replace the body of `src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Discovers all IDiagnosticCheck instances from registered modules and runs them, then prints
///      the informational Parser Platform report. Adding a new module automatically extends doctor.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class DoctorCommandHandler : ICommandHandler
{
    private readonly IReadOnlyList<IDiagnosticCheck> _checks;
    private readonly ParserPlatformReport _parserReport;

    internal DoctorCommandHandler(IEnumerable<IDiagnosticCheck> checks, ParserPlatformReport parserReport)
    {
        _checks = checks.ToList();
        _parserReport = parserReport;
    }

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine("Ferret Doctor");
        context.Services.Output.WriteLine();

        bool healthy = await DiagnosticRunner.RunAsync(_checks, context).ConfigureAwait(false);

        _parserReport.Render(context.Services.Output, context.Verbosity == VerbosityLevel.Verbose);

        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine(healthy
            ? "Ferret is healthy."
            : "Ferret has issues. Review the checks above.");

        return healthy ? CommandResult.Success : CommandResult.Failure;
    }
}
```

- [ ] **Step 4: Compose the parser set once and build the report in `CoreCliModule`**

In `src/Ferret.Cli/Commands/CoreCliModule.cs`, replace the `ConfigureServices` parser wiring and the `InstalledParsersCheck` block so the composed parser list is built once and shared.

Change `ConfigureServices` (the `_checks`/handler registration lines) to:

```csharp
        var parsers = ComposeParsers();
        _checks = BuildChecks(config, workspaceRoot, parsers).ToList();
        var parserReport = new ParserPlatformReport(parsers);
        services.AddTransient<DoctorCommandHandler>(_ => new DoctorCommandHandler(_checks, parserReport));
```

Add the `ComposeParsers` helper (private static) and change `BuildChecks` to accept the parser list:

```csharp
    private static IReadOnlyList<Ferret.Core.Documents.IContentParser> ComposeParsers()
    {
        var parserServices = new ServiceCollection();
        Ferret.Parsers.ParserPackModule.ConfigureServices(parserServices);
        using var provider = parserServices.BuildServiceProvider();
        // Parser instances are plain objects; they remain valid after the provider is disposed.
        return provider.GetServices<Ferret.Core.Documents.IContentParser>().ToList();
    }
```

Change `BuildChecks` signature and its parser block:

```csharp
    private static IEnumerable<IDiagnosticCheck> BuildChecks(
        IConfiguration? config, string workspaceRoot, IReadOnlyList<Ferret.Core.Documents.IContentParser> parsers)
    {
        yield return new ConfigurationCheck();
        yield return new RuntimeLifecycleCheck();
        yield return new WorkspaceRootCheck(workspaceRoot);
        yield return new FerretConfigDirCheck(workspaceRoot);

        yield return new InstalledParsersCheck(
            parsers, parsers.Count, Ferret.ParserPlatform.MimeTypeResolver.KnownExtensionCount);

        // ... (dbPath + IndexFreshnessCheck + AiProviderConfigCheck unchanged) ...
    }
```

Also update the `GetDiagnosticChecks()` fallback (which calls `BuildChecks(null, Environment.CurrentDirectory)`) to pass a composed list:

```csharp
    public override IEnumerable<IDiagnosticCheck> GetDiagnosticChecks() =>
        _checks ?? BuildChecks(null, Environment.CurrentDirectory, ComposeParsers());
```

Add `using Ferret.Cli.Diagnostics;` if not already present (it is — `Checks` namespace is imported; `ParserPlatformReport` is in `Ferret.Cli.Diagnostics`, so add `using Ferret.Cli.Diagnostics;`).

- [ ] **Step 5: Simplify the `InstalledParsersCheck` line (drop the duplicated name list)**

In `src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs`, the detailed section now owns the parser names. Change the `Name` property to drop the parenthesized list; keep the health summary:

```csharp
    /// <inheritdoc/>
    public string Name => string.Create(
        CultureInfo.InvariantCulture,
        $"Parser platform: {_parserCount} parsers, {_supportedExtensionCount} extensions");
```

The `_parserNames` field is no longer used — remove the field and the `.Select(...).OrderBy(...)` line in the constructor (keep the `parsers` null-check). The constructor becomes:

```csharp
    internal InstalledParsersCheck(
        IReadOnlyList<IContentParser> parsers,
        int parserCount,
        int supportedExtensionCount)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        _parserCount = parserCount;
        _supportedExtensionCount = supportedExtensionCount;
    }
```

Remove the now-unused `private readonly IReadOnlyList<string> _parserNames;` field. (Existing `InstalledParsersCheckTests` assert only `Passed`/`IsWarning`, so they still pass unchanged.)

- [ ] **Step 6: Run the CLI test suite**

Run: `dotnet test tests/Ferret.Cli.Tests`
Expected: PASS — new `DoctorCommandHandlerTests` cases green; existing doctor tests unaffected.

- [ ] **Step 7: Manually confirm the report**

Run: `dotnet run --project src/Ferret.Cli -- doctor` and `dotnet run --project src/Ferret.Cli -- doctor --verbose`
Expected: the "Parser Platform" section renders; `--verbose` shows per-parser Priority/Media Type and the full opaque list.

- [ ] **Step 8: Commit**

```bash
git add src/Ferret.Cli/Commands/Handlers/DoctorCommandHandler.cs src/Ferret.Cli/Commands/CoreCliModule.cs src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs tests/Ferret.Cli.Tests/Commands/DoctorCommandHandlerTests.cs
git commit -m "feat(cli): render the Parser Platform report in ferret doctor (verbose-aware)"
```

---

### Task 4: E2E assertion through the published binary

**Files:**
- Modify: `tests/Ferret.E2E.Tests/Tests/DoctorE2ETests.cs`

**Interfaces:**
- Consumes: `WorkspaceFixture.RunAsync("doctor")` (existing).

- [ ] **Step 1: Read the existing test to match its fixture pattern**

Open `tests/Ferret.E2E.Tests/Tests/DoctorE2ETests.cs` and mirror how it runs `doctor` and asserts on stdout (it uses `WorkspaceFixture` + `RunAsync`).

- [ ] **Step 2: Add the failing E2E test**

Add a test method mirroring the file's existing structure (fixture field + `RunAsync`):

```csharp
[Fact]
public async Task Doctor_ReportsParserPlatformSection()
{
    var (_, stdout, _) = await _workspace.RunAsync("doctor");

    Assert.Contains("Parser Platform", stdout, StringComparison.Ordinal);
    Assert.Contains("Excel (XLSX) Parser", stdout, StringComparison.Ordinal);
    Assert.Contains("Parser Packages", stdout, StringComparison.Ordinal);
}
```

> Match the fixture field name used in the existing class (e.g. `_workspace`); if the class constructs the fixture per-test, follow that pattern instead. `doctor` exits non-zero in an uninitialized workspace, but the report is printed regardless — assert only on stdout content, not exit code.

- [ ] **Step 3: Run the E2E test**

Run: `dotnet test tests/Ferret.E2E.Tests --filter DoctorE2ETests`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add tests/Ferret.E2E.Tests/Tests/DoctorE2ETests.cs
git commit -m "test(e2e): assert doctor emits the Parser Platform section"
```

---

### Task 5: Manual updates

**Files:**
- Modify: `src/Ferret.Manual/Content/reference/cli.md`
- Modify: `src/Ferret.Manual/Content/user-guide/parsers.md`
- Modify: `src/Ferret.Manual/Content/troubleshooting.md`

**Interfaces:** none (documentation).

- [ ] **Step 1: Document `doctor` + `--verbose` in `reference/cli.md`**

Find the `doctor` entry (or the commands list) and add/extend it. Match the file's existing formatting. Content to convey:

> `ferret doctor` — validate the local installation and print a **Parser Platform** report:
> installed parsers, extension coverage (Text / Parseable Binary / Opaque Binary / Known Extensions),
> the parseable and opaque extension lists, and loaded parser packages.
> Add `--verbose` to show every opaque extension plus each parser's priority and media type.

- [ ] **Step 2: Note the report in `user-guide/parsers.md`**

Add a short paragraph (match existing headings):

> To see which parsers are active and which file types are indexable, run `ferret doctor`.
> Its **Parser Platform** section lists the installed parsers, how many extensions are indexable
> vs. treated as opaque binary, and which parser packages are loaded. Use `ferret doctor --verbose`
> for the complete opaque-extension list and per-parser details.

- [ ] **Step 3: Add the "My file isn't indexing" decision tree to `troubleshooting.md`**

Add a new section (match the file's heading level and style):

````markdown
## My file isn't indexing

Run `ferret doctor` and check the **Parser Platform** section:

```
Is the extension listed by `ferret doctor`?
  ├─ No  → unsupported extension (not mapped) — the file is skipped
  └─ Yes → Which category?
           ├─ Parseable Binary → is the parser installed? (doctor lists it)
           │     ├─ Yes → re-run `ferret index`
           │     └─ No  → install/enable the parser package
           ├─ Text            → check .ferretignore and your workspace scope
           └─ Opaque Binary   → currently treated as opaque; not indexed
```

Run `ferret doctor --verbose` to see the full opaque-extension list and per-parser details.
````

- [ ] **Step 4: Build the Manual project (confirm content compiles/embeds)**

Run: `dotnet build src/Ferret.Manual`
Expected: build succeeds (Markdown content is embedded; no code change).

- [ ] **Step 5: Commit**

```bash
git add src/Ferret.Manual/Content/reference/cli.md src/Ferret.Manual/Content/user-guide/parsers.md src/Ferret.Manual/Content/troubleshooting.md
git commit -m "docs(manual): document the doctor Parser Platform report and file-not-indexing triage"
```

---

## Final verification

- [ ] **Full solution build + test**

Run: `dotnet build src/Ferret.sln && dotnet test src/Ferret.sln`
Expected: build clean, all tests green.

- [ ] **Acceptance check**

Confirm each: `ferret doctor` prints the Parser Platform section (installed parsers in registration order, Extension Coverage with Known-Extensions total, parseable list, opaque summarized + hint, packages incl. `Ferret.ParserPlatform`); `--verbose` shows all opaque extensions + per-parser priority/media type + parseable MIME mappings; the empty-parser safeguard renders without throwing; the one-line health check still gates health; no JSON output added; Manual documents the command + triage tree.
