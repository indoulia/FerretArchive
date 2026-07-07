# Repository Inventory

> Part of the AEF first-time onboarding package for Ferret. Produced by discovery only — nothing in this document was fixed, redesigned, or renamed. See `AEF-Onboarding-Validation.md` for confidence levels and open questions.

## Top-Level Directory / File Map

| Path | Purpose |
|---|---|
| `README.md` | Product pitch, install/build instructions, repo map |
| `LICENSE` | MIT license ("Ferret Contributors", 2026) |
| `CHANGELOG.md` | Release history (stops at `[0.16.0] — 2026-07-01`; does not cover the Workspace Intelligence Platform work merged since) |
| `CONTRIBUTING.md` | Contribution guide (branching, commit style, PR process, coding standards) |
| `CODE_OF_CONDUCT.md` | Contributor Covenant v2.1 |
| `SECURITY.md` | Vulnerability disclosure policy and SLAs |
| `Directory.Build.props` | Solution-wide MSBuild settings: TFM (`net9.0`), analyzers, StyleCop, versioning, `TreatWarningsAsErrors=true` |
| `Directory.Build.targets` | Two custom architecture-fitness MSBuild targets: ARCH001 (`Ferret.Core` must have zero project references) and ARCH002 (`Ferret.Runtime` must not reference `Ferret.Cli`/`Ferret.Mcp`) |
| `Directory.Packages.props` | Central NuGet Package Management — all package versions pinned here |
| `stylecop.json` / `.editorconfig` | Style/lint rules |
| `.gitattributes` / `.gitignore` | Git configuration |
| `install.ps1` / `uninstall.ps1` | Per-user (no admin) install/uninstall of the released `ferret` binary; PATH management |
| `package.ps1` / `publish.ps1` | Build/packaging scripts producing per-RID release zips + checksums |
| `src/` | Production .NET 9 source — 28 `Ferret.*` projects under solution `src/Ferret.sln` |
| `tests/` | 34 test projects, one per `src` module plus `Architecture.Tests`, `Benchmarks(.Tests)`, `E2E.Tests`, `Integration.Tests` |
| `docs/` | All project documentation — see `Documentation-Inventory.md` for the full breakdown |
| `examples/` | Placeholder for runnable sample projects — currently empty (`_(to be added)_`) |
| `samples/` | Working sample plugin (`samples/plugins/Ferret.Plugins.Sample`) demonstrating the Plugin SDK, plus a sample `.ferret/` workspace |
| `templates/` | Canonical Markdown templates (ADR, spec, PRD, architecture, API, database, plugin, MCP, CLI, testing, release, versioning) |
| `tools/` | Documentation-only placeholder — README lists *planned* Roslyn analyzers/codegen/devcontainer, none exist yet |
| `build/` | `Build.ps1` / `Build.sh` cross-platform build entry scripts |
| `packaging/` | README bundled inside end-user release zips (install/quickstart/troubleshooting) |
| `scripts/` | `bootstrap.ps1`, `init-workspace.ps1`, `build-release-manifest.ps1`, `validate-manifest.js` |
| `Ferret.Npm/` | `@indoulia/ferret` npm wrapper — downloads and launches the native binary; not a reimplementation |
| `BenchmarkDotNet.Artifacts/` | Generated benchmark output (build artifact, not source) |
| `.github/` | CI workflows, `CODEOWNERS`, issue/PR templates |
| `.ai/` | Repo-local AI-agent session/memory state (`ai-config.json`, `session.md`, `current-context.json`, `metrics.json`, `workspace.json`, plus `agents/`, `checklists/`, `commands/`, `decisions/`, `investigations/`, `knowledge/`, `templates/`, `workflows/`) — the AEF-style operating scaffold for this repo |
| `.ferret/` | Ferret's own workspace state from indexing **this repo with itself** (dogfooding) — connectors, indexes, knowledge, memory, models, snapshots, telemetry |
| `.tokensave/` | Local index database for a separate, externally-installed `tokensave` CLI/MCP code-intelligence tool the team dogfoods alongside Ferret — **not a Ferret build artifact** (see `AEF-Onboarding.md` Glossary) |
| `.superpowers/` | `sdd/` scratch subfolder for an AI agent skill framework; excluded from Ferret's own index via `.ferretignore` |
| `.worktrees/` | Git worktrees for isolated branch work — one present: `v2-workspace-intelligence` (stale; content already merged to `main`, see `AEF-Onboarding-Validation.md`) |
| `.claude/` | Local Claude Code settings |
| `.ferretignore` | Ferret's own indexer ignore-list |

## `src/` Module Inventory

All projects are SDK-style, target `net9.0` via `Directory.Build.props`, and use Central Package Management (bare `<PackageReference Include="..." />`, no per-project `Version`). Only `Ferret.Cli` is an executable (`AssemblyName=ferret`); everything else is a class library.

| Project | Purpose |
|---|---|
| `Ferret.Core` | Domain model, contracts, value objects, exceptions. Zero project references (enforced by ARCH001 build target). Largest public surface (~1,414 symbols). |
| `Ferret.Runtime` | Module lifecycle, DI composition, `IRuntimeHost`. Forbidden from referencing `Cli`/`Mcp` (ARCH002). Only ~104 public symbols — generic host/lifecycle infrastructure, not the seven "engines" ARCH-001 describes (see `Architecture-Inventory.md`). |
| `Ferret.Cli` | `ferret` CLI entry point; the only executable project; hosts all `ICliModule`s. |
| `Ferret.Mcp` | MCP server/tools: `search`, `ferret_context`, `read_document`, `workspace_status`, `workspace_list`. |
| `Ferret.Plugins` | Plugin host/isolation boundary. |
| `Ferret.Plugin.SDK` | Public plugin-author contracts (renamed from `Ferret.Sdk` per ADR-0011). |
| `Ferret.Configuration` / `Ferret.Configuration.AI` | Config loading/validation; AI-provider-specific config schema. |
| `Ferret.ConnectorPlatform` / `Ferret.Connectors.Filesystem` | Connector manager/lifecycle; default filesystem connector. |
| `Ferret.Indexing` | Discover → parse → index pipeline, incremental fingerprint store. |
| `Ferret.ParserPlatform` / `Ferret.Parsers` / `Ferret.Parsers.Office` / `Ferret.Parsers.Pdf` | Parser dispatch and format-specific parsers (plain text, Word/Excel/PowerPoint via OpenXml, PDF via PdfPig). |
| `Ferret.Search` | Query parsing + BM25/FTS5 keyword search. |
| `Ferret.AI` / `Ferret.Prompts` / `Ferret.Models` | Context assembly pipeline, prompt templates, shared model/provider contracts. |
| `Ferret.Providers.Ollama` / `Ferret.Providers.OpenAi` | Model provider plugins. |
| `Ferret.Workspace` | Single-repo workspace engine (`ferret workspace init/status`). |
| `Ferret.Workspace.Graph` | Multi-repo workspace registry/DAG (ADR-0026). |
| `Ferret.Knowledge.Federation` | Federated cross-workspace knowledge queries (ADR-0027) — real and populated, but **not yet named in ARCH-001**. |
| `Ferret.Persistence` | V2 dependency-graph persistence (ADR-0022–0024). |
| `Ferret.VerticalSlice` | V2 architecture vertical-slice host. |
| `Ferret.Telemetry` | Logging/metrics/tracing sink pipeline. |
| `Ferret.Manual` | Documentation portal content/generator. |

Non-`src` projects with no `.csproj` counterpart in the table above: `Ferret.Plugins.Sample` (`samples/`), `Ferret.Npm` (Node package, not .NET).

## `tests/` Organization

One `Ferret.<Module>.Tests` project per `src` module (xUnit + `coverlet.collector`), plus:

| Project | Scope |
|---|---|
| `Ferret.Integration.Tests` | Real Core/Runtime/Cli/Connectors/Parsers/Persistence/VerticalSlice wired together in-process (no Docker; despite `CONTRIBUTING.md` describing a Docker-Compose integration setup — see `AEF-Onboarding-Validation.md`) |
| `Ferret.E2E.Tests` | Drives the actually-built CLI binary; no `ProjectReference` to `Ferret.Cli` by design |
| `Ferret.Architecture.Tests` | Dependency-direction / architecture-conformance regression tests |
| `Ferret.Benchmarks` / `Ferret.Benchmarks.Tests` | BenchmarkDotNet performance harness, and unit tests *for* that harness |

Full testing-strategy detail, including a measured coverage signal and stated-vs-observed gaps, is in `Technology-Inventory.md`.
