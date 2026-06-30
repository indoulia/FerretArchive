# FUTURE-001 — Future Architecture

| Field | Value |
|---|---|
| **Document ID** | FUTURE-001 |
| **Version** | 1.0 |
| **Status** | Vision Document |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-29 |

---

## Purpose

This document captures the architectural direction for Ferret beyond Sprint 7 (M1 completion). It does not define committed APIs or implementation schedules — it defines architectural invariants that constrain future decisions and ensures the work done in V1 does not foreclose V2–V4 capabilities.

Every architecture decision in M1 was reviewed against the constraints in this document.

---

## Layer Model (Current → Future)

```
Current (Sprint 7):              Future (V2+):
─────────────────────            ──────────────────────────────────────
Ferret.Cli                       Ferret.Cli
  └─ Ferret.Workspace              └─ Ferret.Workspace
       └─ Ferret.Core                   └─ Ferret.Connectors.*
                                              └─ Ferret.Index.*
                                                   └─ Ferret.Knowledge
                                                        └─ Ferret.Memory
                                                             └─ Ferret.Core
```

**Invariants:**
1. `Ferret.Core` remains at the bottom — zero external dependencies, forever.
2. Each layer only depends on layers below it (enforced by MSBuild targets, ARCH-001 §7).
3. New subsystems are added as new projects, not merged into frozen M1 packages.
4. Every new capability is introduced as a contract in `Ferret.Core.*` (new namespace) before an implementation is written.

---

## Connector Architecture

**Current state (Sprint 7):** Connector contracts defined (`IConnector`, `ConnectorType`, etc.). No implementations.

**Target architecture (V2):**

```
Ferret.Core.Connectors          ← Contracts (Sprint 7, frozen)
  IConnector
  ConnectorMetadata
  ConnectorCapabilities
  ConnectorHealth
  ConnectorType

Ferret.Connectors.Filesystem    ← Sprint 8
Ferret.Connectors.Git           ← Sprint 9+
Ferret.Connectors.Jira          ← Sprint 10+
Ferret.Connectors.GitHub        ← Sprint 11+
Ferret.Connectors.AzureDevOps   ← Sprint 12+
...
```

**Design rules for all connectors:**
- Every connector lives in its own project — no shared implementation project
- Every connector is pure `IConnector` — no connector depends on another connector
- Connector health is async and cancellable — remote connectors are unreliable
- Connector state is persisted to `state.json` under `.ferret/connectors/<id>/`
- A connector failure never fails the platform — connectors are degradable

---

## Index Architecture

**Target architecture (V2):**

```
Ferret.Core.Indexing             ← Contracts
  IIndexEngine
  IIndexQuery
  IIndexResult
  IndexType (Keyword / Semantic / Graph)

Ferret.Index.Keyword             ← Inverted index (local, fast)
Ferret.Index.Semantic            ← Vector embeddings
Ferret.Index.Graph               ← Property graph
```

**Design rules:**
- Indexes are write-once, read-many — no in-place mutation
- All indexes live under `.ferret/indexes/<type>/` in the workspace
- The semantic index requires an embedding model; model configuration lives in `.ferret/config/models.json`
- The graph index is the queryable layer for the knowledge graph (V2)
- Indexing is always incremental (driven by `Changeset` from connectors)

---

## Knowledge Graph Architecture

**Target architecture (V2):**

```
Ferret.Core.Knowledge            ← Contracts
  IKnowledgeEngine
  IKnowledgeQuery
  IEntity
  IRelationship

Ferret.Knowledge                 ← Implementation
  EntityStore (→ .ferret/knowledge/entities/)
  RelationshipStore (→ .ferret/knowledge/relationships/)
  DocumentStore (→ .ferret/knowledge/documents/)
```

**Key design decisions locked in Sprint 7:**
- `.ferret/knowledge/` directory exists from day 1 (Sprint 7 init)
- Entity schema is JSON-LD compatible (future export as RDF)
- Relationships are typed and bidirectional
- Documents include provenance: which connector, which file, which version

---

## Memory Architecture

**Target architecture (V2/V3):**

```
Ferret.Core.Memory               ← Contracts
  IMemoryStore
  IWorkingMemory
  IEpisodicMemory
  ILongTermMemory

Ferret.Memory                    ← Implementation
  WorkingMemory (→ .ferret/memory/working/)    ← session-scoped
  EpisodicMemory (→ .ferret/memory/episodic/)  ← session history
  LongTermMemory (→ .ferret/memory/longterm/)  ← persistent facts
```

**Key design decision:**
The three-tier memory model is not an implementation detail — it is an architecture decision. Working memory is volatile (cleared on session end). Episodic memory persists across sessions but is prunable. Long-term memory persists until explicitly removed and is used for AI learning (V4).

---

## Snapshot Architecture (Enterprise Time Machine)

**Target architecture (V3):**

```
.ferret/snapshots/
  workspace/     ← workspace.json + state.json at each snapshot
  indexes/       ← serialized index state at each snapshot
  knowledge/     ← knowledge graph snapshot at each snapshot
```

**Design decision locked in Sprint 7:**
The `snapshots/` directory is created in Sprint 7 even though no snapshotting logic exists yet. This reserves the namespace and signals to future implementers that the directory layout is intentional.

Snapshot strategy: immutable snapshots tagged by git commit hash. A snapshot is "the complete ContextOS state at commit X." The Enterprise Time Machine is a git checkout + snapshot restore.

---

## Knowledge Space Architecture

A Knowledge Space is the V2 product concept that organises all knowledge components under a single addressable, shareable, permissioned object. It is the unit of collaboration in the federated ContextOS.

**Target architecture (V2):**

```
Ferret.Core.Spaces               ← Contracts
  IKnowledgeSpace
  ISpaceDescriptor
  ISpacePermissions
  ISpaceMount
  SpaceContextPolicy

Ferret.Spaces                    ← Implementation
  PersonalSpace                  ← wraps .ferret/ (RC1 baseline)
  RemoteSpace                    ← mounted team/org space
  SpaceRegistry                  ← local registry of mounted spaces
```

**What a Knowledge Space contains (by contract):**

```
IKnowledgeSpace
  Descriptor         : ISpaceDescriptor     // name, url, version, owner
  Connectors         : IReadOnlyList<IConnector>
  Indexes            : IReadOnlyList<IIndexEngine>  // keyword, semantic, graph
  Metadata           : ISpaceMetadata       // freshness, coverage, connector health
  Prompts            : IPromptRegistry      // context assembly templates
  AiConfiguration    : IFerretConfig       // model routing, embedding model
  Permissions        : ISpacePermissions    // read/write/mount/admin
  ContextPolicy      : SpaceContextPolicy   // token budget, content filters, dedup rules
```

**Design rules:**
- A personal Knowledge Space is exactly what RC1 builds — the V2 abstraction wraps it without changing it
- The index is an internal implementation component; it is never exposed directly to users or callers
- `IKnowledgeSpace.Search()` fans out to all internal indexes and returns a unified `SearchResult`
- Every connector, index, and prompt belongs to exactly one Knowledge Space
- Spaces are identified by URL: `local://personal`, `team://platform-knowledge`, `hub://org-name/space-name`

---

## Federation Architecture

**Target architecture (V2):**

```
Ferret.Core.Federation           ← Contracts
  ISpaceMount
  IMountedSpaceSync
  IFederatedSearchEngine
  IFederatedContextAssembler

Ferret.Federation                ← Implementation
  MountRegistry                  ← reads "mounts" from workspace.json
  MountSync                      ← pull-based sync of mounted space state
  FederatedSearchEngine          ← fans out search across all mounted spaces
  FederatedContextStage          ← plugs into Context Assembly pipeline
```

**Workspace manifest extension (V2):**

```json
{
  "workspaceId": "my-project",
  "mounts": [
    {
      "name": "platform-knowledge",
      "url": "team://platform-knowledge",
      "access": "read",
      "syncInterval": "15m"
    }
  ]
}
```

**Federated search architecture:**

The `FederatedSearchEngine` runs searches against all mounted spaces in parallel and merges results before the deduplication stage. Mounted spaces return lightweight result references (slug, score, source-space); full content is fetched lazily when context assembly needs it.

```
FederatedSearch (V2 stage)
  ├── personal.Search(query)          → SearchProviderResult
  ├── mount["platform"].Search(query) → SearchProviderResult
  └── mount["prompts"].Search(query)  → SearchProviderResult
         ↓ merge + score-normalize
  → Deduplicate (Sprint 13 stage, unchanged)
  → Expand → ContentFilter → TokenBudget → ContextPackage
```

**Shared AI inference architecture:**

`IAiProvider` (Sprint 12) is already workspace-scoped. In V2, the provider configuration can be inherited from a mounted Knowledge Space rather than defined locally:

```json
// .ferret/config/models.json (personal override)
{
  "inherit": "team://platform-knowledge",   // use team's provider config
  "localOverride": {
    "provider": "ollama",
    "model": "llama3"
  }
}
```

**Architecture invariants for federation:**
1. A mounted space's content never writes to the local index — mounts are read-only by default
2. Federation is opt-in — a workspace with no `mounts` array behaves exactly as RC1
3. `Ferret.Core` has no knowledge of federation — all federation logic lives in `Ferret.Federation`
4. Context policies on a mounted space govern what that space contributes; local policies govern what is assembled into the final package

---

## Plugin Architecture

**Current state:** `IModule` + `ICliModule` are the plugin contracts.

**Target architecture (V2):**

```
Ferret.Plugin.Sdk                ← Plugin developer contracts
  IFerretPlugin
  IPluginDescriptor
  IPluginContext
  IPluginCapabilities

Plugin isolation levels:
  In-process (fast, trusted)
  Out-of-process (isolated, untrusted)
  Remote (network, enterprise)
```

Plugins in V2 can contribute: connectors, index parsers, CLI commands, MCP tools, knowledge enrichers.

---

## MCP Architecture

**Target architecture (V1/M4):**

```
Ferret.Mcp                       ← MCP server implementation
  McpServer
  Tool registry (auto-discovered from modules)
  Resource registry (workspace, knowledge graph)
  Prompt registry (context assembly templates)
```

MCP is the primary AI integration surface. Every Ferret capability that makes sense for an AI assistant is exposed as an MCP tool. The MCP server is started with `ferret serve`.

---

## Configuration Architecture

**Sprint 7 baseline:**

```
.ferret/config/
  runtime.json       ← Platform configuration (log level, plugins)
  plugins.json       ← Plugin registry and configuration
  models.json        ← Embedding models, LLMs, rerankers
  connectors.json    ← Connector registry and credentials
```

**Target (V2):** Configuration is validated against JSON schemas. `ferret config validate` checks all config files against their schemas and reports errors.

**Future:** Remote configuration (enterprise: config from Ferret Hub, not local files). Secrets stored in system keychain, not config files.

---

## Security Architecture

**Current:** No security model (single-user local tool).

**Target (V3 enterprise):**
- Connector credentials in system keychain (never in `.ferret/config/`)
- Role-based access to knowledge graph (enterprise multi-user)
- Audit log for all knowledge graph writes
- Plugin sandboxing (process isolation for untrusted plugins)
- Air-gap mode: no outbound network calls, local models only

**Architecture invariant (preserved from M1):** `Ferret.Core` has no outbound network calls. All network activity is in connectors or plugins.

---

## Telemetry Architecture

**Sprint 7 baseline:** `.ferret/telemetry/` directory created (metrics/, events/, diagnostics/).

**Target (V2):**
- Metrics: Prometheus-compatible, exportable
- Events: structured event log (NDJSON), queryable
- Diagnostics: `ferret doctor` output extended by connectors and plugins

**Enterprise (V3):** Telemetry aggregated to Ferret Hub. Org-level dashboards: which teams use which connectors, knowledge graph growth, query patterns.

---

## Related Documents

- `ROADMAP-002-Future-Vision.md` — V2–V4 product vision (V2 = Federated ContextOS with Knowledge Spaces)
- `ARCH-001.md` — Current system architecture
- `docs/adr/README.md` — Architecture decision records
- `TECH-001-Technology-Evaluation.md` — Technology choices
