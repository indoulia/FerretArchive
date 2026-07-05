# Watch

`ferret watch` monitors your workspace for file changes and automatically re-indexes modified files. Use it during active development to keep the index current without manual `ferret index` runs.

## Start watching

```bash
ferret watch
```

```
Ferret Watch started.
Watching: /path/to/my-project
Debounce: 500ms
Press Ctrl+C to stop.
```

## What happens on a change

When a file changes:

1. Watcher detects the filesystem event (create/modify/delete)
2. Debounce timer starts (default: 500ms)
3. If more changes arrive within the debounce window, the timer resets
4. After the debounce window expires, the affected files are re-indexed
5. Status line updates

```
[10:42:31] Change detected: src/Ferret.Search/SearchService.cs (modified)
[10:42:32] Re-indexed 1 document (0.3s)
```

## Debounce

The debounce prevents thrashing during bulk saves (e.g. `git checkout`, IDE reformatting). Default is 500ms. Increase for slow disks or noisy editors:

```bash
ferret watch --debounce 1000
```

Or configure permanently in `ferret.json`:

```json
{
  "watch": {
    "debounceMs": 1000
  }
}
```

## Using watch with Claude

Start `ferret watch` and `ferret serve` together so Claude always has a current index:

```bash
# Terminal 1
ferret serve

# Terminal 2
ferret watch
```

As you edit files, the watch process updates the index, and Claude's next `ferret_search` call picks up the changes immediately.

## File deletion handling

When a file is deleted:

1. Watch event detected
2. Document removed from the SQLite index
3. Subsequent searches no longer return the deleted file

> **Note:** `ferret watch` requires the workspace to be fully indexed before starting. Run `ferret index` first if this is a new workspace.

## Related

- [Indexing](indexing) — full and incremental indexing
- [CLI Reference](../reference/cli) — `ferret watch` flags
- [Connect Claude](../getting-started/connect-claude) — combining watch with MCP
