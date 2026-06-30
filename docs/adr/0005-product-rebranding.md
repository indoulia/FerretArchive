# ADR-0005 — Product Rebranding: AISpace to Ferret

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-06-27 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Sprint 5 (rebrand applied) |

> **Note:** This ADR was written retrospectively during Sprint 6 documentation consolidation. The decision was made during Sprint 5 and applied in a single atomic commit tagged `v0.5.0-ferret`.

---

## Context

The project was originally named **AISpace** — a working title chosen at inception before the product had a clear identity. By Sprint 5, the platform had a stable architecture, a coherent product vision (context intelligence for engineering teams), and a growing number of internal documents referencing the product by name.

"AISpace" was judged problematic on several dimensions:

- **Descriptive, not differentiating.** Every AI tool is "AI space" in some sense. The name carries no identity, no personality, no recall.
- **Generic AI category confusion.** Users associate "AI" prefixed tools with generic chat wrappers or model APIs, not with deep code intelligence.
- **Hard to trademark and search.** "AISpace" is common enough that the product would be invisible in search.
- **Doesn't hint at the product's core behaviour.** The product's primary activity is finding, surfacing, and connecting context — like a ferret searching for things in a burrow.

The underlying technology platform — the operating system for context — was given the name **ContextOS**, which survives as the technology layer regardless of product name.

## Decision

Rename the product from **AISpace** to **Ferret**. The name change applies to:

- Product name in all documentation and UI
- CLI binary: `aispace` → `ferret`
- .NET solution file: `src/AISpace.sln` → `src/Ferret.sln`
- All namespace prefixes: `AISpace.*` → `Ferret.*`
- DI extension: `AddAISpaceRuntime()` → `AddFerretRuntime()`
- Base exception: `AISpaceException` → `FerretException`
- All project folder names: `src/AISpace.*` → `src/Ferret.*`

The following are **not changed** to preserve audit trail integrity:

- Error code strings: `AISP-001` through `AISP-015` (stable identifiers)
- Git commit messages before Sprint 5 (immutable history)
- ADR body text for ADRs 0001–0011 (carry a post-rebrand notice banner instead)
- Git tags: `v0.5.0-sprint5` and earlier remain as-is

The technology platform name **ContextOS** is retained as-is. ContextOS is what Ferret is built on; it is not a product name.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Keep "AISpace" | Generic, not differentiating, hard to trademark |
| "ContextOS" as product name | Too technical, more suited to the platform layer than the user-facing product |
| "Dig" | Evocative but too short, limited domain coverage |
| "Burrow" | Related to ferrets but sounds like a data warehouse product |
| "Scout" | Already widely used in tooling (Scout APM, etc.) |

## Consequences

### Positive
- Clear, memorable product identity
- Personality: Ferret finds things. The name is the value proposition.
- ContextOS survives as the technology brand for technical/enterprise audiences
- Clean separation: Ferret (product) / ContextOS (platform) mirrors how developers think about Chrome/Chromium or Slack/Electron

### Negative
- One-time migration cost for any external consumers (covered by MIGRATION-001)
- `AISP-xxx` error codes are permanently decoupled from the product name (acceptable: error codes are implementation detail)

## Related

- `MIGRATION-001.md` — migration guide for contributors
- `BRAND-001.md` — full brand identity and naming conventions
- Tag: `v0.5.0-ferret` (first commit under the Ferret name)
- Sprint 5 review: `docs/sprint-reviews/sprint-5-review.md`
