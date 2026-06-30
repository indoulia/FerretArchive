# Indexing

The index pipeline discovers assets from connectors, parses them, and writes searchable documents to the SQLite FTS5 index. Understanding how it works helps you tune performance and diagnose issues.

## Running the index

```bash
ferret index
```

The first run is a full index. Subsequent runs are incremental by default.

Add `--verbose` to see per-file details:

```bash
ferret index --verbose
# [filesystem:default]  src/Ferret.Core/SearchService.cs  12ms
# [filesystem:default]  src/Ferret.AI/ContextAssembler.cs  8ms
# Discovered: 1,247  Indexed: 3  Skipped: 1,244  Duration: 0.3s
```

## Incremental Indexing

Ferret tracks the last-modified time of each indexed file. On subsequent runs:

- Files unchanged since the last index are **skipped**
- New files are **indexed**
- Modified files are **re-indexed** (old document replaced)
- Deleted files are **removed** from the index

```bash
ferret index
# Discovered:  1,247  Indexed: 3  Skipped: 1,244  Duration: 0.3s
```

Incremental indexing is controlled by `indexing.enableIncrementalIndex` (default: `true`).

## Force Rebuild

```bash
ferret index --rebuild
```

Drops and rebuilds the entire SQLite index. Use this when:
- The index schema has changed after a Ferret upgrade
- You suspect index corruption (`ferret doctor` reports warnings)
- You've changed the `include`/`exclude` patterns significantly

## Index a Single Connector

```bash
ferret index --connector docs
```

Only re-indexes the `docs` connector instance. Other connectors are unchanged.

## Performance Tuning

Adjust in `ferret.config.json`:

```json
{
  "indexing": {
    "batchSize": 100,
    "parallelism": 8
  }
}
```

| Option | Default | Effect |
|---|---|---|
| `batchSize` | 50 | Documents per SQLite transaction; larger = faster but more memory |
| `parallelism` | 4 | Concurrent parsing workers; tune to your CPU count |

## Index Statistics

```bash
ferret doctor
# Index   OK   keyword-index.db (2.4 MB, 1,231 documents)

ferret search --json "" | jq '.totalDocuments'
```

## Related

- [Connectors](connectors) — configure what gets discovered
- [Parsers](parsers) — how files are converted to documents
- [Watch](watch) — continuous automatic re-indexing
- [Storage Architecture](../architecture/storage) — the SQLite schema
