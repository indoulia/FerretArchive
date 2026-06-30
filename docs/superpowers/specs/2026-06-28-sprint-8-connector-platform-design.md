# Sprint 8 Design — Connector Platform

| Field | Value |
|---|---|
| **Document ID** | SPEC-008 |
| **Sprint** | Sprint 8 — Connector Platform |
| **Status** | Approved |
| **Author** | Ferret Core Team |
| **Date** | 2026-06-28 |
| **Supersedes** | ROADMAP-001 Sprint 8 stub (Filesystem Connector) |

> **Canonical reference:** `docs/002-Architecture/ARCH-019-Connector-Platform-Architecture.md`
> **ADR:** `docs/adr/0013-capability-based-platform-architecture.md`

---

## Goal

Sprint 8 elevates the connector subsystem from a single `IConnector` contract (Sprint 7) into a complete **Connector Platform** — the generic ingestion architecture that all future ContextOS connectors will follow. The `FilesystemConnector` is the first implementation, proving the architecture end-to-end. The CLI delivers `ferret connector list` and `ferret connector info`, both read-only.

**What a user can do after Sprint 8 that they couldn't before:**
- Run `ferret connector list` to see what connectors are available on the platform
- Run `ferret connector info filesystem` to inspect the Filesystem Connector's capabilities and configuration schema
- Understand what connectors are registered and how to configure them for Sprint 9

---

## Platform Principles (established Sprint 8)

These principles apply to all future Ferret and ContextOS subsystems:

1. **Capability composition over inheritance** — `FilesystemConnector : IConnector, IAssetSource`, not `IAssetSource : IConnector`
2. **Universal Asset Model** — `AssetDescriptor` is the lingua franca; connectors produce it, the pipeline consumes it
3. **Identity → Descriptor → Instance → Status lifecycle** — immutable identity, static descriptor, workspace configuration, runtime state are always separate models
4. **Streaming by Default** — everything is `IAsyncEnumerable<T>` through the pipeline; `List<T>` only for bounded, known-small collections
5. **Normalization before processing** — `CanonicalUri` is normalized once at construction; never re-normalized downstream
6. **Separation of discovery, enrichment, indexing, knowledge extraction** — connectors produce `AssetDescriptor`; they do not parse, index, or interpret content
7. **Commands are orchestration, not implementation** — CLI → Handler → Platform Services → Runtime → Connectors; handlers never touch connectors directly

---

## Section 1: Project Structure

### New Projects

| Project | Type | Responsibility |
|---|---|---|
| `Ferret.ConnectorPlatform` | Library | `ConnectorRegistry`, `RegistryBuilder`, `IConnectorManager`, `ConnectorCliModule`, `ICommandResultFormatter<T>` |
| `Ferret.Connectors.Filesystem` | Library | `FilesystemConnector`, `FilesystemConnectorFactory`, `FilesystemConnectorConfiguration`, ignore providers |
| `Ferret.ConnectorPlatform.Tests` | xUnit | Registry, builder, formatter, view model mapping |
| `Ferret.Connectors.Filesystem.Tests` | xUnit | Connector, discovery, ignore providers, session |
| `Ferret.Architecture.Tests` | xUnit | Executable architectural rules via reflection |

### Additions to `Ferret.Core` (non-breaking, M1 compliant)

All additions to `Ferret.Core.Connectors` namespace:
- `ConnectorId` (typed value object)
- `ConnectorInstanceId` (typed value object)
- `AssetId` (typed value object)
- `AssetKind` enum
- `AssetDescriptor` (sealed record)
- `AssetFingerprint` (sealed record, `CreateLightweight(...)`)
- `AssetDiscoveryOptions` (sealed record)
- `ConnectorDescriptor` (sealed record)
- `ConnectorCapability` (sealed record — value object with immutable singletons)
- `ConnectorCapabilities` (static class — singleton singletons: `AssetDiscovery`, `ChangeDetection`, `EventStreaming`, `Write`, `Snapshot`, `Relationships`, `NativeSearch`, `AssetEnrichment`)
- `ConnectorStatus` (sealed record — current runtime state only)
- `IConnectorRegistry` (interface + `GetByCapability`)
- `IConnectorFactory` (interface — `ConnectorId`, `Create(ConnectorInstanceId)`)
- `IConnectorManager` (interface — reserved Sprint 10)
- `IIgnoreProvider` (interface — `ShouldIgnore(AssetDescriptor)`)
- `IAssetSource` (interface — `DiscoverAsync(AssetDiscoveryOptions, ct)`)
- `IConnectorSession` (interface — `IAsyncDisposable`, `InstanceId`)
- `IAssetEnricher` (reserved — interface stub only)
- `IRuntimeStatus` + `IProcessInfo` (reserved stubs)

### Additions to `Ferret.Cli` (non-breaking, M1 compliant)

- `ArgumentDefinition` (sealed record — `Name`, `Description`, `IsRequired`)
- `ICommandResultFormatter<T>` (interface — `Format(T result, IOutputFormatter output)`)
- `CommandDefinition.WithArgument(name, description, required)` extension

---

## Section 2: Core Contracts

### Typed IDs

```csharp
// All follow the same pattern as existing typed IDs in Ferret.Core
public sealed record ConnectorId(string Value);
public sealed record ConnectorInstanceId(string Value);
public sealed record AssetId(string Value)
{
    public static AssetId From(Uri canonicalUri) => new(canonicalUri.ToString());
}
```

### ConnectorMetadata (unchanged from Sprint 7)

Already defined. `ConnectorId` replaces the raw string `id` parameter via a factory overload.

### ConnectorDescriptor

```csharp
public sealed record ConnectorDescriptor
{
    public ConnectorId Id { get; init; }
    public ConnectorMetadata Metadata { get; init; }
    public IReadOnlyList<ConnectorCapability> Capabilities { get; init; }
    public IReadOnlyList<string> SupportedPlatforms { get; init; }
    public string? DocumentationUri { get; init; }
    // Reserved: ConfigurationSchema, MinimumPlatformVersion
}
```

### ConnectorCapability (value object, immutable singletons)

```csharp
public sealed record ConnectorCapability(
    string Id,
    string Name,
    string Version,
    string Description);

public static class ConnectorCapabilities
{
    public static readonly ConnectorCapability AssetDiscovery = new("asset-discovery", "Asset Discovery", "1.0", "...");
    public static readonly ConnectorCapability ChangeDetection = new("change-detection", "Change Detection", "1.0", "...");
    public static readonly ConnectorCapability EventStreaming = new("event-streaming", "Event Streaming", "1.0", "...");
    public static readonly ConnectorCapability Write = new("write", "Write Back", "1.0", "...");
    public static readonly ConnectorCapability Snapshot = new("snapshot", "Snapshot", "1.0", "...");
    public static readonly ConnectorCapability Relationships = new("relationships", "Relationships", "1.0", "...");
    public static readonly ConnectorCapability NativeSearch = new("native-search", "Native Search", "1.0", "...");
    public static readonly ConnectorCapability AssetEnrichment = new("asset-enrichment", "Asset Enrichment", "1.0", "...");

    public static IReadOnlyList<ConnectorCapability> All { get; } = [
        AssetDiscovery, ChangeDetection, EventStreaming, Write, Snapshot, Relationships, NativeSearch, AssetEnrichment
    ];
}
```

### AssetDescriptor

```csharp
public sealed record AssetDescriptor
{
    public AssetId Id { get; init; }
    public ConnectorId ConnectorId { get; init; }
    public ConnectorInstanceId InstanceId { get; init; }
    public AssetKind Kind { get; init; }
    public Uri CanonicalUri { get; init; }           // normalized, stable, workspace-relative
    public string DisplayName { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public AssetFingerprint? Fingerprint { get; init; }
    public long? SizeBytes { get; init; }
    public string? MediaType { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public enum AssetKind { File, Directory }
```

### AssetFingerprint

```csharp
public sealed record AssetFingerprint(string Algorithm, string Value)
{
    // Sprint 8: last-write-time + size (lightweight, no I/O beyond directory enumeration)
    public static AssetFingerprint CreateLightweight(DateTimeOffset lastWrite, long sizeBytes) =>
        new("lightweight", $"{lastWrite.ToUnixTimeMilliseconds()}:{sizeBytes}");

    // Reserved for Sprint 9 (indexing pipeline):
    // public static AssetFingerprint FromSha256(byte[] hash)
    // public static AssetFingerprint FromGitBlobHash(string hash)
    // public static AssetFingerprint FromETag(string etag)
}
```

### IConnectorSession

```csharp
public interface IConnectorSession : IAsyncDisposable
{
    ConnectorInstanceId InstanceId { get; }
}
```

### IConnector (updated from Sprint 7)

```csharp
public interface IConnector
{
    ConnectorType ConnectorType { get; }
    ConnectorMetadata Metadata { get; }
    ConnectorCapabilities Capabilities { get; }      // Sprint 7 type (canRead/canWrite/canStream)
    Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default);
    Task<IConnectorSession> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
```

### IAssetSource

```csharp
public interface IAssetSource
{
    IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        AssetDiscoveryOptions options,
        CancellationToken ct = default);
}

public sealed class AssetDiscoveryOptions
{
    public IIgnoreProvider? IgnoreProvider { get; init; }
    // Reserved: MaxDepth, IncrementalOnly, Since, BatchSize
}
```

### IIgnoreProvider

```csharp
public interface IIgnoreProvider
{
    bool ShouldIgnore(AssetDescriptor asset);
}
```

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

### IConnectorFactory

```csharp
public interface IConnectorFactory
{
    ConnectorId ConnectorId { get; }
    IConnector Create(ConnectorInstanceId instanceId);
}
```

### ConnectorStatus (current runtime state only)

```csharp
public sealed record ConnectorStatus
{
    public ConnectorId ConnectorId { get; init; }
    public ConnectorInstanceId InstanceId { get; init; }
    public bool IsActive { get; init; }
    public ConnectorHealth Health { get; init; }
    public DateTimeOffset? LastSyncAt { get; init; }
    public string? CurrentError { get; init; }
    // Reserved: ConnectorStatistics (Sprint analytics subsystem)
}
```

---

## Section 3: FilesystemConnector + connectors.json Schema

### FilesystemConnector

```csharp
public sealed class FilesystemConnector : IConnector, IAssetSource
```

**`IConnector` implementation:**
- `GetHealthAsync` — `Directory.Exists(RootPath)` + probe read; returns `ConnectorHealth.Connected` or `ConnectorHealth.Disconnected(message, checkedAt)`
- `ConnectAsync` — validates `RootPath` accessible; returns `FilesystemConnectorSession` (trivial `IAsyncDisposable`)
- `DisconnectAsync` — no-op

**`IAssetSource` implementation:**
- `DiscoverAsync` streams files recursively using `IAsyncEnumerable`; never accumulates into a `List<T>`
- Hardcoded skip: `.git/`, `.ferret/`, `.gitmodules`, `.svn/`, `.hg/` (platform internals)
- Per-asset: construct `AssetDescriptor` → call `options.IgnoreProvider?.ShouldIgnore(asset)` → yield if not ignored
- `CanonicalUri` scheme: `filesystem:///relative/path/to/file` (workspace-relative, forward slashes, no trailing slash on files)

### CanonicalUri Normalization Rules (see ARCH-019 §11)

- Scheme: `filesystem` (lowercase)
- Authority: none (`filesystem:///` — three slashes)
- Path: workspace-relative, forward slashes only, no trailing slash, percent-encoded where required, NFC Unicode normalization
- `AssetId.From(CanonicalUri)` — deterministic from URI

### FilesystemConnectorConfiguration

```csharp
public sealed class FilesystemConnectorConfiguration
{
    public string RootPath { get; init; } = ".";
    public IReadOnlyList<string> IncludeExtensions { get; init; } = [];
    public IReadOnlyList<string> ExcludeExtensions { get; init; } = [];
    // Deferred: IncludeHidden (OS-specific; ignore provider covers most cases)
}
```

### Ignore Providers

**Precedence (highest to lowest):**
1. Platform: hardcoded exclusions `.git/`, `.ferret/`, etc. (in `FilesystemConnector`, not provider)
2. Enterprise Policy: reserved
3. Workspace: `FerretIgnoreProvider` (reads `.ferretignore`)
4. Connector: `GitIgnoreProvider` (reads `.gitignore`)

```csharp
public interface IIgnoreProvider { bool ShouldIgnore(AssetDescriptor asset); }

// All implementations return false for non-filesystem URIs
public sealed class GitIgnoreProvider : IIgnoreProvider       // reads {root}/.gitignore
public sealed class FerretIgnoreProvider : IIgnoreProvider    // reads {root}/.ferretignore
public sealed class CompositeIgnoreProvider : IIgnoreProvider // any true = ignored
```

Sprint 8: root-level `.gitignore` only. Nested `.gitignore` traversal deferred to Sprint 9.

### connectors.json Schema (defined Sprint 8; written by CLI in Sprint 9)

```json
{
  "version": "1.0",
  "instances": [
    {
      "instanceId": "src-root",
      "connectorType": "filesystem",
      "displayName": "Source Root",
      "enabled": true,
      "config": {
        "rootPath": ".",
        "excludeExtensions": [".dll", ".exe", ".obj", ".bin"]
      }
    }
  ]
}
```

`ConnectorInstance` lives in `Ferret.ConnectorPlatform` (carries `JsonElement`; not in zero-dependency Core):

```csharp
internal sealed record ConnectorInstance
{
    public ConnectorInstanceId InstanceId { get; init; }
    public string ConnectorType { get; init; }
    public string DisplayName { get; init; }
    public bool IsEnabled { get; init; }
    public ConnectorConfigurationReference Config { get; init; }
}

public sealed record ConnectorConfigurationReference
{
    public string Source { get; init; } = "json";     // json | env | vault | database (reserved)
    public string? RawJson { get; init; }
}
```

In Sprint 8, `IConnectorManager` reads `connectors.json` if present but the CLI never writes it. `ferret connector list` shows registry state regardless of configuration.

---

## Section 4: CLI Commands

### Command Topology

```
ferret connector              ← group (ConnectorCliModule)
  connector list              ← ConnectorListCommandHandler
  connector info <id>         ← ConnectorInfoCommandHandler
  connector doctor            ← RESERVED Sprint 10
```

### Non-Breaking M1 Additions to Ferret.Cli

```csharp
public sealed record ArgumentDefinition(string Name, string Description, bool IsRequired = true);

public interface ICommandResultFormatter<T>
{
    void Format(T result, IOutputFormatter output);
}
```

`CommandDefinition` gains `WithArgument(name, description, required)`. `RootCommandFactory.BuildCommand` wires positional arguments alongside options; values available via `IFerretContext.Arguments["name"]`.

### View Models (in Ferret.ConnectorPlatform)

```
ConnectorDescriptor → ConnectorListItem → ICommandResultFormatter<ConnectorListResult>
ConnectorDescriptor → ConnectorInfoView → ICommandResultFormatter<ConnectorInfoView>
```

Handlers return domain models; formatters own all rendering logic. Later the same handlers support `--output json` by injecting a different formatter.

### ferret connector list

Without workspace:
```
ID            NAME                    VERSION   CAPABILITIES
filesystem    Filesystem Connector    1.0.0     AssetDiscovery
```

With workspace (Sprint 8):
```
ID            NAME                    VERSION   CAPABILITIES     CONFIGURED
filesystem    Filesystem Connector    1.0.0     AssetDiscovery   no
```

No connectors registered:
```
No connectors are registered.
Next: Install a connector package and register it in Program.cs.
```

### ferret connector info <id>

```
Filesystem Connector  v1.0.0
  ID:           filesystem
  Type:         Filesystem
  Description:  Discovers files and directories from the local filesystem.

  Capabilities
    ✓  Asset Discovery     v1.0   Enumerate files and directories as AssetDescriptors.
    ✓  Change Detection    v1.0   (Reserved — Sprint 9)
    ✗  Event Streaming
    ✗  Write Back
    ✗  Native Search

  Configuration
    rootPath            string    required    Root directory path
    excludeExtensions   string[]  optional    Extensions to exclude (e.g. ".dll")

  Platforms:  Linux, macOS, Windows
  Status:     Available (not configured)
```

Unknown ID:
```
Error: connector 'xyz' is not registered.
       Run 'ferret connector list' to see available connectors.
```

### Exit Code Conventions

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | User error (unknown connector, invalid argument) |
| 2 | Configuration error |
| 3 | Runtime failure |
| 10+ | Reserved |

### CLI Evolution Roadmap

**Sprint 8:** `connector list`, `connector info`
**Sprint 9:** `connector enable`, `connector disable`, `connector configure`
**Sprint 10:** `connector sync`, `connector doctor`, `connector status`
**V2:** `connector install`, `connector uninstall`, `connector update`

### Reserved Global Output Modes

`--output text` (default) | `--output json` | `--output markdown` — reserved; not implemented Sprint 8.

### Reserved Filters for connector list

`--configured` | `--active` | `--capability <id>` — reserved; documented in ARCH-019.

### ConnectorCliModule

```csharp
public sealed class ConnectorCliModule : CliModuleBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
        services.AddSingleton<FilesystemConnectorFactory>();
        services.AddSingleton<IConnectorFactory>(sp => sp.GetRequiredService<FilesystemConnectorFactory>());
        services.AddSingleton<ConnectorListCommandHandler>();
        services.AddSingleton<ConnectorInfoCommandHandler>();
    }

    public override IReadOnlyList<CommandDefinition> GetCommands() => [
        CommandDefinition.Group("connector", "Connector management and inspection")
            .WithSubcommand(CommandDefinition.Leaf("list", "List all registered connectors", typeof(ConnectorListCommandHandler)))
            .WithSubcommand(CommandDefinition.Leaf("info", "Show connector details", typeof(ConnectorInfoCommandHandler))
                .WithArgument("id", "Connector ID (e.g. filesystem)"))
    ];
}
```

`ConnectorRegistry` receives `IEnumerable<IConnectorFactory>` via DI — no manual enrollment.

---

## Section 5: Testing Strategy

### Test Projects

| Project | Status | Scope |
|---|---|---|
| `Ferret.Core.Tests` | Existing | New connector contracts |
| `Ferret.ConnectorPlatform.Tests` | **New** | Registry, builder, formatter, view model mapping |
| `Ferret.Connectors.Filesystem.Tests` | **New** | FilesystemConnector, ignore providers |
| `Ferret.Cli.Tests` | Existing | Handler tests, ArgumentDefinition |
| `Ferret.Integration.Tests` | Existing | E2E: `ferret connector list/info` |
| `Ferret.Architecture.Tests` | **New** | Executable architectural rules |

### Key Test Cases

**Streaming proof:** populate 1000-file temp directory; assert first `AssetDescriptor` arrives before enumeration completes. Verifies `IAsyncEnumerable` never secretly buffers.

**Ignore precedence:** assert `.git/` is skipped even when a custom `IIgnoreProvider` returns `false` for that path (hardcoded exclusion wins).

**Capability singletons:** `ConnectorCapabilities.AssetDiscovery == ConnectorCapabilities.AssetDiscovery` (referential equality).

**CanonicalUri portability:** discover files in a temp directory; verify URIs use forward slashes and are workspace-relative regardless of OS path separator.

**Registry DI-independence:** `RegistryBuilder` builds a valid `IConnectorRegistry` from a plain `List<IConnectorFactory>` — no `IServiceProvider` involved.

### Architecture Tests (Ferret.Architecture.Tests)

Executable rules via reflection (no third-party ArchUnit library in Sprint 8 — pure reflection):

| Rule | Implementation |
|---|---|
| `Ferret.Core` has no external package references | Parse project file; assert no `<PackageReference>` outside BCL |
| Connectors never reference `Ferret.Cli` | Assembly reference check |
| All `IConnector` implementations are `sealed` | Reflect over types implementing `IConnector` |
| `IAssetSource.DiscoverAsync` return type is `IAsyncEnumerable<AssetDescriptor>` | Reflect over method signature |
| `ConnectorDescriptor` has no settable public properties | Reflect over properties |
| `AssetDescriptor` has no settable public properties | Reflect over properties |

Architecture tests fail the build on violation — enforcing platform principles as executable constraints rather than documentation.

### TDD Discipline

Every task: failing test → confirm red → implement → verify green → commit.
No mocking filesystem — use real `TempDirectory`.
`FakeConnectorRegistry` for isolated handler tests.

---

## Deferred / Reserved

| Item | Deferred To |
|---|---|
| `IncludeHidden` in `FilesystemConnectorConfiguration` | Sprint 9 (OS-specific) |
| Nested `.gitignore` traversal | Sprint 9 |
| Enterprise Policy ignore layer | V2 |
| `IAssetEnumerator` | Sprint 9 (test isolation) |
| `IAssetEnricher` | Sprint 9 (enrichment pipeline) |
| `IChangeSource` / change detection | Sprint 9 |
| `ConnectorPipeline` (Retry, Circuit Breaker, etc.) | Sprint 10+ |
| `ConnectorStatistics` | Analytics subsystem (post-Sprint 10) |
| `IConnectorManager` activation | Sprint 10 |
| `ferret connector doctor` | Sprint 10 |
| `connectors.json` written by CLI | Sprint 9 |
| `--output json/markdown` | Sprint 9 |
| Connector plugin discovery | V2 |
| `connector install/uninstall/update` | V2 |
