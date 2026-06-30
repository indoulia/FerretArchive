# AI Flow

Ferret's AI capability layer routes completion requests through a model registry and provider abstraction. No caller ever references a vendor SDK — all AI calls go through Ferret-owned contracts.

## Flow Diagram

```
ferret prompt run <template>
        │
        ▼
┌───────────────────┐
│  PromptCommand    │  Ferret.Cli
│  or AI service    │
└────────┬──────────┘
         │  PromptTemplate + PromptVariables
         ▼
┌───────────────────┐
│  IPromptRenderer  │  Ferret.Prompts
│                   │  Substitutes {{variable}} placeholders
└────────┬──────────┘
         │  rendered prompt string
         ▼
┌───────────────────┐
│  IModelRouter     │  Ferret.Models
│                   │  Reads AiOptions.DefaultChatModel
│                   │  Resolves provider + model from IModelRegistry
└────────┬──────────┘
         │  IChatModel (resolved capability)
         ▼
┌───────────────────┐
│  IChatModel       │  Ferret.Core.Ai (contract)
│  .CompleteAsync() │  Implemented by provider package
└────────┬──────────┘
         │  vendor SDK call
         ▼
┌───────────────────┐
│  Provider         │  Ferret.Providers.Ollama
│  Implementation   │  or Ferret.Providers.OpenAi
│                   │  Translates to vendor HTTP API
└────────┬──────────┘
         │  CompletionResponse
         ▼
     caller / CLI
```

## Key Contracts

```csharp
// Ferret.Core.Ai — never import vendor SDKs from here
public interface IChatModel
{
    ModelDescriptor Descriptor { get; }
    Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken ct = default);
}

public interface IModelProvider
{
    ProviderId ProviderId { get; }
    IReadOnlyList<ModelDescriptor> Models { get; }
    IChatModel? GetChatModel(ModelId modelId);
    IEmbeddingModel? GetEmbeddingModel(ModelId modelId);
}
```

## Model Registry

`ModelRegistry` is built once at startup from all registered `IModelProvider` instances. After startup it is immutable (ADR-0019). View available models:

```bash
ferret models list
ferret models info ollama/llama3.2
```

## Provider Selection

Configure the default chat model in `ferret.config.json`:

```json
{
  "ai": {
    "defaultChatModel": "ollama/llama3.2",
    "defaultEmbeddingModel": "ollama/nomic-embed-text"
  }
}
```

## Related

- [Why Providers?](../design/why-providers) — the design rationale
- [Configuration Reference](../reference/configuration) — AI config options
- [Developer Guide: Create AI Provider](../developer-guide/create-ai-provider) — add a new provider
