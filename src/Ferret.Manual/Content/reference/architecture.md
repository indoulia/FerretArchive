# Architecture Reference

Index of all Architecture Decision Records (ADRs) in `docs/adr/`. Each ADR records a significant architectural decision, its context, alternatives considered, and consequences.

## ADR Index

| ADR | Title | Status | Sprint | Decision Summary |
|---|---|---|---|---|
| ADR-0001 | Use Architecture Decision Records | Accepted | Sprint 0 | Adopt ADR format; store in `docs/adr/`; Markdown, PR workflow |
| ADR-0005 | Product Rebranding: AISpace to Ferret | Accepted | Sprint 5 | Rename product, CLI, namespaces, solution; keep `AISP-xxx` error codes |
| ADR-0011 | Rename AISpace SDK to AISpace Plugin SDK | Accepted | Sprint 4 | Plugin SDK renamed for clarity before freeze |
| ADR-0012 | Milestone 1: Platform Foundation Freeze | Accepted | Sprint 6 | Freeze `Ferret.Core`, `Ferret.Runtime`, `Ferret.Hosting`, `Ferret.Cli`, `Ferret.Events`, `Ferret.Health` |
| ADR-0013 | Capability-Based Platform Architecture | Accepted | Sprint 8 | Seven platform principles: composition, asset model, lifecycle, streaming, normalization, stage separation, commands as orchestration |
| ADR-0014 | Document Processing Architecture | Accepted | Sprint 9 | Eight document processing principles: `Document` canonical model, MediaType dispatch, immutability, provenance, parser/indexer separation |
| ADR-0015 | Information Retrieval Architecture | Accepted | Sprint 10 | Canonical query AST; `ISearchProvider` abstraction; canonical result identities; BM25 first |
| ADR-0016 | Integration Platform Architecture | Accepted | Sprint 11 | Host Architecture Pattern: `Capabilities → Platform Services → Hosts → Protocols` |
| ADR-0017 | MCP Runtime Architecture | Accepted | Sprint 11 | Stdio transport; SDK confined to `Transport/Stdio/`; immutable registries; stateless adapters |
| ADR-0018 | Application Layer Reserved | Accepted | Sprint 11 | `Ferret.Application` deferred to Sprint 13 |
| ADR-0019 | AI Platform Architecture | Accepted | Sprint 12 | Ferret owns AI contracts; vendor SDKs confined to provider packages; immutable `ModelRegistry` |
| ADR-0020 | Prompt Platform Architecture | Accepted | Sprint 12 | `{{variable}}` substitution; `PromptRegistry` immutable after startup; DI registration pattern |

## Reading ADRs

ADRs live at `docs/adr/NNNN-kebab-case-title.md`. Each follows this structure:

- **Context** — why the decision was needed
- **Decision** — what was decided
- **Alternatives Considered** — what was rejected and why
- **Consequences** — positive, negative, and neutral outcomes

## Milestone Summary

| Milestone | Sprint | ADR | Scope |
|---|---|---|---|
| M1 — Platform Foundation | Sprint 6 | ADR-0012 | Core, Runtime, Hosting, CLI, Events, Health |
| M2 — Data Platform | Sprint 9 | ADR-0014 | Connectors, Documents, Index |
| M3 — Multi-Host Platform | Sprint 11 | ADR-0016 | MCP, Integration |
| M4 — AI Platform | Sprint 12 | ADR-0019/0020 | Models, Prompts, Providers |
| RC1 | Sprint 14 | — | Context Assembly, File Watching, Installer |

## Related

- [Architecture Explorer](../architecture/index) — visual architecture documentation
- [Design Decisions](../design/index) — human-readable design rationale
