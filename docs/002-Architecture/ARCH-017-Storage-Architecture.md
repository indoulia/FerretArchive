# ARCH-017 — Storage Architecture

| Field | Value |
|---|---|
| **Document ID** | ARCH-017 |
| **Version** | 1.0 |
| **Status** | Reserved — not yet implemented |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

> **This is a reservation document.** It documents the intended storage architecture for V2+ to ensure V1 decisions do not foreclose V2 options. No implementation is required until the relevant sprint.

---

## Overview

Ferret requires multiple storage backends for different data characteristics. This document defines what gets stored where, which technology serves each role, and why. The storage strategy is local-first and repository-rooted — all storage lives under `.ferret/` unless explicitly configured otherwise.

---

## Storage Categories

### 1. Metadata Storage

**Purpose:** Workspace manifest, connector state, index state, plugin registry, run history.

**Characteristics:**
- Structured, relational
- Frequently read, infrequently written
- Small volume (<100MB for typical workspace)
- Must be human-readable and git-diffable for audit purposes

**Preferred technology:** SQLite (`metadata.db` in `.ferret/`)

**Rationale:** SQLite is embedded, zero-configuration, and battle-tested. For workspace metadata, it offers transactional consistency (no partial writes on crash), rich query capability, and a well-understood schema migration path. Newline-separated JSON is the alternative but loses ACID guarantees.

**Alternative considered:** JSON files per entity — rejected for metadata because multi-entity transactions require file-locking.

**Reservation path:** `.ferret/metadata.db`

---

### 2. Context Storage

**Purpose:** Context cache (assembled context windows), context history (what was sent to which model, when), context templates.

**Characteristics:**
- Write-once, read-many
- Medium volume (~1–10GB for active workspace)
- Prunable (old context windows can be deleted)

**Preferred technology:** SQLite (`context.db` in `.ferret/`)

**Rationale:** Context windows are text blobs with metadata (timestamp, source files, model, token count). SQLite BLOB storage is appropriate. Context history enables the Enterprise Time Machine (V3.5).

**Reservation path:** `.ferret/context.db`

---

### 3. Search Index (Keyword)

**Purpose:** Full-text search over source code, documents, comments, identifiers.

**Characteristics:**
- Large volume (grows with codebase size)
- Write-heavy during indexing, read-heavy during queries
- Exact and prefix matching required

**Preferred technology:** SQLite FTS5 (`keyword-index.db` in `.ferret/indexes/keyword/`)

**Rationale:** SQLite FTS5 is a full-text search engine built into SQLite. Zero additional dependency. Supports ranked results, phrase search, prefix matching. For a local code search tool, it handles codebases up to tens of millions of files without external infrastructure.

**Alternative considered:** Tantivy (Rust, excellent performance) — rejected for V2 due to .NET interop complexity. May be revisited for V3 if performance is insufficient.

**Reservation path:** `.ferret/indexes/keyword/`

---

### 4. Vector Database (Semantic Search)

**Purpose:** Embedding vectors for semantic similarity search over code chunks, documents, decision text.

**Characteristics:**
- Large volume (~768–1536 floats per chunk × millions of chunks)
- Insert-heavy during indexing, query-heavy during context assembly
- Approximate nearest-neighbor (ANN) queries

**Preferred technology:** Qdrant (embedded mode or local server)

**Rationale:** Qdrant is a purpose-built vector database with an excellent .NET SDK. Supports HNSW indexing for fast ANN queries. Runs in-process (embedded) for local deployments and as a server for enterprise deployments.

**Alternatives considered:**
- FAISS — excellent performance but C++ binding complexity in .NET
- SQLite-vec — newer, promising, watch for V2.5 evaluation
- ChromaDB — Python-first, .NET client is community-maintained
- Milvus — enterprise-grade but requires Docker deployment

**Reservation path:** `.ferret/indexes/semantic/`

---

### 5. Analytics Storage

**Purpose:** Usage metrics, event log, performance telemetry, cost tracking.

**Characteristics:**
- Append-only write pattern
- Time-series queries (last N days, trend analysis)
- Prunable (retain last N days/events)

**Preferred technology:** SQLite (`analytics.db` in `.ferret/telemetry/`)

**Rationale:** For a local tool, SQLite analytics is sufficient for V2.5. Time-series queries over <10M rows are fast with appropriate indexes. No external telemetry service is required.

**Enterprise (V3+):** Optional push to central Ferret Hub (opt-in, privacy-preserving aggregation).

**Reservation path:** `.ferret/telemetry/`

---

### 6. Cache

**Purpose:** Hot context (recently assembled context windows), session state, connector response cache.

**Characteristics:**
- Volatile (cleared on session end or TTL expiry)
- Low latency required
- Small volume

**Preferred technology:** In-memory (Dictionary<string, object> with TTL)

**Optional enterprise technology:** Redis (for multi-process / multi-machine scenarios)

**Rationale:** For a local CLI tool, in-memory cache is sufficient and adds no dependency. Redis is reserved for the enterprise deployment where multiple Ferret processes (e.g., MCP server + CLI + daemon) share state.

---

### 7. Artifact Storage

**Purpose:** AI-generated artifacts (code reviews, summaries, documentation drafts), plugin outputs.

**Characteristics:**
- File-based
- Versioned (artifacts are immutable once written)
- Referenced from metadata DB

**Preferred technology:** File system (`.ferret/artifacts/`)

**Rationale:** Artifacts are files — they belong in the file system. Metadata about artifacts (timestamp, source, model, token cost) lives in the metadata DB. Binary artifacts (images, PDFs) are stored as files, referenced by path.

**Reservation path:** `.ferret/artifacts/`

---

### 8. Snapshot Storage

**Purpose:** Point-in-time workspace snapshots for the Enterprise Time Machine (V3.5).

**Characteristics:**
- Append-only (snapshots are immutable)
- Large volume over time
- Queryable: "give me the workspace state at commit X"

**Preferred technology:** File system + git tagging

**Strategy:** A snapshot is a copy of the complete `.ferret/` state (metadata.db, state.json, workspace.json, index manifests) at a specific point in time, tagged to a git commit hash. The snapshot directory is:

```
.ferret/snapshots/<git-commit-short-hash>/
  workspace.json
  state.json
  index-manifest.json
  knowledge-manifest.json
```

Full index data is not snapshotted by default — only manifests. Full index snapshots are optional (large, enterprise use case).

**Reservation path:** `.ferret/snapshots/`

---

## Storage Evolution Path

| Version | New Storage Added |
|---|---|
| V1 (Sprint 7) | `.ferret/` directory tree created (all paths reserved) |
| V1 (Sprint 9) | `keyword-index.db` (SQLite FTS5) |
| V2 | `metadata.db` (SQLite), `context.db` (SQLite) |
| V2 | Qdrant vector index (`.ferret/indexes/semantic/`) |
| V2.5 | `analytics.db` (SQLite) |
| V3.5 | Snapshot storage (`.ferret/snapshots/`) |
| V4 | Enterprise: Redis cache, remote Qdrant, Ferret Hub |

---

## Schema Migration Strategy

All SQLite databases use a `schema_version` table. On startup, `Ferret.Workspace` checks the version and applies pending migrations. Migrations are embedded resources in the library.

JSON files (`workspace.json`, `state.json`) use `schemaVersion` fields with the same pattern. `WorkspaceEngine.UpgradeAsync` handles JSON schema migrations.

---

## Privacy Constraints

1. No storage backend sends data outside the local machine by default.
2. Embedding vectors are computed locally; source text never leaves the machine.
3. Analytics events are stored locally; opt-in aggregation to Ferret Hub is V3+.
4. Snapshots are local; remote backup is a user-configured option, not a default.

---

## Related Documents

- `FUTURE-001-Future-Architecture.md`
- `TECH-001-Technology-Evaluation.md`
- `ROADMAP-002-Future-Vision.md`
- `ARCH-001.md` (§14: Storage strategy)
