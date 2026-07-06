# DECISION-LOG — Ferret Project Decision History

This document captures the major decisions made across all sprints: technology choices, architectural patterns adopted or rejected, process decisions, and product pivots. It is the human-readable companion to the ADR index.

**Last updated:** 2026-07-06 (Architecture Conformance Baseline, ADR-0030) | **Status:** Living document

---

## How to Read This Log

Entries are grouped by sprint and category. Within each sprint, decisions are listed as:

- **Accepted** — in effect
- **Rejected** — not pursued and why
- **Deferred** — considered but postponed to a named future sprint

---

## Sprint 0 — Project Foundation

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Use .NET 9 / C# 13 | Technology | Accepted | Current LTS, `net9.0` TFM throughout |
| StyleCop enforced via `AnalysisMode: All` | Standards | Accepted | TreatWarningsAsErrors=true from day 1 |
| xUnit for all tests | Technology | Accepted | Matches .NET open-source convention |
| `Directory.Build.props` for shared props | Build | Accepted | Single source of truth for SDK, nullability, LangVersion |
| Separate src/ and tests/ directory trees | Structure | Accepted | Keeps test projects out of release output |
| PowerShell for dev scripts | Tooling | Accepted | Windows-first, cross-platform via pwsh |
| MIT license | Governance | Accepted | Open-source friendly, enterprise-compatible |
| ADR discipline from Sprint 0 | Process | Accepted | See ADR-0001 |

---

## Sprint 1–3 — Core Kernel

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Zero external dependencies in `Ferret.Core` | Architecture | Accepted | Core = contracts + value objects + exceptions only |
| `WorkspaceId`, `WorkspacePath` as value types | Architecture | Accepted | Typed IDs prevent primitive obsession errors |
| `Result<T>` and `CommandResult` types | Architecture | Accepted | Eliminates exception-as-flow-control for expected failures |
| `IModule` + `IModuleDescriptor` lifecycle | Architecture | Accepted | Every capability is a module; DI-first composition |
| `Ferret.Events` in-process event bus | Architecture | Accepted | Decoupled lifecycle events; no external broker dependency |
| Domain / Integration / System event taxonomy | Architecture | Accepted | Matches DDD event classification |
| `WorkspaceException` hierarchy root | Architecture | Accepted | All domain errors traceable to a single base |
| Use Channels for event routing | Technology | Deferred | Sprint 3+ evaluated; deferred to post-M1 |
| Polly for resilience | Technology | Deferred | Not needed in Core; deferred to connectivity layer |
| Scrutor for DI scanning | Technology | Rejected | Explicit registration preferred for testability and clarity |

---

## Sprint 4 — Architecture Documentation Baseline + Public Contracts

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Architecture document baseline: ARCH-001, ARCH-011, ARCH-013, ARCH-014 | Documentation | Accepted | Locked in system architecture before implementation |
| `IWorkspaceEngine`, `IWorkspaceLocator`, `IWorkspaceStateStore` as frozen contracts | Architecture | Accepted | Interface-first, impl follows in Sprint 5+ |
| `WorkspacePath`, `WorkspaceContext`, `WorkspaceStatistics` as sealed value types | Architecture | Accepted | No inheritance; pure data |
| `.ferret/` as workspace directory (was `.ai/`) | Architecture | Accepted | Matches product rename; `WorkspaceLayout` owns all path constants |
| `WorkspaceOptions` as nullable open-extension bag | Architecture | Accepted | Avoids constructor explosion for options |
| `Changeset` for incremental index updates | Architecture | Accepted | AG-005: incremental at every layer |
| All public types require XML doc comments | Standards | Accepted | `GenerateDocumentationFile=true` enforced by StyleCop |

---

## Sprint 5 — Runtime Host

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Wrap `Microsoft.Extensions.Hosting` internally | Architecture | Accepted | Not exposed through `Ferret.Core` contracts |
| `IHostedService` for `RuntimeHost` lifecycle | Technology | Accepted | Standard .NET hosted service lifecycle |
| `IRuntimeHost.StartAsync` / `StopAsync` / `RunAsync` | Architecture | Accepted | Mirrors `IHost` without coupling to it |
| `ModuleRegistry` eager-load at startup | Architecture | Accepted | Fail fast: missing module surfaced at boot |
| `ILifecycleParticipant` for ordered teardown | Architecture | Accepted | Prevents race conditions on shutdown |
| Product renamed AISpace → Ferret | Product | Accepted | See ADR-0005 |
| ContextOS as technology platform name | Product | Accepted | Survives the product rename; powers Ferret |

---

## Sprint 6 — Platform Entry Point & CLI Host

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| `System.CommandLine` for CLI parsing | Technology | Accepted | Microsoft-backed, .NET-native, composable |
| `ICliModule` / `CommandDefinition` pattern | Architecture | Accepted | Modules contribute commands; root factory builds tree |
| `IFerretContext` wraps every handler invocation | Architecture | Accepted | Handler isolation: no static state, no `Console` direct access |
| `IOutputFormatter` for all CLI output | Architecture | Accepted | Enables testing output without stdout coupling |
| `ICommandHandler.ExecuteAsync` signature | Architecture | Accepted | Single-method interface; easily mockable |
| `CommandDefinition.Group` for subcommand nesting | Architecture | Accepted | Unused until Sprint 7; activates `workspace init` / `workspace status` |
| `DiagnosticRunner` collects all `IDiagnosticCheck` | Architecture | Accepted | `ferret doctor` is module-contributed, not hardcoded |
| `ferret status` — not-running stub | Architecture | Accepted | Full IPC (process liveness) deferred to Sprint 7 |
| M1 Platform Foundation frozen | Architecture | Accepted | See ADR-0012; no breaking changes without superseding ADR |

---

## Sprint 7 — Workspace Engine (planned, not yet started)

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| `.ferret/` directory as ContextOS workspace root | Architecture | Accepted | Full tree: connectors/, indexes/, memory/, knowledge/, models/, snapshots/, telemetry/ |
| `IConnector` + connector contracts in `Ferret.Core` | Architecture | Accepted | Contracts only in Sprint 7; `FilesystemConnector` in Sprint 8 |
| `state.json` nested statistics sub-object | Architecture | Accepted | `WriteStatisticsAsync` reads-before-write to preserve knowledgeVersion / graphVersion |
| `config/` seeded with 4 empty JSON files | Architecture | Accepted | runtime.json, plugins.json, models.json, connectors.json — ContextOS configuration layer |
| Sprint 8 = Filesystem Connector | Architecture | Deferred | First connector proves the IConnector architecture |

---

## Technology Evaluation Summary

See `TECH-001-Technology-Evaluation.md` for the full evaluation grid.

| Technology | Decision | Sprint |
|---|---|---|
| System.CommandLine | Accepted | Sprint 6 |
| Microsoft.Extensions.Hosting | Accepted (internal wrap) | Sprint 5 |
| System.Text.Json | Accepted (BCL) | Sprint 7 |
| xUnit | Accepted | Sprint 0 |
| StyleCop | Accepted | Sprint 0 |
| Scrutor | Rejected | Sprint 3 |
| Polly | Deferred (post-M1) | Sprint 3 |
| MediatR | Rejected | Sprint 3 |
| Channels | Deferred | Sprint 3 |
| Health Checks middleware | Deferred | Sprint 6 |

---

## Governance — DOGFOOD-001 / Ferret V2 Reconciliation (2026-07-04)

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Adopt ADR-0025: uncommitted work during an active governance gate requires explicit authorization before commit | Governance | Accepted | See ADR-0025. Established after a governance audit found the Ferret V2 architecture program was developed while DOGFOOD-001 remained the most recent committed governance decision, with no recorded reconciliation between the two |
| Application of ADR-0025 to the current Ferret V2 working tree | Process | Deferred | Ferret V2 architecture program (ARCH-023–037, ADR-0021–0024, AGR-001–004, `Ferret.Persistence`/`Ferret.VerticalSlice`) remains uncommitted pending a future decision to authorize commit (post-DOGFOOD-001 closure) or discard |

---

## Governance — `ferret status` Interim Implementation Reconciliation (2026-07-06)

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Sprint 6's "`ferret status` — not-running stub, full IPC deferred to Sprint 7" decision | Governance | Preserved, not rewritten | See Sprint 6 above — that entry stands as the accurate record of what was decided and why at the time. This section reconciles it with what has since shipped; it does not replace it. |
| Interim PID-file liveness check for `ferret status` | Architecture | Accepted (interim) | Dogfooding issue #25 found the Sprint 6 stub reported "not running" unconditionally, even while a runtime host was genuinely alive — a real trust gap, not a cosmetic one. Rather than wait for Sprint 7's full IPC design, `ferret start` now writes `.ferret/runtime-status.json` (PID + start time) and `ferret status` verifies liveness via `Process.GetProcessById`, with automatic cleanup of stale markers from a crashed process. Implemented 2026-07-06, commit `3630094`. |
| Sprint 7 named-pipe IPC health endpoint | Architecture | Still planned — not replaced | The interim PID-file check is not a substitute for real IPC: it cannot distinguish the original process from an unrelated one that later reused the same PID (documented limitation in `RuntimeStatusFile`). Sprint 7's named-pipe design remains the intended long-term direction. This entry exists so a future Sprint 7 implementer knows an interim measure is already in place, why it exists, and what it does not solve. |

---

## Governance — Architecture Baseline Established (2026-07-06)

| Decision | Category | Outcome | Notes |
|---|---|---|---|
| Declare an Architecture Conformance Baseline (AC-001, AC-004, AC-008, AC-012; acyclic inward-only dependency graph; docs/governance synchronized; 31/31 architecture fitness tests, 30/30 solution test projects) | Governance | Accepted | See ADR-0030. Closes a four-round Architecture Conformance Review (branch reconciliation, documentation/governance alignment, and the two remaining AC-012 findings — see the two entries immediately above and commits `171e7ee`/`cff94b4`) |
| Future Epics must preserve AC-001/004/008/012 and the dependency graph's shape; violations require a fix before merge or a superseding ADR | Governance | Accepted | See ADR-0030 §Decision, preservation rules 1–4. Future Architecture Conformance Reviews treat this baseline as the starting point, not a re-derivation exercise |
| Automated CI enforcement for AC-012 specifically (semantic Core-purity, distinct from the dependency-graph checks `Ferret.Architecture.Tests` already enforces) | Architecture | Deferred | Not built in this round — verified manually. Compatible with the baseline but not required by it; a natural follow-up, not committed to a sprint |
