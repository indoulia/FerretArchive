# ARCH-024 — Ferret Artifact Inventory

| Field | Value |
|---|---|
| **Document ID** | ARCH-024 |
| **Version** | 1.0 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-001) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document catalogues existing artifacts; it makes no new architectural decision |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (Ferret V2 Architectural Boundary) |

---

## Purpose

This is the first V2 design document built on the boundary ARCH-023 establishes. Its sole purpose is to produce the canonical inventory of every artifact that exists within Ferret today — what it is, who owns it, how it is produced, and its defining characteristics.

This document does not design persistence, caching, storage, or APIs. It does not define validity rules (ARCH-025), persistence strategies (ARCH-026), or reuse mechanisms (ARCH-027). It is the shared factual foundation those documents will build on.

---

## Scope

Covers:
- Every artifact currently produced anywhere in the Ferret repository, traced to the real component that produces it
- For each artifact: name, owning component, production method, AI-derivation status, determinism, current persistence, ephemerality, reuse-eligibility candidacy, the inputs its validity would depend on, and its primary consumers
- A logical categorisation of artifacts, for readability only
- Explicit gaps — architectural concepts named in ARCH-001 or ARCH-023 that have no corresponding implementation today
- Impact of this inventory exercise on existing architecture

Does not cover:
- Storage mechanisms, cache hierarchies, or database schemas
- APIs of any kind
- Validity rules for any artifact (ARCH-025)
- Persistence strategies (ARCH-026)
- Reuse mechanisms (ARCH-027)
- Any redesign of an existing V1 component

---

## Repository-First Method

Per the Repository First Principle, this inventory was produced by direct investigation of the source code under `src/` and the real contents of the repository — not from ARCH-001's descriptions, which are Draft status and, as this document shows, materially diverged from the current implementation in several places. Where ARCH-001 or ARCH-023 names a concept that has no corresponding code, this document says so explicitly rather than inventing the missing piece. Only the eight component names ARCH-023 approved — Workspace Engine, Connector Platform, Parser Platform, Index Engine, Knowledge Engine, Review Engine, Artifact Engine, Domain Event Bus — are used as owning-component labels; where the real implementing project or class differs from what ARCH-001 implies, both are stated.

---

## Critical Findings

Three facts materially shape how to read the inventory below.

**1. The real persisted-state root is `.ferret/`, not `.ai/`.** ARCH-001 §2 (AG-006) and, following it, ARCH-023 state that platform state lives under `.ai/`. `WorkspaceLayout.RootDirectoryName` is `.ferret` (`src/Ferret.Workspace/WorkspaceLayout.cs:7`), and every real write path found in this investigation — workspace manifest, workspace state, connector instances, index state, the keyword index — writes under `.ferret/`. The repository's `.ai/` directory (session.md, current-context.json, checklists, templates, etc.) has no Ferret engine code reading or writing it anywhere in `src/`; it is a separate convention for an external AI coding assistant. This document uses `.ferret/` as the real persisted-state root. ARCH-023's citations of `.ai/workspace.json` and `.ai/state.json` were inherited from ARCH-001 and should be corrected in a future revision of that document.

**2. No artifact in current production use is actually AI-derived.** `IModelProvider`'s chat and embedding paths are fully implemented and functionally real (genuine HTTP calls to Ollama and OpenAI), but nothing in `Ferret.Cli` or `Ferret.Mcp` ever invokes them — `IModelRouter` has no caller anywhere in `src/`. Context assembly (`Ferret.AI.Context.ContextAssembler`), the closest real implementation of ARCH-001's "Knowledge Engine" context-assembly responsibility, is entirely deterministic: BM25 keyword search followed by deduplication, expansion, filtering, sorting, and budgeting — no model call occurs anywhere in it. The formal "AI-derived artifact" category ARCH-023 defines therefore has zero live instances in the product today; it exists only as reachable-but-unexercised code.

**3. Four of ARCH-001's seven named domain engines have no implementation.** Review Engine, Specification Engine, Artifact Engine, and Memory Engine correspond to no real engine code anywhere in the repository. What exists for each is a small number of isolated, unconsumed data-model types in `Ferret.Core` (exercised only by unit tests) and, for two of them, an empty CLI command-group stub explicitly marked as a future-sprint placeholder (`review` → Sprint 10, `memory` → Sprint 9). The real documents people call "reviews," "decisions," and "specifications" today are produced entirely through human/AI-assisted Markdown authorship, outside any Ferret engine.

---

## Artifact Taxonomy

Artifacts are grouped into eight categories below, purely for readability. The categories are not new components — every artifact is still attributed to its real owning component or, where none exists, flagged as a gap.

1. Discovery Artifacts (Connector Platform)
2. Parsing Artifacts (Parser Platform)
3. Indexing & Search Artifacts (Index Engine)
4. Context Assembly Artifacts (Knowledge Engine)
5. AI Model Invocation Artifacts (real, but currently unreachable in production)
6. Workspace & Configuration Artifacts (Workspace Engine)
7. CLI & MCP Surface Artifacts
8. Repository Documentation Artifacts (no owning V1 component)

Domain events (published on the Domain Event Bus) are catalogued separately in §9 — they are signals, not reusable knowledge artifacts, and are excluded from the reuse taxonomy for that reason.

---

## 1. Discovery Artifacts (Connector Platform)

Real implementing projects: `Ferret.ConnectorPlatform`, `Ferret.Connectors.Filesystem`.

### AssetDescriptor
- **Produced by:** `FilesystemConnector.BuildDescriptor`, called from `DiscoverAsync` (`src/Ferret.Connectors.Filesystem/FilesystemConnector.cs`)
- **AI-derived:** No
- **Deterministic:** Yes for identity/media-type fields (pure lookup); its fingerprint/size fields reflect live filesystem state, so re-running against an unchanged file reproduces it exactly
- **Persisted:** No — in-memory, streamed via `IAsyncEnumerable`, never written to disk
- **Ephemeral:** Yes — exists for one discovery pass
- **Reuse-eligible (candidate):** Yes, as an input signal — its determinism given an unchanged file makes it a stable basis for downstream validity decisions (rule itself deferred to ARCH-025)
- **Dependencies for validity:** The source file's content, path, and modification metadata; the registered `IConnector` implementation
- **Primary consumers:** `IndexPipeline.RunAsync`, `ParseContext.For`

### AssetFingerprint
- **Produced by:** `AssetFingerprint.CreateLightweight(lastWrite, sizeBytes)`, called independently in both `FilesystemConnector` and `IndexPipeline`
- **AI-derived:** No
- **Deterministic:** Yes — pure function of last-write time and file size
- **Persisted:** Indirectly — carried inside `Document.SourceFingerprint`, and the equivalent value is kept in the Index Engine's state store for incremental comparison (§3)
- **Ephemeral:** Per-call; the state-store copy is long-lived
- **Reuse-eligible (candidate):** Yes — this is the platform's existing "has this changed?" signal
- **Dependencies for validity:** File modification time and size
- **Primary consumers:** `IndexPipeline`'s fingerprint-diff logic, `Document.SourceFingerprint`

### ConnectorInstance
- **Produced by:** `ConnectorInstanceStore.ToInstance` (loaded from disk) or `ConnectorManager.BuildDefaultFilesystemInstance` (zero-config default)
- **AI-derived:** No
- **Deterministic:** Yes (pure deserialization, or deterministic given the same workspace root)
- **Persisted:** Yes — `.ferret/connectors.json`, written by `ConnectorInstanceStore.SaveAsync`
- **Ephemeral:** No — durable configuration record
- **Reuse-eligible (candidate):** Yes — it is already a persisted, stable configuration artifact
- **Dependencies for validity:** User/administrator configuration; workspace root path
- **Primary consumers:** `ConnectorManager.GetActiveConnectorsAsync`, `Ferret.Cli` connector commands

### ConnectorRuntime
- **Produced by:** `ConnectorManager.GetActiveConnectorsAsync`, cached per process
- **AI-derived:** No
- **Deterministic:** Not strictly — first call constructs and caches; the cache is not invalidated within a process run
- **Persisted:** No — process-memory cache only
- **Ephemeral:** Long-lived for the process, not across runs
- **Reuse-eligible (candidate):** Not meaningfully — it is a live handle, not a knowledge artifact
- **Dependencies for validity:** The underlying `ConnectorInstance`
- **Primary consumers:** `IndexPipeline.RunAsync`

### ConnectorDescriptor
- **Produced by:** `RegistryBuilder.Build`, sourced from each `IConnectorFactory.Descriptor`
- **AI-derived:** No
- **Deterministic:** Yes — static per factory
- **Persisted:** No — in-memory, immutable, built once at startup
- **Ephemeral:** No — long-lived for process lifetime
- **Reuse-eligible (candidate):** N/A — static capability metadata, not a computed knowledge artifact
- **Dependencies for validity:** The set of registered connector factories (a build-time fact)
- **Primary consumers:** `Ferret.Cli` connector commands, `ConnectorRegistry`

### ConnectorStatus / ConnectorHealth
- **Produced by:** `ConnectorManager.GetActiveConnectorsAsync`, which hardcodes `Connected` at cache-build time
- **AI-derived:** No
- **Deterministic:** No — does not reflect actual live connector health; a real health check exists (`FilesystemConnector.GetHealthAsync`) but is never called from the active pipeline
- **Persisted:** No
- **Ephemeral:** Yes, embedded in `ConnectorRuntime`
- **Reuse-eligible (candidate):** No — currently not a trustworthy signal (see gap note, §10)
- **Dependencies for validity:** N/A — flagged as a gap, not a reliable artifact
- **Primary consumers:** None found reading `.Status` downstream

---

## 2. Parsing Artifacts (Parser Platform)

Real implementing projects: `Ferret.ParserPlatform`, `Ferret.Parsers.Pdf`, `Ferret.Parsers.Office`, and the built-in parsers in `Ferret.ParserPlatform.Parsers` (Markdown, PlainText, Json, Csv).

### Document
- **Produced by:** Each parser's `ParseAsync` (e.g. `MarkdownParser`, `PdfParser`, `WordParser`, `ExcelParser`)
- **AI-derived:** No — confirmed by explicit doc comments on the PDF and Office parsers stating no chunking, embedding, or AI processing occurs; extraction uses PdfPig / OpenXml / regex only
- **Deterministic:** Yes for all content fields (`PlainText`, `Title`, `Sections`, `Metadata`), given identical input bytes. `ProducedAt` is the one non-deterministic (wall-clock) field present on every parser's output
- **Persisted:** Partially — `PlainText` and `Title` are written into the keyword index (§3); `Sections` and `Metadata` (author, page count, etc.) are computed but dropped before reaching the index
- **Ephemeral:** Yes in the pipeline (one `Document` per parse); its persisted projection is long-lived
- **Reuse-eligible (candidate):** Yes — deterministic given unchanged source bytes and an unchanged registered parser
- **Dependencies for validity:** Source file content, the specific parser version/registration that produced it
- **Primary consumers:** `IndexPipeline.RunAsync`, the keyword index, `Ferret.Search`

### ParseResult\<Document\>
- **Produced by:** `ParserDispatcher.DispatchAsync`
- **AI-derived:** No
- **Deterministic:** Yes, given identical input stream, media type, and registered parser set
- **Persisted:** No — transient return value
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** N/A — an envelope around `Document`, not independently meaningful to reuse
- **Dependencies for validity:** Same as `Document`, plus the dispatch outcome (`Success`/`Empty`/`Unsupported`/`Failed`)
- **Primary consumers:** `IndexPipeline.RunAsync`

### MediaTypeInfo
- **Produced by:** `MimeTypeResolver.Resolve` (static extension/filename lookup)
- **AI-derived:** No
- **Deterministic:** Yes — pure lookup table
- **Persisted:** No
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** N/A — static classification metadata
- **Dependencies for validity:** File name/extension only
- **Primary consumers:** `FilesystemConnector.BuildDescriptor` (only the `.MediaType` field is read downstream today; `.Category`/`.SuggestedKind`/`.Confidence` have no current consumer — see gaps, §10)

### ParserDescriptor
- **Produced by:** Static fields on each parser class, aggregated by `ParserRegistry`
- **AI-derived:** No
- **Deterministic:** Yes — compile-time constants
- **Persisted:** No
- **Ephemeral:** No — long-lived, process-lifetime registry
- **Reuse-eligible (candidate):** N/A — static capability metadata
- **Dependencies for validity:** The set of compiled/registered parsers (a build-time fact)
- **Primary consumers:** `ParserDispatcher.DispatchAsync`

---

## 3. Indexing & Search Artifacts (Index Engine)

Real implementing project: `Ferret.Indexing` (index build/maintenance), `Ferret.Search` (query). No single project implements a distinct "Search Engine"; ARCH-001 assigns query responsibility to "Knowledge Engine" (§4), but the real query implementation (`Ferret.Search`) sits alongside, not inside, `Ferret.Indexing`.

### Keyword index rows (persisted `Document` projection)
- **Produced by:** `SqliteKeywordIndexEngine.WriteAsync`
- **AI-derived:** No
- **Deterministic:** Yes, given the same `Document` and `AssetDescriptor` inputs
- **Persisted:** Yes — `.ferret/indexes/keyword/keyword-index.db` (only `PlainText`, `Title`, and identifying fields are stored; `Sections`/`Metadata` are not)
- **Ephemeral:** No — the only long-lived, durable artifact found anywhere in this investigation
- **Reuse-eligible (candidate):** Yes — already the platform's one real example of "compute once, read many times"
- **Dependencies for validity:** The source `Document` and its `AssetFingerprint`
- **Primary consumers:** `Bm25SearchProvider`, `Ferret.Indexing.DocumentService`

### Index state fingerprint map
- **Produced by:** `JsonIndexStateStore` (in-memory dictionary, persisted via `SaveAsync`)
- **AI-derived:** No
- **Deterministic:** No — it accumulates per-file fingerprints across index runs; its content depends on run history, not just current repository state
- **Persisted:** Yes — `.ferret/index-state.json`
- **Ephemeral:** No — long-lived, read at every index run
- **Reuse-eligible (candidate):** Yes — this is the platform's existing incremental-indexing mechanism (AG-005 already realised here)
- **Dependencies for validity:** The full history of prior index runs
- **Primary consumers:** `IndexPipeline.RunAsync`'s change-detection logic

### IndexResult / IndexStats
- **Produced by:** `IndexPipeline.RunAsync` on completion
- **AI-derived:** No
- **Deterministic:** Yes, given an unchanged repository and index state
- **Persisted:** No — returned to the caller (CLI) and printed
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** N/A — a run summary, not an artifact to reuse in place of recomputation
- **Dependencies for validity:** The index run it summarises
- **Primary consumers:** `Ferret.Cli` indexing commands

### SearchResult / SearchHit
- **Produced by:** `Bm25SearchProvider.SearchAsync`, `SearchService.SearchAsync`
- **AI-derived:** No — pure BM25 keyword scoring; no embeddings or vector search exist anywhere in `Ferret.Search` today
- **Deterministic:** Yes, given an unchanged keyword index and query
- **Persisted:** No — computed per query
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** Possibly, for an identical query against an unchanged index — a candidate for future consideration, not a rule this document defines
- **Dependencies for validity:** The keyword index content and the query text
- **Primary consumers:** `ContextAssembler` (§4), `Ferret.Cli` search commands, the MCP `search` tool

---

## 4. Context Assembly Artifacts (Knowledge Engine)

Real implementing project: `Ferret.AI` (`ContextAssembler`). No `IKnowledgeEngine` interface exists anywhere in the repository; "Knowledge Engine" is retained here only as the ARCH-023-approved label for this responsibility.

### ContextPackage
- **Produced by:** `ContextAssembler.AssembleAsync` — a pipeline of search, deduplicate, expand, filter, sort, and budget stages
- **AI-derived:** No — no model invocation occurs anywhere in this pipeline, contrary to what its name and ARCH-001's "Knowledge Engine" description might suggest
- **Deterministic:** Yes, given a stable index and identical inputs
- **Persisted:** No — returned to the caller and printed; never written to disk
- **Ephemeral:** Yes — per-invocation
- **Reuse-eligible (candidate):** Yes — deterministic and already index-state-dependent, making it a natural first candidate for the reuse mechanism ARCH-027 will define
- **Dependencies for validity:** The keyword index content, the request parameters (query, scope, token budget)
- **Primary consumers:** `Ferret.Cli`'s context command, the MCP `ferret_context` tool

### Rendered prompt string
- **Produced by:** `ContextPackage.ToPromptString()` — pure string formatting
- **AI-derived:** No
- **Deterministic:** Yes
- **Persisted:** No
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** N/A — a formatting of `ContextPackage`, not independently meaningful
- **Dependencies for validity:** Same as `ContextPackage`
- **Primary consumers:** CLI stdout, MCP tool result payloads

---

## 5. AI Model Invocation Artifacts

Real implementing projects: `Ferret.Providers.Ollama`, `Ferret.Providers.OpenAi`, `Ferret.Models`, `Ferret.Prompts`. As Critical Finding 2 states, everything in this category is real and reachable but currently has **no caller anywhere in the product** — these are the only artifacts in this inventory that would actually satisfy ARCH-023's formal "AI-derived artifact" definition, and none of them are produced in current use.

### ChatResponse / ChatResponseChunk
- **Produced by:** `OllamaChatModel.ChatAsync`, `OpenAiChatModel.ChatAsync` — genuine model calls
- **AI-derived:** Yes
- **Deterministic:** No — model sampling
- **Persisted:** No
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** Not currently applicable — never produced in production use today
- **Dependencies for validity:** The prompt/request content, the specific model and provider invoked
- **Primary consumers:** None found in production code

### EmbeddingResult
- **Produced by:** `OllamaEmbeddingModel.EmbedAsync`, `OpenAiEmbeddingModel.EmbedAsync` — genuine model calls
- **AI-derived:** Yes
- **Deterministic:** Effectively yes for a fixed model version, not guaranteed stable across model versions
- **Persisted:** No
- **Ephemeral:** Yes
- **Reuse-eligible (candidate):** Not currently applicable — never produced in production use today; also structurally disconnected from `Ferret.Search` (no vector search exists)
- **Dependencies for validity:** The embedded content, the specific model and provider invoked
- **Primary consumers:** None found in production code

### ModelDescriptor / ProviderDescriptor catalog
- **Produced by:** `ModelRegistry.CreateAsync`, calling each provider's `ListModelsAsync`
- **AI-derived:** No — this is metadata about providers/models, not model output
- **Deterministic:** The Ollama variant depends on the local daemon's installed models (environment-dependent); the OpenAI variant is a static, deterministic catalog
- **Persisted:** No — rebuilt at process startup, held in-memory
- **Ephemeral:** Long-lived for the process, not across runs
- **Reuse-eligible (candidate):** N/A — catalog metadata, not a knowledge artifact
- **Dependencies for validity:** Installed/available models at query time
- **Primary consumers:** `Ferret.Cli` model commands

---

## 6. Workspace & Configuration Artifacts (Workspace Engine)

Real implementing project: `Ferret.Workspace`, `Ferret.Configuration.AI`.

### WorkspaceManifest
- **Produced by:** `JsonWorkspaceStore.WriteManifestAsync`, called from `WorkspaceInitializer.InitialiseAsync`
- **AI-derived:** No
- **Deterministic:** Yes to regenerate (a fresh identifier and timestamp only)
- **Persisted:** Yes — `.ferret/workspace.json`
- **Ephemeral:** No — durable
- **Reuse-eligible (candidate):** N/A — identity record, not a computed knowledge artifact
- **Dependencies for validity:** Workspace initialization event
- **Primary consumers:** `WorkspaceLocator`, `DefaultWorkspaceContext`

### WorkspaceStateDto
- **Produced by:** `JsonWorkspaceStore.WriteStateAsync`, `WorkspaceStateStore`
- **AI-derived:** No
- **Deterministic:** No — accumulates indexing statistics and per-connector state over time
- **Persisted:** Yes — `.ferret/state.json`
- **Ephemeral:** No — durable, updated across runs
- **Reuse-eligible (candidate):** N/A — operational state, not a computed knowledge artifact to reuse
- **Dependencies for validity:** The full history of workspace operations
- **Primary consumers:** `WorkspaceEngine`, `Ferret.Cli` workspace commands

### Configuration seed files
- **Produced by:** `WorkspaceInitializer.InitialiseAsync` (empty `{}` seed files for runtime/plugins/models/connectors config)
- **AI-derived:** No
- **Deterministic:** Yes — always `{}` at initialization
- **Persisted:** Yes — `.ferret/config/*.json`
- **Ephemeral:** No
- **Reuse-eligible (candidate):** N/A — configuration scaffolding
- **Dependencies for validity:** N/A
- **Primary consumers:** Configuration loaders (where implemented)

### AiOptions / ProviderOptions
- **Produced by:** `AiConfigurationModule.ConfigureServices`, binding and validating from `IConfiguration`
- **AI-derived:** No — describes AI providers, is not AI output
- **Deterministic:** Yes, given the same configuration source and environment variables
- **Persisted:** No — sourced from configuration, held in-memory as a singleton
- **Ephemeral:** No — long-lived for process lifetime
- **Reuse-eligible (candidate):** N/A — configuration, not a computed artifact
- **Dependencies for validity:** Configuration source and environment variables
- **Primary consumers:** `ModelRouter`

---

## 7. CLI & MCP Surface Artifacts

Real implementing projects: `Ferret.Cli`, `Ferret.Mcp`. Every artifact in this category is transient, request-scoped, and not AI-derived — all four registered MCP tools (`search`, `read_document`, `workspace_status`, `ferret_context`) proxy deterministic retrieval/index/workspace services; none invoke a model.

| Artifact | Produced by | Persisted | Ephemeral | Reuse-eligible (candidate) | Primary consumers |
|---|---|---|---|---|---|
| `CommandResult` | Every `ICommandHandler.ExecuteAsync` | No | Yes | N/A — process exit signal | CLI invocation pipeline |
| `DiagnosticCheckResult` | Each `IDiagnosticCheck.RunAsync` | No | Yes | N/A — point-in-time health check | `DiagnosticRunner` |
| `ValidationFailure` / `ValidationResult` | Config/connector validation handlers | No (console only) | Yes | N/A — validation report | Console output |
| View-model DTOs (`IndexSummaryViewModel`, etc.) | CLI command handlers | No | Yes | N/A — rendering only | Console output |
| `McpToolDescriptor` | Each `IMcpTool`'s `Descriptor` | No | No (held for session) | N/A — static tool metadata | `McpToolRegistry` |
| `McpToolResult` | Each tool's `ExecuteAsync` | No | Yes | Inherits reuse-eligibility of the underlying artifact it wraps (e.g. `ContextPackage`, `SearchResult`) | MCP client, `SdkToolAdapter` |
| `McpResourceContent` | Each `IMcpResource`'s `ReadAsync` | No | Yes | Inherits reuse-eligibility of underlying state (index stats, connector list, workspace status) | MCP client |

None of these are AI-derived; all depend deterministically on the state of the component they surface.

---

## 8. Repository Documentation Artifacts (No Owning V1 Component)

These are real, valuable artifacts that exist in the repository today, but — per Critical Finding 3 — are produced entirely outside any Ferret engine. They are included because a "canonical inventory of every artifact that exists within Ferret today" would be incomplete without them; they are not attributed to Review Engine, Specification Engine, or Artifact Engine because no such engine produces them.

### Architecture Review documents
- **Examples:** `docs/Reviews/AR-001.md`, `AR-002.md`
- **Produced by:** Human/AI-assisted Markdown authorship, following the process in `docs/Reviews/README.md`; no tooling in `src/` generates or validates them
- **AI-derived:** Partially, in the informal sense that an AI assistant may help draft them — not AI-derived in ARCH-023's formal sense (no `IModelProvider` invocation is involved)
- **Deterministic:** No — human/editorial judgment
- **Persisted:** Yes — Markdown files under `docs/Reviews/`, committed to git
- **Ephemeral:** No
- **Dependencies for validity:** N/A — not produced by a repeatable process this inventory can characterise
- **Primary consumers:** Maintainers, future architecture decisions

### Architecture Decision Records
- **Location:** `docs/adr/*.md`
- Same production method, persistence, and characteristics as Architecture Review documents above.

### Sprint/feature specifications and plans
- **Location:** `docs/superpowers/specs/*.md`, `docs/superpowers/plans/*.md`
- **Produced by:** AI-assisted authorship via a coding-assistant workflow, capturing human product direction as Markdown
- Same non-deterministic, human/AI-collaborative production method as above; no code in `src/` reads or writes these files.

### Session/context tracking files
- **Location:** `.ai/session.md`, `.ai/current-context.json`
- **Produced by:** An external AI coding assistant's own convention (confirmed: zero references to either path anywhere in `src/`)
- **Not part of the Ferret product's persisted state** (see Critical Finding 1) — included here only because they are real files that exist in the repository, not because any Ferret component owns them.

---

## 9. Domain Events (Signals, Not Artifacts)

The Domain Event Bus (`IEventBus`) is real. Real, currently-published events exist only for two areas:

- **Indexing:** `DocumentDiscoveredEvent`, `DocumentParsedEvent`, `DocumentParsingFailedEvent`, `DocumentSkippedEvent`, `DocumentIndexedEvent`, `IndexingStartedEvent`, `IndexingCompletedEvent`, `IndexingFailedEvent`
- **Runtime:** `ModuleLoaded`, `ModuleActivated`, `ModuleStopped`, `RuntimeStarted`, `RuntimeStopped`

No `SpecificationApproved`, `ReviewCompleted`, or `ArtifactCommitted` event exists anywhere in the repository — consistent with Critical Finding 3. These events are notifications that a change occurred; they are not themselves knowledge to be persisted or reused, so they are excluded from the artifact taxonomy in §1–§8. They are recorded here because a future validity document (ARCH-025) will likely use them as invalidation signals.

---

## 10. Gaps — Named Concepts With No Implementation

Per the Repository First Principle, every concept below is recorded as a gap, not an artifact, because no code produces it today.

| Concept | What exists | What's missing |
|---|---|---|
| Review Engine | `ReviewId`, `ReviewStatus`, `ReviewResult` (Core types, referenced only by unit tests); an empty `review` CLI command-group stub ("Sprint 10") | No engine, no consumer, no producer anywhere |
| Specification Engine | `SpecificationId`, `SpecificationStatus` (Core types, referenced only by unit tests) | No engine, no consumer, no producer; not in ARCH-023's approved component list either |
| Artifact Engine | `ArtifactId` only (Core type, referenced only by unit tests) | No engine, no provenance model, no audit log implementation |
| Memory Engine | `MemoryEntry` (Core type, never constructed outside its own definition); an empty `memory` CLI command-group stub ("Sprint 9") | No engine; `.ai/session.md` is not read or written by any Ferret code |
| `IReranker`, `RerankResult`/`RerankRequest`/`RerankItem` | Interface and model types only | Zero implementations, zero callers |
| `IVisionModel` | Interface only | Zero implementing classes anywhere |
| `IConversationMemory`, `ITaskMemory`, `IWorkspaceMemory` | Null-object implementations only, explicitly documented as placeholders "until Sprint 15" | No real backing store |
| Prompt rendering in production | `IPromptRegistry`/`IPromptRenderer` are implemented and functional | Nothing registers a real `PromptTemplate` or calls `Render` outside `Ferret.Manual` documentation examples |
| Vector/semantic search | `SearchCapabilities.SupportsSemantic`/`SupportsHybrid` flags exist | No provider sets them true; `IEmbeddingModel` is functional but structurally disconnected from `Ferret.Search` |
| `IAssetEnricher`, `IContentNormalizer` | Empty interfaces, bodies commented out, marked "Sprint 9: not implemented" | No implementation, no pipeline stage references them |
| `IConnectorSession` / `ConnectAsync` | Implemented (`FilesystemConnectorSession`) | Never invoked — the active discovery pipeline bypasses it entirely |
| `ConnectorHealth` / `GetHealthAsync` | Implemented (`FilesystemConnector.GetHealthAsync`) | Never called; `ConnectorManager` hardcodes health as `Connected` instead |
| `Ferret.Plugins`, `Ferret.Plugin.SDK` | Empty module classes only | No plugin contract, loader, or manifest type exists anywhere |
| `Ferret.Telemetry` | Empty module class only | No logging/tracing/metrics artifact type exists |
| `Ferret.Configuration` | Empty module class only | All real configuration surface lives in `Ferret.Configuration.AI` instead |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses, as its sole evidence base, the real source code of every V1 component it inventories: Connector Platform, Parser Platform, Index Engine, Knowledge Engine's real implementations (`Ferret.Search`, `Ferret.AI`), Workspace Engine, the AI provider projects, and the CLI/MCP surface. It also reuses ARCH-023's approved vocabulary and its "AI-derived artifact" definition without modification.

**Existing components extended.** None. This document assigns no new responsibility, interface, or behaviour to any V1 component.

**Existing components intentionally unchanged.** All of them, without exception — including every component and every gap catalogued above. This is an observational document; it changes nothing.

**New concepts introduced.** One, purely organisational: the eight-category artifact taxonomy in this document (§1–§8), used only to group findings for readability. It introduces no new owning component beyond the eight ARCH-023 already approved, no new interface, and no new storage or API boundary. It is a reading aid, not architecture.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Parent — this document's scope, vocabulary, and "AI-derived artifact" definition are inherited from it unmodified |
| [ARCH-001 §2, §7](ARCH-001.md) | Source of the V1 goals and engine descriptions this document verifies against actual code, and corrects where they diverge (Critical Findings 1 and 3) |
| [ARCH-013](ARCH-013.md) | Domain event catalogue — real events found in §9 should be reconciled with it |
| [ARCH-019](ARCH-019-Connector-Platform-Architecture.md) | Connector Platform architecture referenced in §1 |
| ARCH-025 (Computation Validity) | Next document in the series — will define validity rules for the artifacts catalogued here |
| ARCH-026 (Persistence) | Will define persistence strategy for artifacts marked reuse-eligible here |
| ARCH-027 (Reuse) | Will define the reuse mechanism for artifacts marked reuse-eligible here |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial artifact inventory — first V2 design document built on ARCH-023. |
