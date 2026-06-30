# ARCH-019 — Connector Platform Architecture

| Field | Value |
|---|---|
| **Document ID** | ARCH-019 |
| **Version** | 1.0 |
| **Status** | Accepted |
| **Sprint** | Sprint 8 |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

> This is the pillar architecture document for the Ferret Connector Platform. All future connector implementations, CLI commands, and pipeline components must conform to the patterns defined here. Changes to core contracts or platform principles require an ADR superseding ADR-0013.

---

## §1 Purpose

The Connector Platform is the ingestion layer of ContextOS. It defines how Ferret discovers, configures, activates, and manages connections to external data sources — the local filesystem, Git repositories, JIRA, Confluence, Slack, SQL databases, cloud storage, and anything else that holds knowledge relevant to a user's context.

This document establishes the canonical architecture. Every section is normative unless marked *(informative)*.

---

## §2 Design Goals

1. **Composable** — capabilities attach to connectors via interfaces, not inheritance
2. **Extensible** — adding a new connector never touches Core contracts
3. **Testable** — every layer is independently unit-testable with `TempDirectory` + fakes
4. **Streamable** — discovery is always `IAsyncEnumerable<AssetDescriptor>`; memory usage is O(batch), not O(corpus)
5. **Portable** — `CanonicalUri` is workspace-relative; workspaces survive moves and clones
6. **Future-proof** — `IConnector.ConnectAsync` returns a session; connector-managed resources live inside `IConnectorSession`

---

## §3 Project Structure

```
Ferret.Core                      ← zero-dependency contracts (frozen M1)
  Ferret.Core.Connectors         ← connector contracts + asset model (additions only)
    Ferret.ConnectorPlatform     ← registry, factory dispatch, CLI module, formatters
      Ferret.Connectors.Filesystem  ← first concrete connector
```

No project in `Ferret.Connectors.*` may reference `Ferret.Cli`. The CLI references `Ferret.ConnectorPlatform` for `ConnectorCliModule`.

---

## §4 Connector Lifecycle (Canonical)

```
1. Discover   — connector type registered in IConnectorRegistry via DI
2. Configure  — workspace connectors.json names instances (Sprint 9 writes it)
3. Activate   — IConnectorManager.Activate(instance) → IConnectorSession (Sprint 10)
4. Runtime    — ConnectorStatus records current health, last sync, current error
```

The lifecycle layers are always separate models:

| Layer | Model | Where |
|---|---|---|
| Identity | `ConnectorMetadata` | `Ferret.Core.Connectors` |
| Static Descriptor | `ConnectorDescriptor` | `Ferret.Core.Connectors` |
| Workspace Config | `ConnectorInstance` (Sprint 9) | `Ferret.ConnectorPlatform` |
| Runtime State | `ConnectorStatus` | `Ferret.Core.Connectors` |
| Analytics | `ConnectorStatistics` | Analytics subsystem (post-Sprint 10) |

`state.json` records runtime state only. `connectors.json` records workspace configuration only. These files must never become each other.

---

## §5 Core Contracts

### IConnector

```csharp
public interface IConnector
{
    ConnectorType ConnectorType { get; }
    ConnectorMetadata Metadata { get; }
    ConnectorCapabilities Capabilities { get; }
    Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default);
    Task<IConnectorSession> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
```

`ConnectAsync` returns `IConnectorSession` — not void. This future-proofs connectors that hold runtime resources (SQL connections, Slack websockets, GitHub API clients). Filesystem returns a trivial no-op session.

### IConnectorSession

```csharp
public interface IConnectorSession : IAsyncDisposable
{
    ConnectorInstanceId InstanceId { get; }
}
```

Callers `await using` the session. Sessions are not cached by the connector — the platform manages session lifetime (Sprint 10).

### IAssetSource

```csharp
public interface IAssetSource
{
    IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        AssetDiscoveryOptions options,
        CancellationToken ct = default);
}
```

**Connectors implementing `IAssetSource` MUST stream.** Never buffer into `List<AssetDescriptor>` before yielding. `IAsyncEnumerable` with `yield return` is the only compliant implementation.

### IConnectorFactory

```csharp
public interface IConnectorFactory
{
    ConnectorId ConnectorId { get; }
    IConnector Create(ConnectorInstanceId instanceId);
}
```

Factories receive connector-specific configuration via constructor DI. The platform layer deserializes `ConnectorConfigurationReference` and configures the factory's DI scope before calling `Create`.

### IConnectorRegistry

```csharp
public interface IConnectorRegistry
{
    IReadOnlyList<ConnectorDescriptor> GetAll();
    ConnectorDescriptor? GetById(ConnectorId id);
    bool IsRegistered(ConnectorId id);
    IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability);
}
```

The registry is immutable after construction. `GetByCapability` is reserved for dashboards and orchestration.

### IIgnoreProvider

```csharp
public interface IIgnoreProvider
{
    bool ShouldIgnore(AssetDescriptor asset);
}
```

Implementations MUST return `false` for URI schemes they do not understand. `ShouldIgnore` is pure — no I/O, no state mutation.

---

## §6 Asset Model

`AssetDescriptor` is the universal connector-agnostic asset abstraction — the lingua franca of ContextOS. Every connector produces it. Every pipeline stage consumes it.

```csharp
public sealed record AssetDescriptor
{
    public AssetId Id { get; init; }
    public ConnectorId ConnectorId { get; init; }
    public ConnectorInstanceId InstanceId { get; init; }
    public AssetKind Kind { get; init; }
    public Uri CanonicalUri { get; init; }
    public string DisplayName { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public AssetFingerprint? Fingerprint { get; init; }
    public long? SizeBytes { get; init; }
    public string? MediaType { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}

public enum AssetKind { File, Directory }
```

`AssetId` is derived from `CanonicalUri` — deterministic, stable, no I/O required.

### Asset Lifecycle States

*(Informative — realized by `IChangeSource`, Sprint 9+)*

| State | Meaning |
|---|---|
| `Discovered` | First time seen |
| `Changed` | Fingerprint differs from last seen |
| `Deleted` | No longer present in source |
| `Unavailable` | Source temporarily unreachable |
| `Ignored` | Excluded by `IIgnoreProvider` |

---

## §7 Capability Model

Connectors declare capabilities via `ConnectorDescriptor.Capabilities`. Each capability is an immutable singleton:

```csharp
public static class ConnectorCapabilities
{
    public static readonly ConnectorCapability AssetDiscovery;
    public static readonly ConnectorCapability ChangeDetection;
    public static readonly ConnectorCapability EventStreaming;
    public static readonly ConnectorCapability Write;
    public static readonly ConnectorCapability Snapshot;
    public static readonly ConnectorCapability Relationships;
    public static readonly ConnectorCapability NativeSearch;
    public static readonly ConnectorCapability AssetEnrichment;   // reserved

    public static IReadOnlyList<ConnectorCapability> All { get; }
}
```

**Rule:** capability composition over inheritance. A connector implementing both discovery and change detection looks like:

```csharp
public sealed class FilesystemConnector : IConnector, IAssetSource
// NOT: IAssetSource : IConnector
// NOT: FilesystemConnector : ConnectorBase
```

`ConnectorDescriptor.Capabilities` lists which capabilities the connector declares. `ConnectorCapabilities.All` drives the `ferret connector info` capability matrix (✓ implemented, ✓ reserved, ✗ not available).

---

## §8 Connector Session Lifecycle

```
ConnectAsync() → IConnectorSession
    │
    ├─ IAssetSource.DiscoverAsync(...)    ← streams AssetDescriptors
    ├─ IChangeSource.WatchAsync(...)      ← reserved Sprint 9
    └─ DisposeAsync()                     ← releases runtime resources
```

For stateless connectors (filesystem): `ConnectAsync` validates the root path and returns a trivial session. `DisposeAsync` is a no-op.

For stateful connectors (SQL, Slack): the session holds and disposes the connection. The platform manages session lifetime via `IConnectorManager` (Sprint 10).

---

## §9 Configuration Model

### connectors.json (workspace configuration)

```json
{
  "version": "1.0",
  "instances": [
    {
      "instanceId": "src-root",
      "connectorType": "filesystem",
      "displayName": "Source Root",
      "enabled": true,
      "config": { "rootPath": ".", "excludeExtensions": [".dll", ".exe"] }
    }
  ]
}
```

**Sprint 8:** schema defined; CLI reads but never writes.
**Sprint 9:** `ferret connector enable/disable/configure` writes it.
**Sprint 10:** `IConnectorManager` reads and activates enabled instances.

### ConnectorConfigurationReference

`ConnectorInstance.Config` is a `ConnectorConfigurationReference`, not a raw `JsonElement`. This decouples connectors from the configuration source:

```csharp
public sealed record ConnectorConfigurationReference
{
    public string Source { get; init; } = "json";   // json | env | vault | database
    public string? RawJson { get; init; }
    // Reserved: SecretUri, EnvironmentPrefix, DatabaseKey
}
```

The platform factory layer deserializes `RawJson` into the connector's strongly-typed configuration record. Connectors never see `JsonElement`.

---

## §10 Ignore Provider Stack

### Precedence (highest to lowest)

1. **Platform** — hardcoded exclusions in `FilesystemConnector` (not a provider):
   `.git/`, `.ferret/`, `.gitmodules`, `.svn/`, `.hg/`
2. **Enterprise Policy** — *reserved; Sprint V2+ (DLP policy layer)*
3. **Workspace** — `FerretIgnoreProvider` reads `.ferretignore` (same format as `.gitignore`)
4. **Connector** — `GitIgnoreProvider` reads `.gitignore`

The `CompositeIgnoreProvider` chains providers in precedence order. Any provider returning `true` causes the asset to be skipped — there is no un-ignore mechanism.

### Sprint 8 scope

- Root-level `.gitignore` and `.ferretignore` only
- `IIgnoreProvider.ShouldIgnore(AssetDescriptor)` — asset-level, not path-level
- `ShouldIgnore` returns `false` for non-`filesystem:` URIs

### Deferred

- Nested `.gitignore` traversal — Sprint 9
- Enterprise Policy layer — V2
- `.gitmodules`, `.svn/`, `.hg/` skip — documented, not implemented Sprint 8

---

## §11 CanonicalUri Normalization Rules

`CanonicalUri` is the stable identity of an asset across process restarts, workspace moves, and analytics queries. Once a URI enters the knowledge graph, its normalization cannot change without invalidating all graph edges.

**Rules (all REQUIRED):**

| Rule | Example |
|---|---|
| Scheme: `filesystem` (lowercase) | `filesystem:///` not `Filesystem:///` |
| Authority: none | `filesystem:///path` (three slashes) |
| Path separator: forward slash only | `filesystem:///src/Program.cs` |
| Path: workspace-relative | `filesystem:///src/Program.cs` not `filesystem:///C:/Work/src/Program.cs` |
| No trailing slash on files | `filesystem:///src/Program.cs` |
| Directory trailing slash: omit | `filesystem:///src` not `filesystem:///src/` |
| No duplicate separators | `filesystem:///a/b` not `filesystem:///a//b` |
| Unicode: NFC normalization | Applied before URI construction |
| Percent-encoding: RFC 3986 | Spaces → `%20`; reserved chars in path → encoded |
| Case: preserve original | `filesystem:///src/MyClass.cs` not `filesystem:///src/myclass.cs` |

**Construction:**
```csharp
var relative = Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
var uri = new Uri($"filesystem:///{relative.TrimStart('/')}");
```

---

## §12 Streaming-by-Default Principle

> **Everything in the Connector → Pipeline → Parser → Knowledge → Analytics chain is `IAsyncEnumerable<T>`. `List<T>` is only acceptable for bounded, known-small collections (e.g., registered connectors in the registry).**

This principle applies to:
- `IAssetSource.DiscoverAsync` → `IAsyncEnumerable<AssetDescriptor>`
- `IChangeSource.WatchAsync` (Sprint 9) → `IAsyncEnumerable<AssetChangeEvent>`
- Index Engine ingestion (Sprint 9) → `IAsyncEnumerable<IndexableDocument>`
- Search results (Sprint 10) → `IAsyncEnumerable<SearchResult>`

Violation: a method that internally calls `.ToList()` on an `IAsyncEnumerable` and then returns an `IEnumerable`. This is detectable via architecture tests.

---

## §13 CLI Presentation Layer

### Principle: Commands Are Orchestration

```
CLI → CommandHandler → Platform Services → Runtime → Connectors
```

Command handlers never reference connector implementations directly. They depend only on `IConnectorRegistry` and `IWorkspaceLocator`.

### View Model Pipeline

```
ConnectorDescriptor → ConnectorListItem → ICommandResultFormatter<ConnectorListResult> → IOutputFormatter → Console
```

`ICommandResultFormatter<T>` is injected via DI. Sprint 8 provides `TextConnectorListFormatter` and `TextConnectorInfoFormatter`. Future sprints add `JsonConnectorListFormatter` etc. when `--output json` is implemented.

### connector list — Staged Column Evolution

| Sprint | Columns |
|---|---|
| Sprint 8 | `ID`, `NAME`, `VERSION`, `CAPABILITIES`, `CONFIGURED` |
| Sprint 9 | + `INSTANCES` |
| Sprint 10 | + `ACTIVE`, `HEALTH` |

### Exit Code Conventions

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | User error (unknown connector, missing required argument) |
| 2 | Configuration error (malformed `connectors.json`) |
| 3 | Runtime failure (unexpected exception) |
| 10+ | Reserved |

---

## §14 CLI Command Evolution

### Sprint 8 (read-only inspection)
- `ferret connector list`
- `ferret connector info <id>`

### Sprint 9 (configuration management)
- `ferret connector enable <id>`
- `ferret connector disable <id>`
- `ferret connector configure <id> [--option value]`

### Sprint 10 (operational)
- `ferret connector sync <id>`
- `ferret connector doctor [id]`
- `ferret connector status [id]`

### V2 (plugin marketplace)
- `ferret connector install <package>`
- `ferret connector uninstall <id>`
- `ferret connector update [id]`

### Reserved global flags
`--output text|json|markdown` — not implemented Sprint 8.
`ferret connector list --configured|--active|--capability <id>` — not implemented Sprint 8.

---

## §15 FilesystemConnector

The reference implementation. All future connectors should follow this template.

**Project:** `Ferret.Connectors.Filesystem`

| Type | Role |
|---|---|
| `FilesystemConnector` | `IConnector + IAssetSource` implementation |
| `FilesystemConnectorSession` | Trivial `IConnectorSession` (no-op dispose) |
| `FilesystemConnectorFactory` | `IConnectorFactory` — builds `FilesystemConnector` from `FilesystemConnectorConfiguration` |
| `FilesystemConnectorConfiguration` | Typed config: `RootPath`, `IncludeExtensions`, `ExcludeExtensions` |
| `GitIgnoreProvider` | `IIgnoreProvider` — reads `.gitignore` |
| `FerretIgnoreProvider` | `IIgnoreProvider` — reads `.ferretignore` |
| `CompositeIgnoreProvider` | `IIgnoreProvider` — chains providers |

`GetHealthAsync` probes `RootPath` for existence and read access. No persistent connection.

`DiscoverAsync` walks directories recursively as `IAsyncEnumerable<AssetDescriptor>`. Hardcoded skips: `.git/`, `.ferret/`. Applies `options.IgnoreProvider` per asset after construction.

---

## §16 Reserved Capabilities and Interfaces

The following are defined in contracts (interface stubs or documented singletons) in Sprint 8 but not implemented:

| Type | Reserved For |
|---|---|
| `IAssetEnricher` | Sprint 9 — enriches `AssetDescriptor` post-discovery, pre-indexing |
| `IChangeSource` | Sprint 9 — `IAsyncEnumerable<AssetChangeEvent>` change streaming |
| `IAssetEnumerator` | Sprint 9 — internal abstraction for easier unit testing |
| `IConnectorManager` | Sprint 10 — activates configured connectors |
| `ConnectorPipeline` | Sprint 10+ — Retry, Rate Limiting, Circuit Breaker, Telemetry, Metrics, Scheduling |
| `ConnectorStatistics` | Analytics subsystem — asset counts, bytes, failure rates |
| `IRuntimeStatus` / `IProcessInfo` | Sprint 10 — IPC, daemon mode |
| Enterprise Policy `IIgnoreProvider` | V2 — DLP policy layer |
| Plugin connector discovery | V2 — `dotnet tool install ferret-connector-*` |

---

## §17 Architecture Tests

Architecture rules are expressed as executable xUnit tests in `Ferret.Architecture.Tests`. Rules fail the build on violation.

| Rule | Mechanism |
|---|---|
| `Ferret.Core` has no external package references | Parse `Ferret.Core.csproj`; assert no `<PackageReference>` |
| Connector assemblies do not reference `Ferret.Cli` | Assembly reference inspection |
| All `IConnector` implementations are `sealed` | Reflect over types in connector assemblies |
| `IAssetSource.DiscoverAsync` return type is `IAsyncEnumerable<AssetDescriptor>` | Reflect over method return type |
| `ConnectorDescriptor` has no public settable properties | Reflect over property setters |
| `AssetDescriptor` has no public settable properties | Reflect over property setters |

Additional rules added per sprint as platform principles are established.

---

## §18 Future Evolution

The following are intentionally deferred and should not be designed prematurely:

**Incremental sync:** `IAssetSource.DiscoverAsync(options with { IncrementalOnly = true, Since = lastSyncAt })` — the options object already supports this; Sprint 9 implements it.

**Knowledge Graph integration:** `CanonicalUri` becomes the node identifier in the property graph (V2). Normalization rules (§11) must be stable before graph population begins.

**Multi-source deduplication:** two connectors (filesystem + Git) may discover the same logical asset. `CanonicalUri` + `AssetFingerprint` together identify duplicates. The knowledge graph merges them by URI.

**ConnectorPipeline:** a declarative pipeline per connector-instance: Retry → Rate Limiting → Batching → Circuit Breaker → Telemetry → Metrics → Scheduling. Not in scope until Sprint 10+.

**Plugin architecture:** `dotnet tool install ferret-connector-jira` installs a NuGet package that self-registers via `IConnectorFactory`. The registry discovers it on next startup. DI wiring is via `IServiceCollection` extensions in the package's `AddFerretConnector()` method. V2.

---

## Traceability

| Input | Role |
|---|---|
| `ADR-0013` | Platform principles that led to this architecture |
| `SPEC-008` | Sprint 8 design specification |
| `ROADMAP-001` | V1 sprint plan |
| `ARCH-017` | Storage architecture (state.json, connectors.json schemas) |
| `Ferret.Core.Connectors` | Contract implementations |
