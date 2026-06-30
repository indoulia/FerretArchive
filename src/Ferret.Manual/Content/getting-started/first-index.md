# First Index

Run `ferret index` from your workspace root:

```bash
ferret index
```

## What happens

1. **Discover** — connector walks the directory tree
2. **Filter** — `.ferretignore` and `exclude` patterns applied
3. **Parse** — each file converted to searchable text
4. **Index** — content written to the SQLite FTS5 index

## Sample output

```
Indexing workspace: my-project
  Connectors: filesystem (default)
  Discovered:  1,247 assets
  Indexed:     1,231 documents
  Skipped:        16
  Failures:        0
  Duration:     4.2s
Index complete.
```

## Incremental re-index

After the first full index, subsequent runs only re-index changed files:

```bash
ferret index
# Discovered: 1,247  Indexed: 3  Skipped: 1,244  Duration: 0.3s
```

## Force full rebuild

```bash
ferret index --rebuild
```

## Related

- [First Search](first-search) — search the indexed workspace
- [Indexing](../user-guide/indexing) — incremental indexing, watching
