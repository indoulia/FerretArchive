# Troubleshooting

Common errors and how to fix them. Run `ferret doctor` first — it diagnoses most issues automatically.

---

## 1. `ferret` command not found

**Symptom:** `ferret: command not found` after installation.

**`ferret doctor` output:** N/A — cannot run doctor.

**Root cause:** The directory containing `ferret` (or `ferret.exe`) is not on your PATH.

**Fix:**
```bash
# Windows: add the directory to PATH permanently
$env:PATH += ";C:\tools"
# Then restart your terminal

# macOS/Linux: verify PATH
echo $PATH
which ferret
```

---

## 2. Workspace not found

**Symptom:** `Error: No Ferret workspace found in /path/to/dir or any parent directory.`

**`ferret doctor` output:** `Workspace: NOT FOUND`

**Root cause:** You ran a Ferret command outside an initialised workspace.

**Fix:**
```bash
cd /path/to/your-project
ferret init
```

---

## 3. Index not found — search returns no results

**Symptom:** `ferret search` returns `No results found` even for known identifiers.

**`ferret doctor` output:** `Index: NOT FOUND — run ferret index`

**Root cause:** The workspace has not been indexed yet.

**Fix:**
```bash
ferret index
```

---

## 4. Index out of date

**Symptom:** Search results don't reflect recent code changes.

**`ferret doctor` output:** `Index: STALE — last indexed 3 days ago`

**Root cause:** Files have changed since the last index run.

**Fix:**
```bash
ferret index          # incremental re-index
ferret index --rebuild  # full rebuild if incremental doesn't help
```

---

## 5. MCP server not appearing in Claude Desktop

**Symptom:** Claude Desktop shows no Ferret tools.

**`ferret doctor` output:** N/A.

**Root cause:** MCP configuration in `claude_desktop_config.json` is missing or incorrect.

**Fix:**
1. Verify `ferret serve` runs without errors from your project directory
2. Check the config path:
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
3. Ensure `cwd` points to an absolute path
4. Restart Claude Desktop completely

```json
{
  "mcpServers": {
    "ferret": {
      "command": "ferret",
      "args": ["serve"],
      "cwd": "/absolute/path/to/your/project"
    }
  }
}
```

---

## 6. Ollama provider: model not found

**Symptom:** `ferret models list` shows the Ollama provider but no models, or `ferret doctor` reports provider unhealthy.

**`ferret doctor` output:** `Ollama provider: UNHEALTHY — cannot reach http://localhost:11434`

**Root cause:** Ollama is not running, or the model has not been pulled.

**Fix:**
```bash
# Start Ollama (if not running)
ollama serve

# Pull the model
ollama pull llama3.2

# Verify
ferret models list
```

---

## 7. OpenAI provider: authentication error

**Symptom:** `ferret prompt run` fails with `401 Unauthorized`.

**Root cause:** API key is missing or incorrect.

**Fix:**
```bash
# Set the API key as an environment variable (never in ferret.config.json)
export FERRET_PROVIDERS__OPENAI__APIKEY="sk-..."
ferret doctor
```

---

## 8. Index is corrupt

**Symptom:** `ferret search` crashes or returns garbled results.

**`ferret doctor` output:** `Index: CORRUPT — SQLite integrity check failed`

**Root cause:** Interrupted indexing, disk error, or disk full.

**Fix:**
```bash
ferret index --rebuild
```

---

## 9. ferret watch misses changes

**Symptom:** File changes don't trigger re-indexing during `ferret watch`.

**Root cause:** Filesystem event polling not supported on the current OS/filesystem (e.g. network drives, WSL).

**Fix:**
```bash
# Increase debounce and use polling mode
ferret watch --debounce 2000

# Or run manual incremental index periodically
ferret index
```

---

## 10. Manual not loading in browser

**Symptom:** `ferret manual` command succeeds but browser shows a blank page or 404.

**Root cause:** Port in use, or browser cache issue.

**Fix:**
```bash
# Try a different port
ferret manual --port 8080

# Check what's using port 4321
netstat -ano | findstr 4321   # Windows
lsof -i :4321                 # macOS/Linux
```

## 11. My file isn't indexing

**Symptom:** A file is present in the workspace but never appears in search results.

**Root cause:** The extension is unmapped, the file type is treated as an opaque binary, the parser package isn't installed, or the path is excluded.

**Fix:** Run `ferret doctor` and read the **Parser Platform** report, then follow the tree:

```
Is the extension listed by `ferret doctor`?
  ├─ No  → unsupported extension (not mapped) — the file is skipped
  └─ Yes → Which category?
           ├─ Parseable Binary → is the parser installed? (doctor lists it)
           │     ├─ Yes → re-run `ferret index`
           │     └─ No  → install/enable the parser package
           ├─ Text            → check .ferretignore and your workspace scope
           └─ Opaque Binary   → currently treated as opaque; not indexed
```

Run `ferret doctor --verbose` to see the full opaque-extension list and per-parser details.

## Related

- [CLI Reference](../reference/cli) — `ferret doctor` flags
- [Getting Started](../getting-started/index) — installation and first workspace
- [FAQ](../faq) — common questions
