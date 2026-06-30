> **Post-rebrand note:** Both names referenced in this ADR now use the `Ferret.*` namespace prefix following the AISpace → Ferret rebrand in Sprint 5.

# ADR-0011 — Rename AISpace.SDK to AISpace.Plugin.SDK

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-06-27 |
| **Deciders** | AISpace Core Team |
| **Sprint** | Sprint 2 |
| **Supersedes** | — |
| **Breaking Change** | No (pre-implementation — no public consumers yet) |

---

## Context

During Sprint 2 (Repository Bootstrap), the project `AISpace.SDK` was scaffolded as the library plugin authors use to build extensions for the AISpace platform. The original name `AISpace.SDK` is generic and ambiguous.

As AISpace grows, the platform is likely to expose multiple integration surfaces — a CLI automation SDK, an MCP client SDK, a REST API SDK, or a CI integration library. If each of these is eventually packaged as an SDK, the name `AISpace.SDK` gives no signal about which SDK it is or who its audience is.

The Plugin SDK's defined scope is narrow and stable: it exposes only the interfaces declared in `AISpace.Core` that external plugin developers need to implement. It does not expose platform internals. ARCH-001 §8.3 states that plugins must reference only `AISpace.Core` interfaces through the Plugin SDK, and must never reference `AISpace.Runtime`, `AISpace.Cli`, or `AISpace.Mcp`. This scoped contract is a permanent architectural boundary, not a temporary implementation detail.

Given that the rename was identified before any public consumers exist (Sprint 2 is scaffolding only), the cost of renaming now is minimal.

## Decision

We will rename `AISpace.SDK` to `AISpace.Plugin.SDK` across all platform artifacts:

- Source project: `src/AISpace.Plugin.SDK/`
- Assembly name: `AISpace.Plugin.SDK`
- Root namespace: `AISpace.Plugin.SDK`
- Test project: `tests/AISpace.Plugin.SDK.Tests/`
- All `<ProjectReference>` and `<PackageReference>` consumers updated accordingly

The naming convention `AISpace.Plugin.SDK` signals:
- **Plugin** — the audience is plugin developers, not platform consumers
- **SDK** — uppercase acronym, consistent with industry convention (AWS SDK, Azure SDK)

References in ARCH-001 and SDK-001 that currently use `AISpace.SDK` will be updated to `AISpace.Plugin.SDK` in the next document revision cycle.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Keep `AISpace.SDK` | Ambiguous as the platform grows; forces consumers to disambiguate by reading docs rather than the package name |
| `AISpace.SDK` (no qualifier) | Slightly better than `AISpace.Sdk` but still gives no signal about audience; conflicts with possible future platform-level SDKs |
| `AISpace.PluginSDK` (no dot) | Non-standard — .NET namespace conventions use dot-separated segments; inconsistent with all other project names in the solution |
| `AISpace.Extensions` | Implies general-purpose extensibility rather than a specific plugin contract; used by many frameworks in a different sense |

## Consequences

### Positive

- Package name is self-documenting: `AISpace.Plugin.SDK` tells a new contributor exactly what it is and who should reference it.
- Future SDKs (`AISpace.Cli.SDK`, `AISpace.Mcp.SDK`) can follow the same pattern without ambiguity.
- The architectural boundary between platform internals and plugin-facing surface is visible at the project name level.

### Negative

- ARCH-001 §7 and SDK-001 currently reference `AISpace.SDK` by name; those documents require a minor update (scheduled for the next architecture review cycle, not blocking).

### Neutral / Risks

- No public NuGet package has been published; no plugin authors have taken a dependency on the old name. Zero breaking change risk at this stage.
- If the platform eventually ships a public `AISpace.Plugin.SDK` NuGet package, the dot in the middle segment (`Plugin.SDK`) is unusual but fully valid in NuGet package IDs.
