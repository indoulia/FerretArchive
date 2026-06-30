# ADR-0019: AI Platform Architecture

**Status:** Accepted  
**Sprint:** 12  
**Date:** 2026-06-29

## Context

Sprint 12 introduces a first-class AI capability layer to Ferret. Multiple vendor AI SDKs (OllamaSharp, OpenAI, and future others) must be integrated without leaking vendor types into shared platform code. Provider capabilities (chat, embedding, reranking, vision) must be composable independently. The model registry must be stable after startup — no runtime mutations.

## Decisions

### 1. Ferret owns all AI contracts; vendor SDKs are confined to provider packages

All AI interfaces (`IModelProvider`, `IChatModel`, `IEmbeddingModel`, `IReranker`, `IVisionModel`) and value types (`ModelId`, `ProviderId`, `ModelCapabilities`, `ModelDescriptor`) live in `Ferret.Core.Ai`. This namespace has **zero** external package references. Vendor SDKs (`OllamaSharp`, `OpenAI`) are referenced only from their respective provider packages (`Ferret.Providers.Ollama`, `Ferret.Providers.OpenAi`). No type from `OllamaSharp.*` or `OpenAI.*` namespaces appears outside its provider package.

### 2. `IModelProvider` is the unit of registration; capabilities are independent interfaces

Providers implement `IModelProvider` and vend capability implementations (`IChatModel`, `IEmbeddingModel`) on request. Capability interfaces are independent — a provider may implement chat but not embedding. Consumers depend on `IChatModel` or `IEmbeddingModel`, not on the provider directly. This enables per-model capability composition without inheritance hierarchies.

### 3. `ModelRegistry` is immutable after startup

`ModelRegistry` is built from `IEnumerable<IModelProvider>` at DI construction time. After startup, no provider or model can be added or removed. This matches the immutability pattern established for `IMcpToolRegistry` (ADR-0017). Runtime correctness guarantees derive from startup-time invariants, not from concurrent mutation guards.

### 4. Model routing is configuration-driven (`AiOptions.DefaultChatModel`)

`ModelRouter` reads `AiOptions.DefaultChatModel` and `AiOptions.DefaultEmbeddingModel` at construction. Resolution delegates to `IModelRegistry`. This keeps routing logic out of business code — callers ask for "the default chat model" and the router resolves provider + capability from configuration. Per-call overrides are possible via `ModelId` parameters on request types.

### 5. Sprint 12 version gate: no LLM calls at runtime

Sprint 12 wires the platform and exposes it via CLI (`ferret models list`, `ferret models info`). No prompt is sent to any model during `dotnet test` or normal `ferret models` usage. The version gate is: zero LLM API calls during Sprint 12. Architecture tests enforce that provider packages are correctly isolated.

## Consequences

- All future AI features in Sprints 13+ build on `Ferret.Core.Ai` contracts, never on vendor SDKs directly.
- Adding a new provider (Anthropic, Cohere, etc.) requires only a new provider package; no changes to `Ferret.Core.Ai`, `Ferret.Models`, or `Ferret.Prompts`.
- Architecture tests in `Ferret.Architecture.Tests` enforce the SDK isolation boundary continuously.
- Null memory implementations (`NullConversationMemory`, `NullWorkspaceMemory`, `NullTaskMemory`) are the defaults until Sprint 15 provides real implementations.
