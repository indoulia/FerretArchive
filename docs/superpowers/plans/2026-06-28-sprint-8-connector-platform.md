# Sprint 8 — Connector Platform Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the full Connector Platform architecture — registry, factory, asset model, FilesystemConnector, ignore providers, CLI commands `ferret connector list` and `ferret connector info` — proving the canonical ContextOS ingestion pipeline end-to-end.

**Architecture:** `Ferret.Core.Connectors` gains typed IDs, asset model, capability singletons, and interface contracts (non-breaking M1 additions). `Ferret.ConnectorPlatform` provides registry, formatters, and CLI module. `Ferret.Connectors.Filesystem` is the first concrete connector.

**Tech Stack:** .NET 9 / C# 13, xUnit, StyleCop + `AnalysisMode=All`, System.CommandLine 2.0 beta, System.Text.Json (BCL), `IAsyncEnumerable<T>` for all discovery streams.

## Prerequisites

Sprint 7 must be **complete** before starting this plan:
- Three worktree branches merged to `master`: `worktree-agent-aa743d94aacbd8963` (Task 3), `worktree-agent-aa2dc1ccab8b65f95` (Tasks 1–8), `worktree-agent-a5ec9dd6701bda6f7` (Task 9)
- Tasks 10–11 implemented (WorkspaceInitCommandHandler, WorkspaceStatusCommandHandler, WorkspaceCliModule)
- Tag `v0.7.0-sprint7` applied
- `dotnet test` passes green on `master`

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes unless inheritance is required
- `required` keyword on record properties that have no sensible default
- No `List<T>` returns in discovery paths — always `IAsyncEnumerable<T>` or `IReadOnlyList<T>`
- Connectors never reference `Ferret.Cli` (enforced by architecture tests in Task 16)
- Commit prefix per task: `feat(sprint-8):`, `test(sprint-8):`, `chore(sprint-8):`
- `dotnet build` and `dotnet test` must pass green before every commit
- `ConnectorCapabilities` (Sprint 7 class with CanRead/CanWrite) is renamed to `ConnectorIoCapabilities` in Task 1

---

## File Inventory

### New Source Files

| File | Project |
|---|---|
| `src/Ferret.Core/Connectors/ConnectorId.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/ConnectorInstanceId.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/AssetId.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/AssetKind.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/AssetDescriptor.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/AssetFingerprint.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/AssetDiscoveryOptions.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/ConnectorCapability.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/ConnectorCapabilities.cs` | Ferret.Core (static singletons) |
| `src/Ferret.Core/Connectors/ConnectorDescriptor.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/ConnectorStatus.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/IConnectorSession.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/IAssetSource.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/IIgnoreProvider.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/IConnectorFactory.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/IConnectorRegistry.cs` | Ferret.Core |
| `src/Ferret.Core/Connectors/IConnectorManager.cs` | Ferret.Core (stub) |
| `src/Ferret.Core/Connectors/IAssetEnricher.cs` | Ferret.Core (stub) |
| `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj` | new project |
| `src/Ferret.ConnectorPlatform/Properties/AssemblyInfo.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/ConnectorRegistry.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/RegistryBuilder.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/ConnectorInstance.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/ViewModels/ConnectorListItem.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/ViewModels/ConnectorListResult.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/ViewModels/ConnectorInfoView.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/Formatting/TextConnectorListFormatter.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/Formatting/TextConnectorInfoFormatter.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorListCommandHandler.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorInfoCommandHandler.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.ConnectorPlatform/ConnectorCliModule.cs` | Ferret.ConnectorPlatform |
| `src/Ferret.Connectors.Filesystem/Ferret.Connectors.Filesystem.csproj` | new project |
| `src/Ferret.Connectors.Filesystem/Properties/AssemblyInfo.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/FilesystemConnectorConfiguration.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/FilesystemConnectorSession.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/Ignore/GitIgnoreProvider.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/Ignore/FerretIgnoreProvider.cs` | Ferret.Connectors.Filesystem |
| `src/Ferret.Connectors.Filesystem/Ignore/CompositeIgnoreProvider.cs` | Ferret.Connectors.Filesystem |

### Modified Source Files

| File | Change |
|---|---|
| `src/Ferret.Core/Connectors/ConnectorCapabilities.cs` | Rename class `ConnectorCapabilities` → `ConnectorIoCapabilities`; rename file |
| `src/Ferret.Core/Connectors/IConnector.cs` | `ConnectAsync` returns `Task<IConnectorSession>`; `Capabilities` type → `ConnectorIoCapabilities` |
| `src/Ferret.Cli/Cli/CommandDefinition.cs` | Add `Arguments` parameter + `WithArgument(...)` helper |
| `src/Ferret.Cli/Cli/IFerretContext.cs` | Add `GetArgument<T>(string name)` |
| `src/Ferret.Cli/Cli/FerretContext.cs` | Implement `GetArgument<T>`; parse positional args |
| `src/Ferret.Cli/Commands/RootCommandFactory.cs` | Wire positional arguments |
| `src/Ferret.Cli/Program.cs` | Add `WorkspaceCliModule`, `ConnectorCliModule` |
| `src/Ferret.Cli/Ferret.Cli.csproj` | Add references to Ferret.ConnectorPlatform, Ferret.Connectors.Filesystem |
| `src/Ferret.sln` | Add 5 new projects |
| `docs/000-Overview/PROJECT-STATE.md` | Sprint 8 current sprint |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.Core.Tests/Connectors/TypedIdTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/AssetDescriptorTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/AssetFingerprintTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/ConnectorCapabilityTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/ConnectorDescriptorTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/` (new project) | — |
| `tests/Ferret.ConnectorPlatform.Tests/ConnectorRegistryTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/RegistryBuilderTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/TextConnectorListFormatterTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/` (new project) | — |
| `tests/Ferret.Connectors.Filesystem.Tests/TempDirectory.cs` | Ferret.Connectors.Filesystem.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorHealthTests.cs` | Ferret.Connectors.Filesystem.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorDiscoveryTests.cs` | Ferret.Connectors.Filesystem.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/GitIgnoreProviderTests.cs` | Ferret.Connectors.Filesystem.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/CompositeIgnoreProviderTests.cs` | Ferret.Connectors.Filesystem.Tests |
| `tests/Ferret.Architecture.Tests/` (new project) | — |
| `tests/Ferret.Architecture.Tests/ConnectorPlatformArchitectureTests.cs` | Ferret.Architecture.Tests |
| `tests/Ferret.Cli.Tests/Commands/ConnectorListCommandHandlerTests.cs` | Ferret.Cli.Tests |
| `tests/Ferret.Cli.Tests/Commands/ConnectorInfoCommandHandlerTests.cs` | Ferret.Cli.Tests |
| `tests/Ferret.Integration.Tests/ConnectorListE2ETests.cs` | Ferret.Integration.Tests |
| `tests/Ferret.Integration.Tests/ConnectorInfoE2ETests.cs` | Ferret.Integration.Tests |

---

### Task 1: Rename ConnectorCapabilities → ConnectorIoCapabilities + Update IConnector

**Why first:** `ConnectorCapabilities` (Sprint 7 class: CanRead/CanWrite/CanStream) conflicts with the Sprint 8 `ConnectorCapabilities` static singletons class. Rename must happen before any Sprint 8 types are added.

**Files:**
- Rename: `src/Ferret.Core/Connectors/ConnectorCapabilities.cs` → `ConnectorIoCapabilities.cs`
- Modify: `src/Ferret.Core/Connectors/IConnector.cs`
- Modify: `tests/Ferret.Core.Tests/Connectors/ConnectorContractTests.cs`

**Interfaces:**
- Produces: `ConnectorIoCapabilities` (replaces `ConnectorCapabilities` on `IConnector.Capabilities`)
- Produces: `IConnector.ConnectAsync()` returning `Task<IConnectorSession>` (not `Task<bool>`)

- [ ] **Step 1: Rename the file and class**

In `src/Ferret.Core/Connectors/`, rename `ConnectorCapabilities.cs` to `ConnectorIoCapabilities.cs`. Replace the class declaration:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>Describes the raw I/O operations a connector supports.</summary>
public sealed class ConnectorIoCapabilities
{
    private ConnectorIoCapabilities(bool canRead, bool canWrite, bool canStream, bool supportsChangeDetection)
    {
        CanRead = canRead;
        CanWrite = canWrite;
        CanStream = canStream;
        SupportsChangeDetection = supportsChangeDetection;
    }

    /// <summary>Gets a value indicating whether this connector can read content.</summary>
    public bool CanRead { get; }

    /// <summary>Gets a value indicating whether this connector can write content.</summary>
    public bool CanWrite { get; }

    /// <summary>Gets a value indicating whether this connector supports streaming.</summary>
    public bool CanStream { get; }

    /// <summary>Gets a value indicating whether this connector can detect changes since last sync.</summary>
    public bool SupportsChangeDetection { get; }

    /// <summary>Creates a <see cref="ConnectorIoCapabilities"/> with explicit values.</summary>
    public static ConnectorIoCapabilities Create(bool canRead, bool canWrite, bool canStream, bool supportsChangeDetection) =>
        new(canRead, canWrite, canStream, supportsChangeDetection);

    /// <summary>Creates a read-only <see cref="ConnectorIoCapabilities"/>.</summary>
    public static ConnectorIoCapabilities ReadOnly() => new(true, false, false, false);
}
```

- [ ] **Step 2: Update IConnector**

Replace `src/Ferret.Core/Connectors/IConnector.cs` fully:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>Contract for all ContextOS context source connectors.</summary>
public interface IConnector
{
    /// <summary>Gets the connector type category.</summary>
    ConnectorType ConnectorType { get; }

    /// <summary>Gets the connector metadata.</summary>
    ConnectorMetadata Metadata { get; }

    /// <summary>Gets the connector's declared I/O capabilities.</summary>
    ConnectorIoCapabilities Capabilities { get; }

    /// <summary>Returns the current health of this connector.</summary>
    Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default);

    /// <summary>Establishes a connection and returns an active session.</summary>
    /// <returns>An <see cref="IConnectorSession"/> that must be disposed when done.</returns>
    Task<IConnectorSession> ConnectAsync(CancellationToken ct = default);

    /// <summary>Closes the connection to the underlying source.</summary>
    Task DisconnectAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Update ConnectorContractTests**

In `tests/Ferret.Core.Tests/Connectors/ConnectorContractTests.cs`, rename all references from `ConnectorCapabilities` to `ConnectorIoCapabilities`:

```csharp
[Fact]
public void ConnectorIoCapabilities_Create_StoresValues()
{
    var caps = ConnectorIoCapabilities.Create(canRead: true, canWrite: false, canStream: true, supportsChangeDetection: true);
    Assert.True(caps.CanRead);
    Assert.False(caps.CanWrite);
    Assert.True(caps.SupportsChangeDetection);
}

[Fact]
public void ConnectorIoCapabilities_ReadOnly_OnlyCanRead()
{
    var caps = ConnectorIoCapabilities.ReadOnly();
    Assert.True(caps.CanRead);
    Assert.False(caps.CanWrite);
    Assert.False(caps.CanStream);
    Assert.False(caps.SupportsChangeDetection);
}
```

- [ ] **Step 4: Build and test**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Core.Tests
```

Expected: all existing tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Core/Connectors/ConnectorIoCapabilities.cs
git add src/Ferret.Core/Connectors/IConnector.cs
git add tests/Ferret.Core.Tests/Connectors/ConnectorContractTests.cs
git commit -m "chore(sprint-8): rename ConnectorCapabilities→ConnectorIoCapabilities; IConnector.ConnectAsync returns IConnectorSession"
```

---

### Task 2: Core Contracts — Typed IDs

**Files:**
- Create: `src/Ferret.Core/Connectors/ConnectorId.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorInstanceId.cs`
- Create: `src/Ferret.Core/Connectors/AssetId.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/TypedIdTests.cs`

**Interfaces:**
- Produces: `ConnectorId`, `ConnectorInstanceId`, `AssetId` — all follow value-equality record pattern

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Connectors/TypedIdTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class TypedIdTests
{
    [Fact]
    public void ConnectorId_Equality_By_Value()
    {
        var a = new ConnectorId("filesystem");
        var b = new ConnectorId("filesystem");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ConnectorId_Inequality_Different_Value()
    {
        Assert.NotEqual(new ConnectorId("filesystem"), new ConnectorId("git"));
    }

    [Fact]
    public void ConnectorId_ToString_Returns_Value()
    {
        Assert.Equal("filesystem", new ConnectorId("filesystem").ToString());
    }

    [Fact]
    public void ConnectorInstanceId_Equality_By_Value()
    {
        Assert.Equal(new ConnectorInstanceId("src-root"), new ConnectorInstanceId("src-root"));
    }

    [Fact]
    public void AssetId_From_Uri_Is_Deterministic()
    {
        var uri = new Uri("filesystem:///src/Program.cs");
        Assert.Equal(AssetId.From(uri), AssetId.From(uri));
    }

    [Fact]
    public void AssetId_From_Different_Uris_Are_Not_Equal()
    {
        var a = AssetId.From(new Uri("filesystem:///src/Program.cs"));
        var b = AssetId.From(new Uri("filesystem:///src/Other.cs"));
        Assert.NotEqual(a, b);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "TypedIdTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Implement**

`src/Ferret.Core/Connectors/ConnectorId.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Strongly-typed identifier for a connector type (e.g. "filesystem").</summary>
/// <param name="Value">The raw string value.</param>
public sealed record ConnectorId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Connectors/ConnectorInstanceId.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Strongly-typed identifier for a workspace-scoped connector instance (e.g. "src-root").</summary>
/// <param name="Value">The raw string value.</param>
public sealed record ConnectorInstanceId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Connectors/AssetId.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Strongly-typed identifier for an asset, derived from its CanonicalUri.</summary>
/// <param name="Value">The canonical URI string.</param>
public sealed record AssetId(string Value)
{
    /// <summary>Derives an <see cref="AssetId"/> from a canonical URI.</summary>
    /// <param name="canonicalUri">The asset's canonical URI.</param>
    /// <returns>A deterministic <see cref="AssetId"/>.</returns>
    public static AssetId From(Uri canonicalUri) => new(canonicalUri.ToString());

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "TypedIdTests"
```

Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Core/Connectors/ConnectorId.cs src/Ferret.Core/Connectors/ConnectorInstanceId.cs src/Ferret.Core/Connectors/AssetId.cs tests/Ferret.Core.Tests/Connectors/TypedIdTests.cs
git commit -m "feat(sprint-8): ConnectorId, ConnectorInstanceId, AssetId typed value object IDs"
```

---

### Task 3: Core Contracts — Asset Model

**Files:**
- Create: `src/Ferret.Core/Connectors/AssetKind.cs`
- Create: `src/Ferret.Core/Connectors/AssetFingerprint.cs`
- Create: `src/Ferret.Core/Connectors/IIgnoreProvider.cs`
- Create: `src/Ferret.Core/Connectors/AssetDescriptor.cs`
- Create: `src/Ferret.Core/Connectors/AssetDiscoveryOptions.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/AssetDescriptorTests.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/AssetFingerprintTests.cs`

**Interfaces:**
- Produces: `AssetDescriptor`, `AssetFingerprint`, `IIgnoreProvider`, `AssetDiscoveryOptions` — consumed by Tasks 7, 9, 10

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Connectors/AssetDescriptorTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class AssetDescriptorTests
{
    [Fact]
    public void AssetDescriptor_CanonicalUri_Is_Preserved()
    {
        var uri = new Uri("filesystem:///src/Program.cs");
        var desc = MakeDescriptor(uri);
        Assert.Equal(uri, desc.CanonicalUri);
    }

    [Fact]
    public void AssetDescriptor_Metadata_Defaults_To_Empty()
    {
        var desc = MakeDescriptor(new Uri("filesystem:///src/A.cs"));
        Assert.Empty(desc.Metadata);
    }

    [Fact]
    public void AssetDescriptor_Id_Matches_CanonicalUri()
    {
        var uri = new Uri("filesystem:///src/Program.cs");
        var desc = MakeDescriptor(uri);
        Assert.Equal(AssetId.From(uri), desc.Id);
    }

    private static AssetDescriptor MakeDescriptor(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = "Program.cs",
        LastModified = DateTimeOffset.UtcNow,
    };
}
```

Create `tests/Ferret.Core.Tests/Connectors/AssetFingerprintTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class AssetFingerprintTests
{
    [Fact]
    public void CreateLightweight_Is_Deterministic()
    {
        var t = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        var a = AssetFingerprint.CreateLightweight(t, 1024);
        var b = AssetFingerprint.CreateLightweight(t, 1024);
        Assert.Equal(a, b);
    }

    [Fact]
    public void CreateLightweight_Differs_For_Different_Size()
    {
        var t = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        var a = AssetFingerprint.CreateLightweight(t, 1024);
        var b = AssetFingerprint.CreateLightweight(t, 2048);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void CreateLightweight_Algorithm_Is_Lightweight()
    {
        var fp = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);
        Assert.Equal("lightweight", fp.Algorithm);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "AssetDescriptorTests|AssetFingerprintTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement**

`src/Ferret.Core/Connectors/AssetKind.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Classifies the kind of an asset.</summary>
public enum AssetKind
{
    /// <summary>A regular file.</summary>
    File = 0,

    /// <summary>A directory.</summary>
    Directory = 1,
}
```

`src/Ferret.Core/Connectors/AssetFingerprint.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Opaque fingerprint used for change detection. Never expose the raw value directly.</summary>
/// <param name="Algorithm">The algorithm used to produce this fingerprint (e.g. "lightweight", "sha256").</param>
/// <param name="Value">The opaque fingerprint value.</param>
public sealed record AssetFingerprint(string Algorithm, string Value)
{
    /// <summary>Creates a lightweight fingerprint from last-write-time and file size. No I/O required.</summary>
    /// <param name="lastWrite">The file's last-write timestamp.</param>
    /// <param name="sizeBytes">The file size in bytes.</param>
    /// <returns>A deterministic lightweight fingerprint.</returns>
    public static AssetFingerprint CreateLightweight(DateTimeOffset lastWrite, long sizeBytes) =>
        new("lightweight", $"{lastWrite.ToUnixTimeMilliseconds()}:{sizeBytes}");
}
```

`src/Ferret.Core/Connectors/IIgnoreProvider.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// Determines whether an asset should be excluded from discovery.
/// Implementations MUST return false for URI schemes they do not understand.
/// ShouldIgnore is pure — no I/O, no state mutation.
/// </summary>
public interface IIgnoreProvider
{
    /// <summary>Returns true if the asset should be excluded from discovery results.</summary>
    /// <param name="asset">The asset to evaluate.</param>
    /// <returns>True to exclude; false to include.</returns>
    bool ShouldIgnore(AssetDescriptor asset);
}
```

`src/Ferret.Core/Connectors/AssetDescriptor.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// Universal connector-agnostic asset abstraction — the lingua franca of ContextOS.
/// Every connector produces AssetDescriptors. Every pipeline stage consumes them.
/// </summary>
public sealed record AssetDescriptor
{
    /// <summary>Gets the stable asset identifier derived from CanonicalUri.</summary>
    public required AssetId Id { get; init; }

    /// <summary>Gets the connector type that produced this asset.</summary>
    public required ConnectorId ConnectorId { get; init; }

    /// <summary>Gets the workspace-scoped instance that produced this asset.</summary>
    public required ConnectorInstanceId InstanceId { get; init; }

    /// <summary>Gets the kind of asset.</summary>
    public required AssetKind Kind { get; init; }

    /// <summary>Gets the stable, normalized, workspace-relative canonical URI.</summary>
    public required Uri CanonicalUri { get; init; }

    /// <summary>Gets the human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the last modification timestamp.</summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>Gets the optional lightweight fingerprint for change detection.</summary>
    public AssetFingerprint? Fingerprint { get; init; }

    /// <summary>Gets the optional file size in bytes.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Gets the optional MIME type.</summary>
    public string? MediaType { get; init; }

    /// <summary>Gets connector-specific metadata key-value pairs.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
```

`src/Ferret.Core/Connectors/AssetDiscoveryOptions.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Options controlling asset discovery behaviour.</summary>
public sealed class AssetDiscoveryOptions
{
    /// <summary>Gets an optional ignore policy applied per asset during discovery.</summary>
    public IIgnoreProvider? IgnoreProvider { get; init; }

    /// <summary>Gets a shared default instance with no options set.</summary>
    public static AssetDiscoveryOptions Default { get; } = new();
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "AssetDescriptorTests|AssetFingerprintTests"
```

Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Core/Connectors/AssetKind.cs src/Ferret.Core/Connectors/AssetFingerprint.cs src/Ferret.Core/Connectors/IIgnoreProvider.cs src/Ferret.Core/Connectors/AssetDescriptor.cs src/Ferret.Core/Connectors/AssetDiscoveryOptions.cs tests/Ferret.Core.Tests/Connectors/AssetDescriptorTests.cs tests/Ferret.Core.Tests/Connectors/AssetFingerprintTests.cs
git commit -m "feat(sprint-8): asset model — AssetDescriptor, AssetFingerprint, AssetKind, IIgnoreProvider, AssetDiscoveryOptions"
```

---

### Task 4: Core Contracts — Capability Model + ConnectorDescriptor + ConnectorStatus

**Files:**
- Create: `src/Ferret.Core/Connectors/ConnectorCapability.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorCapabilities.cs` (static singletons)
- Create: `src/Ferret.Core/Connectors/ConnectorDescriptor.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorStatus.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/ConnectorCapabilityTests.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/ConnectorDescriptorTests.cs`

**Interfaces:**
- Produces: `ConnectorCapability` record, `ConnectorCapabilities` static class with `.All`, `ConnectorDescriptor`, `ConnectorStatus` — consumed by Tasks 6, 9, 11, 12

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Connectors/ConnectorCapabilityTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorCapabilityTests
{
    [Fact]
    public void AssetDiscovery_Singleton_Is_Referentially_Stable()
    {
        Assert.Same(ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.AssetDiscovery);
    }

    [Fact]
    public void All_Contains_AssetDiscovery()
    {
        Assert.Contains(ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.All);
    }

    [Fact]
    public void All_Has_Eight_Entries()
    {
        Assert.Equal(8, ConnectorCapabilities.All.Count);
    }

    [Fact]
    public void ConnectorCapability_Equality_By_Id()
    {
        var a = new ConnectorCapability("asset-discovery", "Asset Discovery", "1.0", "desc");
        var b = new ConnectorCapability("asset-discovery", "Asset Discovery", "1.0", "desc");
        Assert.Equal(a, b);
    }
}
```

Create `tests/Ferret.Core.Tests/Connectors/ConnectorDescriptorTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorDescriptorTests
{
    [Fact]
    public void ConnectorDescriptor_SupportedPlatforms_Defaults_To_Empty()
    {
        var desc = new ConnectorDescriptor
        {
            Id = new ConnectorId("filesystem"),
            Metadata = ConnectorMetadata.Create("filesystem", "Filesystem", "desc", ConnectorType.Filesystem, "1.0"),
            Capabilities = [ConnectorCapabilities.AssetDiscovery],
        };
        Assert.Empty(desc.SupportedPlatforms);
    }

    [Fact]
    public void ConnectorDescriptor_Has_No_Public_Setters()
    {
        var props = typeof(ConnectorDescriptor).GetProperties();
        Assert.All(props, p => Assert.False(p.CanWrite && (p.SetMethod?.IsPublic ?? false),
            $"Property {p.Name} must not have a public setter"));
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "ConnectorCapabilityTests|ConnectorDescriptorTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement**

`src/Ferret.Core/Connectors/ConnectorCapability.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Describes a specific capability a connector can provide. Use ConnectorCapabilities for well-known singletons.</summary>
/// <param name="Id">Unique capability identifier (e.g. "asset-discovery").</param>
/// <param name="Name">Human-readable capability name.</param>
/// <param name="Version">Semantic version of this capability.</param>
/// <param name="Description">Short description for display in CLI and dashboards.</param>
public sealed record ConnectorCapability(string Id, string Name, string Version, string Description);
```

`src/Ferret.Core/Connectors/ConnectorCapabilities.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Well-known connector capabilities as immutable singletons. Use these instead of constructing new ConnectorCapability instances.</summary>
public static class ConnectorCapabilities
{
    /// <summary>Connector can enumerate assets as AssetDescriptors.</summary>
    public static readonly ConnectorCapability AssetDiscovery =
        new("asset-discovery", "Asset Discovery", "1.0", "Enumerate files and directories as AssetDescriptors.");

    /// <summary>Connector can detect assets added, changed, or deleted since last sync.</summary>
    public static readonly ConnectorCapability ChangeDetection =
        new("change-detection", "Change Detection", "1.0", "Detect assets added, changed, or deleted since last sync.");

    /// <summary>Connector supports real-time event streaming.</summary>
    public static readonly ConnectorCapability EventStreaming =
        new("event-streaming", "Event Streaming", "1.0", "Stream change events as they occur in real time.");

    /// <summary>Connector can write back to the source.</summary>
    public static readonly ConnectorCapability Write =
        new("write", "Write Back", "1.0", "Create, update, or delete assets in the source.");

    /// <summary>Connector supports point-in-time snapshots.</summary>
    public static readonly ConnectorCapability Snapshot =
        new("snapshot", "Snapshot", "1.0", "Capture a point-in-time snapshot of all assets.");

    /// <summary>Connector exposes relationships between assets.</summary>
    public static readonly ConnectorCapability Relationships =
        new("relationships", "Relationships", "1.0", "Expose references and relationships between assets.");

    /// <summary>Connector can delegate search queries to the source's native search engine.</summary>
    public static readonly ConnectorCapability NativeSearch =
        new("native-search", "Native Search", "1.0", "Delegate search queries to the source's native engine.");

    /// <summary>Connector supports post-discovery enrichment. Reserved for Sprint 9.</summary>
    public static readonly ConnectorCapability AssetEnrichment =
        new("asset-enrichment", "Asset Enrichment", "1.0", "Enrich AssetDescriptors with additional metadata after discovery.");

    /// <summary>All well-known capabilities in definition order.</summary>
    public static IReadOnlyList<ConnectorCapability> All { get; } = [
        AssetDiscovery, ChangeDetection, EventStreaming, Write,
        Snapshot, Relationships, NativeSearch, AssetEnrichment,
    ];
}
```

`src/Ferret.Core/Connectors/ConnectorDescriptor.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Static descriptor for a registered connector type. Immutable — no public setters.</summary>
public sealed record ConnectorDescriptor
{
    /// <summary>Gets the stable connector type identifier.</summary>
    public required ConnectorId Id { get; init; }

    /// <summary>Gets the connector metadata (name, description, version).</summary>
    public required ConnectorMetadata Metadata { get; init; }

    /// <summary>Gets the capabilities this connector declares.</summary>
    public required IReadOnlyList<ConnectorCapability> Capabilities { get; init; }

    /// <summary>Gets the OS platforms this connector supports (e.g. "Linux", "macOS", "Windows").</summary>
    public IReadOnlyList<string> SupportedPlatforms { get; init; } = [];

    /// <summary>Gets an optional URI pointing to documentation for this connector.</summary>
    public string? DocumentationUri { get; init; }
}
```

`src/Ferret.Core/Connectors/ConnectorStatus.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Current runtime state of a connector instance. Never used as configuration.</summary>
public sealed record ConnectorStatus
{
    /// <summary>Gets the connector type identifier.</summary>
    public required ConnectorId ConnectorId { get; init; }

    /// <summary>Gets the workspace-scoped instance identifier.</summary>
    public required ConnectorInstanceId InstanceId { get; init; }

    /// <summary>Gets a value indicating whether the connector is currently active.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Gets the current connector health.</summary>
    public required ConnectorHealth Health { get; init; }

    /// <summary>Gets the time of the last successful sync, or null if never synced.</summary>
    public DateTimeOffset? LastSyncAt { get; init; }

    /// <summary>Gets the current error message, if any.</summary>
    public string? CurrentError { get; init; }
}
```

Also add two reserved stubs:

`src/Ferret.Core/Connectors/IConnectorManager.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Activates and manages connector instances. Reserved for Sprint 10.</summary>
public interface IConnectorManager
{
    // Sprint 10: Task<IConnectorSession> ActivateAsync(ConnectorInstanceId id, CancellationToken ct = default);
}
```

`src/Ferret.Core/Connectors/IAssetEnricher.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Enriches an AssetDescriptor with additional metadata after discovery. Reserved for Sprint 9.</summary>
public interface IAssetEnricher
{
    // Sprint 9: ValueTask<AssetDescriptor> EnrichAsync(AssetDescriptor asset, CancellationToken ct = default);
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "ConnectorCapabilityTests|ConnectorDescriptorTests"
dotnet test tests/Ferret.Core.Tests
```

Expected: all tests pass including Tasks 1–3 tests.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Core/Connectors/ConnectorCapability.cs src/Ferret.Core/Connectors/ConnectorCapabilities.cs src/Ferret.Core/Connectors/ConnectorDescriptor.cs src/Ferret.Core/Connectors/ConnectorStatus.cs src/Ferret.Core/Connectors/IConnectorManager.cs src/Ferret.Core/Connectors/IAssetEnricher.cs tests/Ferret.Core.Tests/Connectors/ConnectorCapabilityTests.cs tests/Ferret.Core.Tests/Connectors/ConnectorDescriptorTests.cs
git commit -m "feat(sprint-8): capability model — ConnectorCapability, ConnectorCapabilities singletons, ConnectorDescriptor, ConnectorStatus"
```

---

### Task 5: Core Contracts — IConnectorSession + IAssetSource + IConnectorFactory + IConnectorRegistry

**Files:**
- Create: `src/Ferret.Core/Connectors/IConnectorSession.cs`
- Create: `src/Ferret.Core/Connectors/IAssetSource.cs`
- Create: `src/Ferret.Core/Connectors/IConnectorFactory.cs`
- Create: `src/Ferret.Core/Connectors/IConnectorRegistry.cs`

No new tests — these are interface stubs consumed by later tasks. Verified by `dotnet build`.

- [ ] **Step 1: Create IConnectorSession**

`src/Ferret.Core/Connectors/IConnectorSession.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Represents an active connection to a data source. Dispose to release runtime resources.</summary>
public interface IConnectorSession : IAsyncDisposable
{
    /// <summary>Gets the workspace-scoped instance identifier this session belongs to.</summary>
    ConnectorInstanceId InstanceId { get; }
}
```

- [ ] **Step 2: Create IAssetSource**

`src/Ferret.Core/Connectors/IAssetSource.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// A connector capability that discovers assets from a source.
/// Implementors MUST stream — never buffer into List before yielding.
/// </summary>
public interface IAssetSource
{
    /// <summary>Discovers assets, streaming results as they are found.</summary>
    /// <param name="options">Options controlling discovery behaviour (ignore policy, etc.).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async stream of discovered assets. Memory usage is O(batch), not O(corpus).</returns>
    IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        AssetDiscoveryOptions options,
        CancellationToken ct = default);
}
```

- [ ] **Step 3: Create IConnectorFactory**

`src/Ferret.Core/Connectors/IConnectorFactory.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Creates connector instances and exposes the static descriptor for registration.</summary>
public interface IConnectorFactory
{
    /// <summary>Gets the connector type identifier this factory produces.</summary>
    ConnectorId ConnectorId { get; }

    /// <summary>Gets the static descriptor for the connector type this factory produces.</summary>
    ConnectorDescriptor Descriptor { get; }

    /// <summary>Creates a configured connector for the given workspace instance.</summary>
    /// <param name="instanceId">The workspace-scoped instance identifier.</param>
    /// <returns>A connector ready for use.</returns>
    IConnector Create(ConnectorInstanceId instanceId);
}
```

- [ ] **Step 4: Create IConnectorRegistry**

`src/Ferret.Core/Connectors/IConnectorRegistry.cs`:
```csharp
namespace Ferret.Core.Connectors;

/// <summary>Read-only registry of all discovered (DI-registered) connector descriptors.</summary>
public interface IConnectorRegistry
{
    /// <summary>Returns all registered connector descriptors.</summary>
    IReadOnlyList<ConnectorDescriptor> GetAll();

    /// <summary>Returns the descriptor for the given connector ID, or null if not registered.</summary>
    ConnectorDescriptor? GetById(ConnectorId id);

    /// <summary>Returns true if a connector with the given ID is registered.</summary>
    bool IsRegistered(ConnectorId id);

    /// <summary>Returns all connectors that declare the given capability.</summary>
    IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability);
}
```

- [ ] **Step 5: Build**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Core/Connectors/IConnectorSession.cs src/Ferret.Core/Connectors/IAssetSource.cs src/Ferret.Core/Connectors/IConnectorFactory.cs src/Ferret.Core/Connectors/IConnectorRegistry.cs
git commit -m "feat(sprint-8): core interfaces — IConnectorSession, IAssetSource, IConnectorFactory, IConnectorRegistry"
```

---

### Task 6: Ferret.ConnectorPlatform — Project Scaffold + ConnectorRegistry + RegistryBuilder

**Files:**
- Create: `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj`
- Create: `src/Ferret.ConnectorPlatform/Properties/AssemblyInfo.cs`
- Create: `src/Ferret.ConnectorPlatform/ConnectorRegistry.cs`
- Create: `src/Ferret.ConnectorPlatform/RegistryBuilder.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/Ferret.ConnectorPlatform.Tests.csproj`
- Create: `tests/Ferret.ConnectorPlatform.Tests/ConnectorRegistryTests.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/RegistryBuilderTests.cs`
- Modify: `src/Ferret.sln` (add both projects)

**Interfaces:**
- Consumes: `IConnectorFactory`, `IConnectorRegistry`, `ConnectorDescriptor`, `ConnectorCapability` from Task 5/4
- Produces: `ConnectorRegistry`, `RegistryBuilder` — consumed by Task 11 (ConnectorCliModule)

- [ ] **Step 1: Create project files**

`src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.ConnectorPlatform</AssemblyName>
    <RootNamespace>Ferret.ConnectorPlatform</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>
</Project>
```

`src/Ferret.ConnectorPlatform/Properties/AssemblyInfo.cs`:
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.ConnectorPlatform.Tests")]
```

`tests/Ferret.ConnectorPlatform.Tests/Ferret.ConnectorPlatform.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.ConnectorPlatform.Tests</AssemblyName>
    <RootNamespace>Ferret.ConnectorPlatform.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ConnectorPlatform\Ferret.ConnectorPlatform.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

Add both projects to `src/Ferret.sln` using:
```
dotnet sln src/Ferret.sln add src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj
dotnet sln src/Ferret.sln add tests/Ferret.ConnectorPlatform.Tests/Ferret.ConnectorPlatform.Tests.csproj
```

- [ ] **Step 2: Write failing tests**

Create `tests/Ferret.ConnectorPlatform.Tests/Fakes/FakeConnectorFactory.cs` (shared helper for this and later tasks):
```csharp
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.Tests.Fakes;

internal sealed class FakeConnectorFactory : IConnectorFactory
{
    internal FakeConnectorFactory(string id, params ConnectorCapability[] capabilities)
    {
        ConnectorId = new ConnectorId(id);
        Descriptor = new ConnectorDescriptor
        {
            Id = ConnectorId,
            Metadata = ConnectorMetadata.Create(id, id, $"{id} connector", ConnectorType.Custom, "1.0"),
            Capabilities = capabilities,
            SupportedPlatforms = ["Linux", "macOS", "Windows"],
        };
    }

    public ConnectorId ConnectorId { get; }
    public ConnectorDescriptor Descriptor { get; }

    public IConnector Create(ConnectorInstanceId instanceId) =>
        throw new NotImplementedException("FakeConnectorFactory does not create connectors.");
}
```

Create `tests/Ferret.ConnectorPlatform.Tests/ConnectorRegistryTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Ferret.ConnectorPlatform.Tests.Fakes;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class ConnectorRegistryTests
{
    [Fact]
    public void GetAll_Returns_All_Registered_Descriptors()
    {
        var registry = RegistryBuilder.Build([
            new FakeConnectorFactory("filesystem", ConnectorCapabilities.AssetDiscovery),
            new FakeConnectorFactory("git", ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.ChangeDetection),
        ]);

        Assert.Equal(2, registry.GetAll().Count);
    }

    [Fact]
    public void GetById_Returns_Descriptor_For_Known_Id()
    {
        var registry = RegistryBuilder.Build([
            new FakeConnectorFactory("filesystem", ConnectorCapabilities.AssetDiscovery),
        ]);

        var desc = registry.GetById(new ConnectorId("filesystem"));
        Assert.NotNull(desc);
        Assert.Equal("filesystem", desc.Id.Value);
    }

    [Fact]
    public void GetById_Returns_Null_For_Unknown_Id()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.Null(registry.GetById(new ConnectorId("unknown")));
    }

    [Fact]
    public void IsRegistered_Returns_True_For_Known_Id()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.True(registry.IsRegistered(new ConnectorId("filesystem")));
    }

    [Fact]
    public void IsRegistered_Returns_False_For_Unknown_Id()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.False(registry.IsRegistered(new ConnectorId("git")));
    }

    [Fact]
    public void GetByCapability_Returns_Matching_Descriptors()
    {
        var registry = RegistryBuilder.Build([
            new FakeConnectorFactory("filesystem", ConnectorCapabilities.AssetDiscovery),
            new FakeConnectorFactory("git", ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.ChangeDetection),
            new FakeConnectorFactory("slack"),
        ]);

        var results = registry.GetByCapability(ConnectorCapabilities.ChangeDetection);
        Assert.Single(results);
        Assert.Equal("git", results[0].Id.Value);
    }
}
```

Create `tests/Ferret.ConnectorPlatform.Tests/RegistryBuilderTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Ferret.ConnectorPlatform.Tests.Fakes;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class RegistryBuilderTests
{
    [Fact]
    public void Build_Returns_Registry_With_All_Factories()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.Equal(1, registry.GetAll().Count);
    }

    [Fact]
    public void Build_Empty_Returns_Empty_Registry()
    {
        var registry = RegistryBuilder.Build([]);
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void Build_Throws_On_Duplicate_ConnectorId()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RegistryBuilder.Build([
                new FakeConnectorFactory("filesystem"),
                new FakeConnectorFactory("filesystem"),
            ]));
    }
}
```

- [ ] **Step 3: Confirm red**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests
```

Expected: FAIL — `RegistryBuilder` not found.

- [ ] **Step 4: Implement**

`src/Ferret.ConnectorPlatform/ConnectorRegistry.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform;

/// <summary>Immutable registry of connector descriptors. Built once via RegistryBuilder.</summary>
internal sealed class ConnectorRegistry : IConnectorRegistry
{
    private readonly IReadOnlyDictionary<ConnectorId, ConnectorDescriptor> _descriptors;

    internal ConnectorRegistry(IReadOnlyDictionary<ConnectorId, ConnectorDescriptor> descriptors) =>
        _descriptors = descriptors;

    /// <inheritdoc/>
    public IReadOnlyList<ConnectorDescriptor> GetAll() => [.. _descriptors.Values];

    /// <inheritdoc/>
    public ConnectorDescriptor? GetById(ConnectorId id) =>
        _descriptors.GetValueOrDefault(id);

    /// <inheritdoc/>
    public bool IsRegistered(ConnectorId id) => _descriptors.ContainsKey(id);

    /// <inheritdoc/>
    public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) =>
        [.. _descriptors.Values.Where(d => d.Capabilities.Contains(capability))];
}
```

`src/Ferret.ConnectorPlatform/RegistryBuilder.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform;

/// <summary>Builds an IConnectorRegistry from IConnectorFactory instances. Does not depend on DI.</summary>
public static class RegistryBuilder
{
    /// <summary>Builds an immutable registry from the provided factories.</summary>
    /// <param name="factories">The connector factories to register.</param>
    /// <returns>An immutable <see cref="IConnectorRegistry"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if two factories share the same ConnectorId.</exception>
    public static IConnectorRegistry Build(IEnumerable<IConnectorFactory> factories)
    {
        var dict = new Dictionary<ConnectorId, ConnectorDescriptor>();
        foreach (var factory in factories)
        {
            if (!dict.TryAdd(factory.ConnectorId, factory.Descriptor))
            {
                throw new InvalidOperationException(
                    $"Duplicate connector ID: '{factory.ConnectorId.Value}'. Each connector must have a unique ID.");
            }
        }

        return new ConnectorRegistry(dict);
    }
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```
git add src/Ferret.ConnectorPlatform/ tests/Ferret.ConnectorPlatform.Tests/ src/Ferret.sln
git commit -m "feat(sprint-8): Ferret.ConnectorPlatform — ConnectorRegistry, RegistryBuilder"
```

---

### Task 7: Ferret.Connectors.Filesystem — Project Scaffold + FilesystemConnector (IConnector)

**Files:**
- Create: `src/Ferret.Connectors.Filesystem/Ferret.Connectors.Filesystem.csproj`
- Create: `src/Ferret.Connectors.Filesystem/Properties/AssemblyInfo.cs`
- Create: `src/Ferret.Connectors.Filesystem/FilesystemConnectorConfiguration.cs`
- Create: `src/Ferret.Connectors.Filesystem/FilesystemConnectorSession.cs`
- Create: `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` (IConnector only in this task)
- Create: `tests/Ferret.Connectors.Filesystem.Tests/Ferret.Connectors.Filesystem.Tests.csproj`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/TempDirectory.cs`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorHealthTests.cs`
- Modify: `src/Ferret.sln` (add both projects)

**Interfaces:**
- Consumes: `IConnector`, `IConnectorSession`, `ConnectorIoCapabilities`, `ConnectorMetadata`, `ConnectorHealth`, `ConnectorType`
- Produces: `FilesystemConnector`, `FilesystemConnectorConfiguration`, `FilesystemConnectorSession` — IAssetSource added in Task 8

- [ ] **Step 1: Create project files**

`src/Ferret.Connectors.Filesystem/Ferret.Connectors.Filesystem.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.Connectors.Filesystem</AssemblyName>
    <RootNamespace>Ferret.Connectors.Filesystem</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>
</Project>
```

`src/Ferret.Connectors.Filesystem/Properties/AssemblyInfo.cs`:
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.Connectors.Filesystem.Tests")]
```

`tests/Ferret.Connectors.Filesystem.Tests/Ferret.Connectors.Filesystem.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.Connectors.Filesystem.Tests</AssemblyName>
    <RootNamespace>Ferret.Connectors.Filesystem.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Connectors.Filesystem\Ferret.Connectors.Filesystem.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

Add to solution:
```
dotnet sln src/Ferret.sln add src/Ferret.Connectors.Filesystem/Ferret.Connectors.Filesystem.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Connectors.Filesystem.Tests/Ferret.Connectors.Filesystem.Tests.csproj
```

- [ ] **Step 2: Write failing health tests**

`tests/Ferret.Connectors.Filesystem.Tests/TempDirectory.cs`:
```csharp
namespace Ferret.Connectors.Filesystem.Tests;

internal sealed class TempDirectory : IDisposable
{
    internal TempDirectory() => Directory.CreateDirectory(Path);

    internal string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "ferret-fs-tests-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
```

`tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorHealthTests.cs`:
```csharp
using Ferret.Connectors.Filesystem;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorHealthTests
{
    [Fact]
    public async Task GetHealthAsync_Returns_Connected_When_Path_Exists()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);

        var health = await connector.GetHealthAsync();

        Assert.True(health.IsConnected);
        Assert.Null(health.ErrorMessage);
    }

    [Fact]
    public async Task GetHealthAsync_Returns_Disconnected_When_Path_Missing()
    {
        var connector = MakeConnector(@"C:\does\not\exist\ever");

        var health = await connector.GetHealthAsync();

        Assert.False(health.IsConnected);
        Assert.NotNull(health.ErrorMessage);
    }

    [Fact]
    public async Task ConnectAsync_Returns_Session_When_Path_Exists()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);

        await using var session = await connector.ConnectAsync();

        Assert.NotNull(session);
    }

    [Fact]
    public async Task ConnectAsync_Session_DisposeAsync_Does_Not_Throw()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);
        var session = await connector.ConnectAsync();

        var ex = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task DisconnectAsync_Does_Not_Throw()
    {
        using var dir = new TempDirectory();
        var connector = MakeConnector(dir.Path);

        var ex = await Record.ExceptionAsync(() => connector.DisconnectAsync());

        Assert.Null(ex);
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath });
}
```

- [ ] **Step 3: Confirm red**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorHealthTests"
```

Expected: FAIL — types not found.

- [ ] **Step 4: Implement**

`src/Ferret.Connectors.Filesystem/FilesystemConnectorConfiguration.cs`:
```csharp
namespace Ferret.Connectors.Filesystem;

/// <summary>Configuration for a FilesystemConnector instance.</summary>
public sealed class FilesystemConnectorConfiguration
{
    /// <summary>Gets or sets the root directory path to discover from. Defaults to current directory.</summary>
    public string RootPath { get; init; } = ".";

    /// <summary>Gets or sets file extensions to include (empty = all extensions).</summary>
    public IReadOnlyList<string> IncludeExtensions { get; init; } = [];

    /// <summary>Gets or sets file extensions to exclude.</summary>
    public IReadOnlyList<string> ExcludeExtensions { get; init; } = [];
}
```

`src/Ferret.Connectors.Filesystem/FilesystemConnectorSession.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem;

/// <summary>No-op session for the filesystem connector — the filesystem has no persistent connection.</summary>
internal sealed class FilesystemConnectorSession : IConnectorSession
{
    internal FilesystemConnectorSession(ConnectorInstanceId instanceId) =>
        InstanceId = instanceId;

    /// <inheritdoc/>
    public ConnectorInstanceId InstanceId { get; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

`src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` (IConnector only — IAssetSource added in Task 8):
```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem;

/// <summary>Discovers files and directories from the local filesystem.</summary>
public sealed class FilesystemConnector : IConnector
{
    private static readonly ConnectorInstanceId DefaultInstanceId = new("filesystem-default");

    private readonly FilesystemConnectorConfiguration _config;

    /// <summary>Initialises a new FilesystemConnector with the given configuration.</summary>
    public FilesystemConnector(FilesystemConnectorConfiguration config) => _config = config;

    /// <inheritdoc/>
    public ConnectorType ConnectorType => ConnectorType.Filesystem;

    /// <inheritdoc/>
    public ConnectorMetadata Metadata { get; } = ConnectorMetadata.Create(
        "filesystem", "Filesystem Connector",
        "Discovers files and directories from the local filesystem.",
        ConnectorType.Filesystem, "1.0");

    /// <inheritdoc/>
    public ConnectorIoCapabilities Capabilities { get; } = ConnectorIoCapabilities.ReadOnly();

    /// <inheritdoc/>
    public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_config.RootPath))
        {
            return Task.FromResult(
                ConnectorHealth.Disconnected($"Root path does not exist: {_config.RootPath}", DateTimeOffset.UtcNow));
        }

        try
        {
            _ = Directory.GetFileSystemEntries(_config.RootPath);
            return Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ConnectorHealth.Disconnected(ex.Message, DateTimeOffset.UtcNow));
        }
    }

    /// <inheritdoc/>
    public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default)
    {
        IConnectorSession session = new FilesystemConnectorSession(DefaultInstanceId);
        return Task.FromResult(session);
    }

    /// <inheritdoc/>
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorHealthTests"
```

Expected: 5 tests pass.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Connectors.Filesystem/ tests/Ferret.Connectors.Filesystem.Tests/ src/Ferret.sln
git commit -m "feat(sprint-8): Ferret.Connectors.Filesystem — scaffold, FilesystemConnector IConnector, session, configuration"
```

---

### Task 8: FilesystemConnector — IAssetSource + CanonicalUri + Discovery Tests

**Files:**
- Modify: `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` (add IAssetSource)
- Create: `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorDiscoveryTests.cs`

**Interfaces:**
- Consumes: `IAssetSource`, `AssetDescriptor`, `AssetDiscoveryOptions`, `AssetId`, `AssetKind`, `AssetFingerprint`
- Produces: `FilesystemConnector : IConnector, IAssetSource` — consumed by Task 11 (factory)

**CanonicalUri rules:** `filesystem:///relative/path/to/file` — workspace-relative, forward slashes, NFC normalized, no trailing slash on files.

- [ ] **Step 1: Write failing discovery tests**

`tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorDiscoveryTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Ferret.Connectors.Filesystem;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorDiscoveryTests
{
    [Fact]
    public async Task DiscoverAsync_Yields_Files_In_Root()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "a.cs"), "class A {}");
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "b.cs"), "class B {}");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "a.cs");
        Assert.Contains(results, r => r.DisplayName == "b.cs");
    }

    [Fact]
    public async Task DiscoverAsync_Yields_Files_Recursively()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(dir.Path, "sub"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "sub", "nested.cs"), "");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "nested.cs");
    }

    [Fact]
    public async Task DiscoverAsync_Skips_DotGit_Directory()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(dir.Path, ".git"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, ".git", "HEAD"), "ref: refs/heads/main");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.DoesNotContain(results, r => r.CanonicalUri.ToString().Contains("/.git/"));
    }

    [Fact]
    public async Task DiscoverAsync_Skips_DotFerret_Directory()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(dir.Path, ".ferret"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, ".ferret", "state.json"), "{}");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        Assert.DoesNotContain(results, r => r.CanonicalUri.ToString().Contains("/.ferret/"));
    }

    [Fact]
    public async Task DiscoverAsync_CanonicalUri_Is_Workspace_Relative()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "Program.cs"), "");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        var file = Assert.Single(results, r => r.Kind == AssetKind.File);
        Assert.Equal("filesystem:///Program.cs", file.CanonicalUri.ToString());
    }

    [Fact]
    public async Task DiscoverAsync_CanonicalUri_Uses_Forward_Slashes()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(dir.Path, "src"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "src", "A.cs"), "");
        var connector = MakeConnector(dir.Path);

        var results = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).ToListAsync();

        var file = results.Single(r => r.Kind == AssetKind.File);
        Assert.DoesNotContain('\\', file.CanonicalUri.ToString());
    }

    [Fact]
    public async Task DiscoverAsync_AssetId_Is_Deterministic()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "X.cs"), "");
        var connector = MakeConnector(dir.Path);

        var r1 = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).FirstAsync();
        var r2 = await connector.DiscoverAsync(AssetDiscoveryOptions.Default).FirstAsync();

        Assert.Equal(r1.Id, r2.Id);
    }

    [Fact]
    public async Task DiscoverAsync_Skips_Assets_Ignored_By_Provider()
    {
        using var dir = new TempDirectory();
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "keep.cs"), "");
        await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, "skip.log"), "");
        var connector = MakeConnector(dir.Path);
        var options = new AssetDiscoveryOptions { IgnoreProvider = new SkipLogsIgnoreProvider() };

        var results = await connector.DiscoverAsync(options).ToListAsync();

        Assert.Contains(results, r => r.DisplayName == "keep.cs");
        Assert.DoesNotContain(results, r => r.DisplayName == "skip.log");
    }

    [Fact]
    public async Task DiscoverAsync_Respects_CancellationToken()
    {
        using var dir = new TempDirectory();
        for (var i = 0; i < 20; i++)
            await File.WriteAllTextAsync(System.IO.Path.Combine(dir.Path, $"file{i}.cs"), "");
        var connector = MakeConnector(dir.Path);
        using var cts = new CancellationTokenSource();

        var count = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in connector.DiscoverAsync(AssetDiscoveryOptions.Default, cts.Token))
            {
                count++;
                if (count == 1) cts.Cancel();
            }
        });
    }

    private static FilesystemConnector MakeConnector(string rootPath) =>
        new(new FilesystemConnectorConfiguration { RootPath = rootPath });

    private sealed class SkipLogsIgnoreProvider : IIgnoreProvider
    {
        public bool ShouldIgnore(AssetDescriptor asset) =>
            asset.CanonicalUri.ToString().EndsWith(".log", StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorDiscoveryTests"
```

Expected: FAIL — `FilesystemConnector` doesn't implement `IAssetSource`.

- [ ] **Step 3: Implement IAssetSource on FilesystemConnector**

Update class declaration and add discovery method:

```csharp
public sealed class FilesystemConnector : IConnector, IAssetSource
```

Add these private helpers and the `DiscoverAsync` method after `DisconnectAsync`:

```csharp
private static readonly HashSet<string> HardcodedSkipDirs = new(StringComparer.OrdinalIgnoreCase)
{
    ".git", ".ferret", ".svn", ".hg",
};

/// <inheritdoc/>
public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
    AssetDiscoveryOptions options,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
{
    var root = new DirectoryInfo(_config.RootPath);
    if (!root.Exists) yield break;

    await foreach (var descriptor in WalkDirectoryAsync(root, root, options, ct).ConfigureAwait(false))
    {
        yield return descriptor;
    }
}

private static async IAsyncEnumerable<AssetDescriptor> WalkDirectoryAsync(
    DirectoryInfo dir,
    DirectoryInfo root,
    AssetDiscoveryOptions options,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();

    FileSystemInfo[] entries;
    try { entries = dir.GetFileSystemInfos(); }
    catch (UnauthorizedAccessException) { yield break; }

    foreach (var entry in entries)
    {
        ct.ThrowIfCancellationRequested();

        if (entry is DirectoryInfo subDir)
        {
            if (HardcodedSkipDirs.Contains(subDir.Name)) continue;

            var dirDescriptor = BuildDescriptor(subDir, root, AssetKind.Directory);
            if (options.IgnoreProvider?.ShouldIgnore(dirDescriptor) == true) continue;

            yield return dirDescriptor;

            await foreach (var child in WalkDirectoryAsync(subDir, root, options, ct).ConfigureAwait(false))
                yield return child;
        }
        else if (entry is FileInfo file)
        {
            var descriptor = BuildDescriptor(file, root, AssetKind.File);
            if (options.IgnoreProvider?.ShouldIgnore(descriptor) == true) continue;
            yield return descriptor;
        }
    }
}

private static readonly ConnectorId FilesystemConnectorId = new("filesystem");
private static readonly ConnectorInstanceId DefaultInstance = new("filesystem-default");

private static AssetDescriptor BuildDescriptor(FileSystemInfo entry, DirectoryInfo root, AssetKind kind)
{
    var relative = System.IO.Path.GetRelativePath(root.FullName, entry.FullName)
        .Replace('\\', '/');
    var uri = new Uri($"filesystem:///{relative}");

    long? size = kind == AssetKind.File ? ((FileInfo)entry).Length : null;
    AssetFingerprint? fingerprint = kind == AssetKind.File
        ? AssetFingerprint.CreateLightweight(entry.LastWriteTimeUtc, ((FileInfo)entry).Length)
        : null;

    return new AssetDescriptor
    {
        Id = AssetId.From(uri),
        ConnectorId = FilesystemConnectorId,
        InstanceId = DefaultInstance,
        Kind = kind,
        CanonicalUri = uri,
        DisplayName = entry.Name,
        LastModified = entry.LastWriteTimeUtc,
        Fingerprint = fingerprint,
        SizeBytes = size,
    };
}
```

Also add the `using` import at the top of `FilesystemConnector.cs`:
```csharp
using Ferret.Core.Connectors;
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests
```

Expected: all health + discovery tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Connectors.Filesystem/FilesystemConnector.cs tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorDiscoveryTests.cs
git commit -m "feat(sprint-8): FilesystemConnector IAssetSource — streaming DiscoverAsync, CanonicalUri, hardcoded skips"
```

---

### Task 9: Ignore Providers

**Files:**
- Create: `src/Ferret.Connectors.Filesystem/Ignore/GitIgnoreProvider.cs`
- Create: `src/Ferret.Connectors.Filesystem/Ignore/FerretIgnoreProvider.cs`
- Create: `src/Ferret.Connectors.Filesystem/Ignore/CompositeIgnoreProvider.cs`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/GitIgnoreProviderTests.cs`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/CompositeIgnoreProviderTests.cs`

**Interfaces:**
- Consumes: `IIgnoreProvider`, `AssetDescriptor`
- Produces: `GitIgnoreProvider`, `FerretIgnoreProvider`, `CompositeIgnoreProvider` — consumed by Task 11 (ConnectorCliModule default options)

- [ ] **Step 1: Write failing tests**

`tests/Ferret.Connectors.Filesystem.Tests/GitIgnoreProviderTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Ferret.Connectors.Filesystem.Ignore;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class GitIgnoreProviderTests
{
    [Fact]
    public void ShouldIgnore_Returns_False_For_Non_Filesystem_Uri()
    {
        using var dir = new TempDirectory();
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("jira:///PROJ-1"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_When_No_Gitignore_File()
    {
        using var dir = new TempDirectory();
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///src/Program.cs"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_True_For_File_Matching_Pattern()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(System.IO.Path.Combine(dir.Path, ".gitignore"), "*.log\n");
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///debug.log"));

        Assert.True(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_For_File_Not_Matching_Pattern()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(System.IO.Path.Combine(dir.Path, ".gitignore"), "*.log\n");
        var provider = new GitIgnoreProvider(dir.Path);
        var asset = MakeAsset(new Uri("filesystem:///src/Program.cs"));

        Assert.False(provider.ShouldIgnore(asset));
    }

    [Fact]
    public void ShouldIgnore_Ignores_Comment_Lines()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(System.IO.Path.Combine(dir.Path, ".gitignore"), "# this is a comment\n*.log\n");
        var provider = new GitIgnoreProvider(dir.Path);

        Assert.False(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///readme.md"))));
        Assert.True(provider.ShouldIgnore(MakeAsset(new Uri("filesystem:///output.log"))));
    }

    private static AssetDescriptor MakeAsset(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = System.IO.Path.GetFileName(uri.AbsolutePath),
        LastModified = DateTimeOffset.UtcNow,
    };
}
```

`tests/Ferret.Connectors.Filesystem.Tests/CompositeIgnoreProviderTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Ferret.Connectors.Filesystem.Ignore;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class CompositeIgnoreProviderTests
{
    [Fact]
    public void ShouldIgnore_Returns_True_When_Any_Provider_Returns_True()
    {
        var provider = new CompositeIgnoreProvider([new AlwaysIgnore(), new NeverIgnore()]);
        Assert.True(provider.ShouldIgnore(MakeAsset()));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_When_All_Providers_Return_False()
    {
        var provider = new CompositeIgnoreProvider([new NeverIgnore(), new NeverIgnore()]);
        Assert.False(provider.ShouldIgnore(MakeAsset()));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_With_Empty_Provider_List()
    {
        var provider = new CompositeIgnoreProvider([]);
        Assert.False(provider.ShouldIgnore(MakeAsset()));
    }

    private static AssetDescriptor MakeAsset()
    {
        var uri = new Uri("filesystem:///any/file.cs");
        return new AssetDescriptor
        {
            Id = AssetId.From(uri), ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("i"), Kind = AssetKind.File,
            CanonicalUri = uri, DisplayName = "file.cs", LastModified = DateTimeOffset.UtcNow,
        };
    }

    private sealed class AlwaysIgnore : IIgnoreProvider
    {
        public bool ShouldIgnore(AssetDescriptor asset) => true;
    }

    private sealed class NeverIgnore : IIgnoreProvider
    {
        public bool ShouldIgnore(AssetDescriptor asset) => false;
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "GitIgnoreProviderTests|CompositeIgnoreProviderTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement**

`src/Ferret.Connectors.Filesystem/Ignore/GitIgnoreProvider.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Ignore;

/// <summary>Applies root-level .gitignore patterns. Returns false for non-filesystem URIs.</summary>
public sealed class GitIgnoreProvider : IIgnoreProvider
{
    private readonly IReadOnlyList<string> _patterns;

    /// <summary>Reads .gitignore from the given root path at construction time.</summary>
    public GitIgnoreProvider(string rootPath)
    {
        var gitignore = Path.Combine(rootPath, ".gitignore");
        _patterns = File.Exists(gitignore)
            ? File.ReadAllLines(gitignore)
                  .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
                  .Select(l => l.Trim())
                  .ToList()
            : [];
    }

    /// <inheritdoc/>
    public bool ShouldIgnore(AssetDescriptor asset)
    {
        if (!string.Equals(asset.CanonicalUri.Scheme, "filesystem", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = asset.CanonicalUri.AbsolutePath.TrimStart('/');
        var name = Path.GetFileName(path);

        foreach (var pattern in _patterns)
        {
            if (MatchesGlobPattern(pattern, path) || MatchesGlobPattern(pattern, name))
                return true;
        }

        return false;
    }

    private static bool MatchesGlobPattern(string pattern, string input)
    {
        // Simplified glob: support * wildcard only (covers the most common .gitignore patterns)
        if (!pattern.Contains('*'))
            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase)
                || input.EndsWith("/" + pattern, StringComparison.OrdinalIgnoreCase);

        var parts = pattern.Split('*');
        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            var idx = input.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            if (i == 0 && idx > 0 && !pattern.StartsWith('*')) return false;
            pos = idx + parts[i].Length;
        }

        return !pattern.EndsWith('*') ? pos == input.Length || input[pos..].All(c => c == '/') : true;
    }
}
```

`src/Ferret.Connectors.Filesystem/Ignore/FerretIgnoreProvider.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Ignore;

/// <summary>Applies .ferretignore patterns (same format as .gitignore). Returns false for non-filesystem URIs.</summary>
public sealed class FerretIgnoreProvider : IIgnoreProvider
{
    private readonly GitIgnoreProvider _inner;

    /// <summary>Reads .ferretignore from the given root path. No-op if the file does not exist.</summary>
    public FerretIgnoreProvider(string rootPath)
    {
        // Reuse GitIgnoreProvider logic by temporarily symlinking — instead, just delegate
        // to a GitIgnoreProvider pointed at a copy named .gitignore. Simplest approach:
        // read .ferretignore ourselves and delegate pattern matching.
        var ferretIgnore = Path.Combine(rootPath, ".ferretignore");
        if (File.Exists(ferretIgnore))
        {
            // Copy .ferretignore to a temp .gitignore and build a provider against it
            var tempDir = Path.Combine(Path.GetTempPath(), "ferret-fip-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            File.Copy(ferretIgnore, Path.Combine(tempDir, ".gitignore"));
            _inner = new GitIgnoreProvider(tempDir);
            // Note: tempDir is not cleaned up — acceptable for short-lived CLI process
        }
        else
        {
            _inner = new GitIgnoreProvider(rootPath); // no .gitignore either, returns false always
        }
    }

    /// <inheritdoc/>
    public bool ShouldIgnore(AssetDescriptor asset) => _inner.ShouldIgnore(asset);
}
```

`src/Ferret.Connectors.Filesystem/Ignore/CompositeIgnoreProvider.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Ignore;

/// <summary>Chains multiple IIgnoreProvider instances. Returns true if any provider returns true.</summary>
public sealed class CompositeIgnoreProvider : IIgnoreProvider
{
    private readonly IReadOnlyList<IIgnoreProvider> _providers;

    /// <summary>Creates a composite from the given providers in priority order.</summary>
    public CompositeIgnoreProvider(IReadOnlyList<IIgnoreProvider> providers) => _providers = providers;

    /// <inheritdoc/>
    public bool ShouldIgnore(AssetDescriptor asset) =>
        _providers.Any(p => p.ShouldIgnore(asset));
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```
git add src/Ferret.Connectors.Filesystem/Ignore/ tests/Ferret.Connectors.Filesystem.Tests/GitIgnoreProviderTests.cs tests/Ferret.Connectors.Filesystem.Tests/CompositeIgnoreProviderTests.cs
git commit -m "feat(sprint-8): GitIgnoreProvider, FerretIgnoreProvider, CompositeIgnoreProvider"
```

---

### Task 10: FilesystemConnectorFactory + Ferret.Cli Non-Breaking Additions

**Files:**
- Create: `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs`
- Create: `src/Ferret.Cli/Cli/ArgumentDefinition.cs`
- Create: `src/Ferret.Cli/Cli/ICommandResultFormatter.cs`
- Modify: `src/Ferret.Cli/Cli/CommandDefinition.cs` (add `Arguments` + `WithArgument`)
- Modify: `src/Ferret.Cli/Commands/RootCommandFactory.cs` (wire positional args)
- Modify: `src/Ferret.Cli/Cli/IFerretContext.cs` (already has `GetOption<T>` — arguments merge into options dict)
- Modify: `src/Ferret.Cli/Cli/FerretContext.cs` (parse arguments into options dict)

**Interfaces:**
- Consumes: `IConnectorFactory`, `ConnectorDescriptor`, `ConnectorCapabilities`, `FilesystemConnector`, `FilesystemConnectorConfiguration`
- Produces: `FilesystemConnectorFactory.Descriptor` (ConnectorDescriptor for filesystem), `ArgumentDefinition`, `ICommandResultFormatter<T>` — consumed by Task 11

- [ ] **Step 1: Create FilesystemConnectorFactory**

`src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem;

/// <summary>Factory that creates FilesystemConnector instances from configuration.</summary>
public sealed class FilesystemConnectorFactory : IConnectorFactory
{
    private readonly FilesystemConnectorConfiguration _defaultConfig;

    /// <summary>Creates a factory using the given default configuration.</summary>
    public FilesystemConnectorFactory(FilesystemConnectorConfiguration defaultConfig) =>
        _defaultConfig = defaultConfig;

    /// <inheritdoc/>
    public ConnectorId ConnectorId { get; } = new("filesystem");

    /// <inheritdoc/>
    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Id = new ConnectorId("filesystem"),
        Metadata = ConnectorMetadata.Create(
            "filesystem", "Filesystem Connector",
            "Discovers files and directories from the local filesystem.",
            ConnectorType.Filesystem, "1.0"),
        Capabilities = [ConnectorCapabilities.AssetDiscovery],
        SupportedPlatforms = ["Linux", "macOS", "Windows"],
    };

    /// <inheritdoc/>
    public IConnector Create(ConnectorInstanceId instanceId) =>
        new FilesystemConnector(_defaultConfig);
}
```

- [ ] **Step 2: Add ArgumentDefinition to Ferret.Cli**

Create `src/Ferret.Cli/Cli/ArgumentDefinition.cs`:
```csharp
namespace Ferret.Cli.Cli;

/// <summary>Defines a positional argument for a CLI command.</summary>
/// <param name="Name">Argument name — used as the key in context.GetOption&lt;string&gt;("name").</param>
/// <param name="Description">Human-readable description shown in help text.</param>
/// <param name="IsRequired">Whether the argument is required. Defaults to true.</param>
internal sealed record ArgumentDefinition(string Name, string Description, bool IsRequired = true);
```

Create `src/Ferret.Cli/Cli/ICommandResultFormatter.cs`:
```csharp
namespace Ferret.Cli.Cli;

/// <summary>Formats a command result model into CLI output. Inject a different implementation for --output json.</summary>
/// <typeparam name="T">The result model type.</typeparam>
internal interface ICommandResultFormatter<in T>
{
    /// <summary>Formats the result and writes it to the output formatter.</summary>
    /// <param name="result">The model to format.</param>
    /// <param name="output">The CLI output formatter to write to.</param>
    void Format(T result, IOutputFormatter output);
}
```

- [ ] **Step 3: Update CommandDefinition**

Add `Arguments` and `WithArgument` to `src/Ferret.Cli/Cli/CommandDefinition.cs`:

```csharp
internal sealed record CommandDefinition(
    CommandMetadata Metadata,
    Type? HandlerType,
    string? Group = null,
    IReadOnlyList<OptionDefinition>? Options = null,
    IReadOnlyList<string>? PlannedSubcommands = null,
    string? PlannedSprint = null,
    IReadOnlyList<ArgumentDefinition>? Arguments = null)   // new
{
    // ... existing EmptyGroup stays unchanged ...

    /// <summary>Returns a copy of this definition with the given positional argument added.</summary>
    internal CommandDefinition WithArgument(string name, string description, bool isRequired = true) =>
        this with { Arguments = [.. (Arguments ?? []), new ArgumentDefinition(name, description, isRequired)] };
}
```

- [ ] **Step 4: Update RootCommandFactory to wire arguments**

In `src/Ferret.Cli/Commands/RootCommandFactory.cs`, add argument wiring inside `BuildCommand` after the options loop, and update `RegisterHandlerAction` to parse them:

After the `optMap` construction block, add:
```csharp
var argList = def.Arguments ?? [];
var argMap = new Dictionary<string, Argument<string>>(StringComparer.Ordinal);
foreach (var argDef in argList)
{
    var arg = new Argument<string>(argDef.Name) { Description = argDef.Description };
    if (!argDef.IsRequired) arg.SetDefaultValue(null);
    cmd.Add(arg);
    argMap[argDef.Name] = arg;
}
```

Update `RegisterHandlerAction` call to pass `argMap`:
```csharp
RegisterHandlerAction(cmd, def.HandlerType, provider, config, optMap, argMap, output);
```

Update `RegisterHandlerAction` signature and body:
```csharp
private static void RegisterHandlerAction(
    Command cmd,
    Type handlerType,
    IServiceProvider provider,
    IConfiguration config,
    Dictionary<string, Option> optMap,
    Dictionary<string, Argument<string>> argMap,
    TextWriter? output)
{
    cmd.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
    {
        var writer = output ?? Console.Out;
        var formatter = new ConsoleFormatter(writer, VerbosityLevel.Normal);
        var ferretServices = new FerretServices(
            provider, config, NullLoggerFactory.Instance, formatter);
        var parsedOpts = ParseOptions(parseResult, optMap);

        // Merge positional arguments into the options dict so GetOption<T>("name") works for both
        foreach (var (name, arg) in argMap)
        {
            parsedOpts[name] = parseResult.GetValue(arg);
        }

        var context = FerretContext.From(parseResult, ferretServices, parsedOpts, ct);
        var handler = (ICommandHandler)provider.GetRequiredService(handlerType);
        return (int)await handler.ExecuteAsync(context).ConfigureAwait(false);
    });
}
```

Note: `parsedOpts` must be mutable — change its type from `Dictionary<string, object?>` return in `ParseOptions` (it already returns `Dictionary`, so `parsedOpts[name] = ...` works).

- [ ] **Step 5: Build**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Cli.Tests
```

Expected: all existing CLI tests still pass. No new tests yet — handler tests come in Task 12.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs src/Ferret.Cli/Cli/ArgumentDefinition.cs src/Ferret.Cli/Cli/ICommandResultFormatter.cs src/Ferret.Cli/Cli/CommandDefinition.cs src/Ferret.Cli/Commands/RootCommandFactory.cs
git commit -m "feat(sprint-8): FilesystemConnectorFactory; ArgumentDefinition, ICommandResultFormatter<T>, CommandDefinition.WithArgument"
```

---

### Task 11: View Models + Formatters + ConnectorCliModule

**Files:**
- Create: `src/Ferret.ConnectorPlatform/ViewModels/ConnectorListItem.cs`
- Create: `src/Ferret.ConnectorPlatform/ViewModels/ConnectorListResult.cs`
- Create: `src/Ferret.ConnectorPlatform/ViewModels/ConnectorInfoView.cs`
- Create: `src/Ferret.ConnectorPlatform/Formatting/TextConnectorListFormatter.cs`
- Create: `src/Ferret.ConnectorPlatform/Formatting/TextConnectorInfoFormatter.cs`
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorListCommandHandler.cs`
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorInfoCommandHandler.cs`
- Create: `src/Ferret.ConnectorPlatform/ConnectorCliModule.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/TextConnectorListFormatterTests.cs`

**Interfaces:**
- Consumes: `IConnectorRegistry`, `ConnectorDescriptor`, `ConnectorCapabilities.All`, `ICommandHandler`, `IFerretContext`, `CommandExitCode`, `ICommandResultFormatter<T>`, `IOutputFormatter`
- Produces: `ConnectorCliModule` (implements `ICliModule`) — consumed by Task 12 (Program.cs)

- [ ] **Step 1: View models**

`src/Ferret.ConnectorPlatform/ViewModels/ConnectorListItem.cs`:
```csharp
namespace Ferret.ConnectorPlatform.ViewModels;

/// <summary>Presentation model for a single row in 'ferret connector list'.</summary>
internal sealed record ConnectorListItem(
    string Id,
    string Name,
    string Version,
    string PrimaryCapability,
    bool IsConfigured);
```

`src/Ferret.ConnectorPlatform/ViewModels/ConnectorListResult.cs`:
```csharp
namespace Ferret.ConnectorPlatform.ViewModels;

/// <summary>Presentation model for the full output of 'ferret connector list'.</summary>
internal sealed record ConnectorListResult(IReadOnlyList<ConnectorListItem> Items);
```

`src/Ferret.ConnectorPlatform/ViewModels/ConnectorInfoView.cs`:
```csharp
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.ViewModels;

/// <summary>Presentation model for 'ferret connector info &lt;id&gt;'.</summary>
internal sealed record ConnectorInfoView(
    ConnectorDescriptor Descriptor,
    bool IsConfigured);
```

- [ ] **Step 2: Write formatter tests**

`tests/Ferret.ConnectorPlatform.Tests/TextConnectorListFormatterTests.cs`:
```csharp
using Ferret.Core.Connectors;
using Ferret.ConnectorPlatform.Formatting;
using Ferret.ConnectorPlatform.ViewModels;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class TextConnectorListFormatterTests
{
    [Fact]
    public void Format_Contains_Connector_Id()
    {
        var output = FormatSingleItem("filesystem", "Filesystem Connector", "AssetDiscovery");
        Assert.Contains("filesystem", output);
    }

    [Fact]
    public void Format_Contains_Connector_Name()
    {
        var output = FormatSingleItem("filesystem", "Filesystem Connector", "AssetDiscovery");
        Assert.Contains("Filesystem Connector", output);
    }

    [Fact]
    public void Format_Empty_List_Shows_No_Connectors_Message()
    {
        using var sw = new StringWriter();
        var formatter = new TextConnectorListFormatter();
        formatter.Format(new ConnectorListResult([]), new Ferret.Cli.Cli.ConsoleFormatter(sw, Ferret.Cli.Cli.VerbosityLevel.Normal));
        Assert.Contains("No connectors", sw.ToString());
    }

    private static string FormatSingleItem(string id, string name, string capability)
    {
        using var sw = new StringWriter();
        var formatter = new TextConnectorListFormatter();
        var item = new ConnectorListItem(id, name, "1.0.0", capability, false);
        formatter.Format(new ConnectorListResult([item]),
            new Ferret.Cli.Cli.ConsoleFormatter(sw, Ferret.Cli.Cli.VerbosityLevel.Normal));
        return sw.ToString();
    }
}
```

- [ ] **Step 3: Implement formatters**

`src/Ferret.ConnectorPlatform/Formatting/TextConnectorListFormatter.cs`:
```csharp
using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;

namespace Ferret.ConnectorPlatform.Formatting;

/// <summary>Formats ConnectorListResult as plain-text tabular output.</summary>
internal sealed class TextConnectorListFormatter : ICommandResultFormatter<ConnectorListResult>
{
    /// <inheritdoc/>
    public void Format(ConnectorListResult result, IOutputFormatter output)
    {
        if (result.Items.Count == 0)
        {
            output.WriteLine("No connectors are registered.");
            output.WriteLine();
            output.WriteLine("Next: Install a connector package and register it in Program.cs.");
            return;
        }

        const int IdWidth = 14;
        const int NameWidth = 24;
        const int VerWidth = 9;

        output.WriteLine(
            $"{"ID",-IdWidth}  {"NAME",-NameWidth}  {"VERSION",-VerWidth}  {"CAPABILITIES",-16}  CONFIGURED");
        output.WriteLine(new string('-', 80));

        foreach (var item in result.Items)
        {
            output.WriteLine(
                $"{item.Id,-IdWidth}  {item.Name,-NameWidth}  {item.Version,-VerWidth}  {item.PrimaryCapability,-16}  {(item.IsConfigured ? "yes" : "no")}");
        }
    }
}
```

`src/Ferret.ConnectorPlatform/Formatting/TextConnectorInfoFormatter.cs`:
```csharp
using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.Formatting;

/// <summary>Formats ConnectorInfoView as plain-text detail output.</summary>
internal sealed class TextConnectorInfoFormatter : ICommandResultFormatter<ConnectorInfoView>
{
    /// <inheritdoc/>
    public void Format(ConnectorInfoView view, IOutputFormatter output)
    {
        var d = view.Descriptor;
        output.WriteLine($"{d.Metadata.Name}  v{d.Metadata.Version}");
        output.WriteLine($"  ID:           {d.Id.Value}");
        output.WriteLine($"  Type:         {d.Metadata.ConnectorType}");
        output.WriteLine($"  Description:  {d.Metadata.Description}");
        output.WriteLine();
        output.WriteLine("  Capabilities");

        foreach (var known in ConnectorCapabilities.All)
        {
            var implemented = d.Capabilities.Any(c => c.Id == known.Id);
            var marker = implemented ? "✓" : "✗";
            var label = implemented ? $"{known.Name}  v{d.Capabilities.First(c => c.Id == known.Id).Version}" : known.Name;
            output.WriteLine($"    {marker}  {label}");
        }

        if (d.SupportedPlatforms.Count > 0)
        {
            output.WriteLine();
            output.WriteLine($"  Platforms:  {string.Join(", ", d.SupportedPlatforms)}");
        }

        output.WriteLine();
        var status = view.IsConfigured ? "Configured" : "Available (not configured)";
        output.WriteLine($"  Status:     {status}");
    }
}
```

- [ ] **Step 4: Implement command handlers**

`src/Ferret.ConnectorPlatform/Commands/ConnectorListCommandHandler.cs`:
```csharp
using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.Formatting;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles 'ferret connector list'.</summary>
internal sealed class ConnectorListCommandHandler : ICommandHandler
{
    private readonly IConnectorRegistry _registry;
    private readonly TextConnectorListFormatter _formatter;

    /// <summary>Initialises the handler with the given registry and formatter.</summary>
    public ConnectorListCommandHandler(IConnectorRegistry registry, TextConnectorListFormatter formatter)
    {
        _registry = registry;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public Task<CommandExitCode> ExecuteAsync(IFerretContext context)
    {
        var items = _registry.GetAll()
            .Select(d => new ConnectorListItem(
                Id: d.Id.Value,
                Name: d.Metadata.Name,
                Version: d.Metadata.Version,
                PrimaryCapability: d.Capabilities.FirstOrDefault()?.Name ?? "(none)",
                IsConfigured: false))
            .ToList();

        _formatter.Format(new ConnectorListResult(items), context.Services.Output);
        return Task.FromResult(CommandExitCode.Success);
    }
}
```

`src/Ferret.ConnectorPlatform/Commands/ConnectorInfoCommandHandler.cs`:
```csharp
using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.Formatting;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles 'ferret connector info &lt;id&gt;'.</summary>
internal sealed class ConnectorInfoCommandHandler : ICommandHandler
{
    private readonly IConnectorRegistry _registry;
    private readonly TextConnectorInfoFormatter _formatter;

    /// <summary>Initialises the handler with the given registry and formatter.</summary>
    public ConnectorInfoCommandHandler(IConnectorRegistry registry, TextConnectorInfoFormatter formatter)
    {
        _registry = registry;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public Task<CommandExitCode> ExecuteAsync(IFerretContext context)
    {
        var id = context.GetOption<string>("id");
        if (string.IsNullOrWhiteSpace(id))
        {
            context.Services.Output.WriteLine("Error: connector ID is required.");
            context.Services.Output.WriteLine("       Run 'ferret connector list' to see available connectors.");
            return Task.FromResult(CommandExitCode.UserError);
        }

        var descriptor = _registry.GetById(new ConnectorId(id));
        if (descriptor is null)
        {
            context.Services.Output.WriteLine($"Error: connector '{id}' is not registered.");
            context.Services.Output.WriteLine("       Run 'ferret connector list' to see available connectors.");
            return Task.FromResult(CommandExitCode.UserError);
        }

        _formatter.Format(new ConnectorInfoView(descriptor, IsConfigured: false), context.Services.Output);
        return Task.FromResult(CommandExitCode.Success);
    }
}
```

- [ ] **Step 5: Implement ConnectorCliModule**

`src/Ferret.ConnectorPlatform/ConnectorCliModule.cs`:
```csharp
using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.Commands;
using Ferret.ConnectorPlatform.Formatting;
using Ferret.Core.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.ConnectorPlatform;

/// <summary>CLI module for connector management commands.</summary>
public sealed class ConnectorCliModule : CliModuleBase
{
    private readonly IReadOnlyList<IConnectorFactory> _factories;

    /// <summary>Creates the module with the connector factories to register.</summary>
    /// <param name="factories">Connector factories that will be available in the registry.</param>
    public ConnectorCliModule(IReadOnlyList<IConnectorFactory> factories) => _factories = factories;

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        foreach (var factory in _factories)
            services.AddSingleton(factory);

        services.AddSingleton<IConnectorRegistry>(sp =>
            RegistryBuilder.Build(sp.GetServices<IConnectorFactory>()));

        services.AddSingleton<TextConnectorListFormatter>();
        services.AddSingleton<TextConnectorInfoFormatter>();
        services.AddSingleton<ConnectorListCommandHandler>();
        services.AddSingleton<ConnectorInfoCommandHandler>();
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CommandDefinition> GetCommands() =>
    [
        CommandDefinition.Group("connector", "Connector management and inspection")
            .WithSubcommand(
                CommandDefinition.Leaf("list", "List all registered connectors",
                    typeof(ConnectorListCommandHandler)))
            .WithSubcommand(
                CommandDefinition.Leaf("info", "Show connector details",
                    typeof(ConnectorInfoCommandHandler))
                    .WithArgument("id", "Connector ID (e.g. filesystem)")),
    ];
}
```

Note: `CommandDefinition.Group(...).WithSubcommand(...)` requires the `WithSubcommand` builder to exist. Check `CommandDefinition` — if it was added in Sprint 7 Task 9 (RootCommandFactory grouping), use it directly. If not, add `WithSubcommand` to `CommandDefinition` alongside `WithArgument`.

- [ ] **Step 6: Confirm green**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.ConnectorPlatform.Tests
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```
git add src/Ferret.ConnectorPlatform/ tests/Ferret.ConnectorPlatform.Tests/TextConnectorListFormatterTests.cs
git commit -m "feat(sprint-8): view models, formatters, ConnectorListCommandHandler, ConnectorInfoCommandHandler, ConnectorCliModule"
```

---

### Task 12: Wire Up (Program.cs, csproj References, Solution, E2E Tests)

**Files:**
- Modify: `src/Ferret.Cli/Program.cs` (register ConnectorCliModule)
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj` (add reference to Ferret.ConnectorPlatform, Ferret.Connectors.Filesystem)
- Modify: `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj` (add reference to Ferret.Cli)
- Modify: `src/Ferret.sln` (add Ferret.ConnectorPlatform, Ferret.Connectors.Filesystem projects)
- Modify: `tests/Ferret.sln` (add Ferret.ConnectorPlatform.Tests, Ferret.Connectors.Filesystem.Tests)
- Create: `tests/Ferret.Integration.Tests/ConnectorCommandE2ETests.cs`

**Interfaces:**
- Consumes: `ConnectorCliModule`, `FilesystemConnectorFactory`, `FilesystemConnectorConfiguration`, `ICliModule`
- Produces: Working `ferret connector list` and `ferret connector info filesystem` commands

- [ ] **Step 1: Add solution references**

```
dotnet sln src/Ferret.sln add src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj
dotnet sln src/Ferret.sln add src/Ferret.Connectors.Filesystem/Ferret.Connectors.Filesystem.csproj
dotnet sln tests/Ferret.sln add tests/Ferret.ConnectorPlatform.Tests/Ferret.ConnectorPlatform.Tests.csproj
dotnet sln tests/Ferret.sln add tests/Ferret.Connectors.Filesystem.Tests/Ferret.Connectors.Filesystem.Tests.csproj
```

- [ ] **Step 2: Update csproj references**

`src/Ferret.Cli/Ferret.Cli.csproj` — add:
```xml
<ProjectReference Include="..\Ferret.ConnectorPlatform\Ferret.ConnectorPlatform.csproj" />
<ProjectReference Include="..\Ferret.Connectors.Filesystem\Ferret.Connectors.Filesystem.csproj" />
```

`src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj` — add:
```xml
<ProjectReference Include="..\Ferret.Cli\Ferret.Cli.csproj" />
<ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
```

`tests/Ferret.ConnectorPlatform.Tests/Ferret.ConnectorPlatform.Tests.csproj` — add:
```xml
<ProjectReference Include="..\..\src\Ferret.ConnectorPlatform\Ferret.ConnectorPlatform.csproj" />
<ProjectReference Include="..\..\src\Ferret.Cli\Ferret.Cli.csproj" />
```

- [ ] **Step 3: Register ConnectorCliModule in Program.cs**

Check the current single-line `src/Ferret.Cli/Program.cs`. Match existing module registration pattern. Typical expansion:
```csharp
using Ferret.Cli;
using Ferret.ConnectorPlatform;
using Ferret.Connectors.Filesystem;

var workspaceRoot = Directory.GetCurrentDirectory();
var filesystemConfig = new FilesystemConnectorConfiguration(workspaceRoot);
var filesystemFactory = new FilesystemConnectorFactory(filesystemConfig);

return await FerretApp.RunAsync(args, [
    new CoreCliModule(),
    new ConnectorCliModule([filesystemFactory]),
]);
```

If `FerretApp.RunAsync` has a different signature, read `src/Ferret.Cli/FerretApp.cs` first and match exactly.

- [ ] **Step 4: Write E2E tests**

`tests/Ferret.Integration.Tests/ConnectorCommandE2ETests.cs`:
```csharp
using Ferret.Cli;
using Ferret.ConnectorPlatform;
using Ferret.Connectors.Filesystem;
using Xunit;

namespace Ferret.Integration.Tests;

public sealed class ConnectorCommandE2ETests
{
    [Fact]
    public async Task ConnectorList_Returns_Filesystem_Connector()
    {
        using var sw = new StringWriter();
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(Directory.GetCurrentDirectory()));

        var exitCode = await FerretApp.RunAsync(
            ["connector", "list"],
            [new CoreCliModule(), new ConnectorCliModule([factory])],
            output: sw);

        Assert.Equal(0, exitCode);
        Assert.Contains("filesystem", sw.ToString());
    }

    [Fact]
    public async Task ConnectorInfo_Returns_Filesystem_Detail()
    {
        using var sw = new StringWriter();
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(Directory.GetCurrentDirectory()));

        var exitCode = await FerretApp.RunAsync(
            ["connector", "info", "filesystem"],
            [new CoreCliModule(), new ConnectorCliModule([factory])],
            output: sw);

        Assert.Equal(0, exitCode);
        Assert.Contains("Filesystem Connector", sw.ToString());
        Assert.Contains("asset-discovery", sw.ToString());
    }

    [Fact]
    public async Task ConnectorInfo_Unknown_Id_Returns_UserError_ExitCode()
    {
        using var sw = new StringWriter();
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(Directory.GetCurrentDirectory()));

        var exitCode = await FerretApp.RunAsync(
            ["connector", "info", "nonexistent"],
            [new CoreCliModule(), new ConnectorCliModule([factory])],
            output: sw);

        Assert.Equal(1, exitCode);
    }
}
```

Note: Check existing `tests/Ferret.Integration.Tests/` for how `FerretApp` is invoked. If `output:` is not yet a parameter, read `FerretApp.cs` and add it the same way the existing integration tests capture output.

- [ ] **Step 5: Build and run all tests**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.sln
```

Expected: all existing tests pass; new E2E tests pass.

- [ ] **Step 6: Smoke test manually**

```
dotnet run --project src/Ferret.Cli -- connector list
dotnet run --project src/Ferret.Cli -- connector info filesystem
dotnet run --project src/Ferret.Cli -- connector info unknown
```

Expected:
- `connector list`: table with `filesystem`, `Filesystem Connector`, `AssetDiscovery`, `no`
- `connector info filesystem`: detail block with capability ✓/✗ matrix
- `connector info unknown`: error message, exit code 1

- [ ] **Step 7: Commit**

```
git add src/Ferret.Cli/ src/Ferret.ConnectorPlatform/ src/Ferret.sln tests/Ferret.sln tests/Ferret.Integration.Tests/ConnectorCommandE2ETests.cs
git commit -m "feat(sprint-8): wire up ConnectorCliModule, FilesystemConnectorFactory in Program.cs + E2E tests"
```

---

### Task 13: Architecture Tests (Ferret.Architecture.Tests)

**Files:**
- Create: `tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj`
- Create: `tests/Ferret.Architecture.Tests/ConnectorArchitectureTests.cs`
- Modify: `tests/Ferret.sln` (add new test project)

**Interfaces:**
- Consumes: All `Ferret.*` assemblies via reflection
- Produces: 6 executable rules enforcing ADR-0013 §Compliance

- [ ] **Step 1: Create project file**

`tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ConnectorPlatform\Ferret.ConnectorPlatform.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Connectors.Filesystem\Ferret.Connectors.Filesystem.csproj" />
  </ItemGroup>
</Project>
```

```
dotnet sln tests/Ferret.sln add tests/Ferret.Architecture.Tests/Ferret.Architecture.Tests.csproj
```

- [ ] **Step 2: Write failing tests**

`tests/Ferret.Architecture.Tests/ConnectorArchitectureTests.cs`:
```csharp
using System.Reflection;
using Ferret.Core.Connectors;
using Ferret.Connectors.Filesystem;
using Xunit;

namespace Ferret.Architecture.Tests;

public sealed class ConnectorArchitectureTests
{
    private static readonly Assembly CoreAssembly = typeof(IConnector).Assembly;
    private static readonly Assembly FilesystemAssembly = typeof(FilesystemConnector).Assembly;

    [Fact]
    public void IConnector_Implementations_Must_Be_Sealed()
    {
        var assemblies = new[] { CoreAssembly, FilesystemAssembly };
        var violations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IConnector).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"IConnector implementations must be sealed: {string.Join(", ", violations)}");
    }

    [Fact]
    public void AssetDescriptor_Must_Have_No_Public_Setters()
    {
        var violations = typeof(AssetDescriptor)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
            .Select(p => p.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"AssetDescriptor must be immutable. Public setters found: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ConnectorDescriptor_Must_Have_No_Public_Setters()
    {
        var violations = typeof(ConnectorDescriptor)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
            .Select(p => p.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"ConnectorDescriptor must be immutable. Public setters found: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Filesystem_Assembly_Must_Not_Reference_Ferret_Cli()
    {
        var referenced = FilesystemAssembly.GetReferencedAssemblies()
            .Select(r => r.Name ?? string.Empty);

        Assert.DoesNotContain("Ferret.Cli", referenced);
    }

    [Fact]
    public void IAssetSource_DiscoverAsync_Must_Return_IAsyncEnumerable_Of_AssetDescriptor()
    {
        var method = typeof(IAssetSource).GetMethod(nameof(IAssetSource.DiscoverAsync));
        Assert.NotNull(method);
        Assert.True(
            typeof(IAsyncEnumerable<AssetDescriptor>).IsAssignableFrom(method.ReturnType),
            $"IAssetSource.DiscoverAsync must return IAsyncEnumerable<AssetDescriptor>, returns: {method.ReturnType}");
    }

    [Fact]
    public void IIgnoreProvider_ShouldIgnore_Must_Return_Bool()
    {
        var method = typeof(IIgnoreProvider).GetMethod(nameof(IIgnoreProvider.ShouldIgnore));
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
    }
}
```

- [ ] **Step 3: Confirm tests pass**

```
dotnet test tests/Ferret.Architecture.Tests
```

Expected: all 6 rules pass with the types produced in Tasks 3–9.

- [ ] **Step 4: Commit**

```
git add tests/Ferret.Architecture.Tests/ tests/Ferret.sln
git commit -m "test(sprint-8): Ferret.Architecture.Tests — 6 executable architectural rules from ADR-0013"
```

---

### Task 14: PROJECT-STATE.md + Sprint Tag

**Files:**
- Modify: `docs/PROJECT-STATE.md`

- [ ] **Step 1: Update PROJECT-STATE.md**

Find the Sprint 7 status line and ensure it reads "Complete". Add Sprint 8 block:

```markdown
## Sprint 8 — Connector Platform (v0.8.0)

**Status:** Complete
**Tag:** v0.8.0-sprint8
**Date:** 2026-06-28

### Delivered

- `Ferret.ConnectorPlatform` — connector registry, capability model, typed IDs (`ConnectorId`, `ConnectorInstanceId`, `AssetId`), `AssetDescriptor`, `AssetKind`, `AssetFingerprint`, `IIgnoreProvider`, `AssetDiscoveryOptions`, `ConnectorCapability`, `ConnectorCapabilities` (8 singletons), `ConnectorDescriptor`, `IConnectorFactory`, `IConnectorSession`, `IAssetSource`, `IConnectorRegistry`, `ConnectorRegistry`, `RegistryBuilder`
- `Ferret.Connectors.Filesystem` — `FilesystemConnector` (IConnector + IAssetSource), `FilesystemConnectorFactory`, `FilesystemConnectorSession`, `FilesystemConnectorConfiguration`, `GitIgnoreProvider`, `FerretIgnoreProvider`, `CompositeIgnoreProvider`
- `Ferret.Cli` additions (non-breaking) — `ArgumentDefinition`, `ICommandResultFormatter<T>`, `CommandDefinition.WithArgument`, `RootCommandFactory` positional argument wiring
- `ferret connector list` — tabular list of registered connectors with capabilities
- `ferret connector info <id>` — capability detail with ✓/✗ matrix over all 8 singletons
- `Ferret.Architecture.Tests` — 6 executable architectural rules enforcing ADR-0013
- `Ferret.Core` updates — `ConnectorIoCapabilities` (renamed), `IConnector.ConnectAsync` → `Task<IConnectorSession>`

### Architecture Documents

- SPEC-008: `docs/superpowers/specs/2026-06-28-sprint-8-connector-platform-design.md`
- ARCH-019: `docs/002-Architecture/ARCH-019-Connector-Platform-Architecture.md`
- ADR-0013: `docs/adr/0013-capability-based-platform-architecture.md`

### What a new user can do after Sprint 8

Run `ferret connector list` and `ferret connector info filesystem` to inspect the platform's connectors.
```

- [ ] **Step 2: Commit state update**

```
git add docs/PROJECT-STATE.md
git commit -m "docs(sprint-8): update PROJECT-STATE.md — Sprint 8 complete"
```

- [ ] **Step 3: Tag the sprint**

```
git tag v0.8.0-sprint8
```

Push when ready:
```
git push origin v0.8.0-sprint8
```

---

## Self-Review

### Spec Coverage

| Spec Requirement | Task |
|---|---|
| Rename ConnectorCapabilities → ConnectorIoCapabilities | Task 1 |
| Typed IDs: ConnectorId, ConnectorInstanceId, AssetId | Task 2 |
| AssetKind, AssetFingerprint, IIgnoreProvider, AssetDescriptor | Task 3 |
| ConnectorCapability, ConnectorCapabilities (8 singletons), ConnectorDescriptor | Task 4 |
| IConnectorSession, IAssetSource, IConnectorFactory (with Descriptor), IConnectorRegistry | Task 5 |
| ConnectorRegistry, RegistryBuilder + tests | Task 6 |
| FilesystemConnector scaffold + health tests | Task 7 |
| FilesystemConnector.DiscoverAsync streaming + CanonicalUri | Task 8 |
| Ignore providers: Git, Ferret, Composite | Task 9 |
| FilesystemConnectorFactory | Task 10 |
| CLI ArgumentDefinition, ICommandResultFormatter, CommandDefinition.WithArgument | Task 10 |
| View models + formatters | Task 11 |
| ConnectorListCommandHandler, ConnectorInfoCommandHandler, ConnectorCliModule | Task 11 |
| Wire up Program.cs + E2E tests | Task 12 |
| Architecture tests (6 rules from ADR-0013) | Task 13 |
| PROJECT-STATE.md + v0.8.0-sprint8 tag | Task 14 |

All spec requirements covered.

### Type Consistency

- `ConnectorCapabilities.All` used in `TextConnectorInfoFormatter` — defined in Task 4. ✓
- `IIgnoreProvider` consumed by `CompositeIgnoreProvider` — defined in Task 3. ✓
- `IConnectorFactory.Descriptor` used in `RegistryBuilder` — defined in Task 5. ✓
- `ConnectorCliModule` registered in `Program.cs` — defined in Task 11. ✓
