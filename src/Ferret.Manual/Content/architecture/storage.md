# Storage

Ferret stores all workspace state in the `.ferret/` directory at the workspace root. No database server, no global state — everything is local to the workspace.

## Directory Layout

```
.ferret/
├── workspace.json          workspace configuration (user-editable)
├── state.json              index state (auto-managed, do not edit)
└── indexes/
    └── keyword/
        └── keyword-index.db    SQLite FTS5 index
```

## workspace.json

The user-editable configuration file. Created by `ferret init`:

```json
{
  "workspaceId": "my-project",
  "schemaVersion": 1,
  "connectors": [
    {
      "type": "filesystem",
      "instanceId": "default",
      "root": ".",
      "include": ["**/*.cs", "**/*.md", "**/*.json"],
      "exclude": ["**/bin/**", "**/obj/**"]
    }
  ]
}
```

## state.json

Auto-managed by `ferret index`. Records the last indexed state for incremental indexing:

```json
{
  "schemaVersion": 1,
  "lastIndexedAt": "2026-06-29T10:00:00Z",
  "documentCount": 1231,
  "connectorStates": {
    "filesystem:default": {
      "lastSyncAt": "2026-06-29T10:00:00Z",
      "assetCount": 1247
    }
  }
}
```

## SQLite Schema

The keyword index (`keyword-index.db`) has two tables:

### documents

Stores document metadata and raw content:

```sql
CREATE TABLE documents (
    id          TEXT PRIMARY KEY,  -- DocumentId (stable hash of CanonicalUri)
    uri         TEXT NOT NULL,     -- CanonicalUri (filesystem:///src/...)
    display     TEXT NOT NULL,     -- Human-friendly display path
    content     TEXT NOT NULL,     -- Raw text content
    indexed_at  TEXT NOT NULL      -- ISO-8601 timestamp
);
```

### documents_fts (FTS5 virtual table)

Full-text search index backed by FTS5:

```sql
CREATE VIRTUAL TABLE documents_fts USING fts5(
    content,
    display,
    content='documents',
    content_rowid='rowid',
    tokenize='porter ascii'
);
```

Queries use BM25 ranking:

```sql
SELECT d.id, d.uri, d.display,
       snippet(documents_fts, 0, '<b>', '</b>', '...', 10) AS snippet,
       bm25(documents_fts) AS score
FROM documents_fts
JOIN documents d ON d.rowid = documents_fts.rowid
WHERE documents_fts MATCH ?
ORDER BY score
LIMIT ?
```

## Related

- [Why SQLite?](../design/why-sqlite) — why SQLite was chosen
- [Search Flow](search-flow) — how queries hit the FTS5 index
- [Configuration Reference](../reference/configuration) — workspace.json full schema
