# CLI Reference

Every Ferret command, its flags, exit codes, and one example. All commands run from a workspace root unless noted.

## Global Flags

| Flag | Default | Description |
|---|---|---|
| `--version` | — | Print version and exit |
| `--help` | — | Print help for any command |
| `--json` | off | Output as JSON (most commands) |
| `--no-color` | off | Disable ANSI color output |
| `--log-level` | `Information` | Minimum log level: `Trace` `Debug` `Information` `Warning` `Error` `Critical` |

---

## ferret init

Initialise a new workspace in the current directory.

```bash
ferret init [--id <workspace-id>]
```

**Flags:**

| Flag | Default | Description |
|---|---|---|
| `--id` | Directory name | Workspace identifier |

**Creates:** `.ferret/workspace.json`, `.ferret/state.json`

**Exit codes:** `0` success · `1` already initialised · `2` permission error

**Example:**
```bash
cd /path/to/my-project
ferret init --id my-project
```

---

## ferret index

Index the workspace. First run is full; subsequent runs are incremental.

```bash
ferret index [--rebuild] [--verbose] [--connector <id>]
```

**Flags:**

| Flag | Description |
|---|---|
| `--rebuild` | Force full re-index (drops and rebuilds the SQLite index) |
| `--verbose` | Print one line per indexed file (connector, path, duration) |
| `--connector` | Index only the specified connector instance |

**Exit codes:** `0` success · `1` workspace not found · `2` index error

**Example:**
```bash
ferret index
ferret index --rebuild
```

---

## ferret search

Search the indexed workspace.

```bash
ferret search <query> [--top <n>] [--json]
```

**Flags:**

| Flag | Default | Description |
|---|---|---|
| `--top` | `10` | Maximum results to return |
| `--json` | off | Output as JSON array |

**Exit codes:** `0` results found · `0` no results · `1` workspace not found · `2` index not found

**Example:**
```bash
ferret search "IIndexPipeline"
ferret search "context assembly" --top 5 --json
```

---

## ferret serve

Start the MCP server (stdio transport).

```bash
ferret serve
```

The server runs until interrupted (`Ctrl+C`). It reads JSON-RPC from stdin and writes to stdout.

**Exit codes:** `0` clean shutdown · `1` startup error

**Example:**
```bash
ferret serve
# Ferret MCP server running.
# Tools: ferret_search, ferret_read_document, ferret_context, ferret_workspace_status
```

---

## ferret watch

Watch the workspace for file changes and re-index automatically.

```bash
ferret watch [--debounce <ms>]
```

**Flags:**

| Flag | Default | Description |
|---|---|---|
| `--debounce` | `500` | Milliseconds to wait after last change before indexing |

**Exit codes:** `0` clean shutdown · `1` workspace not found

**Example:**
```bash
ferret watch
# Watching: /path/to/my-project
# Change detected: src/Ferret.Search/SearchService.cs
# Re-indexed 1 document (0.3s)
```

---

## ferret doctor

Run health checks on the workspace, index, and providers.

```bash
ferret doctor [--verbose]
```

**Flags:**

| Flag | Description |
|---|---|
| `--verbose` | Show full parser diagnostics: every opaque extension plus each parser's priority and media type |

**Exit codes:** `0` all healthy · `1` one or more checks failed

**Checks performed:**

| Check | What it verifies |
|---|---|
| WorkspaceRoot | `.ferret/workspace.json` exists and is readable |
| FerretConfigDir | `.ferret/` directory present and writable |
| Parser platform | Content parsers are registered; reports the supported-extension count |
| IndexFreshness | Index exists; age relative to most recently changed source file |
| AiProviderConfig | Configured AI provider is reachable; model IDs resolve |

After the checks, `doctor` prints a **Parser Platform** report: the installed parsers,
extension coverage (Text / Parseable Binary / Opaque Binary / Known Extensions), the
parseable and opaque extension lists, and the loaded parser packages. This is the first
place to look when a file is not being indexed. `--verbose` shows every opaque extension
plus each parser's priority and media type.

**Example:**
```bash
ferret doctor
# ... health checks ...
#
# Parser Platform
#
# Installed Parsers (7)
#   ✓ Plain Text Parser
#   ✓ Markdown Parser
#   ✓ JSON Parser
#   ✓ CSV Parser
#   ✓ PDF Parser
#   ✓ Word (DOCX) Parser
#   ✓ Excel (XLSX) Parser
#
# Extension Coverage
#   Text: 76
#   Parseable Binary: 3
#   Opaque Binary: 50
#   Known Extensions: 129
#
# Parseable Binary (3)
#   .docx  .pdf  .xlsx
#
# Parser Packages (3)
#   Ferret.ParserPlatform
#   Ferret.Parsers.Office
#   Ferret.Parsers.Pdf
```

---

## ferret config

Show, set, or validate configuration.

```bash
ferret config get <key>
ferret config set <key> <value>
ferret config list
ferret config validate
```

**Exit codes:** `0` success · `1` key not found · `2` invalid value

**Example:**
```bash
ferret config list
ferret config get ai.defaultChatModel
ferret config set search.defaultMaxResults 20
```

### ferret config validate

Reads `ferret.config.json` and reports any missing required fields or schema violations. Useful in CI pipelines before running `ferret index`.

```bash
ferret config validate
# ferret.config.json OK

ferret config validate
# ERROR: workspace.name is required
# ERROR: workspace.root is required
```

**Exit codes:** `0` valid · `1` validation errors found

---

## ferret manual

Open the Ferret Manual in the default browser.

```bash
ferret manual [--port <n>] [<page>]
```

**Flags:**

| Flag | Default | Description |
|---|---|---|
| `--port` | `4321` | Port to serve the manual on |

**Example:**
```bash
ferret manual
ferret manual getting-started/installation
ferret manual --port 8080
```

---

## ferret models

List and inspect available AI models.

```bash
ferret models list
ferret models info <model-id>
```

**Exit codes:** `0` success · `1` no providers configured

**Example:**
```bash
ferret models list
# Provider        Model                      Capabilities
# ollama          ollama/llama3.2            chat
# ollama          ollama/nomic-embed-text    embedding

ferret models info ollama/llama3.2
```

---

## ferret prompt

List and run registered prompt templates.

```bash
ferret prompt list
ferret prompt run <template-id> [--var <key>=<value>...]
```

**Example:**
```bash
ferret prompt list
ferret prompt run summarise-file --var filename=README.md --var content="$(cat README.md)"
```

## Related

- [Configuration Reference](configuration) — `ferret.config.json` schema
- [MCP Reference](mcp) — MCP tools called by AI assistants
- [Troubleshooting](../troubleshooting) — common errors and fixes
