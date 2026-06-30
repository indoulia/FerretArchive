# ADR-0013 — Capability-Based Platform Architecture

| Field | Value |
|---|---|
| **ADR** | 0013 |
| **Title** | Capability-Based Platform Architecture |
| **Status** | Accepted |
| **Date** | 2026-06-28 |
| **Sprint** | Sprint 8 |
| **Author** | Ferret Core Team |
| **Supersedes** | — |
| **Superseded by** | — |

---

## Context

Sprint 8 established the Connector Platform as the first major subsystem beyond the Runtime Host. In designing the connector architecture, a set of cross-cutting principles emerged that now govern how all future Ferret and ContextOS subsystems must be structured.

These principles apply beyond connectors to parsers, indexes, AI models, analytics engines, and any future ContextOS capability. Capturing them here as a formal ADR ensures they are discoverable, versioned, and actionable — not buried in a sprint spec.

The decisions in this ADR are realized in `ARCH-019-Connector-Platform-Architecture.md` for the connector subsystem. Future subsystem ARCs should cite ADR-0013 when adopting these patterns.

---

## Decision

Ferret and ContextOS adopt the following canonical platform principles. All are binding. Changes require a superseding ADR.

---

### Principle 1: Capability Composition Over Inheritance

Capabilities attach to platform components via interfaces, not class hierarchies.

**Correct:**
```csharp
public sealed class FilesystemConnector : IConnector, IAssetSource
```

**Incorrect:**
```csharp
public abstract class ConnectorBase { }
public sealed class FilesystemConnector : ConnectorBase { }
public interface IAssetSource : IConnector { }   // ← wrong: forces inheritance chain
```

Rationale: A connector may have zero, one, or many capabilities. Inheritance forces a combinatorial explosion of base classes. Composition allows a connector to opt into exactly the capabilities it supports without inheriting unrelated surface area.

---

### Principle 2: Universal Asset Model

`AssetDescriptor` is the lingua franca of the ContextOS platform. Every connector produces it. Every pipeline stage consumes it.

Connectors produce `AssetDescriptor` instances. They do not parse, index, enrich, or interpret content. The connector's job ends at discovery and metadata collection.

This separation means:
- Connectors can be swapped without changing the pipeline
- Parsers and enrichers can be swapped without changing connectors
- The knowledge graph can consume assets from any source using the same model

---

### Principle 3: Identity → Descriptor → Instance → Status Lifecycle

Every major platform component (connectors, parsers, AI models, plugins) follows this four-layer model:

| Layer | Model | Characteristics |
|---|---|---|
| **Identity** | `ConnectorMetadata` | Immutable, lightweight, no runtime state |
| **Descriptor** | `ConnectorDescriptor` | Static characteristics: capabilities, config schema, docs, supported platforms |
| **Instance** | `ConnectorInstance` | Workspace-scoped configuration (what a user configured) |
| **Status** | `ConnectorStatus` | Current runtime state: health, last sync, errors |

These four models are always separate. Configuration never bleeds into status. Status never becomes configuration. Identity is always the smallest possible fact set.

Future subsystems (parsers, models, analytics engines) should define their own equivalents of these four layers rather than reusing connector-specific types.

---

### Principle 4: Streaming by Default

> Every pipeline in ContextOS uses `IAsyncEnumerable<T>`. `List<T>` is only acceptable for bounded, known-small collections.

```
Connector → IAsyncEnumerable<AssetDescriptor>
           → IAsyncEnumerable<IndexableDocument>
           → IAsyncEnumerable<SearchResult>
           → IAsyncEnumerable<AnalyticsEvent>
```

Rationale: The target scale is millions of assets, thousands of documents, and continuous event streams. Any `ToList()` at a pipeline boundary creates an unbounded memory allocation that fails silently until the corpus is large enough to matter.

All streaming is backed by `IAsyncEnumerable` with `yield return`. Buffering is only permitted at the consumer end, never at the producer end, and only when the consumer's algorithm requires it (e.g., deduplication across a bounded batch).

---

### Principle 5: Normalization Before Processing

> Data is normalized once at the point of ingestion. Downstream systems assume normalized data and never re-normalize.

For `CanonicalUri`: normalized at construction in the connector. The knowledge graph, index engine, and analytics subsystem consume the URI as-is. If normalization rules change, a migration is required — not a re-normalization on read.

This principle applies to:
- `CanonicalUri` (§11 of ARCH-019)
- Asset fingerprints (computed once, stored, compared — never recomputed for equality)
- Connector IDs (canonical form at registration time)

---

### Principle 6: Separation of Discovery, Enrichment, Indexing, and Knowledge Extraction

Each pipeline stage has a single responsibility. Stages do not reach into adjacent stages.

| Stage | Input | Output | Responsibility |
|---|---|---|---|
| Discovery | (connector config) | `AssetDescriptor` | Find assets; collect metadata |
| Enrichment | `AssetDescriptor` | `AssetDescriptor` (enriched) | Add MIME type, language, ownership — no parsing |
| Indexing | `AssetDescriptor` | `IndexableDocument` | Parse content; extract tokens and structure |
| Knowledge | `IndexableDocument` | Graph nodes + edges | Identify relationships, entities, decisions |

A connector that indexes content is violating this principle. A parser that issues connector API calls is violating this principle.

---

### Principle 7: Commands Are Orchestration, Not Implementation

```
CLI → CommandHandler → Platform Services → Runtime → Connectors
```

Command handlers depend only on platform service interfaces (`IConnectorRegistry`, `IWorkspaceLocator`, `IIndexEngine`). They never reference connector implementations directly.

This means the same platform services can be invoked from:
- The CLI (`ferret connector list`)
- An MCP tool (`search_context`)
- A REST API endpoint (future)
- A web dashboard (future)

without duplicating business logic.

---

## Consequences

### Positive

- All future subsystems have a clear architectural template
- Platform principles are executable via architecture tests (`Ferret.Architecture.Tests`)
- Sprint-to-sprint design reviews can cite principles by number rather than re-litigating fundamentals
- Plugin and extension authors have a well-defined extension model (implement interfaces, compose capabilities)

### Negative / Trade-offs

- More types than a simpler inheritance-based design: `ConnectorMetadata`, `ConnectorDescriptor`, `ConnectorInstance`, `ConnectorStatus` are four separate models where one might suffice for a small system
- `IAsyncEnumerable` throughout the pipeline requires care at test boundaries (need `ToListAsync()` helper in tests)
- Streaming-by-default means synchronous APIs are never the "easy path" — developers must always think in terms of async enumeration

### Neutral

- This ADR does not specify which connectors to build or which pipeline stages to implement — only the shape of how they must be built
- Connectors that cannot stream (e.g., a connector to a legacy system that returns only bulk responses) must wrap bulk responses in `IAsyncEnumerable` via `yield return` to remain compliant

---

## Compliance

Architecture tests in `Ferret.Architecture.Tests` enforce:
- `IConnector` implementations are `sealed`
- `AssetDescriptor` and `ConnectorDescriptor` are immutable (no public setters)
- Connector assemblies do not reference `Ferret.Cli`
- `IAssetSource.DiscoverAsync` return type is `IAsyncEnumerable<AssetDescriptor>`

Additional rules are added per sprint as new principles require enforcement.

---

## References

- `ARCH-019-Connector-Platform-Architecture.md` — full connector platform specification
- `SPEC-008` — Sprint 8 design specification (`docs/superpowers/specs/2026-06-28-sprint-8-connector-platform-design.md`)
- `ADR-0012` — M1 Platform Foundation Freeze (governs which packages are frozen)
- `ROADMAP-001` — V1 sprint plan
