# AI

Ferret's AI capability layer connects your workspace to language models for completions and embeddings. RC1 ships with Ollama and OpenAI providers.

## List Available Models

```bash
ferret models list
```

```
Provider    Model                       Capabilities
ollama      ollama/llama3.2             chat
ollama      ollama/nomic-embed-text     embedding
openai      openai/gpt-4o               chat
openai      openai/text-embedding-3-small  embedding
```

## Inspect a Model

```bash
ferret models info ollama/llama3.2
```

```
Model:       ollama/llama3.2
Provider:    ollama
Capability:  chat
Base URL:    http://localhost:11434
Status:      reachable
```

## Configure the Default Model

Set the default chat model in `ferret.json`:

```json
{
  "ai": {
    "defaultChatModel": "ollama/llama3.2",
    "defaultEmbeddingModel": "ollama/nomic-embed-text"
  }
}
```

Or via environment variable:
```bash
FERRET_AI__DEFAULTCHATMODEL=openai/gpt-4o ferret prompt run summarise-file ...
```

## Ollama Provider

Ollama runs models locally — no API key, no network calls, no cost.

1. Install Ollama: https://ollama.ai
2. Pull a model: `ollama pull llama3.2`
3. Configure Ferret:

```json
{
  "ai": { "defaultChatModel": "ollama/llama3.2" },
  "providers": {
    "ollama": { "baseUrl": "http://localhost:11434" }
  }
}
```

## OpenAI Provider

Requires an OpenAI API key. Store the key as an environment variable:

```bash
# Windows
$env:FERRET_PROVIDERS__OPENAI__APIKEY = "sk-..."

# macOS/Linux
export FERRET_PROVIDERS__OPENAI__APIKEY="sk-..."
```

Configure the model:
```json
{
  "ai": { "defaultChatModel": "openai/gpt-4o" }
}
```

## Run a Prompt

```bash
ferret prompt list                         # list registered templates
ferret prompt run summarise-file \
  --var filename=README.md \
  --var content="$(cat README.md)"
```

## Related

- [AI Flow Architecture](../architecture/ai-flow) — how the provider chain works
- [Developer Guide: Create an AI Provider](../developer-guide/create-ai-provider) — add a new provider
- [Configuration Reference](../reference/configuration) — AI config options
