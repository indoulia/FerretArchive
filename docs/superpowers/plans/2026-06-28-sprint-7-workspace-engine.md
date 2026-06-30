# Sprint 7 — Workspace Engine Implementation Plan (v2 — ContextOS Foundation)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `ferret workspace init` and `ferret workspace status` — a user can create and inspect a `.ferret/` workspace that is the long-term foundation for ContextOS: connectors, multi-index search, memory systems, knowledge graph, and the Enterprise Time Machine.

**Architecture:** A new `Ferret.Workspace` library implements `IWorkspaceEngine`, `IWorkspaceLocator`, and `IWorkspaceStateStore` from `Ferret.Core`. New connector contracts (`IConnector`, `ConnectorType`, `ConnectorMetadata`, `ConnectorCapabilities`, `ConnectorHealth`) are added to `Ferret.Core.Connectors` — contracts only, no implementation. The CLI gains a `WorkspaceCliModule`. `RootCommandFactory` is extended to support grouped subcommands via the existing `Group` property on `CommandDefinition`.

**Tech Stack:** .NET 9, C# 13, xUnit, `System.Text.Json` (BCL), `System.CommandLine`, existing `Ferret.Core` contracts.

## Global Constraints

- Target framework: `net9.0`; `LangVersion: latest`; `Nullable: enable`; `TreatWarningsAsErrors: true`
- `AnalysisMode: All` — all public types require XML doc comments; StyleCop enforced
- No breaking changes to any frozen M1 package (ADR-0012); adding new types to `Ferret.Core` is permitted as non-breaking addition
- TDD: write the failing test first, confirm red, then implement
- Commit after every task; use `feat(sprint-7):`, `test(sprint-7):`, `chore(sprint-7):` prefixes
- `WorkspaceStatistics.Create(int totalFiles, int indexedFiles, DateTimeOffset lastIndexed, string schemaVersion)` — `lastIndexed` is non-nullable; use `DateTimeOffset.MinValue` for "never indexed"
- `WorkspaceId.Create(string id)` takes a string; `IFerretContext.CancellationToken` is available on every command context

---

## Sub-Sprint 7a — Workspace Model (Tasks 1–3)

### Task 1: `Ferret.Workspace` project scaffold + `WorkspaceLayout`

**Files:**
- Create: `src/Ferret.Workspace/Ferret.Workspace.csproj`
- Create: `src/Ferret.Workspace/WorkspaceLayout.cs`
- Create: `tests/Ferret.Workspace.Tests/Ferret.Workspace.Tests.csproj`
- Create: `tests/Ferret.Workspace.Tests/WorkspaceLayoutTests.cs`

**Interfaces:**
- Produces: `WorkspaceLayout` static class — all `.ferret/` path constants plus `AllDirectories` (flat ordered list of every directory to create, including nested) and `ConfigFileNames`

- [ ] **Step 1: Create the library project file**

```xml
<!-- src/Ferret.Workspace/Ferret.Workspace.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.Workspace</AssemblyName>
    <RootNamespace>Ferret.Workspace</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create the test project file**

```xml
<!-- tests/Ferret.Workspace.Tests/Ferret.Workspace.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.Workspace.Tests</AssemblyName>
    <RootNamespace>Ferret.Workspace.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Workspace\Ferret.Workspace.csproj" />
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

- [ ] **Step 3: Add both projects to the solution**

```powershell
dotnet sln src/Ferret.sln add src/Ferret.Workspace/Ferret.Workspace.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Workspace.Tests/Ferret.Workspace.Tests.csproj
```

Expected: `Project '...' added to the solution.` (×2)

- [ ] **Step 4: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/WorkspaceLayoutTests.cs
namespace Ferret.Workspace.Tests;

public sealed class WorkspaceLayoutTests
{
    [Fact]
    public void RootDirectoryName_IsDotFerret() =>
        Assert.Equal(".ferret", WorkspaceLayout.RootDirectoryName);

    [Fact]
    public void ManifestFileName_IsWorkspaceJson() =>
        Assert.Equal("workspace.json", WorkspaceLayout.ManifestFileName);

    [Fact]
    public void StateFileName_IsStateJson() =>
        Assert.Equal("state.json", WorkspaceLayout.StateFileName);

    [Fact]
    public void AllDirectories_ContainsTopLevelContextOsDirectories()
    {
        Assert.Contains("config", WorkspaceLayout.AllDirectories);
        Assert.Contains("connectors", WorkspaceLayout.AllDirectories);
        Assert.Contains("indexes", WorkspaceLayout.AllDirectories);
        Assert.Contains("knowledge", WorkspaceLayout.AllDirectories);
        Assert.Contains("memory", WorkspaceLayout.AllDirectories);
        Assert.Contains("models", WorkspaceLayout.AllDirectories);
        Assert.Contains("snapshots", WorkspaceLayout.AllDirectories);
        Assert.Contains("telemetry", WorkspaceLayout.AllDirectories);
        Assert.Contains("temp", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedConnectorDirectories()
    {
        Assert.Contains("connectors/git", WorkspaceLayout.AllDirectories);
        Assert.Contains("connectors/jira", WorkspaceLayout.AllDirectories);
        Assert.Contains("connectors/filesystem", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedIndexDirectories()
    {
        Assert.Contains("indexes/semantic", WorkspaceLayout.AllDirectories);
        Assert.Contains("indexes/keyword", WorkspaceLayout.AllDirectories);
        Assert.Contains("indexes/graph", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedMemoryDirectories()
    {
        Assert.Contains("memory/working", WorkspaceLayout.AllDirectories);
        Assert.Contains("memory/episodic", WorkspaceLayout.AllDirectories);
        Assert.Contains("memory/longterm", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void AllDirectories_ContainsNestedSnapshotDirectories()
    {
        Assert.Contains("snapshots/workspace", WorkspaceLayout.AllDirectories);
        Assert.Contains("snapshots/indexes", WorkspaceLayout.AllDirectories);
        Assert.Contains("snapshots/knowledge", WorkspaceLayout.AllDirectories);
    }

    [Fact]
    public void ConfigFileNames_ContainsFourFiles()
    {
        Assert.Contains("runtime.json", WorkspaceLayout.ConfigFileNames);
        Assert.Contains("plugins.json", WorkspaceLayout.ConfigFileNames);
        Assert.Contains("models.json", WorkspaceLayout.ConfigFileNames);
        Assert.Contains("connectors.json", WorkspaceLayout.ConfigFileNames);
        Assert.Equal(4, WorkspaceLayout.ConfigFileNames.Count);
    }
}
```

- [ ] **Step 5: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests 2>&1 | Select-String "error|FAILED"
```

Expected: Build error — `WorkspaceLayout` not found.

- [ ] **Step 6: Implement `WorkspaceLayout`**

```csharp
// src/Ferret.Workspace/WorkspaceLayout.cs
namespace Ferret.Workspace;

/// <summary>Canonical file and directory names within a .ferret workspace root.</summary>
internal static class WorkspaceLayout
{
    internal const string RootDirectoryName = ".ferret";
    internal const string ManifestFileName = "workspace.json";
    internal const string StateFileName = "state.json";
    internal const string ConfigDirectoryName = "config";
    internal const string CacheDirectoryName = "cache";
    internal const string LogsDirectoryName = "logs";
    internal const string PluginsDirectoryName = "plugins";
    internal const string ConnectorsDirectoryName = "connectors";
    internal const string IndexesDirectoryName = "indexes";
    internal const string KnowledgeDirectoryName = "knowledge";
    internal const string MemoryDirectoryName = "memory";
    internal const string ArtifactsDirectoryName = "artifacts";
    internal const string ModelsDirectoryName = "models";
    internal const string SnapshotsDirectoryName = "snapshots";
    internal const string TelemetryDirectoryName = "telemetry";
    internal const string TempDirectoryName = "temp";

    // Flat ordered list — every path relative to .ferret/ root.
    // Directory.CreateDirectory is idempotent; create in order so parents precede children.
    internal static readonly IReadOnlyList<string> AllDirectories =
    [
        ConfigDirectoryName,
        CacheDirectoryName,
        LogsDirectoryName,
        PluginsDirectoryName,
        ConnectorsDirectoryName,
        "connectors/git",
        "connectors/jira",
        "connectors/github",
        "connectors/azuredevops",
        "connectors/confluence",
        "connectors/filesystem",
        "connectors/logs",
        IndexesDirectoryName,
        "indexes/semantic",
        "indexes/keyword",
        "indexes/graph",
        KnowledgeDirectoryName,
        "knowledge/entities",
        "knowledge/relationships",
        "knowledge/documents",
        MemoryDirectoryName,
        "memory/working",
        "memory/episodic",
        "memory/longterm",
        ArtifactsDirectoryName,
        ModelsDirectoryName,
        "models/embeddings",
        "models/rerankers",
        "models/llms",
        SnapshotsDirectoryName,
        "snapshots/workspace",
        "snapshots/indexes",
        "snapshots/knowledge",
        TelemetryDirectoryName,
        "telemetry/metrics",
        "telemetry/events",
        "telemetry/diagnostics",
        TempDirectoryName,
    ];

    // Empty JSON config files written to config/ on init.
    internal static readonly IReadOnlyList<string> ConfigFileNames =
    [
        "runtime.json",
        "plugins.json",
        "models.json",
        "connectors.json",
    ];
}
```

- [ ] **Step 7: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 9`

- [ ] **Step 8: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/ src/Ferret.sln
git commit -m "chore(sprint-7): Ferret.Workspace scaffold + WorkspaceLayout ContextOS directory tree"
```

---

### Task 2: `WorkspaceManifest` and `WorkspaceStateDto` JSON models

**Files:**
- Create: `src/Ferret.Workspace/Persistence/WorkspaceManifest.cs`
- Create: `src/Ferret.Workspace/Persistence/WorkspaceStateDto.cs`
- Create: `src/Ferret.Workspace/Persistence/StatisticsDto.cs`
- Create: `src/Ferret.Workspace/Persistence/ConnectorStateDto.cs`
- Create: `tests/Ferret.Workspace.Tests/Persistence/WorkspaceJsonModelsTests.cs`

**Interfaces:**
- Produces:
  - `WorkspaceManifest` — serializes to/from workspace.json; includes ContextOS fields: `contextOsVersion`, `workspaceType`, `features`, `enabledConnectors`, `enabledModels`
  - `WorkspaceStateDto` — serializes to/from state.json; nested `Statistics` sub-object; top-level `KnowledgeVersion`, `GraphVersion`, `LastIndex`, `Connectors`
  - `StatisticsDto` — nested DTO within `WorkspaceStateDto.Statistics`
  - `ConnectorStateDto` — per-connector state within `WorkspaceStateDto.Connectors`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/Persistence/WorkspaceJsonModelsTests.cs
using System.Text.Json;
using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests.Persistence;

public sealed class WorkspaceJsonModelsTests
{
    [Fact]
    public void WorkspaceManifest_SerializesContextOsFields()
    {
        var manifest = new WorkspaceManifest
        {
            Id = "ws-001",
            Name = "my-project",
            SchemaVersion = "1.0",
            FerretVersion = "0.7.0",
            ContextOsVersion = "1.0",
            CreatedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
            WorkspaceType = "repository",
        };

        var json = JsonSerializer.Serialize(manifest);
        var restored = JsonSerializer.Deserialize<WorkspaceManifest>(json)!;

        Assert.Equal("ws-001", restored.Id);
        Assert.Equal("1.0", restored.ContextOsVersion);
        Assert.Equal("repository", restored.WorkspaceType);
        Assert.Contains("contextOsVersion", json);
        Assert.Contains("workspaceType", json);
    }

    [Fact]
    public void WorkspaceStateDto_NestedStatistics_RoundTrips()
    {
        var dto = new WorkspaceStateDto
        {
            KnowledgeVersion = 1,
            GraphVersion = 2,
            Statistics = new StatisticsDto { TotalFiles = 50, IndexedFiles = 40, SchemaVersion = "1.0" },
        };

        var json = JsonSerializer.Serialize(dto);
        var restored = JsonSerializer.Deserialize<WorkspaceStateDto>(json)!;

        Assert.Equal(1, restored.KnowledgeVersion);
        Assert.Equal(2, restored.GraphVersion);
        Assert.Equal(50, restored.Statistics.TotalFiles);
        Assert.Equal(40, restored.Statistics.IndexedFiles);
    }

    [Fact]
    public void WorkspaceStateDto_LastIndex_NullableRoundTrips()
    {
        var dto = new WorkspaceStateDto { LastIndex = null };
        var json = JsonSerializer.Serialize(dto);
        var restored = JsonSerializer.Deserialize<WorkspaceStateDto>(json)!;
        Assert.Null(restored.LastIndex);
    }

    [Fact]
    public void ConnectorStateDto_RoundTrips()
    {
        var state = new ConnectorStateDto { Enabled = true, LastSyncAt = DateTimeOffset.UtcNow };
        var json = JsonSerializer.Serialize(state);
        var restored = JsonSerializer.Deserialize<ConnectorStateDto>(json)!;
        Assert.True(restored.Enabled);
        Assert.NotNull(restored.LastSyncAt);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests --filter "WorkspaceJsonModels" 2>&1 | Select-String "error|FAILED"
```

Expected: Build error — types not found.

- [ ] **Step 3: Implement `WorkspaceManifest`**

```csharp
// src/Ferret.Workspace/Persistence/WorkspaceManifest.cs
using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

internal sealed class WorkspaceManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    [JsonPropertyName("ferretVersion")]
    public string FerretVersion { get; set; } = string.Empty;

    [JsonPropertyName("contextOsVersion")]
    public string ContextOsVersion { get; set; } = "1.0";

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("workspaceType")]
    public string WorkspaceType { get; set; } = "repository";

    [JsonPropertyName("features")]
    public Dictionary<string, bool>? Features { get; set; }

    [JsonPropertyName("enabledConnectors")]
    public List<string>? EnabledConnectors { get; set; }

    [JsonPropertyName("enabledModels")]
    public List<string>? EnabledModels { get; set; }
}
```

- [ ] **Step 4: Implement `StatisticsDto`**

```csharp
// src/Ferret.Workspace/Persistence/StatisticsDto.cs
using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

internal sealed class StatisticsDto
{
    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("indexedFiles")]
    public int IndexedFiles { get; set; }

    [JsonPropertyName("lastIndexedAt")]
    public DateTimeOffset? LastIndexedAt { get; set; }

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";
}
```

- [ ] **Step 5: Implement `ConnectorStateDto`**

```csharp
// src/Ferret.Workspace/Persistence/ConnectorStateDto.cs
using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

internal sealed class ConnectorStateDto
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("lastSyncAt")]
    public DateTimeOffset? LastSyncAt { get; set; }
}
```

- [ ] **Step 6: Implement `WorkspaceStateDto`**

```csharp
// src/Ferret.Workspace/Persistence/WorkspaceStateDto.cs
using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

internal sealed class WorkspaceStateDto
{
    [JsonPropertyName("knowledgeVersion")]
    public int KnowledgeVersion { get; set; }

    [JsonPropertyName("graphVersion")]
    public int GraphVersion { get; set; }

    [JsonPropertyName("lastIndex")]
    public DateTimeOffset? LastIndex { get; set; }

    [JsonPropertyName("connectors")]
    public Dictionary<string, ConnectorStateDto>? Connectors { get; set; }

    [JsonPropertyName("statistics")]
    public StatisticsDto Statistics { get; set; } = new();
}
```

- [ ] **Step 7: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 13`

- [ ] **Step 8: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/
git commit -m "feat(sprint-7): WorkspaceManifest + WorkspaceStateDto — ContextOS-ready JSON schemas"
```

---

### Task 3: Connector contracts in `Ferret.Core`

**Files:**
- Create: `src/Ferret.Core/Connectors/ConnectorType.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorMetadata.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorCapabilities.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorHealth.cs`
- Create: `src/Ferret.Core/Connectors/IConnector.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/ConnectorContractTests.cs`

**Interfaces:**
- Produces (all public, contracts only — zero implementation):
  - `ConnectorType` enum: `Filesystem`, `Git`, `Jira`, `GitHub`, `AzureDevOps`, `Confluence`, `SharePoint`, `Slack`, `Teams`, `Logs`, `Custom`
  - `ConnectorMetadata` — id, name, description, type, version; factory method `Create(...)`
  - `ConnectorCapabilities` — canRead, canWrite, canStream, supportsChangeDetection; `Create(...)` and `ReadOnly()` factories
  - `ConnectorHealth` — isConnected, errorMessage, checkedAt; `Connected(...)` and `Disconnected(...)` factories
  - `IConnector` — ConnectorType, Metadata, Capabilities; `GetHealthAsync`, `ConnectAsync`, `DisconnectAsync`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Core.Tests/Connectors/ConnectorContractTests.cs
using Ferret.Core.Connectors;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorContractTests
{
    [Fact]
    public void ConnectorType_HasExpectedValues()
    {
        Assert.Equal(0, (int)ConnectorType.Filesystem);
        Assert.Equal(1, (int)ConnectorType.Git);
        Assert.Equal(99, (int)ConnectorType.Custom);
    }

    [Fact]
    public void ConnectorMetadata_Create_StoresValues()
    {
        var meta = ConnectorMetadata.Create("fs-001", "Filesystem", "Local filesystem connector", ConnectorType.Filesystem, "1.0");
        Assert.Equal("fs-001", meta.Id);
        Assert.Equal("Filesystem", meta.Name);
        Assert.Equal(ConnectorType.Filesystem, meta.ConnectorType);
        Assert.Equal("1.0", meta.Version);
    }

    [Fact]
    public void ConnectorCapabilities_Create_StoresValues()
    {
        var caps = ConnectorCapabilities.Create(canRead: true, canWrite: false, canStream: true, supportsChangeDetection: true);
        Assert.True(caps.CanRead);
        Assert.False(caps.CanWrite);
        Assert.True(caps.SupportsChangeDetection);
    }

    [Fact]
    public void ConnectorCapabilities_ReadOnly_OnlyCanRead()
    {
        var caps = ConnectorCapabilities.ReadOnly();
        Assert.True(caps.CanRead);
        Assert.False(caps.CanWrite);
        Assert.False(caps.CanStream);
        Assert.False(caps.SupportsChangeDetection);
    }

    [Fact]
    public void ConnectorHealth_Connected_IsConnected()
    {
        var health = ConnectorHealth.Connected(DateTimeOffset.UtcNow);
        Assert.True(health.IsConnected);
        Assert.Null(health.ErrorMessage);
    }

    [Fact]
    public void ConnectorHealth_Disconnected_HasErrorMessage()
    {
        var health = ConnectorHealth.Disconnected("timeout", DateTimeOffset.UtcNow);
        Assert.False(health.IsConnected);
        Assert.Equal("timeout", health.ErrorMessage);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Core.Tests --filter "ConnectorContract" 2>&1 | Select-String "error|FAILED"
```

Expected: Build error — namespace not found.

- [ ] **Step 3: Implement `ConnectorType`**

```csharp
// src/Ferret.Core/Connectors/ConnectorType.cs
namespace Ferret.Core.Connectors;

/// <summary>Identifies the category of a context source connector.</summary>
public enum ConnectorType
{
    /// <summary>Local file system.</summary>
    Filesystem = 0,
    /// <summary>Git version control.</summary>
    Git = 1,
    /// <summary>Atlassian JIRA.</summary>
    Jira = 2,
    /// <summary>GitHub.</summary>
    GitHub = 3,
    /// <summary>Azure DevOps.</summary>
    AzureDevOps = 4,
    /// <summary>Atlassian Confluence.</summary>
    Confluence = 5,
    /// <summary>Microsoft SharePoint.</summary>
    SharePoint = 6,
    /// <summary>Slack messaging.</summary>
    Slack = 7,
    /// <summary>Microsoft Teams.</summary>
    Teams = 8,
    /// <summary>Log files and streams.</summary>
    Logs = 9,
    /// <summary>User-defined connector.</summary>
    Custom = 99,
}
```

- [ ] **Step 4: Implement `ConnectorMetadata`**

```csharp
// src/Ferret.Core/Connectors/ConnectorMetadata.cs
namespace Ferret.Core.Connectors;

/// <summary>Descriptive metadata for a context source connector.</summary>
public sealed class ConnectorMetadata
{
    private ConnectorMetadata(string id, string name, string description, ConnectorType connectorType, string version)
    {
        Id = id;
        Name = name;
        Description = description;
        ConnectorType = connectorType;
        Version = version;
    }

    /// <summary>Gets the unique connector identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the human-readable connector name.</summary>
    public string Name { get; }

    /// <summary>Gets the connector description.</summary>
    public string Description { get; }

    /// <summary>Gets the connector type category.</summary>
    public ConnectorType ConnectorType { get; }

    /// <summary>Gets the connector version string.</summary>
    public string Version { get; }

    /// <summary>Creates a new <see cref="ConnectorMetadata"/> instance.</summary>
    public static ConnectorMetadata Create(string id, string name, string description, ConnectorType connectorType, string version) =>
        new(id ?? string.Empty, name ?? string.Empty, description ?? string.Empty, connectorType, version ?? string.Empty);
}
```

- [ ] **Step 5: Implement `ConnectorCapabilities`**

```csharp
// src/Ferret.Core/Connectors/ConnectorCapabilities.cs
namespace Ferret.Core.Connectors;

/// <summary>Describes what operations a connector supports.</summary>
public sealed class ConnectorCapabilities
{
    private ConnectorCapabilities(bool canRead, bool canWrite, bool canStream, bool supportsChangeDetection)
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

    /// <summary>Creates a <see cref="ConnectorCapabilities"/> with explicit values.</summary>
    public static ConnectorCapabilities Create(bool canRead, bool canWrite, bool canStream, bool supportsChangeDetection) =>
        new(canRead, canWrite, canStream, supportsChangeDetection);

    /// <summary>Creates a read-only <see cref="ConnectorCapabilities"/>.</summary>
    public static ConnectorCapabilities ReadOnly() => new(true, false, false, false);
}
```

- [ ] **Step 6: Implement `ConnectorHealth`**

```csharp
// src/Ferret.Core/Connectors/ConnectorHealth.cs
namespace Ferret.Core.Connectors;

/// <summary>Represents the health status of a connector at a point in time.</summary>
public sealed class ConnectorHealth
{
    private ConnectorHealth(bool isConnected, string? errorMessage, DateTimeOffset checkedAt)
    {
        IsConnected = isConnected;
        ErrorMessage = errorMessage;
        CheckedAt = checkedAt;
    }

    /// <summary>Gets a value indicating whether the connector is currently reachable.</summary>
    public bool IsConnected { get; }

    /// <summary>Gets the error message if the connector is not connected; otherwise <see langword="null"/>.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets the UTC timestamp when this health check was performed.</summary>
    public DateTimeOffset CheckedAt { get; }

    /// <summary>Creates a healthy <see cref="ConnectorHealth"/>.</summary>
    public static ConnectorHealth Connected(DateTimeOffset checkedAt) => new(true, null, checkedAt);

    /// <summary>Creates an unhealthy <see cref="ConnectorHealth"/>.</summary>
    public static ConnectorHealth Disconnected(string errorMessage, DateTimeOffset checkedAt) =>
        new(false, errorMessage ?? string.Empty, checkedAt);
}
```

- [ ] **Step 7: Implement `IConnector`**

```csharp
// src/Ferret.Core/Connectors/IConnector.cs
namespace Ferret.Core.Connectors;

/// <summary>
/// Contract for all ContextOS context source connectors.
/// Sprint 8 delivers the first implementation: FilesystemConnector.
/// </summary>
public interface IConnector
{
    /// <summary>Gets the connector type category.</summary>
    ConnectorType ConnectorType { get; }

    /// <summary>Gets the connector metadata.</summary>
    ConnectorMetadata Metadata { get; }

    /// <summary>Gets the connector's declared capabilities.</summary>
    ConnectorCapabilities Capabilities { get; }

    /// <summary>Returns the current health of this connector.</summary>
    Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default);

    /// <summary>Establishes the connection to the underlying source.</summary>
    /// <returns><see langword="true"/> if the connection was established successfully.</returns>
    Task<bool> ConnectAsync(CancellationToken ct = default);

    /// <summary>Closes the connection to the underlying source.</summary>
    Task DisconnectAsync(CancellationToken ct = default);
}
```

- [ ] **Step 8: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests -v minimal
```

Expected: All Core tests pass including 6 new connector contract tests.

- [ ] **Step 9: Commit**

```powershell
git add src/Ferret.Core/ tests/Ferret.Core.Tests/
git commit -m "feat(sprint-7): IConnector + ConnectorType/Metadata/Capabilities/Health contracts in Ferret.Core"
```

---

## Sub-Sprint 7b — Workspace Persistence (Tasks 4–5)

### Task 4: `JsonWorkspaceStore` — read/write workspace.json and state.json

**Files:**
- Create: `src/Ferret.Workspace/Persistence/JsonWorkspaceStore.cs`
- Create: `tests/Ferret.Workspace.Tests/TempDirectory.cs`
- Create: `tests/Ferret.Workspace.Tests/Persistence/JsonWorkspaceStoreTests.cs`

**Interfaces:**
- Consumes: `WorkspaceLayout`, `WorkspaceManifest`, `WorkspaceStateDto`, `WorkspacePath`
- Produces: `JsonWorkspaceStore` — internal; `ReadManifestAsync`, `WriteManifestAsync`, `ReadStateAsync`, `WriteStateAsync`

- [ ] **Step 1: Create the `TempDirectory` test helper**

```csharp
// tests/Ferret.Workspace.Tests/TempDirectory.cs
namespace Ferret.Workspace.Tests;

internal sealed class TempDirectory : IDisposable
{
    internal string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "ferret-tests-" + Guid.NewGuid().ToString("N")[..8]);

    internal TempDirectory() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/Persistence/JsonWorkspaceStoreTests.cs
using Ferret.Core.Workspace;
using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests.Persistence;

public sealed class JsonWorkspaceStoreTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly JsonWorkspaceStore _store = new();

    public void Dispose() => _dir.Dispose();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    private void CreateFerretDir() =>
        Directory.CreateDirectory(System.IO.Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName));

    [Fact]
    public async Task ReadManifest_WhenFileNotExists_ReturnsNull()
    {
        CreateFerretDir();
        var result = await _store.ReadManifestAsync(RootPath, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteManifest_ThenRead_RoundTrips()
    {
        CreateFerretDir();
        var manifest = new WorkspaceManifest
        {
            Id = "ws-001",
            Name = "test-project",
            ContextOsVersion = "1.0",
            WorkspaceType = "repository",
            CreatedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
        };

        await _store.WriteManifestAsync(RootPath, manifest, CancellationToken.None);
        var restored = await _store.ReadManifestAsync(RootPath, CancellationToken.None);

        Assert.NotNull(restored);
        Assert.Equal("ws-001", restored.Id);
        Assert.Equal("1.0", restored.ContextOsVersion);
        Assert.Equal("repository", restored.WorkspaceType);
    }

    [Fact]
    public async Task ReadState_WhenFileNotExists_ReturnsNull()
    {
        CreateFerretDir();
        var result = await _store.ReadStateAsync(RootPath, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteState_ThenRead_RoundTrips_NestedStatistics()
    {
        CreateFerretDir();
        var dto = new WorkspaceStateDto
        {
            KnowledgeVersion = 1,
            GraphVersion = 2,
            Statistics = new StatisticsDto { TotalFiles = 5, IndexedFiles = 3 },
        };

        await _store.WriteStateAsync(RootPath, dto, CancellationToken.None);
        var restored = await _store.ReadStateAsync(RootPath, CancellationToken.None);

        Assert.NotNull(restored);
        Assert.Equal(1, restored.KnowledgeVersion);
        Assert.Equal(5, restored.Statistics.TotalFiles);
    }
}
```

- [ ] **Step 3: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests --filter "JsonWorkspaceStore" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 4: Implement `JsonWorkspaceStore`**

```csharp
// src/Ferret.Workspace/Persistence/JsonWorkspaceStore.cs
using System.Text.Json;
using Ferret.Core.Workspace;

namespace Ferret.Workspace.Persistence;

internal sealed class JsonWorkspaceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal async Task<WorkspaceManifest?> ReadManifestAsync(WorkspacePath rootPath, CancellationToken ct)
    {
        var path = ManifestPath(rootPath);
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<WorkspaceManifest>(stream, JsonOptions, ct).ConfigureAwait(false);
    }

    internal async Task WriteManifestAsync(WorkspacePath rootPath, WorkspaceManifest manifest, CancellationToken ct)
    {
        var path = ManifestPath(rootPath);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, ct).ConfigureAwait(false);
    }

    internal async Task<WorkspaceStateDto?> ReadStateAsync(WorkspacePath rootPath, CancellationToken ct)
    {
        var path = StatePath(rootPath);
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<WorkspaceStateDto>(stream, JsonOptions, ct).ConfigureAwait(false);
    }

    internal async Task WriteStateAsync(WorkspacePath rootPath, WorkspaceStateDto dto, CancellationToken ct)
    {
        var path = StatePath(rootPath);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, dto, JsonOptions, ct).ConfigureAwait(false);
    }

    private static string ManifestPath(WorkspacePath rootPath) =>
        Path.Combine(rootPath.FullPath, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.ManifestFileName);

    private static string StatePath(WorkspacePath rootPath) =>
        Path.Combine(rootPath.FullPath, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.StateFileName);
}
```

- [ ] **Step 5: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 17`

- [ ] **Step 6: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/
git commit -m "feat(sprint-7): JsonWorkspaceStore — read/write ContextOS workspace.json and state.json"
```

---

### Task 5: `WorkspaceStateStore : IWorkspaceStateStore`

**Files:**
- Create: `src/Ferret.Workspace/WorkspaceStateStore.cs`
- Create: `tests/Ferret.Workspace.Tests/WorkspaceStateStoreTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceStateStore`, `JsonWorkspaceStore`, `WorkspaceStateDto`, `StatisticsDto`, `WorkspaceStatistics`
- Produces: `WorkspaceStateStore` — `WriteStatisticsAsync` reads the existing state.json first to preserve `KnowledgeVersion`, `GraphVersion`, and `Connectors` before overwriting the `Statistics` sub-object

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/WorkspaceStateStoreTests.cs
using Ferret.Core.Workspace;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceStateStoreTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceStateStore _store = new();

    public void Dispose() => _dir.Dispose();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    private void CreateFerretDir() =>
        Directory.CreateDirectory(Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName));

    [Fact]
    public async Task ReadStatistics_WhenNoFile_ReturnsDefaults()
    {
        CreateFerretDir();
        var stats = await _store.ReadStatisticsAsync(RootPath);

        Assert.Equal(0, stats.TotalFiles);
        Assert.Equal(0, stats.IndexedFiles);
        Assert.Equal(DateTimeOffset.MinValue, stats.LastIndexed);
    }

    [Fact]
    public async Task WriteStatistics_ThenRead_RoundTrips()
    {
        CreateFerretDir();
        var expected = WorkspaceStatistics.Create(100, 80,
            new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero), "1.0");

        await _store.WriteStatisticsAsync(RootPath, expected);
        var restored = await _store.ReadStatisticsAsync(RootPath);

        Assert.Equal(100, restored.TotalFiles);
        Assert.Equal(80, restored.IndexedFiles);
        Assert.Equal(expected.LastIndexed, restored.LastIndexed);
    }

    [Fact]
    public async Task WriteStatistics_PreservesExistingStateFields()
    {
        CreateFerretDir();
        // Write initial state with KnowledgeVersion set
        var store = new Persistence.JsonWorkspaceStore();
        await store.WriteStateAsync(RootPath,
            new Persistence.WorkspaceStateDto { KnowledgeVersion = 7 },
            CancellationToken.None);

        // Now update statistics — KnowledgeVersion must survive
        var stats = WorkspaceStatistics.Create(10, 10, DateTimeOffset.MinValue, "1.0");
        await _store.WriteStatisticsAsync(RootPath, stats);

        var dto = await store.ReadStateAsync(RootPath, CancellationToken.None);
        Assert.Equal(7, dto!.KnowledgeVersion);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests --filter "WorkspaceStateStore" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 3: Implement `WorkspaceStateStore`**

```csharp
// src/Ferret.Workspace/WorkspaceStateStore.cs
using Ferret.Core.Workspace;
using Ferret.Workspace.Persistence;

namespace Ferret.Workspace;

/// <summary>Reads and writes workspace statistics from the statistics sub-object in state.json.</summary>
public sealed class WorkspaceStateStore : IWorkspaceStateStore
{
    private readonly JsonWorkspaceStore _store = new();

    /// <inheritdoc/>
    public async Task<WorkspaceStatistics> ReadStatisticsAsync(WorkspacePath rootPath, CancellationToken ct = default)
    {
        var dto = await _store.ReadStateAsync(rootPath, ct).ConfigureAwait(false);
        if (dto is null)
            return WorkspaceStatistics.Create(0, 0, DateTimeOffset.MinValue, "1.0");

        var s = dto.Statistics;
        return WorkspaceStatistics.Create(
            s.TotalFiles,
            s.IndexedFiles,
            s.LastIndexedAt ?? DateTimeOffset.MinValue,
            s.SchemaVersion);
    }

    /// <inheritdoc/>
    public async Task WriteStatisticsAsync(WorkspacePath rootPath, WorkspaceStatistics statistics, CancellationToken ct = default)
    {
        // Read first so KnowledgeVersion, GraphVersion, and Connectors are preserved.
        var dto = await _store.ReadStateAsync(rootPath, ct).ConfigureAwait(false) ?? new WorkspaceStateDto();
        dto.Statistics = new StatisticsDto
        {
            TotalFiles = statistics.TotalFiles,
            IndexedFiles = statistics.IndexedFiles,
            LastIndexedAt = statistics.LastIndexed == DateTimeOffset.MinValue ? null : statistics.LastIndexed,
            SchemaVersion = statistics.SchemaVersion,
        };
        await _store.WriteStateAsync(rootPath, dto, ct).ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 20`

- [ ] **Step 5: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/
git commit -m "feat(sprint-7): WorkspaceStateStore — preserves state.json fields on statistics write"
```

---

## Sub-Sprint 7c — Workspace Lifecycle (Tasks 6–8)

### Task 6: `WorkspaceLocator : IWorkspaceLocator`

**Files:**
- Create: `src/Ferret.Workspace/WorkspaceLocator.cs`
- Create: `tests/Ferret.Workspace.Tests/WorkspaceLocatorTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceLocator`, `WorkspaceLayout`, `WorkspacePath`
- Produces: `WorkspaceLocator` — walks up the directory tree looking for `.ferret/workspace.json`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/WorkspaceLocatorTests.cs
using Ferret.Core.Workspace;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceLocatorTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceLocator _locator = new();

    public void Dispose() => _dir.Dispose();

    private void CreateWorkspaceAt(string root)
    {
        var ferretDir = Path.Combine(root, WorkspaceLayout.RootDirectoryName);
        Directory.CreateDirectory(ferretDir);
        File.WriteAllText(Path.Combine(ferretDir, WorkspaceLayout.ManifestFileName), "{}");
    }

    [Fact]
    public async Task LocateAsync_WhenDotFerretAtRoot_ReturnsRoot()
    {
        CreateWorkspaceAt(_dir.Path);
        var result = await _locator.LocateAsync(WorkspacePath.Create(_dir.Path));
        Assert.NotNull(result);
        Assert.Equal(_dir.Path, result.FullPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LocateAsync_WhenCalledFromSubdirectory_FindsAncestorRoot()
    {
        CreateWorkspaceAt(_dir.Path);
        var subDir = Path.Combine(_dir.Path, "src", "core");
        Directory.CreateDirectory(subDir);
        var result = await _locator.LocateAsync(WorkspacePath.Create(subDir));
        Assert.NotNull(result);
        Assert.Equal(_dir.Path, result.FullPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LocateAsync_WhenNoWorkspaceExists_ReturnsNull()
    {
        var result = await _locator.LocateAsync(WorkspacePath.Create(_dir.Path));
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenDotFerretWithManifest_ReturnsTrue()
    {
        CreateWorkspaceAt(_dir.Path);
        Assert.True(await _locator.ExistsAsync(WorkspacePath.Create(_dir.Path)));
    }

    [Fact]
    public async Task ExistsAsync_WhenNoDotFerret_ReturnsFalse()
    {
        Assert.False(await _locator.ExistsAsync(WorkspacePath.Create(_dir.Path)));
    }

    [Fact]
    public async Task ExistsAsync_WhenDotFerretExistsButNoManifest_ReturnsFalse()
    {
        Directory.CreateDirectory(Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName));
        Assert.False(await _locator.ExistsAsync(WorkspacePath.Create(_dir.Path)));
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests --filter "WorkspaceLocator" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 3: Implement `WorkspaceLocator`**

```csharp
// src/Ferret.Workspace/WorkspaceLocator.cs
using Ferret.Core.Workspace;

namespace Ferret.Workspace;

/// <summary>Locates workspace roots by walking up the file system from a starting path.</summary>
public sealed class WorkspaceLocator : IWorkspaceLocator
{
    /// <inheritdoc/>
    public Task<WorkspacePath?> LocateAsync(WorkspacePath searchPath, CancellationToken ct = default)
    {
        var current = searchPath.FullPath;
        while (current is not null)
        {
            var ferretDir = Path.Combine(current, WorkspaceLayout.RootDirectoryName);
            var manifest = Path.Combine(ferretDir, WorkspaceLayout.ManifestFileName);
            if (Directory.Exists(ferretDir) && File.Exists(manifest))
                return Task.FromResult<WorkspacePath?>(WorkspacePath.Create(current));
            current = Path.GetDirectoryName(current);
        }

        return Task.FromResult<WorkspacePath?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(WorkspacePath rootPath, CancellationToken ct = default)
    {
        var ferretDir = Path.Combine(rootPath.FullPath, WorkspaceLayout.RootDirectoryName);
        var manifest = Path.Combine(ferretDir, WorkspaceLayout.ManifestFileName);
        return Task.FromResult(Directory.Exists(ferretDir) && File.Exists(manifest));
    }
}
```

- [ ] **Step 4: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 26`

- [ ] **Step 5: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/
git commit -m "feat(sprint-7): WorkspaceLocator — walk-up discovery of .ferret workspace root"
```

---

### Task 7: `WorkspaceInitializer` (internal)

**Files:**
- Create: `src/Ferret.Workspace/WorkspaceInitializer.cs`
- Create: `tests/Ferret.Workspace.Tests/WorkspaceInitializerTests.cs`

**Interfaces:**
- Consumes: `WorkspaceLayout`, `JsonWorkspaceStore`, `WorkspaceManifest`, `WorkspaceStateDto`, `WorkspacePath`, `WorkspaceOptions`, `WorkspaceContext`, `WorkspaceMetadata`, `WorkspaceCapabilities`, `WorkspaceId`
- Produces: `WorkspaceInitializer` — internal sealed; creates all `WorkspaceLayout.AllDirectories`, writes all `WorkspaceLayout.ConfigFileNames` as empty JSON (`{}`), writes workspace.json and state.json, returns `WorkspaceContext`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/WorkspaceInitializerTests.cs
using Ferret.Core.Workspace;
using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceInitializerTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceInitializer _initializer = new(new JsonWorkspaceStore());

    public void Dispose() => _dir.Dispose();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    [Fact]
    public async Task InitialiseAsync_CreatesDotFerretDirectory()
    {
        await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        Assert.True(Directory.Exists(Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName)));
    }

    [Fact]
    public async Task InitialiseAsync_CreatesAllContextOsDirectories()
    {
        await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var ferretDir = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName);
        foreach (var sub in WorkspaceLayout.AllDirectories)
        {
            var fullPath = Path.Combine(ferretDir, sub.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(Directory.Exists(fullPath), $"Missing directory: {sub}");
        }
    }

    [Fact]
    public async Task InitialiseAsync_WritesManifestFile()
    {
        await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var manifestPath = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.ManifestFileName);
        Assert.True(File.Exists(manifestPath));
    }

    [Fact]
    public async Task InitialiseAsync_WritesStateFile()
    {
        await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var statePath = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.StateFileName);
        Assert.True(File.Exists(statePath));
    }

    [Fact]
    public async Task InitialiseAsync_WritesAllConfigFiles()
    {
        await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var configDir = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.ConfigDirectoryName);
        foreach (var fileName in WorkspaceLayout.ConfigFileNames)
            Assert.True(File.Exists(Path.Combine(configDir, fileName)), $"Missing config: {fileName}");
    }

    [Fact]
    public async Task InitialiseAsync_ManifestContainsContextOsVersion()
    {
        await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var store = new JsonWorkspaceStore();
        var manifest = await store.ReadManifestAsync(RootPath, CancellationToken.None);
        Assert.NotNull(manifest);
        Assert.Equal("1.0", manifest.ContextOsVersion);
        Assert.Equal("repository", manifest.WorkspaceType);
    }

    [Fact]
    public async Task InitialiseAsync_ReturnsContextWithCorrectRootPath()
    {
        var context = await _initializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        Assert.Equal(RootPath, context.RootPath);
        Assert.NotEmpty(context.Metadata.Name);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests --filter "WorkspaceInitializer" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 3: Implement `WorkspaceInitializer`**

```csharp
// src/Ferret.Workspace/WorkspaceInitializer.cs
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Workspace.Persistence;

namespace Ferret.Workspace;

internal sealed class WorkspaceInitializer
{
    private readonly JsonWorkspaceStore _store;

    internal WorkspaceInitializer(JsonWorkspaceStore store) => _store = store;

    internal async Task<WorkspaceContext> InitialiseAsync(
        WorkspacePath rootPath,
        WorkspaceOptions? options,
        CancellationToken ct)
    {
        var ferretRoot = Path.Combine(rootPath.FullPath, WorkspaceLayout.RootDirectoryName);
        Directory.CreateDirectory(ferretRoot);

        foreach (var sub in WorkspaceLayout.AllDirectories)
            Directory.CreateDirectory(Path.Combine(ferretRoot, sub.Replace('/', Path.DirectorySeparatorChar)));

        var configDir = Path.Combine(ferretRoot, WorkspaceLayout.ConfigDirectoryName);
        foreach (var fileName in WorkspaceLayout.ConfigFileNames)
            await File.WriteAllTextAsync(Path.Combine(configDir, fileName), "{}", ct).ConfigureAwait(false);

        var idString = Guid.NewGuid().ToString();
        var rawName = Path.GetFileName(rootPath.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var name = string.IsNullOrEmpty(rawName) ? "workspace" : rawName;
        var now = DateTimeOffset.UtcNow;

        var manifest = new WorkspaceManifest
        {
            Id = idString,
            Name = name,
            Description = string.Empty,
            SchemaVersion = "1.0",
            FerretVersion = "0.7.0",
            ContextOsVersion = "1.0",
            CreatedAt = now,
            WorkspaceType = "repository",
        };
        await _store.WriteManifestAsync(rootPath, manifest, ct).ConfigureAwait(false);

        var stateDto = new WorkspaceStateDto { Statistics = new StatisticsDto { SchemaVersion = "1.0" } };
        await _store.WriteStateAsync(rootPath, stateDto, ct).ConfigureAwait(false);

        var id = WorkspaceId.Create(idString);
        var metadata = WorkspaceMetadata.Create(name, string.Empty, "1.0", now);
        var capabilities = WorkspaceCapabilities.Create(options?.ReadOnly ?? false, 0, 0);
        return WorkspaceContext.Create(rootPath, id, metadata, capabilities);
    }
}
```

- [ ] **Step 4: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 33`

- [ ] **Step 5: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/
git commit -m "feat(sprint-7): WorkspaceInitializer — ContextOS directory tree + config scaffolding"
```

---

### Task 8: `WorkspaceEngine : IWorkspaceEngine` — init and load

**Files:**
- Create: `src/Ferret.Workspace/WorkspaceEngine.cs`
- Create: `tests/Ferret.Workspace.Tests/WorkspaceEngineTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceEngine`, `WorkspaceLocator`, `WorkspaceInitializer`, `JsonWorkspaceStore`, `WorkspaceManifest`, `WorkspaceId`
- Produces: `WorkspaceEngine` — `InitialiseAsync` guards against re-init then delegates; `LoadAsync` reads manifest and reconstructs `WorkspaceContext`; remaining interface methods throw `NotImplementedException` (Sprint 8)

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Tests/WorkspaceEngineTests.cs
using Ferret.Core.Workspace;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceEngineTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceEngine _engine = new();

    public void Dispose() => _dir.Dispose();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    [Fact]
    public async Task InitialiseAsync_OnFreshDirectory_Succeeds()
    {
        var result = await _engine.InitialiseAsync(RootPath);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Context);
    }

    [Fact]
    public async Task InitialiseAsync_WhenAlreadyInitialised_ReturnsFailure()
    {
        await _engine.InitialiseAsync(RootPath);
        var second = await _engine.InitialiseAsync(RootPath);
        Assert.False(second.Succeeded);
        Assert.Contains("already exists", second.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_AfterInit_ReturnsContextWithSameId()
    {
        var initResult = await _engine.InitialiseAsync(RootPath);
        var context = await _engine.LoadAsync(RootPath);
        Assert.Equal(initResult.Context!.Id, context.Id);
    }

    [Fact]
    public async Task LoadAsync_WhenNoManifest_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.LoadAsync(RootPath));
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Workspace.Tests --filter "WorkspaceEngine" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 3: Implement `WorkspaceEngine`**

```csharp
// src/Ferret.Workspace/WorkspaceEngine.cs
using Ferret.Core.Primitives;
using Ferret.Core.Results;
using Ferret.Core.Workspace;
using Ferret.Workspace.Persistence;

namespace Ferret.Workspace;

/// <summary>
/// Implements IWorkspaceEngine for Sprint 7: init and load.
/// GetHealthAsync, GetChangesetAsync, UpgradeAsync, ValidateAsync are deferred to Sprint 8.
/// </summary>
public sealed class WorkspaceEngine : IWorkspaceEngine
{
    private readonly WorkspaceLocator _locator = new();
    private readonly JsonWorkspaceStore _store = new();
    private readonly WorkspaceInitializer _initializer;

    /// <summary>Initialises a new instance of <see cref="WorkspaceEngine"/>.</summary>
    public WorkspaceEngine() => _initializer = new WorkspaceInitializer(_store);

    /// <inheritdoc/>
    public async Task<WorkspaceInitResult> InitialiseAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default)
    {
        if (await _locator.ExistsAsync(rootPath, ct).ConfigureAwait(false))
            return WorkspaceInitResult.Failure($"Workspace already exists at: {rootPath.FullPath}");

        var context = await _initializer.InitialiseAsync(rootPath, options, ct).ConfigureAwait(false);
        return WorkspaceInitResult.Success(context);
    }

    /// <inheritdoc/>
    public async Task<WorkspaceContext> LoadAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default)
    {
        var manifest = await _store.ReadManifestAsync(rootPath, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No workspace manifest found at: {rootPath.FullPath}");

        var id = WorkspaceId.Create(manifest.Id);
        var metadata = WorkspaceMetadata.Create(manifest.Name, manifest.Description, manifest.SchemaVersion, manifest.CreatedAt);
        var capabilities = WorkspaceCapabilities.Create(options?.ReadOnly ?? false, 0, 0);
        return WorkspaceContext.Create(rootPath, id, metadata, capabilities);
    }

    /// <inheritdoc/>
    public Task<WorkspaceHealthReport> GetHealthAsync(WorkspaceContext context, HealthCheckDepth depth = HealthCheckDepth.Quick, CancellationToken ct = default) =>
        throw new NotImplementedException("Workspace health — Sprint 8.");

    /// <inheritdoc/>
    public Task<Changeset> GetChangesetAsync(WorkspaceContext context, CancellationToken ct = default) =>
        throw new NotImplementedException("Change detection — Sprint 8.");

    /// <inheritdoc/>
    public Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext context, CancellationToken ct = default) =>
        throw new NotImplementedException("Workspace upgrade — Sprint 8.");

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateAsync(WorkspaceContext context, CancellationToken ct = default) =>
        throw new NotImplementedException("Workspace validation — Sprint 8.");
}
```

- [ ] **Step 4: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Workspace.Tests -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 37`

- [ ] **Step 5: Commit**

```powershell
git add src/Ferret.Workspace/ tests/Ferret.Workspace.Tests/
git commit -m "feat(sprint-7): WorkspaceEngine — init (ContextOS scaffold) + load; Sprint 8 stubs"
```

---

## Sub-Sprint 7d — Workspace CLI (Tasks 9–11)

### Task 9: Update `RootCommandFactory` for grouped subcommands

**Files:**
- Modify: `src/Ferret.Cli/Commands/RootCommandFactory.cs`
- Modify: `src/Ferret.Cli/Properties/AssemblyInfo.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/RootCommandFactoryGroupingTests.cs`

**Interfaces:**
- Consumes: `CommandDefinition.Group` property (defined; currently unused)
- Produces: commands with a non-null `Group` are added as subcommands of the parent command with that name; a parent with `HandlerType = null` and no `PlannedSubcommands` does NOT call `RegisterGroupStubAction` — System.CommandLine shows auto-help

- [ ] **Step 1: Check for existing `Ferret.Cli.Tests` project; create if missing**

```powershell
Test-Path tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj
```

If false, create:

```xml
<!-- tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Ferret.Cli.Tests</AssemblyName>
    <RootNamespace>Ferret.Cli.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Cli\Ferret.Cli.csproj" />
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

Then: `dotnet sln src/Ferret.sln add tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`

- [ ] **Step 2: Add `InternalsVisibleTo` to `Ferret.Cli`**

Read `src/Ferret.Cli/Properties/AssemblyInfo.cs`. If it does not already contain `InternalsVisibleTo("Ferret.Cli.Tests")`, append:

```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ferret.Cli.Tests")]
```

- [ ] **Step 3: Write the failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/RootCommandFactoryGroupingTests.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class RootCommandFactoryGroupingTests
{
    private sealed class StubGroupModule : CliModuleBase
    {
        public override string Name => "stub";
        public override string Description => "Stub.";

        public override IEnumerable<CommandDefinition> GetCommands()
        {
            yield return new CommandDefinition(new CommandMetadata("grp", "A group."), HandlerType: null);
            yield return new CommandDefinition(new CommandMetadata("sub1", "Sub one."), typeof(object), Group: "grp");
            yield return new CommandDefinition(new CommandMetadata("sub2", "Sub two."), typeof(object), Group: "grp");
        }
    }

    [Fact]
    public void Build_WithGroupedCommands_SubcommandsAppearUnderParent()
    {
        var app = RootCommandFactory.Build([new StubGroupModule()]);
        var output = new StringWriter();
        app.Invoke(["grp", "--help"], output);
        var text = output.ToString();
        Assert.Contains("sub1", text);
        Assert.Contains("sub2", text);
    }

    [Fact]
    public void Build_EmptyGroupStub_StillShowsPlannedSubcommands()
    {
        var app = RootCommandFactory.Build([new CoreCliModule()]);
        var output = new StringWriter();
        app.Invoke(["index"], output);
        Assert.Contains("No commands are currently installed", output.ToString());
    }
}
```

- [ ] **Step 4: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Cli.Tests --filter "RootCommandFactoryGrouping" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 5: Update `RootCommandFactory.Build` — replace the command loop**

Replace the loop (currently `foreach (var def in moduleList.SelectMany...)`) with:

```csharp
var allDefs = moduleList.SelectMany(m => m.GetCommands()).ToList();
var grouped = allDefs
    .Where(d => d.Group is not null)
    .GroupBy(d => d.Group!, StringComparer.Ordinal)
    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

foreach (var def in allDefs.Where(d => d.Group is null))
{
    var cmd = BuildCommand(def, provider, config, output);
    if (grouped.TryGetValue(def.Metadata.Name, out var subDefs))
    {
        foreach (var subDef in subDefs)
            cmd.Add(BuildCommand(subDef, provider, config, output));
    }

    root.Add(cmd);
}
```

- [ ] **Step 6: Update `BuildCommand` — skip stub action when no planned subcommands**

In `BuildCommand`, replace:

```csharp
if (def.HandlerType is null)
{
    RegisterGroupStubAction(cmd, def, output);
}
```

with:

```csharp
if (def.HandlerType is null)
{
    if (def.PlannedSubcommands is { Count: > 0 })
        RegisterGroupStubAction(cmd, def, output);
    // else: real subcommands attached by caller — System.CommandLine shows auto-help
}
```

- [ ] **Step 7: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Cli.Tests -v minimal
```

- [ ] **Step 8: Run full suite to verify no regressions**

```powershell
dotnet test src/Ferret.sln -v minimal
```

- [ ] **Step 9: Commit**

```powershell
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/
git commit -m "feat(sprint-7): RootCommandFactory — grouped subcommand support via CommandDefinition.Group"
```

---

### Task 10: `WorkspaceInitCommandHandler` and `WorkspaceStatusCommandHandler`

**Files:**
- Create: `src/Ferret.Cli/Commands/Workspace/WorkspaceInitCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Workspace/WorkspaceStatusCommandHandler.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/Workspace/WorkspaceCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ICommandHandler`, `IFerretContext`, `IWorkspaceEngine` (Ferret.Core), `IWorkspaceLocator` (Ferret.Core), `CommandResult`, `WorkspacePath`
- Produces:
  - `WorkspaceInitCommandHandler(IWorkspaceEngine)` — calls `InitialiseAsync(WorkspacePath.Create(Environment.CurrentDirectory))`; prints success or error
  - `WorkspaceStatusCommandHandler(IWorkspaceLocator, IWorkspaceEngine)` — locates from CWD; prints name/ID/root/created or "Not in a Ferret workspace."

- [ ] **Step 1: Check `IFerretServices` and `IOutputFormatter` interfaces**

Read `src/Ferret.Cli/Cli/IFerretServices.cs` and `src/Ferret.Cli/Cli/IOutputFormatter.cs` before writing fakes. Adjust the fake implementations in the test file to match the actual interface members.

- [ ] **Step 2: Write the failing tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/Workspace/WorkspaceCommandHandlerTests.cs
using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspace;
using Ferret.Core.Primitives;
using Ferret.Core.Results;
using Ferret.Core.Workspace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Workspace;

// ── Minimal fakes ─────────────────────────────────────────────────────────

internal sealed class FakeOutput : IOutputFormatter
{
    private readonly List<string> _lines = [];
    public IReadOnlyList<string> Lines => _lines;
    public void WriteLine(string? text = null) => _lines.Add(text ?? string.Empty);
    public void Write(string text) { if (_lines.Count == 0) _lines.Add(string.Empty); _lines[^1] += text; }
}

internal sealed class FakeServices : IFerretServices
{
    public FakeServices(FakeOutput output) => Output = output;
    public IOutputFormatter Output { get; }
    public IConfiguration Configuration => new ConfigurationBuilder().Build();
    public Microsoft.Extensions.Logging.ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;
    public IServiceProvider ServiceProvider => new ServiceCollection().BuildServiceProvider();
}

internal sealed class FakeContext : IFerretContext
{
    public FakeContext(IFerretServices services) => Services = services;
    public CancellationToken CancellationToken => CancellationToken.None;
    public VerbosityLevel Verbosity => VerbosityLevel.Normal;
    public OutputFormat OutputFormat => OutputFormat.Text;
    public IFerretServices Services { get; }
    public T? GetOption<T>(string name) => default;
}

internal sealed class FakeWorkspaceEngine : IWorkspaceEngine
{
    private static WorkspaceContext MakeCtx(string name) =>
        WorkspaceContext.Create(
            WorkspacePath.Create(@"C:\fake"),
            WorkspaceId.Create("ws-fake"),
            WorkspaceMetadata.Create(name, string.Empty, "1.0", DateTimeOffset.UtcNow),
            WorkspaceCapabilities.Create(false, 0, 0));

    public WorkspaceInitResult InitResult { get; set; } = WorkspaceInitResult.Success(MakeCtx("fake"));
    public WorkspaceContext LoadResult { get; set; } = MakeCtx("fake");

    public Task<WorkspaceInitResult> InitialiseAsync(WorkspacePath r, WorkspaceOptions? o = null, CancellationToken ct = default) => Task.FromResult(InitResult);
    public Task<WorkspaceContext> LoadAsync(WorkspacePath r, WorkspaceOptions? o = null, CancellationToken ct = default) => Task.FromResult(LoadResult);
    public Task<WorkspaceHealthReport> GetHealthAsync(WorkspaceContext c, HealthCheckDepth d = HealthCheckDepth.Quick, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Changeset> GetChangesetAsync(WorkspaceContext c, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext c, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<ValidationResult> ValidateAsync(WorkspaceContext c, CancellationToken ct = default) => throw new NotImplementedException();
}

internal sealed class FakeLocator : IWorkspaceLocator
{
    public WorkspacePath? LocateResult { get; set; }
    public Task<WorkspacePath?> LocateAsync(WorkspacePath s, CancellationToken ct = default) => Task.FromResult(LocateResult);
    public Task<bool> ExistsAsync(WorkspacePath r, CancellationToken ct = default) => Task.FromResult(LocateResult is not null);
}

// ── Tests ─────────────────────────────────────────────────────────────────

public sealed class WorkspaceInitCommandHandlerTests
{
    private static (FakeOutput output, FakeContext ctx) Ctx()
    {
        var o = new FakeOutput();
        return (o, new FakeContext(new FakeServices(o)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitSucceeds_WritesSuccessAndReturnsSuccess()
    {
        var (output, ctx) = Ctx();
        var result = await new WorkspaceInitCommandHandler(new FakeWorkspaceEngine()).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(output.Lines, l => l.Contains("Initialised"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitFails_WritesErrorAndReturnsFailure()
    {
        var engine = new FakeWorkspaceEngine { InitResult = WorkspaceInitResult.Failure("already exists") };
        var (output, ctx) = Ctx();
        var result = await new WorkspaceInitCommandHandler(engine).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(output.Lines, l => l.Contains("error"));
    }
}

public sealed class WorkspaceStatusCommandHandlerTests
{
    private static (FakeOutput output, FakeContext ctx) Ctx()
    {
        var o = new FakeOutput();
        return (o, new FakeContext(new FakeServices(o)));
    }

    [Fact]
    public async Task ExecuteAsync_NotInWorkspace_WritesNotInWorkspace()
    {
        var locator = new FakeLocator { LocateResult = null };
        var (output, ctx) = Ctx();
        var result = await new WorkspaceStatusCommandHandler(locator, new FakeWorkspaceEngine()).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(output.Lines, l => l.Contains("Not in a Ferret workspace"));
    }

    [Fact]
    public async Task ExecuteAsync_InWorkspace_WritesDetails()
    {
        var locator = new FakeLocator { LocateResult = WorkspacePath.Create(@"C:\fake") };
        var (output, ctx) = Ctx();
        var result = await new WorkspaceStatusCommandHandler(locator, new FakeWorkspaceEngine()).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(output.Lines, l => l.Contains("fake"));
    }
}
```

- [ ] **Step 3: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Cli.Tests --filter "WorkspaceCommand" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 4: Implement `WorkspaceInitCommandHandler`**

```csharp
// src/Ferret.Cli/Commands/Workspace/WorkspaceInitCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Workspace;

internal sealed class WorkspaceInitCommandHandler : ICommandHandler
{
    private readonly IWorkspaceEngine _engine;

    internal WorkspaceInitCommandHandler(IWorkspaceEngine engine) => _engine = engine;

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var rootPath = WorkspacePath.Create(Environment.CurrentDirectory);
        var result = await _engine.InitialiseAsync(rootPath, ct: context.CancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            context.Services.Output.WriteLine($"error: {result.ErrorMessage}");
            return CommandResult.Failure;
        }

        context.Services.Output.WriteLine($"Initialised Ferret workspace at {rootPath.FullPath}");
        context.Services.Output.WriteLine("  .ferret/ created with ContextOS directory structure");
        return CommandResult.Success;
    }
}
```

- [ ] **Step 5: Implement `WorkspaceStatusCommandHandler`**

```csharp
// src/Ferret.Cli/Commands/Workspace/WorkspaceStatusCommandHandler.cs
using Ferret.Cli.Cli;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Workspace;

internal sealed class WorkspaceStatusCommandHandler : ICommandHandler
{
    private readonly IWorkspaceLocator _locator;
    private readonly IWorkspaceEngine _engine;

    internal WorkspaceStatusCommandHandler(IWorkspaceLocator locator, IWorkspaceEngine engine)
    {
        _locator = locator;
        _engine = engine;
    }

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var cwd = WorkspacePath.Create(Environment.CurrentDirectory);
        var root = await _locator.LocateAsync(cwd, context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            context.Services.Output.WriteLine("Not in a Ferret workspace.");
            return CommandResult.Success;
        }

        var workspace = await _engine.LoadAsync(root, ct: context.CancellationToken).ConfigureAwait(false);
        context.Services.Output.WriteLine($"Workspace: {workspace.Metadata.Name}");
        context.Services.Output.WriteLine($"  ID:      {workspace.Id}");
        context.Services.Output.WriteLine($"  Root:    {workspace.RootPath}");
        context.Services.Output.WriteLine($"  Created: {workspace.Metadata.CreatedAt:yyyy-MM-dd}");
        return CommandResult.Success;
    }
}
```

- [ ] **Step 6: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Cli.Tests -v minimal
```

- [ ] **Step 7: Commit**

```powershell
git add src/Ferret.Cli/ tests/Ferret.Cli.Tests/
git commit -m "feat(sprint-7): WorkspaceInitCommandHandler + WorkspaceStatusCommandHandler"
```

---

### Task 11: `WorkspaceCliModule` + wire-up + E2E test

**Files:**
- Create: `src/Ferret.Cli/Commands/Workspace/WorkspaceCliModule.cs`
- Modify: `src/Ferret.Cli/Commands/CoreCliModule.cs` — remove `workspace` EmptyGroup
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj` — add `Ferret.Workspace` project reference
- Modify: `src/Ferret.Cli/Program.cs` — add `WorkspaceCliModule`
- Create: `tests/Ferret.Cli.Tests/Commands/Workspace/WorkspaceCliModuleTests.cs`
- Create: `tests/Ferret.Integration.Tests/WorkspaceE2ETests.cs`

**Interfaces:**
- Consumes: `CliModuleBase`, both handlers, `WorkspaceEngine`, `WorkspaceLocator`
- Produces: `WorkspaceCliModule` — parent `workspace` command + `init` + `status` subcommands; DI registers engine, locator, handlers

- [ ] **Step 1: Add `Ferret.Workspace` reference to `Ferret.Cli.csproj`**

Inside the existing `<ItemGroup>` in `src/Ferret.Cli/Ferret.Cli.csproj`:

```xml
<ProjectReference Include="..\Ferret.Workspace\Ferret.Workspace.csproj" />
```

- [ ] **Step 2: Write the failing module tests**

```csharp
// tests/Ferret.Cli.Tests/Commands/Workspace/WorkspaceCliModuleTests.cs
using Ferret.Cli.Commands.Workspace;
using Ferret.Core.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Tests.Commands.Workspace;

public sealed class WorkspaceCliModuleTests
{
    private readonly WorkspaceCliModule _module = new();

    [Fact]
    public void GetCommands_ContainsWorkspaceParentWithNoGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "workspace" && c.Group is null);
    }

    [Fact]
    public void GetCommands_ContainsInitSubcommandInWorkspaceGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "init" && c.Group == "workspace");
    }

    [Fact]
    public void GetCommands_ContainsStatusSubcommandInWorkspaceGroup()
    {
        var cmds = _module.GetCommands().ToList();
        Assert.Contains(cmds, c => c.Metadata.Name == "status" && c.Group == "workspace");
    }

    [Fact]
    public void ConfigureServices_RegistersIWorkspaceEngine()
    {
        var services = new ServiceCollection();
        _module.ConfigureServices(services);
        Assert.NotNull(services.BuildServiceProvider().GetService<IWorkspaceEngine>());
    }
}
```

- [ ] **Step 3: Run tests to confirm red**

```powershell
dotnet test tests/Ferret.Cli.Tests --filter "WorkspaceCliModule" 2>&1 | Select-String "error|FAILED"
```

- [ ] **Step 4: Implement `WorkspaceCliModule`**

```csharp
// src/Ferret.Cli/Commands/Workspace/WorkspaceCliModule.cs
using Ferret.Cli.Cli;
using Ferret.Core.Workspace;
using Ferret.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Contributes workspace subcommands to the Ferret CLI.</summary>
internal sealed class WorkspaceCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.workspace";

    /// <inheritdoc/>
    public override string Description => "ContextOS workspace management.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(new CommandMetadata("workspace", "Manage Ferret workspaces."), HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("init", "Initialise a new Ferret workspace at the current directory."),
            typeof(WorkspaceInitCommandHandler),
            Group: "workspace");

        yield return new CommandDefinition(
            new CommandMetadata("status", "Show the status of the current workspace."),
            typeof(WorkspaceStatusCommandHandler),
            Group: "workspace");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWorkspaceEngine, WorkspaceEngine>();
        services.AddSingleton<IWorkspaceLocator, WorkspaceLocator>();
        services.AddTransient<WorkspaceInitCommandHandler>();
        services.AddTransient<WorkspaceStatusCommandHandler>();
    }
}
```

- [ ] **Step 5: Remove the `workspace` EmptyGroup from `CoreCliModule`**

In `src/Ferret.Cli/Commands/CoreCliModule.cs`, delete:

```csharp
yield return CommandDefinition.EmptyGroup(
    "workspace",
    "Workspace management.",
    "Sprint 7",
    ["workspace init", "workspace status", "workspace open"]);
```

- [ ] **Step 6: Update `Program.cs`**

```csharp
// src/Ferret.Cli/Program.cs
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Workspace;

return await RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()]).InvokeAsync(args).ConfigureAwait(false);
```

- [ ] **Step 7: Run module tests**

```powershell
dotnet test tests/Ferret.Cli.Tests -v minimal
```

- [ ] **Step 8: Write the E2E tests**

Add `Ferret.Workspace` and `Ferret.Cli` references to `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj` if not already present:

```xml
<ProjectReference Include="..\..\src\Ferret.Workspace\Ferret.Workspace.csproj" />
<ProjectReference Include="..\..\src\Ferret.Cli\Ferret.Cli.csproj" />
```

```csharp
// tests/Ferret.Integration.Tests/WorkspaceE2ETests.cs
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Workspace;

namespace Ferret.Integration.Tests;

public sealed class WorkspaceE2ETests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "ferret-e2e-" + Guid.NewGuid().ToString("N")[..8]);

    public WorkspaceE2ETests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static string[] Modules() => [new CoreCliModule(), new WorkspaceCliModule()];

    [Fact]
    public async Task WorkspaceInit_CreatesDotFerretDirectory()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            var output = new StringWriter();
            var exitCode = await RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()], output)
                .InvokeAsync(["workspace", "init"]);

            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(Path.Combine(_tempDir, ".ferret")));
            Assert.True(File.Exists(Path.Combine(_tempDir, ".ferret", "workspace.json")));
            Assert.True(File.Exists(Path.Combine(_tempDir, ".ferret", "state.json")));
        }
        finally { Environment.CurrentDirectory = prev; }
    }

    [Fact]
    public async Task WorkspaceInit_CreatesContextOsDirectoryTree()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            await RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()])
                .InvokeAsync(["workspace", "init"]);

            var ferretDir = Path.Combine(_tempDir, ".ferret");
            Assert.True(Directory.Exists(Path.Combine(ferretDir, "connectors", "git")));
            Assert.True(Directory.Exists(Path.Combine(ferretDir, "indexes", "semantic")));
            Assert.True(Directory.Exists(Path.Combine(ferretDir, "memory", "working")));
            Assert.True(Directory.Exists(Path.Combine(ferretDir, "snapshots", "knowledge")));
            Assert.True(File.Exists(Path.Combine(ferretDir, "config", "connectors.json")));
        }
        finally { Environment.CurrentDirectory = prev; }
    }

    [Fact]
    public async Task WorkspaceInit_ThenStatus_ShowsWorkspaceName()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            await RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()])
                .InvokeAsync(["workspace", "init"]);

            var output = new StringWriter();
            var exitCode = await RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()], output)
                .InvokeAsync(["workspace", "status"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Workspace:", output.ToString());
        }
        finally { Environment.CurrentDirectory = prev; }
    }

    [Fact]
    public async Task WorkspaceInit_WhenAlreadyInitialised_ReturnsNonZero()
    {
        var prev = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _tempDir;
        try
        {
            var app = RootCommandFactory.Build([new CoreCliModule(), new WorkspaceCliModule()]);
            await app.InvokeAsync(["workspace", "init"]);
            var exitCode = await app.InvokeAsync(["workspace", "init"]);
            Assert.NotEqual(0, exitCode);
        }
        finally { Environment.CurrentDirectory = prev; }
    }
}
```

- [ ] **Step 9: Run the full suite**

```powershell
dotnet test src/Ferret.sln -v minimal
```

Expected: All tests pass. ~245 existing + ~60 new workspace/connector/CLI tests.

- [ ] **Step 10: Commit**

```powershell
git add src/ tests/
git commit -m "feat(sprint-7): WorkspaceCliModule wired — ferret workspace init + status ship ContextOS foundation"
```

- [ ] **Step 11: Suggest sprint tag to user**

Suggest: `git tag v0.7.0-sprint7` — apply after confirming all tests green.

---

## Self-Review

| Requirement | Covered by |
|---|---|
| `ferret workspace init` creates `.ferret/` | Task 11 E2E |
| Full ContextOS directory tree (connectors/*, indexes/*, memory/*, knowledge/*, models/*, snapshots/*, telemetry/*, temp) | Tasks 1, 7 |
| `workspace.json` with `contextOsVersion`, `workspaceType`, `features`, `enabledConnectors`, `enabledModels` | Tasks 2, 7 |
| `state.json` with `knowledgeVersion`, `graphVersion`, `lastIndex`, `connectors`, `statistics` | Tasks 2, 5 |
| `config/` seeded with `runtime.json`, `plugins.json`, `models.json`, `connectors.json` | Tasks 1, 7 |
| `IConnector` + `ConnectorType/Metadata/Capabilities/Health` contracts in `Ferret.Core` | Task 3 |
| `ferret workspace status` shows name/ID/root/created | Tasks 10, 11 |
| M1 frozen packages unchanged (only additions to `Ferret.Core.Connectors`) | Verified — no existing Core types modified |
| `WriteStatisticsAsync` preserves `knowledgeVersion`/`graphVersion`/`connectors` | Task 5 test "PreservesExistingStateFields" |

**Placeholder scan:** No TBDs. All code is complete.

**Type consistency:** `WorkspaceId.Create(string)`, `WorkspaceMetadata.Create(name, description, schemaVersion, createdAt)`, `WorkspaceStatistics.Create(int, int, DateTimeOffset, string)`, `ConnectorHealth.Connected/Disconnected` — consistent across all tasks.
