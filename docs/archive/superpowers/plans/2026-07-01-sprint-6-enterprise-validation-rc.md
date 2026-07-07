# Sprint 6 — Enterprise Validation & RC Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the whole pack works together on a realistic multi-format corpus — one integration test that generates a Small enterprise corpus, parses PDF + DOCX + XLSX + CSV through the composed `ParserPackModule` dispatcher, and confirms opaque binaries are excluded — then verify every milestone acceptance criterion and cut the Enterprise Content Pack 1 release candidate.

**Architecture:** This is the milestone's convergence point. Sprints 2–5 delivered each parser, the composition, `ferret doctor`, the corpus generator, and benchmarks in isolation; this sprint adds a single cross-format integration test in `Ferret.Integration.Tests` that exercises the *public* dispatcher path (`IParserDispatcher.DispatchAsync`, what production uses) end-to-end against real generated documents. The opaque-binary exclusion is asserted by dropping a loose `.so` into the tree and confirming the resolver yields `application/octet-stream` and the dispatcher finds no parser. After validation, a milestone acceptance-criteria pass and a finishing-the-branch handoff produce the RC.

**Tech Stack:** .NET 9, C#, xUnit, Microsoft.Extensions.DependencyInjection.

**Milestone spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md` (§ Testing & Deliverables → Acceptance criteria)
**Parent plan (source of reused code):** `docs/superpowers/plans/2026-07-01-parser-pack-1.md` (Task 8)
**Predecessors (hard):** Sprints 1–5 must all be implemented and green. Task 1 here references `SyntheticEnterpriseCorpusGenerator` (Sprint 4) and `ParserPackModule` (Sprints 2/3).

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props`.
- **Central Package Management:** no new packages; `<PackageReference>` carries no `Version` attribute.
- **Public-API validation:** assert through `IParserDispatcher.DispatchAsync` (production's entry point), not the registry (an implementation detail).
- **Generated corpus is not committed:** the test generates into a temp directory and deletes it.
- **No production code changes** are expected in Task 1; if the integration test surfaces a genuine defect, fix it in the owning sprint's project and note it — do not paper over it in the test.
- **RC is validate-and-tag, not re-publish.** Distribution (npm / GitHub Release) is owned by the already-shipped Distribution Platform; this sprint validates the milestone and hands off an integration decision, it does not re-implement release plumbing.
- **StyleCop:** public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | Cross-format integration test (PDF+DOCX+XLSX+CSV; opaque excluded) | `Ferret.Integration.Tests` |
| 2 | Milestone acceptance-criteria verification + RC handoff | (whole solution) |

Task 1 is the enterprise-validation deliverable. Task 2 is the sign-off gate: run the full suite, walk the spec's acceptance criteria, and use the finishing-a-development-branch skill to decide integration.

---

### Task 1: Cross-format integration test (index PDF + DOCX + XLSX + CSV, exclude opaque binaries)

**Files:**
- Modify: `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj` (reference `Ferret.Parsers`, `Ferret.Benchmarks`)
- Create: `tests/Ferret.Integration.Tests/TestAsset.cs` (asset-descriptor helper for this project)
- Create: `tests/Ferret.Integration.Tests/ParserPackIndexingTests.cs`

**Interfaces:**
- Consumes: `SyntheticEnterpriseCorpusGenerator`, `CorpusSize` (Sprint 4); `ParserPackModule` (Sprints 2/3); `IParserDispatcher`, `IMimeTypeResolver`, `MimeTypeResolver`, `ParseResult<Document>`, `ParseResultKind`, `DocumentKind` (`Ferret.Core` / `Ferret.ParserPlatform`); `AssetDescriptor` (`Ferret.Core`).
- Produces: `internal static class TestAsset { static AssetDescriptor For(string path, string mediaType); }`; `public sealed class ParserPackIndexingTests`.

- [ ] **Step 1: Add project references**

In `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj`, add to the ProjectReference `ItemGroup`:

```xml
<ProjectReference Include="..\..\src\Ferret.Parsers\Ferret.Parsers.csproj" />
<ProjectReference Include="..\Ferret.Benchmarks\Ferret.Benchmarks.csproj" />
```

(The project already references `Ferret.Core`, `Ferret.ParserPlatform`, and `Ferret.Connectors.Filesystem`.)

- [ ] **Step 2: Add the asset-descriptor helper**

Mirror the field names of a real `AssetDescriptor` construction in the codebase.

```csharp
// tests/Ferret.Integration.Tests/TestAsset.cs
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Integration.Tests;

/// <summary>Builds an <see cref="AssetDescriptor"/> from a filesystem path for dispatcher tests.</summary>
internal static class TestAsset
{
    /// <summary>Creates an asset descriptor for the given file path and resolved media type.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="mediaType">The resolved media type.</param>
    /// <returns>An <see cref="AssetDescriptor"/>.</returns>
    public static AssetDescriptor For(string path, string mediaType)
    {
        var name = Path.GetFileName(path);
        var uri = new Uri("filesystem:///" + name);
        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("integration"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = name,
            LastModified = DateTimeOffset.UnixEpoch,
            MediaType = mediaType,
        };
    }
}
```

- [ ] **Step 3: Write the integration test**

```csharp
// tests/Ferret.Integration.Tests/ParserPackIndexingTests.cs
using Ferret.Benchmarks.Corpus;
using Ferret.Core.Documents;
using Ferret.Parsers;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Integration.Tests;

public sealed class ParserPackIndexingTests
{
    [Fact]
    public async Task Pdf_Docx_Xlsx_Csv_Parsed_And_Opaque_Binaries_Excluded()
    {
        // 1. Generate a Small corpus (laid out under a realistic enterprise tree).
        var root = Path.Join(Path.GetTempPath(), "pp-int-" + Guid.NewGuid().ToString("N"));
        new SyntheticEnterpriseCorpusGenerator(seed: 7).Generate(CorpusSize.Small, root);

        // 2. Drop a loose opaque binary into the tree (must NOT be parseable).
        var soPath = Path.Join(root, "Engineering", "Source", "native.so");
        await File.WriteAllBytesAsync(soPath, [0x7F, 0x45, 0x4C, 0x46, 0x00, 0x01]);

        // 3. Drop a CSV export into the tree (structure-aware CsvParser in the platform).
        var csvPath = Path.Join(root, "Management", "Notes", "jira-export.csv");
        await File.WriteAllTextAsync(csvPath, "Key,Summary,Severity\nBUG-1,SSO login fails,High\n");

        // 4. Resolve the full parser pack dispatcher (the public API production uses).
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IParserDispatcher>();
        var resolver = (IMimeTypeResolver)new MimeTypeResolver();

        // 5. Parse one document of each format through the dispatcher (recurse the hierarchy by extension).
        var pdfPath = Directory.GetFiles(root, "*.pdf", SearchOption.AllDirectories).OrderBy(p => p).First();
        var docxPath = Directory.GetFiles(root, "*.docx", SearchOption.AllDirectories).OrderBy(p => p).First();
        var xlsxPath = Directory.GetFiles(root, "*.xlsx", SearchOption.AllDirectories).OrderBy(p => p).First();

        var pdfResult = await DispatchFile(dispatcher, resolver, pdfPath);
        var docxResult = await DispatchFile(dispatcher, resolver, docxPath);
        var xlsxResult = await DispatchFile(dispatcher, resolver, xlsxPath);
        var csvResult = await DispatchFile(dispatcher, resolver, csvPath);
        var soResult = await DispatchFile(dispatcher, resolver, soPath);

        // PDF and DOCX: parsed as prose with non-empty text.
        Assert.Equal(ParseResultKind.Success, pdfResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(pdfResult.Value!.PlainText));
        Assert.Equal(ParseResultKind.Success, docxResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(docxResult.Value!.PlainText));

        // XLSX: parsed as Data; a header token from the enterprise archetype is present.
        Assert.Equal(ParseResultKind.Success, xlsxResult.Kind);
        Assert.Equal(DocumentKind.Data, xlsxResult.Value!.Kind);
        Assert.Contains("Priority", xlsxResult.Value!.PlainText, StringComparison.Ordinal);

        // CSV: structure-aware, Data kind, cell value searchable (CsvParser beats PlainTextParser).
        Assert.Equal(ParseResultKind.Success, csvResult.Kind);
        Assert.Equal(DocumentKind.Data, csvResult.Value!.Kind);
        Assert.Contains("SSO login fails", csvResult.Value!.PlainText, StringComparison.Ordinal);

        // Opaque binary: resolver yields application/octet-stream, dispatcher finds no parser.
        Assert.Equal(ParseResultKind.Unsupported, soResult.Kind);

        // Metadata round-trips renderer -> package/document info -> parser, verified per format.
        Assert.True(pdfResult.Value!.Metadata.ContainsKey(DocumentMetadata.PageCount));
        Assert.True(docxResult.Value!.Metadata.ContainsKey(DocumentMetadata.Author));
        Assert.True(xlsxResult.Value!.Metadata.ContainsKey(DocumentMetadata.SheetCount));

        Directory.Delete(root, true);
    }

    [Fact]
    public void Manifest_Matches_Generated_Corpus()
    {
        var root = Path.Join(Path.GetTempPath(), "pp-manifest-" + Guid.NewGuid().ToString("N"));
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 11).Generate(CorpusSize.Small, root);

            using var manifest = System.Text.Json.JsonDocument.Parse(
                File.ReadAllText(Path.Join(root, "corpus.json")));
            var documentCount = manifest.RootElement.GetProperty("documentCount").GetInt32();

            // Every generated file except the manifest itself is a document.
            var actual = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Count(p => !string.Equals(Path.GetFileName(p), "corpus.json", StringComparison.Ordinal));
            Assert.Equal(documentCount, actual);

            // formatCounts must sum to the document count.
            var formatSum = manifest.RootElement.GetProperty("formatCounts")
                .EnumerateObject().Sum(p => p.Value.GetInt32());
            Assert.Equal(documentCount, formatSum);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static async Task<ParseResult<Document>> DispatchFile(
        IParserDispatcher dispatcher, IMimeTypeResolver resolver, string path)
    {
        var mediaType = resolver.Resolve(Path.GetFileName(path)).MediaType;
        var asset = TestAsset.For(path, mediaType);
        await using var fs = File.OpenRead(path);
        return await dispatcher.DispatchAsync(fs, asset);
    }
}
```

> Confirm the exact `ParseResult<Document>` / `ParseResultKind` member names (`Kind`, `Value`, `Success`, `Unsupported`) against the `IParserDispatcher` contract before running; the milestone plan's dispatcher uses these. If the resolver classifies an unmapped extension as text rather than returning `Unsupported` for the `.so`, verify the `.so` denylist entry from Sprint 1 is present — the assertion depends on it. If a full `ferret index` → SQLite → `search` pipeline is preferred over direct dispatch, mirror the wiring in `IndexPipelineBenchmark.cs` (swap the single-parser registry for `ParserPackModule`) and assert a `search` for a known corpus word returns a `.pdf`/`.docx` hit — but the direct-dispatch version above is the minimal reliable assertion.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Ferret.Integration.Tests --filter ParserPackIndexingTests`
Expected: PASS (2 tests — cross-format dispatch + opaque exclusion with per-format metadata assertions, and the `corpus.json` manifest match).

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj tests/Ferret.Integration.Tests/TestAsset.cs tests/Ferret.Integration.Tests/ParserPackIndexingTests.cs
git commit -m "test(parsers): end-to-end PDF/DOCX/XLSX/CSV parsing and opaque-binary exclusion"
```

---

### Task 2: Milestone acceptance-criteria verification + RC handoff

**Files:** none (verification + release decision).

**Interfaces:** none.

> This task uses the **superpowers:verification-before-completion** skill for the evidence gate and **superpowers:finishing-a-development-branch** for the integration decision. Do not claim the milestone complete without the command output below.

- [ ] **Step 1: Full solution build + test**

Run: `dotnet build src/Ferret.sln && dotnet test src/Ferret.sln`
Expected: build clean; all test projects green — `Ferret.Core.Tests`, `Ferret.ParserPlatform.Tests`, `Ferret.Parsers.Pdf.Tests`, `Ferret.Parsers.Office.Tests`, `Ferret.Parsers.Tests`, `Ferret.Cli.Tests`, `Ferret.Benchmarks.Tests`, `Ferret.Integration.Tests`, `Ferret.E2E.Tests`.

- [ ] **Step 2: Verify `ferret doctor` reports the full pack**

Run: `dotnet run --project src/Ferret.Cli -- doctor`
Expected: output lists the 7 installed parsers (Plain Text, Markdown, JSON, CSV, PDF, Word (DOCX), Excel (XLSX)) and the supported-extension count; overall doctor status is healthy.

- [ ] **Step 3: Walk the milestone acceptance criteria (from the spec)**

Confirm each item against test output — cite the test or command that proves it:

1. A `.pdf`, `.docx`, and `.xlsx` produce non-empty indexed content and are searchable — Sprint 2 `PdfIndexE2ETests`, Sprint 3 `OfficeIndexE2ETests`, Task 1 `ParserPackIndexingTests`.
2. Searching a cell value from a generated requirement-traceability / bug-report `.xlsx` returns that file — Sprint 3 `Search_XlsxCellValue_ReturnsBugExport`; archetype header token in Task 1.
3. Opaque binaries never enter the index — Task 1 (`native.so` → `Unsupported`).
4. `MediaCategory` classifies `.pdf`/`.docx`/`.xlsx` as `BinaryParseable`; `.xlsx` is `DocumentKind.Data` — Sprint 1 `MimeTypeResolverTests`; Task 1 XLSX `Data` assertion.
5. Default (unlimited) extraction limit indexes completely; a configured limit truncates + sets `Truncated` — Sprint 1 `ExtractionLimiterTests`, Sprint 3 `ParseAsync_Honors_Configured_Extraction_Limit`.
6. `ferret doctor` lists the installed parsers and the supported-extension count — Step 2 above; Sprint 3 `InstalledParsersCheckTests`.
7. All existing parser/index/search tests still pass; the parser registry is unchanged — Step 1 full-suite run.

Also confirm the review-added evidence:
- **Per-format metadata** round-trips (renderer → package/document info → parser → `Document.Metadata`) — `ParserPackIndexingTests` metadata assertions (PDF `PageCount`, DOCX `Author`, XLSX `SheetCount`), plus Sprint 3 Task 1b metadata round-trip tests.
- **`corpus.json` matches the generated corpus** — `ParserPackIndexingTests.Manifest_Matches_Generated_Corpus`.
- **Cross-format semantic equivalence** (MD/DOCX/PDF preserve content, normalized) — Sprint 4 `RendererTests.Cross_Format_Renderers_Preserve_Content_Tokens` (referenced, not duplicated here).

- [ ] **Step 3b: Produce the format coverage report**

Fill in this table from the passing tests above — it makes release readiness visible at a glance. Every ✅ must point to a real test:

| Format | Parsed | Metadata | Search (e2e) | Validation |
| ------ | ------ | -------- | ------------ | ---------- |
| PDF    | ✅ PdfParserTests | ✅ PdfParserTests + ParserPackIndexingTests | ✅ PdfIndexE2ETests | ✅ ParserPackIndexingTests |
| DOCX   | ✅ WordParserTests | ✅ WordParserTests (Task 1b) | ✅ OfficeIndexE2ETests | ✅ ParserPackIndexingTests |
| XLSX   | ✅ ExcelParserTests | ✅ ExcelParserTests (Task 1b) | ✅ OfficeIndexE2ETests | ✅ ParserPackIndexingTests |
| CSV    | ✅ CsvParserTests (S1) | N/A (no package metadata) | ✅ CsvIndexE2ETests (S1) | ✅ ParserPackIndexingTests |

- [ ] **Step 4: Confirm no opaque binary leaks and no dependency escapes**

Verify (by inspection or a quick `grep`) that `Ferret.ParserPlatform` references neither `UglyToad.PdfPig` nor `DocumentFormat.OpenXml`, and that `Ferret.Parsers.Pdf` and `Ferret.Parsers.Office` do not reference each other. This is the package-isolation guarantee the milestone is built on.

- [ ] **Step 5: RC handoff**

Use the **superpowers:finishing-a-development-branch** skill to choose the integration path (merge / PR / keep the branch) for the completed Enterprise Content Pack 1 work. Actual distribution (npm publish, GitHub Release) is handled by the existing Distribution Platform workflow and is out of scope here — this step produces a validated, mergeable release candidate, not a new publish pipeline.

- [ ] **Step 6: Commit any doc updates**

If the acceptance walk produced report figures or README corrections, commit them:

```bash
git add docs/benchmarks/parser-pack-1/README.md README.md
git commit -m "docs(parsers): record Enterprise Content Pack 1 acceptance results"
```

---

## Final verification

- [ ] **Milestone complete**

All six sprints implemented; full solution green; `ferret doctor` shows 7 parsers; all 7 acceptance criteria cited with passing evidence; package isolation confirmed; integration decision made via finishing-a-development-branch. Enterprise Content Pack 1 is a release candidate.
