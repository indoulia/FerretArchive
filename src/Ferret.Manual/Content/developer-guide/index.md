# Developer Guide

Ferret is designed to be extended. All four primary extension points — connectors, parsers, AI providers, and prompt templates — are plain C# interfaces registered via dependency injection. No code generation, no base class inheritance, no reflection magic.

## Extension Points

- [Create a Connector](create-connector) — index content from a new source (GitHub, Confluence, SharePoint)
- [Create a Parser](create-parser) — extract text from a new file format (PDF, DOCX, CSV)
- [Create an AI Provider](create-ai-provider) — integrate with a new AI vendor (Anthropic, Cohere, Azure OpenAI)
- [Create a Prompt](create-prompt) — register a reusable prompt template

## Prerequisites

- .NET 9 SDK
- A Ferret workspace for testing
- Familiarity with dependency injection (`Microsoft.Extensions.DependencyInjection`)

## General Pattern

Every extension follows the same three steps:

1. **Implement the interface** — `IConnector`, `IContentParser`, `IModelProvider`, or `PromptTemplate`
2. **Register in DI** — `services.AddSingleton<IInterface, Implementation>()`
3. **Package as a project** — reference `Ferret.Core`; never reference `Ferret.Cli`

## Architecture Rules

- Your extension package must reference only `Ferret.Core` (and optionally vendor SDKs for provider packages)
- Never reference `Ferret.Cli`, `Ferret.Mcp`, or `Ferret.Manual` from an extension
- All public types in your extension should be `sealed` (enforced by architecture tests)
- Streaming pipelines use `IAsyncEnumerable<T>` — no `List<T>` at pipeline boundaries

## Related

- [Extension Points](../architecture/extension-points) — architecture diagrams for each interface
- [Dependency Graph](../architecture/dependency-graph) — package reference rules
- [Architecture Reference](../reference/architecture) — ADR index
