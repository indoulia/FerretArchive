# Technology Inventory

> Part of the AEF first-time onboarding package for Ferret. Covers tech stack, build system, testing strategy, and deployment/release/security/performance practice. Discovery only.

## Technology Stack

- **Language/runtime**: C#, .NET 9 (`net9.0`) exclusively — set once in `Directory.Build.props` (`LangVersion=latest`, nullable + implicit usings enabled, `TreatWarningsAsErrors=true`, full analyzer set `AnalysisMode=All`/`AnalysisLevel=latest`, docs required for public members).
- **CLI framework**: `System.CommandLine` 2.0.9.
- **Hosting/DI**: `Microsoft.Extensions.{Hosting,DependencyInjection,Configuration,Options,Logging}` 9.0.0.
- **Storage**: `Microsoft.Data.Sqlite` 9.0.0.
- **AI provider SDKs**: `OpenAI` 2.1.0, `OllamaSharp` 4.0.22; MCP server via `ModelContextProtocol` 1.4.0.
- **Document parsing**: `DocumentFormat.OpenXml` 3.1.0 (Word/Excel/PowerPoint), `UglyToad.PdfPig` 1.7.0-custom-5 (PDF — see dependency note below), `Markdig` 0.38.0.
- **Test stack**: xUnit 2.9.2 + `Microsoft.NET.Test.Sdk` 17.12.0 + `coverlet.collector`; `BenchmarkDotNet` 0.14.0 for perf.
- **Static analysis**: StyleCop.Analyzers 1.2.0-beta.556, applied globally.
- **Secondary ecosystem**: Node.js/npm, used only for the `Ferret.Npm` distribution wrapper (ESLint 9, Prettier 3, `extract-zip` 2.0.1) — not product code.
- **Target platforms**: self-contained single-file native binaries for `win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`.

### Dependency notes worth knowing on day one

- **`UglyToad.PdfPig 1.7.0-custom-5`** is a documented workaround: the intended stable `0.1.9` isn't available from configured feeds, so a "curated stable" prerelease is pinned instead, with the `NU5104` prerelease-dependency warning suppressed repo-wide because it flows into packable projects. A real supply-chain wrinkle to be aware of.
- **`Ferret.Cli.csproj`** sets `CentralPackageTransitivePinningEnabled=false` locally (overriding the repo-wide default) with an inline comment: "MCP SDK pulls Extensions v10+; allow transitive resolution without CPM pinning." This is a deliberate, documented exception — worth watching for version drift between the CPM-pinned `Microsoft.Extensions.*` 9.0.0 and whatever the MCP SDK resolves transitively.
- The self-contained publish embeds native SQLite (`IncludeNativeLibrariesForSelfExtract=true`) so the single-file binary works with no sidecar DLLs — increases publish complexity/binary size versus a framework-dependent deploy.

## Build System

- **Solution**: `src/Ferret.sln` is the single entry point; `dotnet build src/Ferret.sln` (or `dotnet restore` + `dotnet build --configuration Release`) builds everything including `tests/`.
- **Central configuration**: `Directory.Build.props` (metadata, TFM, analyzer/warning policy, StyleCop, symbol packages), `Directory.Build.targets` (ARCH001/ARCH002 architecture-fitness checks enforced as build errors), `Directory.Packages.props` (Central Package Management — all versions pinned here; project files use bare `<PackageReference Include="..." />`).
- **Release pipeline** (all PowerShell, repo root):
  1. `publish.ps1` — `dotnet publish` of `src/Ferret.Cli/Ferret.Cli.csproj` per RID, self-contained, single-file, to `artifacts/<rid>/`.
  2. `package.ps1` — wraps `publish.ps1`; stages binary + `LICENSE` + `CHANGELOG.md` + `packaging/README.md` (+ `install.ps1`/`uninstall.ps1` for Windows), writes `SHA256SUMS.txt`, zips to `artifacts/Ferret-<version>-<rid>.zip`.
  3. `install.ps1` / `uninstall.ps1` — per-user, no-admin install; copies `ferret`/`ferret.exe` and manages the user `PATH`.

## Packaging & Distribution

Ferret is **not a deployed service** — it is a self-contained CLI distributed for installation onto individual developer machines, with two modes at runtime: interactive CLI commands and an MCP server over **stdio** (`ferret serve`) for AI-agent/IDE integration. Three distribution surfaces:

1. **Native binary (primary)** — per-OS/arch self-contained executable, packaged with a SHA256 manifest, published to **GitHub Releases**. Because the source repo is **private** but GitHub only serves release assets anonymously from public repos, binaries are additionally mirrored to a separate public repo, `indoulia/ferret-dist`, so anonymous `npm install`/download works.
2. **npm wrapper** (`Ferret.Npm/`, published as `@indoulia/ferret`) — a thin launcher, not a JS reimplementation: `postinstall` downloads the matching platform binary from GitHub Releases, verifies its checksum, and `bin/ferret.js` `spawnSync`s it, forwarding args/stdio/exit code. Published via npm **OIDC Trusted Publishing** (no stored npm token).
3. **Plugin SDK** (`Ferret.Plugin.SDK`) — for third-party extensibility, not an end-user channel. `docs/007-SDK/SDK-001.md` (1,864 lines, Draft, Pending Architecture Review) specifies a manifest schema, a 19-extension-type catalogue, a capability-based permission model, and `.aiplugin`/`.nupkg` packaging — this is largely a forward-looking contract spec; the actual implemented surface in `Ferret.Plugin.SDK`/`Ferret.Plugins` was not independently verified against it.

## Release Process

- **Versioning**: SemVer, pre-1.0 (`Ferret.Cli.csproj` currently `0.16.0` at HEAD of the researched branch). `docs/012-Releases/README.md` points to `docs/templates/versioning.md` for the full policy — **that file does not exist**.
- **Cadence**: releases have landed roughly daily during active sprints (`v0.14.0` → `v0.15.0` → `v0.16.0` across three consecutive days, 2026-06-29 to 2026-07-01) — sprint-driven, not a fixed cadence. An unexplained tag `pkm-v0.1` also exists, consistent with the repo's prior "AISpace"/"PKM" naming history (see `AEF-Onboarding.md` Glossary).
- **Channels**: single-channel today (GitHub Releases → npm). Stable/beta channel splitting is explicitly reserved as *future* work, not present.
- **Runbook** (`docs/012-Releases/RELEASE-PROCESS.md`): set version → write release notes at `docs/012-Releases/v<version>.md` → tag `v<version>` on `main` → `release.yml` builds per-RID zips + manifest + creates a **draft** GitHub Release → maintainer reviews/publishes the draft → `release: published` fires `npm-publish.yml` → npm publish via OIDC.
- **`docs/012-Releases/README.md`'s own release index still says "(no releases yet)"** despite three real, dated releases existing in the same folder — stale relative to its own directory contents.

## CI/CD Pipeline

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | push/PR to `main`/`develop`, manual | Matrix build (ubuntu-latest, windows-latest): `dotnet restore/build/test src/Ferret.sln` (Release), uploads TRX + coverage artifacts; separate `format-check` job runs `dotnet format --verify-no-changes` |
| `release.yml` | push of `v*` tags, manual | Builds/tests/packs, runs `package.ps1` for all 4 RIDs, builds `release-manifest.json`, creates a draft GitHub Release, mirrors assets to public `indoulia/ferret-dist` |
| `npm-publish.yml` | `release: published`, manual | Publishes `@indoulia/ferret` via npm OIDC Trusted Publishing |
| `security.yml` | push/PR to `main`, weekly cron, manual | Single job: `dotnet list package --vulnerable --include-transitive` |

No dedicated "deploy" workflow exists — `release.yml` + `npm-publish.yml` together are the entire deployment surface.

## Security Posture

- **SECURITY.md**: private disclosure only (GitHub Private Vulnerability Reporting or `security@ferret.dev`), 48h acknowledgement, 5-business-day triage, 30-day fix (7 days critical), disclosure after fix or 90 days. Links to `docs/guides/security-hardening.md`, which **does not exist** (no `docs/guides` directory at all).
- **`docs/010-Security/README.md`'s "Automated Scanning" table overstates the real pipeline.** It claims CodeQL (PR + weekly), `dotnet list package --vulnerable` (every build), OWASP Dependency Check (weekly), and GitHub Dependabot (continuous) are all active. In reality only the NuGet vulnerability audit exists and runs. CodeQL and Dependency Review were **explicitly removed** — an inline comment in `security.yml` states this is because they require GitHub Advanced Security, which returns 403 on a private repo under the free plan. No OWASP job exists anywhere. No `.github/dependabot.yml` exists. Treat this doc's scanning table as aspirational, not current.
- No authentication/access-control model exists in the product itself; `CHANGELOG.md` (Sprint 14) states this is explicitly deferred to V2.
- Package integrity: SHA256 checksums on release artifacts, npm provenance via OIDC.

## Performance Practice

- `docs/011-Performance/README.md` defines SLO targets (agent invocation p50<100ms/p99<500ms, MCP tool call p50<50ms/p99<250ms, CLI startup p50<500ms/p99<1s, plugin load p50<200ms/p99<1s) — but is explicitly marked "to be refined in Sprint 1+," and no CI job was found that measures or gates on these SLOs specifically (CI only runs `dotnet test`).
- Actual measured performance lives in `CHANGELOG.md`/release notes, not in `docs/011-Performance/`: e.g. 10,000-file workspace indexed in <60s, search <200ms on a 10K-doc index, `ferret serve` cold start <3s (Sprint 14); PDF ~2,900 docs/sec, Word ~600 docs/sec, Excel ~122 docs/sec (v0.16.0, full detail in `docs/benchmarks/parser-pack-1/`).
- Tooling: BenchmarkDotNet (`tests/Ferret.Benchmarks`, with a separate `tests/Ferret.Benchmarks.Tests` testing the harness itself) plus documented (but unconfigured — no files found) k6 for load testing. Benchmarks build in CI but are run on demand, not automatically.
- One real BenchmarkDotNet run is captured under `BenchmarkDotNet.Artifacts/results/` (`ParserThroughputBenchmark.ParseLargeWorkbook`: mean 708.0ms, 206.79MB allocated).

## Testing Strategy

31 dedicated test projects (see `Repository-Inventory.md` for the list), all xUnit + `coverlet.collector`. `docs/009-Testing/README.md` states a classic test-pyramid philosophy with a strict TDD mandate and "no mocking the database in integration tests" — but its own Index table is empty and several of its specifics don't match reality:

| Stated (docs/009-Testing, CONTRIBUTING.md) | Observed |
|---|---|
| Coverage targets: Core ≥90%/85%, Application ≥80%/75%, Infrastructure ≥70%/65%, API ≥80%/75% (line/branch) | `tokensave test_risk` call-graph signal: **17.0%** for `Ferret.Core` (243 functions, 42 tested), **6.0%** for `Ferret.Search` (17 functions, 1 tested). CI collects coverage (`XPlat Code Coverage`) but does not gate on any threshold. **Caveat**: the tool was independently verified to *undercount* — `SearchService.SearchAsync` is directly tested in `tests/Ferret.Search.Tests/SearchServiceTests.cs` but the tool reports it as uncovered — so treat these percentages as a floor, not ground truth. Even so, they suggest the stated targets are not being met. |
| Unit tests live in `src/**/Tests/` | All unit tests actually live in top-level `tests/Ferret.<Module>.Tests/` projects |
| Integration tests use **Docker Compose**, real infrastructure, no DB mocking | No `docker-compose*` file exists anywhere; `Ferret.Integration.Tests` exercises real SQLite/filesystem/CLI in-process, not containers |
| `dotnet test ... --filter "Category=Integration"` selective-run commands in CONTRIBUTING.md | `ci.yml` runs `dotnet test src/Ferret.sln` with no `--filter` — full solution every time |
| TDD mandate ("failing test → red → fix → green — always") | `Ferret.Integration.Tests/PlaceholderIntegrationTests.cs` contains a scaffold `Assert.True(true)` test — can't verify TDD discipline statically, but this is at minimum a non-test placeholder shipped in the suite |

Benchmarking is kept structurally separate from correctness testing (see Performance Practice above).

## Gaps / Unknowns

- `docs/templates/versioning.md`, referenced as the authoritative SemVer policy, does not exist.
- `docs/guides/security-hardening.md`, referenced from `SECURITY.md`, does not exist — no `docs/guides/` directory at all.
- `docs/006-CLI/README.md` and `docs/007-SDK/README.md` are largely stubs; the SDK component table lists packages (`Ferret.Client`, `Ferret.Mcp.Sdk`) that do not exist in `src/`.
- `tokensave tool dependencies` did not enumerate a full per-project dependency graph for this repo (returned empty `members`) — the module inventory above relies on direct `.csproj`/glob inspection instead, which should be treated as authoritative.
- `.github/workflows/release.yml` was only skimmed (trigger + job name), not read start-to-end.
- No k6 load-test configuration was found despite being named as the load-testing standard.
