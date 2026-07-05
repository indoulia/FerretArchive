# Configuration

Ferret uses a three-layer configuration system. Each layer overrides the one before it, so environment variables always win over file configuration.

## Layer Stack

```
┌───────────────────────────────┐  Highest priority
│  Environment Variables        │  FERRET_AI_DEFAULT_CHAT_MODEL=...
├───────────────────────────────┤
│  ferret.json           │  Project-level config
│  (workspace root)             │
├───────────────────────────────┤
│  Compiled Defaults            │  Lowest priority
│  (appsettings defaults)       │
└───────────────────────────────┘
```

## ferret.json

Place `ferret.json` at the workspace root (same level as `.ferret/`):

```json
{
  "workspace": {
    "maxFileSizeBytes": 1048576
  },
  "indexing": {
    "batchSize": 50,
    "parallelism": 4
  },
  "search": {
    "defaultMaxResults": 10,
    "highlightEnabled": true
  },
  "ai": {
    "defaultChatModel": "ollama/llama3.2",
    "defaultEmbeddingModel": "ollama/nomic-embed-text"
  },
  "context": {
    "defaultTokenBudget": 8000
  },
  "providers": {
    "ollama": {
      "baseUrl": "http://localhost:11434"
    },
    "openai": {
      "baseUrl": "https://api.openai.com/v1"
    }
  }
}
```

## Environment Variables

All config values can be overridden via environment variables using double-underscore (`__`) as the section separator:

| Environment Variable | Config Path | Example |
|---|---|---|
| `FERRET_AI__DEFAULTCHATMODEL` | `ai.defaultChatModel` | `ollama/llama3.2` |
| `FERRET_PROVIDERS__OPENAI__APIKEY` | `providers.openai.apiKey` | `sk-...` |
| `FERRET_PROVIDERS__OLLAMA__BASEURL` | `providers.ollama.baseUrl` | `http://localhost:11434` |
| `FERRET_SEARCH__DEFAULTMAXRESULTS` | `search.defaultMaxResults` | `20` |
| `FERRET_INDEXING__BATCHSIZE` | `indexing.batchSize` | `100` |

> **Note:** Environment variables are the recommended way to pass secrets like API keys. Never store API keys in `ferret.json`.

## IConfiguration Binding

Platform services receive configuration via typed options bound from `IConfiguration`:

```csharp
services.Configure<AiOptions>(configuration.GetSection("ai"));
services.Configure<SearchOptions>(configuration.GetSection("search"));
services.Configure<IndexingOptions>(configuration.GetSection("indexing"));
```

`PostConfigure` applies environment variable overrides after the JSON file is read.

## Related

- [Configuration Reference](../reference/configuration) — full field schema
- [Storage](storage) — workspace.json (connector config)
- [CLI Reference](../reference/cli) — `ferret config` command
