# Sprint 1 — Parser Platform & CSV

> **Milestone:** Enterprise Content Pack 1.
> **Parent spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md`
> **Parent plan:** `docs/superpowers/plans/2026-07-01-parser-pack-1.md`
>
> This document freezes the scope of **Sprint 1** — the first shippable slice of
> the milestone. It records what Sprint 1 delivers, the delta from the frozen
> parent plan, and the sprint roadmap that follows.

## Goal

Ship a real release. After Sprint 1 merges, a user can run `ferret index`
followed by `ferret search` and get hits against enterprise **CSV/TSV exports**
(Jira / Azure DevOps issue dumps, RTMs, bug reports, risk registers). At the
same time, the MIME resolution layer is finished **once** for the entire
milestone, so later sprints add parsers without revisiting it.

No new heavyweight dependencies. No new CLI features. No PDF or Office work.

**Backward compatibility:** Sprint 1 introduces no breaking changes to existing
indexes, parser contracts, CLI behavior, or public APIs. Existing text /
markdown / JSON indexing is unchanged, and indexes built before Sprint 1 remain
compatible.

## Frozen scope

Sprint 1 is exactly these four deliverables:

1. **Core parser foundation** (`Ferret.Core`)
2. **MimeTypeResolver expansion** (`Ferret.ParserPlatform`)
3. **CSV/TSV parser** (`Ferret.ParserPlatform`)
4. **End-to-end indexing validation** (`ferret index` → `ferret search`)

### 1. Core parser foundation

Corresponds to parent-plan **Task 1**, unchanged. All in `src/Ferret.Core/Documents/`:

- `MediaCategory` enum — `Text`, `BinaryParseable`, `BinaryOpaque`.
- `MediaTypeInfo` — add required `Category`; derive `IsText`/`IsBinary` from it
  (remove the independently-set booleans).
- `DocumentMetadata` — canonical metadata-key constants (`Author`, `Subject`,
  `Keywords`, `PageCount`, `SheetCount`, `Created`, `Modified`, `Category`,
  `Truncated`) so keys never drift across parsers.
- `ParserOptions` — `long? MaxExtractedCharacters` (null = unlimited).
- `ExtractionLimiter` — the single shared truncation helper
  (`ApplyCharacterLimit`) every future parser calls.
- `ParserCapabilities.StructuredExtraction` — reserved, declared but unused this
  milestone.

TDD throughout. This is a sequential barrier: it lands before Task 2/2b build.

### 2. MimeTypeResolver expansion

Corresponds to parent-plan **Task 2**, **plus a filename-resolution addition**
agreed for this sprint. This extends the existing resolver — the intent is to
touch this file **once** for the whole milestone rather than revisit it every
sprint.

**Delta from today's resolver** (already mapped: Rust, Go, Java, Kotlin, C, C++,
HTML, XML, YAML, TOML, CSV, TSV, and the common code/binary set — these are not
re-added):

- **Reclassify** `.pdf`, `.docx`, `.xlsx` from opaque `Binary()` to
  `BinaryParseable` with the correct media types and suggested kinds
  (`.pdf`/`.docx` → `Prose`, `.xlsx` → `Data`). This is what lets the Sprint 2/3
  parsers get dispatched — no parser ships in Sprint 1, but the classification
  does. `.pptx` stays opaque.
- **Add** the expanded code/config family: php, scala, scss, less, clj/cljs,
  dart, lua, r, pl, groovy, `.gradle`, bat, cmd, psm1/psd1, vb, fs/fsx,
  ini/cfg/conf, `.env`, `.properties`, csproj/vbproj/fsproj/props/targets, resx,
  xaml, rst, adoc, tex, `.gitignore`, `.editorconfig`.
- **Expand** the binary denylist: so, dylib, a, o, lib, class, pyc, pyo, wasm,
  node, nupkg, snk, pfx, jar, war, ear, db, sqlite, parquet, dat, keystore, psd,
  ai, otf.
- **Migrate** the `Text()` / `Binary()` / `UnknownText` helpers to set
  `Category` instead of the removed `IsText`/`IsBinary` setters.

**Filename-based resolution (new this sprint).** The resolver matches purely on
`Path.GetExtension()` today, so extensionless files can never be classified.
Add a **filename lookup that runs after the extension lookup** so extensionless
files are first-class:

- New `FileNameMap` keyed on the full file name (case-insensitive), seeded with
  `Dockerfile` and `Makefile`, and kept trivially extensible for future names.
- `Resolve()` order: **extension lookup → filename lookup → `UnknownText`
  fallback.** A present, known extension always wins. The filename map is
  consulted only when the extension lookup misses, matching the full file name
  (so bare `Dockerfile` and `Makefile` resolve). Variants with an extension
  (e.g. `Dockerfile.dev`) are out of scope unless explicitly seeded later — the
  map is designed to make that a one-line addition.

### 3. CSV/TSV parser

Corresponds to parent-plan **Task 2b**, unchanged in design. In
`src/Ferret.ParserPlatform/Parsers/`:

- `CsvRecordReader` — internal, quote-aware RFC-4180 record reader (handles
  quoted fields, embedded delimiters/newlines, doubled-quote escapes).
- `CsvParser : IContentParser` — ctor takes `ParserOptions`; `CanParse` matches
  `text/csv` + `text/tab-separated-values`; **priority 200** so it beats
  `PlainTextParser` (100); emits header + data rows as `DocumentKind.Data`;
  applies `ExtractionLimiter`; never disposes the stream.
- Registered in `ParserPlatformModule` alongside the existing built-ins, with a
  default `ParserOptions` via `TryAddSingleton`. **No CLI wiring change** — CSV
  reaches `ferret index` through the already-wired platform module the moment it
  merges.

Dependency-free; lives beside JSON/Markdown in the platform.

### 4. End-to-end indexing validation

**Expansion beyond the parent plan.** The parent plan verifies Phase-2 parsers
by unit + dispatch tests only, deferring CLI-level tests to integration. Because
CSV composes through the already-wired platform with no new dependency, Sprint 1
takes the opportunity to validate the **complete pipeline** — discovery →
resolution → parse → index → search:

- End-to-end tests driving **`ferret index`** then **`ferret search`** against a
  temp workspace.
- **Realistic enterprise CSV fixtures** (e.g. a Jira / Azure DevOps issue
  export: `Key, Summary, Severity, Status, Assignee, Sprint, …`), not trivial
  two-cell samples. Include a quoted-field-with-comma row to exercise the reader.
- Assert that column tokens and cell values are searchable — e.g. searching a
  bug key, an assignee, a severity, or free-text from a summary returns the row's
  document.

## Test strategy

- **Unit:** `MediaTypeInfoTests`, `ExtractionLimiterTests`, `MimeTypeResolverTests`
  (including Dockerfile/Makefile filename cases and the PDF/DOCX/XLSX
  reclassification), `CsvParserTests`, `CsvRecordReader` edge cases.
- **Dispatch:** CSV routed correctly by `ParserDispatcher` over `CanParse` +
  priority.
- **End-to-end:** `ferret index` + `ferret search` against enterprise CSV
  fixtures (deliverable 4).
- All new work is TDD: failing test → red → implement → green.

## Non-goals (explicitly deferred)

- PDF parser and `Ferret.Parsers.Pdf` package → **Sprint 2**.
- Word + Excel parsers and `Ferret.Parsers.Office` package → **Sprint 3**.
- `ParserPackModule` composition + CLI wiring swap → deferred (only needed once
  heavyweight packages exist; CSV needs none).
- Multi-format corpus generator → **Sprint 4**.
- Benchmarks → **Sprint 5**.
- Enterprise validation + RC packaging → **Sprint 6**.
- No new NuGet packages, no new `.csproj`, no `doctor` changes in Sprint 1.

## Sprint 1 acceptance criteria

Sprint 1 is signed off when all of the following hold:

- [ ] Existing text / markdown / JSON indexing unchanged
- [ ] CSV searchable end-to-end (`ferret index` → `ferret search`)
- [ ] TSV searchable end-to-end
- [ ] `Dockerfile` resolved correctly (filename resolution)
- [ ] `Makefile` resolved correctly (filename resolution)
- [ ] `.pdf` / `.docx` / `.xlsx` classified as `BinaryParseable` with correct
      media types and suggested kinds
- [ ] Expanded binary denylist prevents accidental text indexing of opaque files
- [ ] 100% of existing regression tests green (no regressions)
- [ ] No new runtime / NuGet dependencies
- [ ] No CLI changes
- [ ] Existing indexes remain compatible (no breaking changes)

## Milestone roadmap

1. **Sprint 1 — Parser Platform & CSV** (this doc): core foundation, one-time
   MIME overhaul (incl. filename resolution), CSV/TSV parser, end-to-end
   indexing validation.
2. **Sprint 2 — PDF Intelligence:** `Ferret.Parsers.Pdf` (PdfPig) in isolation.
3. **Sprint 3 — Office Intelligence:** `Ferret.Parsers.Office` — Word + Excel in
   one package (shared dependency, shared review).
4. **Sprint 4 — Enterprise Corpus:** deterministic multi-format corpus generator.
5. **Sprint 5 — Benchmarking:** throughput / large-workbook / memory / report.
6. **Sprint 6 — Enterprise Validation & RC:** integration composition, validation,
   packaging, release candidate.

Each sprint after this one is a focused, low-integration-risk increment that
adds visible value on top of the platform Sprint 1 establishes.
