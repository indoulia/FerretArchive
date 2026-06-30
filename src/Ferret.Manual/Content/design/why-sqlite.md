# Why SQLite?

Ferret uses SQLite as its index store. This was not the default choice — it was a deliberate decision after evaluating several alternatives.

## What we considered

- **Embedded key-value stores** (LevelDB, RocksDB): fast, but no full-text search without a separate layer
- **Server-based databases** (PostgreSQL, MySQL): powerful, but require a running server — a non-starter for a local CLI tool
- **In-memory indexes** (Lucene, Elasticsearch): excellent search, but heavyweight and require JVM or a separate process
- **SQLite + FTS5**: embedded, zero-dependency, ACID, and ships with a production-quality full-text search engine built in

## Why SQLite won

**Zero deployment cost.** SQLite is a single file. Every workspace gets its own `.ferret/indexes/keyword/keyword-index.db`. No service to start, no port to manage, no credentials to configure.

**FTS5 is genuinely good.** SQLite's FTS5 extension supports BM25 ranking, prefix queries, phrase matching, and column weighting. It is not a toy.

**Single-file durability.** The entire index is one file. Backup means `cp`. Migration means `rm` and re-index. Recovery is trivial.

**Transactional correctness.** Every batch write is a transaction. Interrupted indexing leaves the database consistent, not corrupted.

## What we gave up

- **Distributed scale**: SQLite is single-writer. For a local developer tool, this is irrelevant.
- **Advanced vector search**: FTS5 has no native vector similarity. Sprint 16 will add hybrid search via a separate vector store.

## Related

- [Storage Architecture](../architecture/storage) — the SQLite schema
- [Why BM25 Before Vectors?](why-bm25) — why vectors come later
