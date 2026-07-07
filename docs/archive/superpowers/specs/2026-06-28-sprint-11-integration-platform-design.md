# Sprint 11 Design Specification: Host Platform (MCP Runtime v1)

**Project:** Ferret (ContextOS)
**Date:** 2026-06-28
**Status:** Authoritative
**Sprint tag:** `v0.11.0-sprint11`

---

## Executive Summary

Sprint 11 introduces Ferret's **Integration Platform** through the first MCP Runtime. The MCP Runtime exposes existing Ferret platform capabilities — Search, Document Retrieval, and Workspace Status — to any MCP-compatible AI host (Claude Code, Claude Desktop, Cursor) via stdio transport.

The sprint's deeper contribution is architectural: it establishes the **Ferret Host Architecture Pattern**, a reusable template for exposing platform capabilities through any presentation or integration host. Every future host — REST, Web UI, Agent Runtime, Desktop — follows the same pattern. MCP is the first implementation of it.

Sprint 11 contains no new business logic. Every tool delegates to an existing platform service. The MCP layer is a thin integration adapter.

---

## Architectural Outcomes

1. **Established the Host Architecture Pattern** — Capabilities → Platform Services → Hosts → Protocols
2. **Introduced the Integration Platform** as a first-class architectural concept in Ferret
3. **Defined Transport as Adapter and SDK Isolation** as core architectural principles
4. **Delivered MCP Runtime v1** as the first integration host alongside the existing CLI host
5. **Reserved clear evolutionary paths** for `Ferret.Application`, additional transports, and future hosts — without introducing premature abstractions

---

## Section 1: Sprint Identity

### 1.1 Sprint Name and Tag

**Name:** Sprint 11 – Host Platform (MCP Runtime v1)
**Tag:** `v0.11.0-sprint11`

### 1.2 Theme

> Expose Ferret's platform capabilities through the first Integration Adapter (MCP Runtime).

MCP is a transport, not a product. The real deliverable is the reusable Host Architecture.

### 1.3 Sprint Goal

> Deliver the first Integration Platform by introducing an MCP Runtime (stdio transport) that exposes existing Ferret platform capabilities to AI hosts without duplicating business logic.

### 1.4 User Story

A developer adds Ferret to any MCP-compatible AI host (Claude Code, Claude Desktop, Cursor, VS Code, and future clients), points it at an indexed workspace, and asks questions. The AI host uses `search` to find relevant content, `read_document` to retrieve full text, and `workspace_status` to understand the workspace state — all through the standard MCP protocol without Ferret-specific integration code.

### 1.5 What a New User Can Do After Sprint 11

Run `ferret serve`. Any MCP-compatible AI host can connect and execute a complete RAG workflow: search the indexed workspace, retrieve full document content, and inspect workspace health — all without leaving the AI host's interface.

### 1.6 Non-Goals

Sprint 11 explicitly does not deliver:

- HTTP transport, SSE, WebSocket, Named Pipes
- REST API or ASP.NET Core hosting
- Authentication or Authorization
- MCP Prompt support (registry reserved; no implementation)
- Agent execution, conversation memory, LLM integration
- Semantic search or knowledge graph queries
- `Ferret.Application` layer (reserved via ADR-0018)
- Multi-workspace MCP runtime
- Dynamic tool registration at runtime

**Version Gate Rule:** Sprint 11 must not introduce compensating implementations for missing Sprint 10 functionality. Any gap is a Sprint 10 reconciliation item, not a Sprint 11 workaround.

---

## Section 2: Architecture

### 2.1 Position in the Platform

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Presentation / Integration Hosts                   │
│   Ferret.Cli         Ferret.Mcp       Future REST    Future Web UI  │
│   (Sprint 6+)        (Sprint 11)                                    │
└──────────────┬──────────────────────────────────────────────────────┘
               │ depends on (downward only)
┌──────────────▼──────────────────────────────────────────────────────┐
│                       Platform Services                             │
│  ISearchService  IDocumentService  WorkspaceEngine                  │
│  IConnectorRegistry  IIndexEngine                                   │
└──────────────┬──────────────────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────────────────┐
│                     Platform Libraries                              │
│  Ferret.Search  Ferret.Workspace  Ferret.ConnectorPlatform          │
│  Ferret.IndexEngine  Ferret.ParserPlatform                          │
└──────────────┬──────────────────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────────────────┐
│                         Ferret.Core                                 │
│                    (M1 frozen — ADR-0012)                           │
└─────────────────────────────────────────────────────────────────────┘
```

`Ferret.Mcp` sits at the same layer as `Ferret.Cli`. Both depend downward on platform services. Neither owns business logic.

### 2.2 Dependency Rules

**Allowed:**
```
Ferret.Mcp → Platform Services → Platform Libraries → Ferret.Core
```

**Forbidden:**
```
Platform Libraries → Ferret.Mcp      (platform must not know MCP exists)
Ferret.Search → Ferret.Mcp           (capabilities are host-independent)
```

These rules should become executable architecture tests in `Ferret.Architecture.Tests`.

### 2.3 Internal Architecture — `Ferret.Mcp` Project Structure

Single project. Organized by responsibility. The Microsoft SDK is an implementation detail of `Transport/Stdio/` only.

```
Ferret.Mcp/
  Runtime/
    IMcpRuntime.cs
    McpRuntime.cs

  Protocol/
    IMcpTool.cs
    IMcpResource.cs
    McpToolDescriptor.cs
    McpResourceDescriptor.cs
    McpArguments.cs
    McpToolResult.cs
    McpContent.cs
    McpResourceContent.cs

  Registry/
    IMcpToolRegistry.cs
    IMcpResourceRegistry.cs
    IMcpPromptRegistry.cs          ← reserved; no implementation Sprint 11
    ToolRegistryBuilder.cs         ← internal composition helper
    ResourceRegistryBuilder.cs     ← internal composition helper

  Tools/
    SearchTool.cs
    ReadDocumentTool.cs
    WorkspaceStatusTool.cs

  Resources/
    WorkspaceStatusResource.cs
    IndexStatsResource.cs
    ConnectorsResource.cs

  Transport/
    IMcpTransport.cs
    McpTransportDescriptor.cs
    IMcpTransportFactory.cs        ← reserved; not implemented Sprint 11
    Stdio/
      StdioTransport.cs
      SdkRuntimeAdapter.cs
      SdkToolAdapter.cs
      SdkResourceAdapter.cs
      McpArgumentsFactory.cs

  Hosting/
    McpModule.cs

  Extensions/
    ServiceCollectionExtensions.cs
```

**The invariant:** `using ModelContextProtocol` appears only inside `Transport/Stdio/`.

### 2.4 Architectural Principles

**Principle 1 — Integration Adapters contain no business logic.**
Their responsibility is protocol translation only.

**Principle 2 — Platform Services are transport-agnostic.**
`ISearchService` never knows whether the caller is CLI, MCP, REST, or an Agent.

**Principle 3 — Transport is pluggable.**
Current: `StdioTransport`. Future: `HttpTransport`, `WebSocketTransport`. No platform changes required.

**Principle 4 — External SDKs are implementation details.**
Only `Transport/Stdio/` references `ModelContextProtocol`. Everything else depends on Ferret abstractions.

**Principle 5 — Tools compose platform capabilities; they never reimplement them.**
Mirrors the pattern established for Connectors, Parsers, and Search Providers.

**Principle 6 — Capabilities are host-independent.**
Every platform capability is usable from multiple hosts without modification.

**Principle 7 — Protocol Translation is One-to-One.**
One incoming message → one platform call → one response. No orchestration, batching, caching, or retries in adapters.

**Principle 8 — Hosts are Launchers, Not Owners.**
Presentation and integration hosts launch runtimes and expose capabilities. They do not own capabilities. Capabilities belong to the platform.

**Principle 9 — Platform First.**
New capabilities are introduced into the platform before they are exposed through any host. Hosts never invent capabilities; they expose existing platform capabilities. If a host appears to need capability that doesn't exist, that capability belongs in the platform first.

### 2.5 Ferret Host Architecture Pattern

```
Capabilities
    │  implement domain behavior (search, indexing, connectors, context)
    ▼
Platform Services
    │  orchestrate reusable use cases over capabilities
    ▼
Hosts
    │  (CLI, MCP, REST, Web, Agent) expose services without owning business logic
    ▼
Protocols
    │  (stdio, HTTP, JSON-RPC, REST, WebSocket) provide transport and serialization
```

This pattern applies to every current and future external interface in Ferret. Adding a new host means writing an adapter — not touching platform code.

### 2.6 Reserved Evolution — `Ferret.Application`

A dedicated Application Layer is intentionally deferred. Presentation and integration hosts delegate directly to platform services until orchestration becomes a **reusable platform concern** rather than a host-specific concern — for example, when multiple hosts require a shared workflow such as Search → Knowledge → Context → Prompt → Model.

`Ferret.Application` is a reserved namespace, not a premature abstraction. See ADR-0018.

*Platform Services may evolve into a distinct orchestration layer over platform capabilities as additional reusable workflows emerge. The current Platform Services layer contains both orchestration services (`ISearchService`, `IDocumentService`) and infrastructure-facing components (`WorkspaceEngine`, `IConnectorRegistry`, `IIndexEngine`). Future sprints may formally separate these into explicit sub-layers without changing the Host Architecture Pattern above them.*

---

## Section 3: Protocol Contracts

All types in this section are Ferret-owned. No SDK types appear here or anywhere outside `Transport/Stdio/`.

### 3.1 Tool Contract

```csharp
public interface IMcpTool
{
    McpToolDescriptor Descriptor { get; }
    Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken cancellationToken);
}

public sealed record McpToolDescriptor(
    string Name,
    string Description,
    string? Category = null,
    string? Version = null);
    // InputSchema, Examples reserved for future extension without interface change
```

### 3.2 Resource Contract

```csharp
public interface IMcpResource
{
    McpResourceDescriptor Descriptor { get; }
    Task<McpResourceContent> ReadAsync(CancellationToken cancellationToken);
}

public sealed record McpResourceDescriptor(
    string ResourceUri,      // follows <domain>://<resource>/<subresource> convention
    string Name,
    string Description,
    string MimeType = "text/plain");
```

Resources are read-only projections. No parameters, no side effects.

### 3.3 Transport Contract

```csharp
public interface IMcpTransport
{
    McpTransportDescriptor Descriptor { get; }
    Task RunAsync(CancellationToken cancellationToken);
}

public sealed record McpTransportDescriptor(
    string Name,
    string TransportType);   // e.g. "stdio", "http"
```

### 3.4 Runtime Contract

```csharp
public interface IMcpRuntime
{
    Task RunAsync(CancellationToken cancellationToken);
    // Task StopAsync(...) reserved for future graceful shutdown API
}
```

### 3.5 Content and Result Model

```csharp
public sealed record McpContent(string MimeType, string Content);

public sealed record McpToolResult(
    IReadOnlyList<McpContent> Content,
    bool IsError = false)
{
    public static McpToolResult Text(string content) =>
        new([new McpContent("text/plain", content)]);

    public static McpToolResult Error(string message) =>
        new([new McpContent("text/plain", message)], IsError: true);
}

public sealed record McpResourceContent(
    string ResourceUri,
    string Content,
    string MimeType = "text/plain");
```

### 3.6 Argument Model

```csharp
public sealed class McpArguments
{
    // Constructed by McpArgumentsFactory inside Transport/Stdio/ — constructor is internal
    public bool TryGetString(string name, out string? value);
    public bool TryGetInt32(string name, out int value);
    public bool TryGetBoolean(string name, out bool value);
}
```

`JsonElement` and all JSON-RPC types are confined to `McpArgumentsFactory`. Tools never see raw JSON.

---

## Section 4: Registry, Tools, and Resources

### 4.1 Registry Contracts

```csharp
public interface IMcpToolRegistry
{
    IReadOnlyList<IMcpTool> GetAll();
    IMcpTool? Get(string name);
}

public interface IMcpResourceRegistry
{
    IReadOnlyList<IMcpResource> GetAll();
    IMcpResource? Get(string resourceUri);
}

// Reserved — no implementation in Sprint 11
public interface IMcpPromptRegistry { }
```

Registries are **immutable after composition**. `ToolRegistryBuilder` and `ResourceRegistryBuilder` (internal) build them during `McpModule` composition. The runtime never mutates a registry.

### 4.2 Sprint 11 Tools

| Tool | MCP Name | Delegates To | Parameters |
|---|---|---|---|
| `SearchTool` | `search` | `ISearchService` | `query` (string, required) · `limit` (int, optional) · `passages` (bool, optional) |
| `ReadDocumentTool` | `read_document` | `IDocumentService` | `document_id` (string, required) |
| `WorkspaceStatusTool` | `workspace_status` | `WorkspaceEngine` + `IIndexEngine` | none |

`SearchTool` returns a collection of search hits as MCP content. Presentation formatting belongs to the adapter or serialization layer — not the tool.

### 4.3 Sprint 11 Resources

| Resource | URI | Delegates To |
|---|---|---|
| `WorkspaceStatusResource` | `workspace://status` | `WorkspaceEngine` |
| `IndexStatsResource` | `workspace://index/stats` | `IIndexEngine.GetStatsAsync()` |
| `ConnectorsResource` | `workspace://connectors` | `IConnectorRegistry` |

**Resource URI convention:** `<domain>://<resource>/<subresource>`

Future resources follow the same convention: `workspace://documents`, `workspace://knowledge`, `workspace://graph`, `workspace://telemetry`, `workspace://models`.

### 4.4 Capability Growth Roadmap

| Sprint | Tools | Resources |
|---|---|---|
| 11 | `search`, `read_document`, `workspace_status` | `workspace://status`, `workspace://index/stats`, `workspace://connectors` |
| 12 | `context` | `workspace://context` |
| 13 | `knowledge_search`, `graph` | `workspace://knowledge` |
| Future | `agent`, `architect_analysis`, `explain`, `find_references` | `workspace://memory`, `workspace://models` |

**The design contract:** Adding a new MCP tool or resource requires no changes to `McpRuntime`, `StdioTransport`, or any existing tool or resource. This mirrors the extension pattern for Connectors, Parsers, and Search Providers.

### 4.5 Sprint 10 Reconciliation — `IDocumentService`

Sprint 11's `ReadDocumentTool` requires document retrieval by `DocumentId`. This is a **platform service**, not an MCP service. Its first consumer is MCP, but it will be reused by CLI commands, REST, Context Assembly, RAG pipelines, Agent Runtime, and Web UI.

**Do not add `GetDocumentAsync` to `IIndexEngine`.** The index engine's responsibility is indexing, not document storage.

Extend Sprint 10 with:

```csharp
// Ferret.Core.Search — alongside ISearchService and other search contracts
public interface IDocumentService
{
    Task<Document?> GetAsync(DocumentId id, CancellationToken cancellationToken);
    // Task<Stream> OpenStreamAsync(...) reserved for binary assets (PDF, images, Office)
}

public sealed record Document(
    DocumentId Id,
    CanonicalUri CanonicalUri,
    string Content,          // primary extracted representation, not the original asset
    string MimeType,
    long ContentLength,
    DateTimeOffset ParsedAt);
```

`Document.Content` is the extracted text representation. For a PDF, it is the parsed text. For source code, it is the source. This design accommodates future binary and multimedia connectors without changing the service contract.

**Platform vs. Runtime Dependency distinction:**
- `IDocumentService` is a **platform dependency** — owned by platform libraries, consumed by any host
- `IMcpRuntime` is a **runtime dependency** — owned by `Ferret.Mcp`, consumed only by the CLI launcher

---

## Section 5: Transport Layer

**The boundary:** Everything above `Transport/` is Ferret. Everything below `Transport/Stdio/` is the SDK.

This line is the core architectural statement of ADR-0017.

### 5.1 Structure

```
Transport/
  IMcpTransport.cs
  McpTransportDescriptor.cs
  IMcpTransportFactory.cs        ← reserved; not implemented Sprint 11
  Stdio/
    StdioTransport.cs
    SdkRuntimeAdapter.cs
    SdkToolAdapter.cs
    SdkResourceAdapter.cs
    McpArgumentsFactory.cs
```

### 5.2 Responsibility Map

| Class | Responsibility |
|---|---|
| `StdioTransport` | Owns stdio I/O loop; delegates to `SdkRuntimeAdapter` |
| `SdkRuntimeAdapter` | Configures SDK builder; registers tools/resources; owns SDK lifetime |
| `SdkToolAdapter` | Wraps one `IMcpTool`; translates SDK call → `McpArguments` → `McpToolResult` → SDK response |
| `SdkResourceAdapter` | Wraps one `IMcpResource`; translates SDK read → `McpResourceContent` → SDK response |
| `McpArgumentsFactory` | Constructs `McpArguments` from SDK parameter payload; only class that reads `JsonElement` |

### 5.3 Translation Flow — Tool Call

```
AI Host (MCP JSON-RPC over stdio)
   │
SDK (ModelContextProtocol)       ← only place SDK types exist
   │  SDK CallToolRequest
SdkToolAdapter
   │  McpArgumentsFactory.Create(request.Parameters)
McpArguments                     ← Ferret type; no SDK types above this line
   │
IMcpTool.ExecuteAsync(arguments, ct)
   │
McpToolResult(IReadOnlyList<McpContent>)
   │
SdkToolAdapter                   ← translates back to SDK CallToolResult
   │
SDK → AI Host
```

### 5.4 Translation Flow — Resource Read

```
AI Host (MCP JSON-RPC over stdio)
   │
SDK
   │  SDK ReadResourceRequest
SdkResourceAdapter               ← read-only resources take no parameters
   │
IMcpResource.ReadAsync(ct)
   │
McpResourceContent(ResourceUri, Content, MimeType)
   │
SdkResourceAdapter               ← translates to SDK ReadResourceResult
   │
SDK → AI Host
```

### 5.5 Transport Invariants

- **Stateless adapters.** `SdkToolAdapter` and `SdkResourceAdapter` hold no state between calls. Concurrency is safe by construction.
- **One-to-one translation.** One SDK message → one platform call → one response. No orchestration, batching, or retries in adapters.
- **Centralised error mapping.** Exceptions from `IMcpTool.ExecuteAsync` are translated by `IMcpErrorMapper` to `McpToolResult(IsError: true)`. Never silently dropped.
- **Cancellation propagation.** The cancellation token flows from transport → runtime → tool → platform service without interception. No new tokens are created inside the adapter chain.
- **No SDK type leakage.** `ModelContextProtocol.*` types appear only inside `Transport/Stdio/`.

---

## Section 6: CLI Integration and Hosting

### 6.1 `McpModule`

`McpModule` implements `IModule` — the same contract every Ferret subsystem uses. It is responsible for **composition, not discovery**: it wires dependencies together but does not decide which tools exist and contains no business logic.

`McpModule` registers at composition time:
- `IMcpTool` implementations: `SearchTool`, `ReadDocumentTool`, `WorkspaceStatusTool`
- `IMcpResource` implementations: `WorkspaceStatusResource`, `IndexStatsResource`, `ConnectorsResource`
- `IMcpToolRegistry` (immutable singleton, built by internal `ToolRegistryBuilder`)
- `IMcpResourceRegistry` (immutable singleton, built by internal `ResourceRegistryBuilder`)
- `IMcpTransport` → `StdioTransport`
- `IMcpErrorMapper` → `DefaultMcpErrorMapper`
- `IMcpRuntime` → `McpRuntime`

`ToolRegistryBuilder` and `ResourceRegistryBuilder` are internal composition helpers. They are not public platform concepts.

### 6.2 `ServeCliModule`

`ServeCliModule` implements `ICliModule` and contributes:

```
ferret serve [--transport stdio]
```

The `--transport` flag is **accepted but hidden from `--help`** in Sprint 11 (value is always `stdio`). This reserves the flag shape for future `--transport http` without a breaking CLI change.

### 6.3 `ServeCommandHandler`

```csharp
public sealed class ServeCommandHandler : ICommandHandler
{
    public async Task<CommandResult> ExecuteAsync(IFerretContext context, CancellationToken ct)
    {
        var runtime = context.Services.GetRequiredService<IMcpRuntime>();
        await runtime.RunAsync(ct);
        return CommandResult.Success();
    }
}
```

**Design goal:** Presentation handlers are orchestration-free — Resolve → Run → Return. If a handler grows beyond this shape, the logic belongs elsewhere.

### 6.4 Lifecycle

```
ferret serve
      │
ServeCommandHandler.ExecuteAsync
      │
  Validate workspace exists      ← friendly error if outside workspace
  Validate index exists          ← friendly error if ferret index not run
      │
IMcpRuntime.RunAsync
      │
  Resolve IMcpToolRegistry (immutable)
  Resolve IMcpResourceRegistry (immutable)
  Create SdkRuntimeAdapter
  Register tools (SdkToolAdapter × N)
  Register resources (SdkResourceAdapter × N)
  IMcpTransport.RunAsync (stdio loop)
  … await cancellation …
  Dispose SdkRuntimeAdapter
      │
CommandResult.Success()
```

**One runtime per process. One workspace per runtime.** No multiplexing.

### 6.5 Startup Banner (stderr)

```
Ferret MCP Runtime v0.11.0

Workspace:   C:\Projects\Ferret
Transport:   stdio
Tools:       3  (search, read_document, workspace_status)
Resources:   3  (workspace://status, workspace://index/stats, workspace://connectors)

Ready.
```

Emitted to **stderr** only. Stdout belongs entirely to the MCP protocol.

### 6.6 Graceful Shutdown

```
Cancellation signal (Ctrl+C or host termination)
      │
Stop accepting new requests
      │
Complete active request (if any)
      │
Dispose SdkRuntimeAdapter
      │
Dispose SDK transport
      │
Exit
```

### 6.7 Example MCP Client Configuration

```json
{
  "mcpServers": {
    "ferret": {
      "command": "ferret",
      "args": ["serve"]
    }
  }
}
```

No ports, no networking, no process management. The AI host launches `ferret serve` as a child process and communicates over stdio. Compatible with Claude Code, Claude Desktop, Cursor, VS Code MCP extensions, and any MCP-compliant client.

---

## Section 7: ADRs and Pre-Implementation Dependencies

### 7.1 ADR-0016 — Integration Platform Architecture

**Status:** To be committed before Sprint 11 implementation begins.

**Decision:** MCP is the first Integration Adapter. All future integration hosts (REST, Web, Agent, Background) follow the same pattern: thin adapter over shared platform services, no business logic in the host layer.

**Principles recorded:** Principles 1–8 from Section 2.4.

**Deferred — `Ferret.Application`:** A dedicated Application Layer is deferred until orchestration becomes a reusable platform concern. See ADR-0018.

### 7.2 ADR-0017 — MCP Runtime Architecture

**Status:** To be committed before Sprint 11 implementation begins.

**Decision:** Sprint 11 delivers an MCP Runtime (stdio transport). The Microsoft `ModelContextProtocol` SDK is used for protocol compliance and is isolated entirely within `Transport/Stdio/`.

**Key decisions recorded:**

- "Everything above `Transport/` is Ferret. Everything below `Transport/Stdio/` is the SDK."
- Tool and resource registries are immutable after composition
- SDK adapters are stateless; one-to-one protocol translation
- Cancellation flows without interception from transport to platform service
- `IMcpTransportFactory` reserved — not implemented until a second transport ships
- **Runtime Independence:** The MCP Runtime owns no platform state. It is disposable and may be created or destroyed without affecting workspace state or platform services.

### 7.3 Recommended — Elevate Host Architecture Pattern to a Core ADR

The Ferret Host Architecture Pattern (Section 2.5) is currently scoped to Sprint 11. It should be extracted into a standalone **Core Platform Pattern ADR** so all future ADRs — REST, Web UI, Model Platform, Agent Runtime, Enterprise Server — can simply state: *"This architecture conforms to the Ferret Host Architecture Pattern (ADR-XXXX)."*

This elevation is recommended after Sprint 11 is complete and the pattern is validated by implementation. The Core Pattern ADR supersedes the Host Architecture content in ADR-0016, leaving ADR-0016 focused on Sprint 11 integration-specific decisions.

### 7.4 ADR-0018 — Reserved: Application Layer Introduction

**Status:** Reserved. Not implemented.

**Trigger:** When orchestration becomes a reusable platform concern shared by multiple hosts — e.g., when multiple hosts require a workflow such as Search → Knowledge → Context → Prompt → Model.

**Reserved namespace:** `Ferret.Application`

### 7.4 Pre-Implementation Dependencies

**Design Gates** — must be resolved before Sprint 11 design is finalized:

| Item | Required By |
|---|---|
| ADR-0015 (Search Architecture) committed | Sprint 10 implementation gate |
| `IDocumentService` contract approved | `ReadDocumentTool` |
| `Document` model finalized | `IDocumentService` |
| `IIndexEngine.GetStatsAsync()` verified accessible | `IndexStatsResource`, `WorkspaceStatusTool` |

**Implementation Gates** — must be resolved before Sprint 11 code is written:

| Item | Note |
|---|---|
| Sprint 10 fully implemented | All search contracts, providers, CLI wire-up |
| `v0.10.0-sprint10` tag created | |
| All Sprint 10 search tests passing | |
| `ferret search` CLI command complete | |

**Version Gate Rule:** Sprint 11 must not introduce compensating implementations for missing Sprint 10 functionality. Any missing capability required by Sprint 11 must first be added to the Search Platform as part of Sprint 10 reconciliation.

---

## Section 8: Out of Scope and Exit Criteria

### 8.1 Out of Scope

**Transport:**
- HTTP, SSE, WebSocket, Named Pipes
- `IMcpTransportFactory` (reserved, not implemented)
- Remote hosting, multi-client support

**Integration:**
- REST API, ASP.NET Core hosting
- Authentication, Authorization
- MCP Prompt implementation (registry reserved only)

**AI / Intelligence:**
- LLM integration, model providers, semantic search
- Agent execution, conversation memory, knowledge graph queries

**Platform:**
- `Ferret.Application` layer
- Multi-workspace runtime, dynamic tool registration

### 8.2 Functional Exit Criteria

*Answers: Can the user use the feature?*

- `ferret serve` starts an MCP Runtime and blocks
- Startup banner emitted to stderr (workspace, transport, tool count, resource count)
- Friendly error when run outside a workspace or before `ferret index`
- Any MCP-compatible AI host connects over stdio
- `search` tool returns ranked results via `ISearchService`
- `read_document` tool retrieves full document content via `IDocumentService`
- `workspace_status` tool returns health, index stats, connector count, version
- `workspace://status`, `workspace://index/stats`, `workspace://connectors` return correct data
- Graceful shutdown on `Ctrl+C` and host termination

### 8.3 Architectural Exit Criteria

*Answers: Was the feature built correctly?*

- No `ModelContextProtocol.*` types appear outside `Transport/Stdio/`
- No business logic in any tool, resource, or adapter class
- All MCP tools delegate to existing platform services — no compensating implementations
- SDK adapters are stateless
- Immutable registries verified (no runtime mutation path exists)
- Cancellation propagation verified end-to-end
- Dependency rule holds: no platform library references `Ferret.Mcp`
- ADR-0016 and ADR-0017 committed; ADR-0018 reserved entry created

### 8.4 Operational Exit Criteria

*Answers: Does the runtime behave correctly under real conditions?*

- Runtime startup completes in < 1 second (excluding workspace validation)
- Tool registration is deterministic
- No background polling or idle CPU usage during stdio wait
- Graceful shutdown completes within a reasonable timeout

### 8.5 Extensibility Exit Criteria

*Answers: Does the architecture work as designed?*

- Adding a new MCP tool or resource requires **no changes** to `McpRuntime`, `StdioTransport`, or any existing tool
- The architecture admits future transports (HTTP, WebSocket, etc.) without changes to tools, resources, registries, or platform services
- Platform services (`ISearchService`, `IDocumentService`, `WorkspaceEngine`) remain unaware that MCP exists

### 8.6 Definition of Done

Sprint 11 is complete when:

1. A new integration host has been added without introducing new business logic
2. Existing platform capabilities are reused rather than duplicated
3. The MCP runtime behaves as a thin integration adapter
4. The transport layer is demonstrably replaceable without touching tools, resources, or platform services
5. External SDKs remain isolated to `Transport/Stdio/`
6. Future hosts can follow the same pattern without redesign
7. All prior tests remain green (651+ from Sprint 9, plus Sprint 10 additions)
8. Host integration tests pass: start runtime → connect → invoke each tool and resource → verify shutdown
9. `git tag v0.11.0-sprint11` applied
10. `PROJECT-STATE.md` updated

**Sprint Exit Statement:**
> After Sprint 11, every Ferret capability is accessible through at least one presentation host (CLI) and one integration host (MCP), both invoking the same platform services without duplication.

---

## Milestone M3 — Multi-Host Platform

Sprint 11 completion marks Milestone M3: Ferret transitions from a CLI-centric tool into a true multi-host platform.

**M3 is an architectural checkpoint, not a sprint.** It has no code deliverables beyond Sprint 11. Its purpose is to formally record that the Host Architecture Pattern has been validated by implementation, and that the conditions for future hosts are in place.

**M3 success criteria:**

- Platform capabilities are reusable across multiple hosts
- At least two hosts (CLI and MCP) expose the same services without duplication
- The Host Architecture Pattern has been validated by implementation (not just design)
- Future hosts require only adapter code — no platform changes
- The Core Platform Pattern ADR (see Section 7.3) has been elevated

**M3 unlock:** All future hosts (REST, Web UI, Desktop, Agent Runtime, Background) are unblocked. Each requires only an adapter project following the established pattern.
