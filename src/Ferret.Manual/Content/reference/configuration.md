# Configuration Reference

Full schema for `ferret.json` and all supported environment variable overrides.

## ferret.json

Place this file at the workspace root (next to `.ferret/`). All fields are optional; Ferret uses compiled defaults if not specified.

### workspace section

| Field | Type | Default | Description |
|---|---|---|---|
| `workspace.maxFileSizeBytes` | integer | `1048576` (1 MB) | Files larger than this are skipped during indexing |
| `workspace.encoding` | string | `"utf-8"` | Default file encoding for text parsing |

### indexing section

| Field | Type | Default | Description |
|---|---|---|---|
| `indexing.batchSize` | integer | `50` | Documents per SQLite transaction |
| `indexing.parallelism` | integer | `4` | Concurrent parsing workers |
| `indexing.enableIncrementalIndex` | boolean | `true` | Skip unchanged files on re-index |

### search section

| Field | Type | Default | Description |
|---|---|---|---|
| `search.defaultMaxResults` | integer | `10` | Default result limit for `ferret search` |
| `search.highlightEnabled` | boolean | `true` | Include snippet highlights in results |
| `search.minScore` | float | `0.0` | Minimum normalised BM25 score to include in results |

### ai section

| Field | Type | Default | Description |
|---|---|---|---|
| `ai.defaultChatModel` | string | `""` | Model ID for chat completions (e.g. `"ollama/llama3.2"`) |
| `ai.defaultEmbeddingModel` | string | `""` | Model ID for embeddings (e.g. `"ollama/nomic-embed-text"`) |

### context section

| Field | Type | Default | Description |
|---|---|---|---|
| `context.defaultTokenBudget` | integer | `8000` | Maximum tokens in assembled context |
| `context.expansionEnabled` | boolean | `true` | Include caller/callee expansion |
| `context.filterGeneratedFiles` | boolean | `true` | Exclude `*.generated.cs` and similar |

### providers.ollama section

| Field | Type | Default | Description |
|---|---|---|---|
| `providers.ollama.baseUrl` | string | `"http://localhost:11434"` | Ollama server base URL |
| `providers.ollama.timeoutSeconds` | integer | `60` | HTTP request timeout |

### providers.openai section

| Field | Type | Default | Description |
|---|---|---|---|
| `providers.openai.baseUrl` | string | `"https://api.openai.com/v1"` | API base URL (override for Azure OpenAI) |
| `providers.openai.apiKey` | string | `""` | API key — prefer `FERRET_PROVIDERS__OPENAI__APIKEY` env var |
| `providers.openai.timeoutSeconds` | integer | `30` | HTTP request timeout |

## Environment Variables

All config values can be overridden. Use double-underscore (`__`) as section separator, prefixed with `FERRET_`:

### Shorthand variables

These single-purpose variables are easier to use in CI and Docker than the full `FERRET__` form:

| Shorthand Variable | Effect |
|---|---|
| `FERRET_AI_PROVIDER` | Sets the prefix for `ai.defaultChatModel` and `ai.defaultEmbeddingModel` (e.g. `ollama` rewrites models to `ollama/<model>`) |
| `FERRET_OPENAI_API_KEY` | Overrides `providers.openai.apiKey` |
| `FERRET_OLLAMA_BASE_URL` | Overrides `providers.ollama.baseUrl` |

### Full config path variables

| Environment Variable | Config Equivalent |
|---|---|
| `FERRET_AI__DEFAULTCHATMODEL` | `ai.defaultChatModel` |
| `FERRET_AI__DEFAULTEMBEDDINGMODEL` | `ai.defaultEmbeddingModel` |
| `FERRET_PROVIDERS__OPENAI__APIKEY` | `providers.openai.apiKey` |
| `FERRET_PROVIDERS__OPENAI__BASEURL` | `providers.openai.baseUrl` |
| `FERRET_PROVIDERS__OLLAMA__BASEURL` | `providers.ollama.baseUrl` |
| `FERRET_SEARCH__DEFAULTMAXRESULTS` | `search.defaultMaxResults` |
| `FERRET_INDEXING__BATCHSIZE` | `indexing.batchSize` |
| `FERRET_CONTEXT__DEFAULTTOKENBUDGET` | `context.defaultTokenBudget` |

> **Priority:** shorthand variables are applied after `ferret.json` and after full `FERRET__` variables, so they always win. Use them for secrets and CI overrides.

## Example ferret.json

```json
{
  "indexing": {
    "batchSize": 100,
    "parallelism": 8
  },
  "search": {
    "defaultMaxResults": 15
  },
  "ai": {
    "defaultChatModel": "ollama/llama3.2"
  },
  "context": {
    "defaultTokenBudget": 12000
  },
  "providers": {
    "ollama": {
      "baseUrl": "http://localhost:11434"
    }
  }
}
```

## Related

- [Configuration Architecture](../architecture/configuration) — how layering works
- [CLI Reference](cli) — `ferret config` command
- [Workspace](../user-guide/workspace) — workspace.json connector configuration
