# Why Providers?

Ferret abstracts AI model access behind `IModelProvider`. You configure a provider in `ferret.json`; the rest of the system never knows which one is running.

## The problem providers solve

AI model APIs are not stable. OpenAI changes pricing. Ollama changes its HTTP API. New providers appear. Users have different needs: some want local-only (Ollama), some want cloud (OpenAI), some will want Anthropic directly.

If we hardcoded OpenAI calls throughout the codebase, every model change would require code changes. If we hardcoded Ollama, enterprise users couldn't use their existing API access.

The provider abstraction means the switching cost is one config file change.

## The design

`IModelProvider` vends typed model handles: `GetChatModel(ModelId) → IChatModel?`, `GetEmbeddingModel(ModelId) → IEmbeddingModel?`, and `GetReranker(ModelId) → IReranker?`. `IChatModel` has two methods: `ChatAsync(ChatRequest, CancellationToken) → Task<ChatResponse>` and `ChatStreamAsync(ChatRequest, CancellationToken) → IAsyncEnumerable<ChatResponseChunk>`. Every provider implements this contract. `IModelRouter` selects the right provider and model for each request based on the configuration.

`Ferret.Providers.Ollama` implements `IModelProvider` using Ollama's HTTP API.
`Ferret.Providers.OpenAI` implements `IModelProvider` using the OpenAI SDK.

Adding a new provider is a new package, a new implementation, and a DI registration. Nothing else changes.

## What we decided against

- **Separate provider per use case**: routing by task type (summarisation vs. classification vs. embedding) is desirable but adds complexity. Sprint 12 ships one provider per workspace, which covers RC1 needs.
- **Auto-discovery**: dynamically loading provider DLLs would add startup complexity. Explicit registration is simpler and safer.

## Related

- [AI Flow Architecture](../architecture/ai-flow) — the provider chain
- [Configuration Reference](../reference/configuration) — provider config
