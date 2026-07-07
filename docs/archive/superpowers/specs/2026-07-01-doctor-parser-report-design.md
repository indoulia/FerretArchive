# Doctor Parser Platform Report — Design

**Status:** approved (brainstormed 2026-07-01), pending implementation on branch `feat/enterprise-content-pack-1`.
**Related:** Enterprise Content Pack 1 (`docs/superpowers/specs/2026-07-01-parser-pack-1-design.md`), Sprint 3 `InstalledParsersCheck`.

## Motivation

Now that Ferret indexes multiple formats (7 parsers across text, PDF, and Office),
the most common support question will be **"my PDF isn't indexing."** Today `ferret
doctor` answers this with a single collapsed line. This feature adds an informative,
human-readable **Parser Platform** report so a user (or supporter) can, in one command,
see which parsers are installed, which extensions are indexable, which are treated as
opaque, and which packages are loaded.

## Principles

- **Additive.** The existing health check stays as the one-line pass/warn gate; the
  report is extra detail rendered after the checks. No behavioral change to other checks.
- **Human-readable only.** No JSON output this pass. If `ferret doctor --format json`
  is ever introduced, the whole doctor infrastructure should evolve together — doing it
  only for parser diagnostics would make the CLI inconsistent.
- **Free information.** Everything shown is already held (parser instances, the resolver
  map) — no new subsystems.
- **Wording ages well.** Opaque extensions are described as *currently treated as opaque
  binary*, not *never indexed* — formats like PPTX/ZIP/Outlook may become parseable later.

## Components

### 1. `MimeTypeResolver` — grouped extension access (additive, `Ferret.ParserPlatform`)

Alongside the existing `KnownExtensionCount`, expose the extension→media-type map grouped
by `MediaCategory`, so counts, extension lists, and verbose MIME display all come from one
source of truth:

```csharp
/// <summary>An extension and the media type it resolves to.</summary>
public sealed record ExtensionMediaType(string Extension, string MediaType);

/// <summary>Gets the mapped extensions in the given category, ordered by extension (ordinal).</summary>
public static IReadOnlyList<ExtensionMediaType> ExtensionsInCategory(MediaCategory category);
```

- `Text` → 76, `BinaryParseable` → 3 (`.docx`/`.pdf`/`.xlsx`), `BinaryOpaque` → 50.
- Sorted ordinal for deterministic output/tests.
- `KnownExtensionCount` is retained (Text + Parseable = 79) and can be re-expressed as
  `ExtensionsInCategory(Text).Count + ExtensionsInCategory(BinaryParseable).Count`.
- Pure, deterministic, no I/O, no new dependencies.

### 2. `ParserPlatformReport` (new, `Ferret.Cli.Diagnostics`)

A focused renderer with one job: turn the composed parser set + resolver data into the
report text. Constructed from `IReadOnlyList<IContentParser>` (the same set
`InstalledParsersCheck` receives) and read from `MimeTypeResolver`.

```csharp
internal sealed class ParserPlatformReport
{
    internal ParserPlatformReport(IReadOnlyList<IContentParser> parsers);
    internal void Render(IOutputFormatter output, bool verbose);
}
```

Derived data:
- **Installed parsers** — `parser.Descriptor.Name`, in **registration order** (the order
  `GetServices<IContentParser>()` returns; matches the layered pack: text → PDF → Office).
  Not sorted — order is a tested contract.
- **Parser packages** — distinct `parser.GetType().Assembly.GetName().Name`, ordered
  ordinal: `Ferret.ParserPlatform`, `Ferret.Parsers.Office`, `Ferret.Parsers.Pdf`.
- **Extension coverage / lists** — from `MimeTypeResolver.ExtensionsInCategory(...)`.
  A derived **Known Extensions** total (Text + Parseable + Opaque = 129) gives a quick
  sense of overall coverage without changing the categories.

**Empty-parser safeguard:** if the parser set is empty (should not happen in normal use,
but keeps the renderer robust), the Installed Parsers section renders as:

```
Installed Parsers (0)
  No parsers are registered.
```

and Parser Packages renders `(0)` with a matching "No parser packages loaded." line. The
health gate (`InstalledParsersCheck`) already warns in this case.

**Reserved extension points (not implemented now):** the per-parser verbose block is
structured so future lines can be added without reshaping the renderer —
(a) a **Capabilities** line from `Descriptor.Capabilities` (Text / Metadata / Structured
Extraction / OCR), and (b) a **Version** line from `Descriptor.Version` (helps diagnose
mixed-version deployments). Neither ships in this change.

### 3. `DoctorCommandHandler` + `CoreCliModule` wiring

- **No new option.** `--verbose` already exists as a global, hidden option
  (`GlobalOptions.Verbose`) that the root command parses into
  `IFerretContext.Verbosity == VerbosityLevel.Verbose`. `ferret doctor --verbose` therefore
  works with no command-definition change; the doctor command stays `Cmd("doctor", …)`.
- `DoctorCommandHandler` gains a `ParserPlatformReport` dependency; after
  `DiagnosticRunner.RunAsync(...)` it computes
  `bool verbose = context.Verbosity == VerbosityLevel.Verbose` and calls
  `report.Render(context.Services.Output, verbose)`.
- `CoreCliModule` builds the report from the same parser `ServiceProvider` it already
  constructs for `InstalledParsersCheck`, and passes it into the handler factory
  (`new DoctorCommandHandler(_checks, report)`).
- `DoctorCommandHandlerTests` is updated for the new constructor parameter.
- **Simplify `InstalledParsersCheck`'s line** to drop the now-duplicated parser-name list;
  it remains the health gate: `Parser platform: 7 parsers, 79 extensions`. The names now
  live in the detailed section.

## Output

### Default (`ferret doctor`)

```
Parser Platform

Installed Parsers (7)
  ✓ Plain Text Parser
  ✓ Markdown Parser
  ✓ JSON Parser
  ✓ CSV Parser
  ✓ PDF Parser
  ✓ Word (DOCX) Parser
  ✓ Excel (XLSX) Parser

Extension Coverage
  Text: 76
  Parseable Binary: 3
  Opaque Binary: 50
  Known Extensions: 129

Parseable Binary (3)
  .docx  .pdf  .xlsx

Opaque Binary (50) — currently treated as opaque binary
  .7z .a .bin .class .dll .dylib .exe .jar ...
  run `ferret doctor --verbose` for the full list

Parser Packages (3)
  Ferret.ParserPlatform
  Ferret.Parsers.Office
  Ferret.Parsers.Pdf
```

### Verbose (`ferret doctor --verbose`)

- Each installed parser gains indented detail:
  ```
  ✓ PDF Parser
      Priority: 200
      Media Type: application/pdf
  ```
- Parseable Binary extensions show their MIME mapping:
  ```
  .docx → application/vnd.openxmlformats-officedocument.wordprocessingml.document
  .pdf  → application/pdf
  .xlsx → application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
  ```
- Opaque Binary shows **all 50** extensions (wrapped) instead of the truncated sample.

> A parser can declare more than one supported media type; the verbose "Media Type" line
> shows `string.Join(", ", Descriptor.SupportedMediaTypes)`.

## Testing

- **`MimeTypeResolver` grouping** (`Ferret.ParserPlatform.Tests`): category counts
  (Text 76, Parseable 3, Opaque 50); membership (`.pdf`/`.docx`/`.xlsx` in Parseable,
  a known opaque such as `.exe` in Opaque); ordinal ordering; `.docx`→OOXML media type.
- **`ParserPlatformReport.Render`** (`Ferret.Cli.Tests`) against a capturing fake
  `IOutputFormatter`:
  - all section headers present;
  - **registration-order stability** — assert the parser lines appear in the exact
    sequence Plain Text → Markdown → JSON → CSV → PDF → Word → Excel (guards against a
    future accidental sort);
  - default output summarizes opaque (sample + hint) and omits per-parser priority/MIME;
  - verbose output lists all 50 opaque extensions and includes `Priority:` and
    `Media Type:` lines and the `.docx → …` MIME mapping;
  - packages listed (`Ferret.ParserPlatform`, `Ferret.Parsers.Office`, `Ferret.Parsers.Pdf`);
  - **empty-parser safeguard** — with an empty parser list, renders "Installed Parsers (0)"
    and "No parsers are registered." (no exception).
- **`InstalledParsersCheckTests`** — updated for the simplified one-line `Name`.
- **`DoctorE2ETests`** — assert the `Parser Platform` section (and a format name, e.g.
  `Excel (XLSX) Parser`) appears in `ferret doctor` output through the published binary.

## Manual updates (`Ferret.Manual/Content`)

- **`reference/cli.md`** — document `ferret doctor` and the `--verbose` flag.
- **`user-guide/parsers.md`** — note that `ferret doctor` reports installed parsers,
  extension coverage, and parser packages.
- **`troubleshooting.md`** — add a **"My file isn't indexing"** entry with a decision tree:

  ```
  Is the extension listed by `ferret doctor`?
    ├─ No  → unsupported extension (not mapped)
    └─ Yes → Which category?
             ├─ Parseable Binary → is the parser installed? (doctor lists it)
             │     ├─ Yes → re-run `ferret index`
             │     └─ No  → install/enable the parser package
             ├─ Text            → check .ferretignore / workspace scope
             └─ Opaque Binary   → currently treated as opaque; not indexed
  ```

## Non-goals

- No JSON / machine-readable output (see Principles).
- No capability matrix rendering (reserved extension point only).
- No new `ferret parsers` command — the report lives where users run `doctor`.
- No changes to parsing behavior, the registry, or parser packages.
