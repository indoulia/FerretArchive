# Sprint 9 — Section 4: Connector Config CLI

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Complete the Connector Platform with a full lifecycle owner (`ConnectorManager`), a persistence store (`ConnectorInstanceStore`), first-class Core contracts (`ConnectorConfiguration`, `ConnectorRuntime`, `ConnectorInstance`, `ValidationResult`/`ValidationIssue`/`ValidationSeverity`), and five CLI commands (`enable`, `disable`, `configure`, `inspect`, `validate`). This section also corrects `IndexPipeline` (from S3) to use `IConnectorManager` + `ConnectorRuntime` instead of the `IConnectorRegistry` approach, and updates `IConnectorFactory.Create` to accept a full `ConnectorInstance` instead of a bare `ConnectorInstanceId`.

**Architecture:** Only `ConnectorManager` creates and disposes connector runtime instances (ADR-0014 Principle 10). Pipelines receive `ConnectorRuntime` objects from the manager — they never construct connectors directly. `ConnectorInstanceStore` owns `.ferret/connectors.json` I/O with atomic writes (temp + rename). `ConnectorManager` is process-scoped cached; a `SemaphoreSlim` guards concurrent access.

**ADR:** `docs/adr/0014-document-processing-architecture.md` — Principle 10 + `ConnectorPolicy`, `ConnectorProfile`, `ferret connector doctor` reservations added in Task 1.

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `sealed` on all concrete classes, `required` on record/class properties with no sensible default.

---

## Prerequisites

Section 3 (Index Engine) must be **complete** before starting this section:
- `Ferret.Indexing` project merged and green
- `IndexPipeline` implemented (currently using `IConnectorRegistry` — corrected in Task 3)
- `dotnet test` passes on all existing test projects
- `dotnet build src/Ferret.sln` passes

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes
- `required` keyword on record/class properties with no sensible default
- `Ferret.ConnectorPlatform` references `Ferret.Core` only — never `Ferret.Cli`
- `ConnectorManager` is the sole creator and disposer of `ConnectorRuntime` instances
- `ConnectorInstanceStore` uses atomic write (temp file → rename)
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-9):`, `test(sprint-9):`, `chore(sprint-9):`
- **No intermediate commit until all Sprint 9 sections are complete** — accumulate changes, single commit at sprint end

---

## File Inventory

### New Source Files (Ferret.Core)

| File |
|---|
| `src/Ferret.Core/Connectors/ConnectorConfiguration.cs` |
| `src/Ferret.Core/Connectors/ConnectorRuntime.cs` |
| `src/Ferret.Core/Connectors/ConnectorInstance.cs` |
| `src/Ferret.Core/Connectors/ValidationSeverity.cs` |
| `src/Ferret.Core/Connectors/ValidationIssue.cs` |
| `src/Ferret.Core/Connectors/ValidationResult.cs` |
| `src/Ferret.Core/Connectors/IConnectorInstanceStore.cs` |

### Modified Source Files (Ferret.Core)

| File | Change |
|---|---|
| `src/Ferret.Core/Connectors/IConnectorManager.cs` | Full definition replacing empty stub (removes `#pragma warning disable CA1040`) |
| `src/Ferret.Core/Connectors/IConnectorFactory.cs` | `Create(ConnectorInstance)` replaces `Create(ConnectorInstanceId)` (breaking change) |
| `docs/adr/0014-document-processing-architecture.md` | Principle 10 + `ConnectorPolicy`/`ConnectorProfile`/`doctor` reservations |

### New Source Files (Ferret.ConnectorPlatform)

| File |
|---|
| `src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs` |
| `src/Ferret.ConnectorPlatform/ConnectorManager.cs` |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorEnableCommandHandler.cs` |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorDisableCommandHandler.cs` |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorConfigureCommandHandler.cs` |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorInspectCommandHandler.cs` |
| `src/Ferret.ConnectorPlatform/Commands/ConnectorValidateCommandHandler.cs` |

### Modified Source Files (Ferret.ConnectorPlatform)

| File | Change |
|---|---|
| `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj` | Add `System.Text.Json` package reference for `connectors.json` serialization |
| `tests/Ferret.ConnectorPlatform.Tests/Fakes/FakeConnectorFactory.cs` | Update `Create(ConnectorInstanceId)` → `Create(ConnectorInstance)` |

### Modified Source Files (Ferret.Connectors.Filesystem)

| File | Change |
|---|---|
| `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs` | `Create(ConnectorInstance)` — reads config keys from `ConnectorInstance.Configuration` |

### Modified Source Files (Ferret.Indexing)

| File | Change |
|---|---|
| `src/Ferret.Indexing/IndexPipeline.cs` | Constructor takes `IConnectorManager`; iterates `ConnectorRuntime` list |
| `tests/Ferret.Indexing.Tests/Fakes/FakeConnectorRegistry.cs` | Replaced by `FakeConnectorManager.cs` returning `IReadOnlyList<ConnectorRuntime>` |
| `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs` | Update fakes and pipeline construction to use `FakeConnectorManager` |

### New Doc Files

| File | Change |
|---|---|
| `docs/adr/0014-document-processing-architecture.md` | Principle 10 + `ConnectorPolicy`/`ConnectorProfile`/`doctor` reservations |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.Core.Tests/Connectors/ConnectorConfigurationTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/ConnectorRuntimeTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/ConnectorInstanceTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Connectors/ValidationResultTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/ConnectorInstanceStoreTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/ConnectorManagerTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorEnableCommandTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorConfigureCommandTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorValidateCommandTests.cs` | Ferret.ConnectorPlatform.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorFactoryTests.cs` | Ferret.Connectors.Filesystem.Tests |
| `tests/Ferret.Indexing.Tests/Fakes/FakeConnectorManager.cs` | Ferret.Indexing.Tests |
| `tests/Ferret.Indexing.Tests/IndexPipelineConnectorManagerTests.cs` | Ferret.Indexing.Tests |

---

## Task 1: Core Contract Updates

**Why first:** Every subsequent task depends on the new Core types. `ConnectorInstance` is required before `ConnectorInstanceStore` (Task 2), `ConnectorManager` (Task 3), and `FilesystemConnectorFactory` (Task 4). `ConnectorRuntime` is required before `ConnectorManager` (Task 3) and `IndexPipeline` correction (Task 3). `IConnectorFactory.Create(ConnectorInstance)` is a breaking change that must be resolved before Task 4. ADR-0014 Principle 10 documents the lifecycle ownership rule that Task 3 enforces.

**Files:**
- Create: `src/Ferret.Core/Connectors/ConnectorConfiguration.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorInstance.cs`
- Create: `src/Ferret.Core/Connectors/ConnectorRuntime.cs`
- Create: `src/Ferret.Core/Connectors/ValidationSeverity.cs`
- Create: `src/Ferret.Core/Connectors/ValidationIssue.cs`
- Create: `src/Ferret.Core/Connectors/ValidationResult.cs`
- Create: `src/Ferret.Core/Connectors/IConnectorInstanceStore.cs`
- Modify: `src/Ferret.Core/Connectors/IConnectorManager.cs`
- Modify: `src/Ferret.Core/Connectors/IConnectorFactory.cs`
- Modify: `tests/Ferret.ConnectorPlatform.Tests/Fakes/FakeConnectorFactory.cs`
- Modify: `docs/adr/0014-document-processing-architecture.md`
- Create: `tests/Ferret.Core.Tests/Connectors/ConnectorConfigurationTests.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/ConnectorRuntimeTests.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/ConnectorInstanceTests.cs`
- Create: `tests/Ferret.Core.Tests/Connectors/ValidationResultTests.cs`

**Interfaces:**
- Produces: `ConnectorConfiguration`, `ConnectorInstance`, `ConnectorRuntime`, `ValidationResult`/`ValidationIssue`/`ValidationSeverity`, `IConnectorInstanceStore`, revised `IConnectorManager`, revised `IConnectorFactory` — consumed by Tasks 2, 3, 4, 5

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Connectors/ConnectorConfigurationTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorConfigurationTests
{
    [Fact]
    public void GetValue_Returns_Null_For_Missing_Key()
    {
        var config = ConnectorConfiguration.Empty;

        Assert.Null(config.GetValue("missing"));
    }

    [Fact]
    public void GetValue_Is_Case_Insensitive()
    {
        var config = new ConnectorConfiguration(new Dictionary<string, string> { ["RootPath"] = "/src" });

        Assert.Equal("/src", config.GetValue("rootpath"));
        Assert.Equal("/src", config.GetValue("ROOTPATH"));
        Assert.Equal("/src", config.GetValue("RootPath"));
    }

    [Fact]
    public void GetValueOrDefault_Returns_Default_For_Missing_Key()
    {
        var config = ConnectorConfiguration.Empty;

        Assert.Equal("fallback", config.GetValueOrDefault("missing", "fallback"));
    }

    [Fact]
    public void With_Returns_New_Instance_With_Key_Set()
    {
        var original = ConnectorConfiguration.Empty;

        var updated = original.With("rootPath", "/src");

        Assert.Null(original.GetValue("rootPath"));
        Assert.Equal("/src", updated.GetValue("rootPath"));
    }

    [Fact]
    public void With_Overwrites_Existing_Key_Case_Insensitively()
    {
        var config = new ConnectorConfiguration(new Dictionary<string, string> { ["rootPath"] = "/old" });

        var updated = config.With("ROOTPATH", "/new");

        Assert.Equal("/new", updated.GetValue("rootPath"));
    }

    [Fact]
    public void Empty_Is_Shared_Singleton()
    {
        Assert.Same(ConnectorConfiguration.Empty, ConnectorConfiguration.Empty);
    }

    [Fact]
    public void AsReadOnlyDictionary_Returns_All_Keys()
    {
        var config = new ConnectorConfiguration(new Dictionary<string, string>
        {
            ["rootPath"] = ".",
            ["excludeExtensions"] = ".dll,.exe",
        });

        Assert.Equal(2, config.AsReadOnlyDictionary().Count);
    }

    [Fact]
    public void FromDictionary_Creates_Configuration_From_Dictionary()
    {
        var config = ConnectorConfiguration.FromDictionary(new Dictionary<string, string> { ["key"] = "val" });

        Assert.Equal("val", config.GetValue("key"));
    }
}
```

Create `tests/Ferret.Core.Tests/Connectors/ConnectorInstanceTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorInstanceTests
{
    [Fact]
    public void SchemaVersion_Defaults_To_1_0()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.Equal("1.0", instance.SchemaVersion);
    }

    [Fact]
    public void IsEnabled_Defaults_To_True()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.True(instance.IsEnabled);
    }

    [Fact]
    public void Tags_Defaults_To_Empty()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.Empty(instance.Tags);
    }

    [Fact]
    public void Configuration_Defaults_To_Empty()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.Same(ConnectorConfiguration.Empty, instance.Configuration);
    }

    [Fact]
    public void Value_Equality_By_All_Properties()
    {
        var a = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("x"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "X",
        };
        var b = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("x"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "X",
        };

        Assert.Equal(a, b);
    }
}
```

Create `tests/Ferret.Core.Tests/Connectors/ConnectorRuntimeTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorRuntimeTests
{
    [Fact]
    public void ConnectorRuntime_Is_A_Record()
    {
        Assert.True(typeof(ConnectorRuntime).IsClass);
        // records are classes; verify it's a record by checking compiler-generated EqualityContract
        var method = typeof(ConnectorRuntime).GetMethod("EqualityContract",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
    }

    [Fact]
    public void ConnectorRuntime_Has_Instance_Property()
    {
        var prop = typeof(ConnectorRuntime).GetProperty("Instance");

        Assert.NotNull(prop);
        Assert.Equal(typeof(ConnectorInstance), prop.PropertyType);
    }

    [Fact]
    public void ConnectorRuntime_Has_Connector_Property()
    {
        var prop = typeof(ConnectorRuntime).GetProperty("Connector");

        Assert.NotNull(prop);
        Assert.Equal(typeof(IConnector), prop.PropertyType);
    }

    [Fact]
    public void ConnectorRuntime_Has_Status_Property()
    {
        var prop = typeof(ConnectorRuntime).GetProperty("Status");

        Assert.NotNull(prop);
        Assert.Equal(typeof(ConnectorStatus), prop.PropertyType);
    }
}
```

Create `tests/Ferret.Core.Tests/Connectors/ValidationResultTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ValidationResultTests
{
    [Fact]
    public void IsValid_True_When_No_Issues()
    {
        var result = ValidationResult.Ok();

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void IsValid_False_When_Any_Error_Issue()
    {
        var result = ValidationResult.WithError("something went wrong", "instance-1");

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
    }

    [Fact]
    public void IsValid_True_When_Only_Warning_Issues()
    {
        var result = new ValidationResult
        {
            Issues = [new ValidationIssue { Message = "advisory", Severity = ValidationSeverity.Warning }],
        };

        Assert.True(result.IsValid);
    }

    [Fact]
    public void WithError_Sets_InstanceId()
    {
        var result = ValidationResult.WithError("msg", "my-instance");

        Assert.Equal("my-instance", result.Issues[0].InstanceId);
    }

    [Fact]
    public void Combine_Merges_All_Issues()
    {
        var a = ValidationResult.WithError("err-a", "inst-a");
        var b = new ValidationResult
        {
            Issues = [new ValidationIssue { Message = "warn-b", Severity = ValidationSeverity.Warning }],
        };

        var combined = ValidationResult.Combine([a, b]);

        Assert.Equal(2, combined.Issues.Count);
        Assert.False(combined.IsValid);
    }

    [Fact]
    public void Combine_Empty_Returns_Valid()
    {
        var combined = ValidationResult.Combine([]);

        Assert.True(combined.IsValid);
        Assert.Empty(combined.Issues);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "ConnectorConfigurationTests|ConnectorInstanceTests|ConnectorRuntimeTests|ValidationResultTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `ConnectorConfiguration.cs`**

`src/Ferret.Core/Connectors/ConnectorConfiguration.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>Configuration values for a connector instance. Internally a case-insensitive string dictionary.</summary>
public sealed class ConnectorConfiguration
{
    private readonly IReadOnlyDictionary<string, string> _values;

    /// <summary>Creates an empty configuration.</summary>
    public ConnectorConfiguration()
        => _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a configuration from the given values.</summary>
    /// <param name="values">The initial key-value pairs.</param>
    public ConnectorConfiguration(IDictionary<string, string> values)
        => _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the value for the given key, or null if not present.</summary>
    /// <param name="key">The configuration key (case-insensitive).</param>
    public string? GetValue(string key) => _values.GetValueOrDefault(key);

    /// <summary>Gets the value for the given key, or the default value if not present.</summary>
    /// <param name="key">The configuration key (case-insensitive).</param>
    /// <param name="defaultValue">The default value to return when the key is absent.</param>
    public string GetValueOrDefault(string key, string defaultValue = "")
        => _values.GetValueOrDefault(key, defaultValue);

    /// <summary>Returns a new configuration with the given key set to the given value.</summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value to set.</param>
    public ConnectorConfiguration With(string key, string value)
    {
        var dict = new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase)
            { [key] = value };
        return new ConnectorConfiguration(dict);
    }

    /// <summary>Returns the underlying dictionary for serialization purposes.</summary>
    public IReadOnlyDictionary<string, string> AsReadOnlyDictionary() => _values;

    /// <summary>Creates a <see cref="ConnectorConfiguration"/> from a dictionary.</summary>
    /// <param name="values">The key-value pairs to initialise from.</param>
    public static ConnectorConfiguration FromDictionary(IDictionary<string, string> values)
        => new(values);

    /// <summary>Gets a shared empty configuration instance.</summary>
    public static ConnectorConfiguration Empty { get; } = new();
}
```

- [ ] **Step 4: Create `ConnectorInstance.cs`**

`src/Ferret.Core/Connectors/ConnectorInstance.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// Stored configuration for a single connector instance.
/// Represents a user-named, persisted connector binding (e.g. "workspace" → filesystem at ".").
/// Part of the Metadata → Descriptor → Instance → Status / Runtime pattern.
/// </summary>
public sealed record ConnectorInstance
{
    /// <summary>Gets the workspace-scoped instance identifier.</summary>
    public required ConnectorInstanceId Id { get; init; }

    /// <summary>Gets the connector type identifier (e.g. "filesystem").</summary>
    public required ConnectorId ConnectorType { get; init; }

    /// <summary>Gets the human-readable display name for this instance.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets a value indicating whether this instance is enabled. Default: true.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Gets the schema version for migration purposes. Default: "1.0".</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Gets the optional tags associated with this instance.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets the connector-type-specific configuration values for this instance.</summary>
    public ConnectorConfiguration Configuration { get; init; } = ConnectorConfiguration.Empty;

    // Reserved: ConnectorPolicy? Policy — read-only, bandwidth limits, security constraints
    // Reserved: string? ProfileId — credential sharing via ConnectorProfile
}
```

- [ ] **Step 5: Create `ConnectorRuntime.cs`**

`src/Ferret.Core/Connectors/ConnectorRuntime.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// Runtime wrapper for an active connector: stored instance configuration + live connector + current status.
/// Only <c>ConnectorManager</c> creates and disposes <see cref="ConnectorRuntime"/> instances (ADR-0014 Principle 10).
/// Pipelines receive <see cref="ConnectorRuntime"/> from the manager and never construct connectors directly.
/// </summary>
public sealed record ConnectorRuntime
{
    /// <summary>Gets the stored instance configuration.</summary>
    public required ConnectorInstance Instance { get; init; }

    /// <summary>Gets the live connector.</summary>
    public required IConnector Connector { get; init; }

    /// <summary>Gets the current runtime status.</summary>
    public required ConnectorStatus Status { get; init; }

    // Reserved: IConnectorSession Session — active session (post-ConnectAsync)
}
```

- [ ] **Step 6: Create `ValidationSeverity.cs`, `ValidationIssue.cs`, `ValidationResult.cs`**

`src/Ferret.Core/Connectors/ValidationSeverity.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>Indicates the severity of a validation issue.</summary>
public enum ValidationSeverity
{
    /// <summary>Advisory — does not block operation.</summary>
    Warning,

    /// <summary>Blocking — marks the overall result as invalid.</summary>
    Error,
}
```

`src/Ferret.Core/Connectors/ValidationIssue.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>A single diagnostic issue produced by a validation pass.</summary>
public sealed record ValidationIssue
{
    /// <summary>Gets the human-readable description of the issue.</summary>
    public required string Message { get; init; }

    /// <summary>Gets the severity of this issue.</summary>
    public required ValidationSeverity Severity { get; init; }

    /// <summary>Gets the instance ID this issue relates to, or null if not instance-specific.</summary>
    public string? InstanceId { get; init; }
}
```

`src/Ferret.Core/Connectors/ValidationResult.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// The aggregated result of a validation pass over one or more connector instances.
/// <see cref="IsValid"/> is true when no <see cref="ValidationSeverity.Error"/> issues are present.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>Gets all validation issues. May be empty.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    /// <summary>Gets a value indicating whether no error-severity issues are present.</summary>
    public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>Creates a valid result with no issues.</summary>
    public static ValidationResult Ok() => new();

    /// <summary>Creates a result with a single error-severity issue.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="instanceId">Optional instance ID this error relates to.</param>
    public static ValidationResult WithError(string message, string? instanceId = null)
        => new()
        {
            Issues =
            [
                new ValidationIssue
                {
                    Message = message,
                    Severity = ValidationSeverity.Error,
                    InstanceId = instanceId,
                },
            ],
        };

    /// <summary>Merges multiple <see cref="ValidationResult"/> instances into one.</summary>
    /// <param name="results">The results to merge.</param>
    public static ValidationResult Combine(IEnumerable<ValidationResult> results)
        => new() { Issues = results.SelectMany(r => r.Issues).ToList() };
}
```

- [ ] **Step 7: Create `IConnectorInstanceStore.cs`**

`src/Ferret.Core/Connectors/IConnectorInstanceStore.cs`:

```csharp
using Ferret.Core.Workspace;

namespace Ferret.Core.Connectors;

/// <summary>
/// Loads and persists <see cref="ConnectorInstance"/> records to a workspace-local store.
/// The concrete implementation (<c>ConnectorInstanceStore</c>) uses <c>.ferret/connectors.json</c>.
/// </summary>
public interface IConnectorInstanceStore
{
    /// <summary>Loads all connector instances from the workspace store.
    /// Returns an empty list when the store file does not yet exist.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<IReadOnlyList<ConnectorInstance>> LoadAllAsync(WorkspacePath rootPath, CancellationToken ct = default);

    /// <summary>Saves the given instances to the workspace store, replacing all previous content.
    /// Uses an atomic write (temp file → rename) to prevent partial writes.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="instances">The complete list of instances to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    Task SaveAsync(WorkspacePath rootPath, IReadOnlyList<ConnectorInstance> instances, CancellationToken ct = default);
}
```

- [ ] **Step 8: Replace `IConnectorManager` stub**

Read `src/Ferret.Core/Connectors/IConnectorManager.cs` first, then replace entirely:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// Activates, caches, and vends connector runtimes for the process lifetime.
/// Only <c>ConnectorManager</c> implements this — no other subsystem constructs connectors directly.
/// </summary>
public interface IConnectorManager
{
    /// <summary>Returns all active (enabled) connector runtimes.
    /// Results are process-scoped cached — the same <see cref="ConnectorRuntime"/> instance
    /// is returned across calls for the same instance ID.</summary>
    /// <param name="ct">A cancellation token.</param>
    Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(CancellationToken ct = default);

    /// <summary>Returns the stored instance configuration for the given ID, or null if not found.</summary>
    /// <param name="id">The workspace-scoped instance identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<ConnectorInstance?> GetInstanceAsync(ConnectorInstanceId id, CancellationToken ct = default);

    // Reserved: Task ReconnectAsync(ConnectorInstanceId id, CancellationToken ct = default);
    // Reserved: Task<ConnectorHealth> CheckHealthAsync(ConnectorInstanceId id, CancellationToken ct = default);
}
```

- [ ] **Step 9: Update `IConnectorFactory.cs` — breaking change**

Read `src/Ferret.Core/Connectors/IConnectorFactory.cs` first, then replace the `Create` method:

Remove:
```csharp
    /// <summary>Creates a configured connector for the given workspace instance.</summary>
    /// <param name="instanceId">The workspace-scoped instance identifier.</param>
    /// <returns>A connector ready for use.</returns>
    IConnector Create(ConnectorInstanceId instanceId);
```

Replace with:
```csharp
    /// <summary>Creates a configured connector from a stored instance record.
    /// The factory reads <see cref="ConnectorInstance.Configuration"/> to populate its
    /// connector-type-specific configuration object.</summary>
    /// <param name="instance">The stored instance configuration.</param>
    /// <returns>A connector ready for use.</returns>
    IConnector Create(ConnectorInstance instance);
```

- [ ] **Step 10: Update `FakeConnectorFactory` in tests**

Read `tests/Ferret.ConnectorPlatform.Tests/Fakes/FakeConnectorFactory.cs` first, then update the `Create` signature:

```csharp
    public IConnector Create(ConnectorInstance instance) =>
        throw new NotImplementedException("FakeConnectorFactory does not create connectors.");
```

- [ ] **Step 11: Update ADR-0014**

Read `docs/adr/0014-document-processing-architecture.md` first to locate:
1. The existing Principles section — add Principle 10 after the last existing principle:
   > **Principle 10: Only `ConnectorManager` creates and disposes connector runtime instances.** No other subsystem constructs connectors directly. Pipelines receive `ConnectorRuntime` objects from the manager — they never call `IConnectorFactory.Create` themselves.

2. The Reserved Extension Points section — add three entries:
   > `ConnectorPolicy` — future read-only, bandwidth-limit, max-asset-size, security constraints attached to a `ConnectorInstance`
   > `ConnectorProfile` — credential sharing across multiple instances of the same connector type
   > `ferret connector doctor` — health, permissions, credentials, connectivity, and performance checks (documented but not implemented in Sprint 9)

- [ ] **Step 12: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "ConnectorConfigurationTests|ConnectorInstanceTests|ConnectorRuntimeTests|ValidationResultTests"
dotnet test tests/Ferret.Core.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 2: `ConnectorInstanceStore` (`connectors.json` persistence)

**Why:** `ConnectorManager` (Task 3) depends on `IConnectorInstanceStore` to load instances. CLI command handlers (Task 5) depend on it to read and write. The atomic write pattern must be proven correct in isolation before wiring it into the manager and CLI.

**Files:**
- Create: `src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs`
- Modify: `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj`
- Create: `tests/Ferret.ConnectorPlatform.Tests/ConnectorInstanceStoreTests.cs`

**Interfaces:**
- Consumes: `IConnectorInstanceStore`, `ConnectorInstance`, `ConnectorConfiguration`, `WorkspacePath` (Core)
- Produces: `ConnectorInstanceStore` — consumed by Tasks 3, 5, 6

**`connectors.json` schema:**

```json
{
  "schemaVersion": "1.0",
  "instances": [
    {
      "id": "default",
      "connectorType": "filesystem",
      "displayName": "Workspace",
      "schemaVersion": "1.0",
      "enabled": true,
      "tags": [],
      "configuration": {
        "rootPath": ".",
        "excludeExtensions": ".dll,.exe,.pdb,.bin"
      }
    }
  ]
}
```

`configuration` is an open `Dictionary<string,string>` — each connector type defines its own keys.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.ConnectorPlatform.Tests/ConnectorInstanceStoreTests.cs`:

```csharp
using Ferret.ConnectorPlatform;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class ConnectorInstanceStoreTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;

    public ConnectorInstanceStoreTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    [Fact]
    public async Task LoadAllAsync_Returns_Empty_When_File_Does_Not_Exist()
    {
        var store = new ConnectorInstanceStore();

        var result = await store.LoadAllAsync(_root);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_Creates_Parent_Directory_And_File()
    {
        var store = new ConnectorInstanceStore();
        var instances = new[]
        {
            new ConnectorInstance
            {
                Id = new ConnectorInstanceId("default"),
                ConnectorType = new ConnectorId("filesystem"),
                DisplayName = "Workspace",
            },
        };

        await store.SaveAsync(_root, instances);

        var filePath = Path.Combine(_tmpDir, ".ferret", "connectors.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveAsync_Then_LoadAllAsync_Round_Trips_All_Fields()
    {
        var store = new ConnectorInstanceStore();
        var config = new ConnectorConfiguration(new Dictionary<string, string>
        {
            ["rootPath"] = "./src",
            ["excludeExtensions"] = ".dll,.exe",
        });
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("my-conn"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "My Connector",
            IsEnabled = false,
            SchemaVersion = "1.0",
            Tags = ["tag-a", "tag-b"],
            Configuration = config,
        };

        await store.SaveAsync(_root, [instance]);
        var loaded = await store.LoadAllAsync(_root);

        Assert.Single(loaded);
        var l = loaded[0];
        Assert.Equal("my-conn", l.Id.Value);
        Assert.Equal("filesystem", l.ConnectorType.Value);
        Assert.Equal("My Connector", l.DisplayName);
        Assert.False(l.IsEnabled);
        Assert.Equal(["tag-a", "tag-b"], l.Tags);
        Assert.Equal("./src", l.Configuration.GetValue("rootPath"));
        Assert.Equal(".dll,.exe", l.Configuration.GetValue("excludeExtensions"));
    }

    [Fact]
    public async Task LoadAllAsync_Throws_InvalidOperationException_For_Malformed_Json()
    {
        var ferretDir = Path.Combine(_tmpDir, ".ferret");
        Directory.CreateDirectory(ferretDir);
        await File.WriteAllTextAsync(Path.Combine(ferretDir, "connectors.json"), "{ not valid json }}}");
        var store = new ConnectorInstanceStore();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.LoadAllAsync(_root).AsTask());

        Assert.Contains("connectors.json", ex.Message);
    }

    [Fact]
    public async Task Configuration_Keys_Are_Case_Insensitive_After_Load()
    {
        var store = new ConnectorInstanceStore();
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("ci"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "CI",
            Configuration = new ConnectorConfiguration(new Dictionary<string, string> { ["RootPath"] = "/ci" }),
        };

        await store.SaveAsync(_root, [instance]);
        var loaded = await store.LoadAllAsync(_root);

        Assert.Equal("/ci", loaded[0].Configuration.GetValue("rootpath"));
        Assert.Equal("/ci", loaded[0].Configuration.GetValue("ROOTPATH"));
    }

    [Fact]
    public async Task SaveAsync_Is_Atomic_Temp_Then_Rename()
    {
        // Verify no partial files remain after save
        var store = new ConnectorInstanceStore();
        await store.SaveAsync(_root, []);

        var ferretDir = Path.Combine(_tmpDir, ".ferret");
        var tmpFiles = Directory.GetFiles(ferretDir, "*.tmp");
        Assert.Empty(tmpFiles);
    }
}
```

Note: Read `WorkspacePath` in `Ferret.Core.Workspace` to confirm how `FullPath` works before finalising the file path logic inside `ConnectorInstanceStore`.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests --filter "ConnectorInstanceStoreTests"
```

Expected: FAIL — `ConnectorInstanceStore` not found.

- [ ] **Step 3: Add `System.Text.Json` reference to `Ferret.ConnectorPlatform.csproj`**

Read `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj` first. Add an `ItemGroup` with:

```xml
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="9.*" />
  </ItemGroup>
```

- [ ] **Step 4: Create `ConnectorInstanceStore.cs`**

`src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs`:

Uses an internal private JSON model (separate from Core types) to control serialization shape. Backup-before-overwrite: when the loaded `schemaVersion` differs from `"1.0"`, copy the existing file to `.ferret/connectors.json.bak.{timestamp}` before saving. Sprint 9 does not migrate — only preserves.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform;

/// <summary>
/// Persists <see cref="ConnectorInstance"/> records to <c>.ferret/connectors.json</c>.
/// Writes are atomic: content is written to a temp file, then renamed over the target.
/// When the loaded schema version differs from the current version, the existing file
/// is backed up as <c>connectors.json.bak.{timestamp}</c> before overwriting.
/// </summary>
public sealed class ConnectorInstanceStore : IConnectorInstanceStore
{
    private const string CurrentSchemaVersion = "1.0";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConnectorInstance>> LoadAllAsync(
        WorkspacePath rootPath,
        CancellationToken ct = default)
    {
        var filePath = GetFilePath(rootPath);
        if (!File.Exists(filePath))
            return [];

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Failed to read connectors.json at '{filePath}'.", ex);
        }

        JsonConnectorsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<JsonConnectorsFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse connectors.json at '{filePath}'. The file may be corrupt.", ex);
        }

        if (file is null)
            return [];

        return file.Instances.Select(ToInstance).ToList();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        WorkspacePath rootPath,
        IReadOnlyList<ConnectorInstance> instances,
        CancellationToken ct = default)
    {
        var filePath = GetFilePath(rootPath);
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        // Backup if existing file has a different schema version
        if (File.Exists(filePath))
        {
            await BackupIfNeededAsync(filePath, ct).ConfigureAwait(false);
        }

        var file = new JsonConnectorsFile
        {
            SchemaVersion = CurrentSchemaVersion,
            Instances = instances.Select(ToJson).ToList(),
        };

        var json = JsonSerializer.Serialize(file, JsonOptions);
        var tmpPath = filePath + ".tmp";

        await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);
        File.Move(tmpPath, filePath, overwrite: true);
    }

    private static string GetFilePath(WorkspacePath rootPath) =>
        Path.Combine(rootPath.FullPath, ".ferret", "connectors.json");

    private static async Task BackupIfNeededAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var existing = JsonSerializer.Deserialize<JsonConnectorsFile>(json, JsonOptions);
            if (existing?.SchemaVersion != CurrentSchemaVersion)
            {
                var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
                var backupPath = filePath + $".bak.{timestamp}";
                File.Copy(filePath, backupPath, overwrite: false);
            }
        }
        catch (Exception)
        {
            // Backup is best-effort — never block a save due to backup failure
        }
    }

    private static ConnectorInstance ToInstance(JsonConnectorInstance j) =>
        new()
        {
            Id = new ConnectorInstanceId(j.Id),
            ConnectorType = new ConnectorId(j.ConnectorType),
            DisplayName = j.DisplayName,
            IsEnabled = j.Enabled,
            SchemaVersion = j.SchemaVersion ?? CurrentSchemaVersion,
            Tags = j.Tags ?? [],
            Configuration = j.Configuration is null
                ? ConnectorConfiguration.Empty
                : ConnectorConfiguration.FromDictionary(j.Configuration),
        };

    private static JsonConnectorInstance ToJson(ConnectorInstance i) =>
        new()
        {
            Id = i.Id.Value,
            ConnectorType = i.ConnectorType.Value,
            DisplayName = i.DisplayName,
            Enabled = i.IsEnabled,
            SchemaVersion = i.SchemaVersion,
            Tags = i.Tags.Count > 0 ? i.Tags.ToList() : null,
            Configuration = i.Configuration.AsReadOnlyDictionary().Count > 0
                ? new Dictionary<string, string>(i.Configuration.AsReadOnlyDictionary())
                : null,
        };

    // ---- Private JSON model ----

    private sealed class JsonConnectorsFile
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonPropertyName("instances")]
        public List<JsonConnectorInstance> Instances { get; set; } = [];
    }

    private sealed class JsonConnectorInstance
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("connectorType")]
        public string ConnectorType { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("schemaVersion")]
        public string? SchemaVersion { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("configuration")]
        public Dictionary<string, string>? Configuration { get; set; }
    }
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests --filter "ConnectorInstanceStoreTests"
dotnet test tests/Ferret.ConnectorPlatform.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 3: `ConnectorManager` + `IndexPipeline` Correction

**Why:** `ConnectorManager` wires `IConnectorInstanceStore` + `IConnectorFactory` instances into a process-scoped cache of `ConnectorRuntime`. `IndexPipeline` must be corrected in parallel because the S3 implementation uses `IConnectorRegistry.GetEnabled()` — now incorrect; `IConnectorManager.GetActiveConnectorsAsync()` is the right abstraction. Both changes are in the same task to keep the breaking change contained.

**Files:**
- Create: `src/Ferret.ConnectorPlatform/ConnectorManager.cs`
- Modify: `src/Ferret.Indexing/IndexPipeline.cs`
- Create: `tests/Ferret.Indexing.Tests/Fakes/FakeConnectorManager.cs`
- Modify (replace): `tests/Ferret.Indexing.Tests/Fakes/FakeConnectorRegistry.cs` → delete or repurpose
- Create: `tests/Ferret.Indexing.Tests/IndexPipelineConnectorManagerTests.cs`
- Modify: `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/ConnectorManagerTests.cs`

**Interfaces:**
- Consumes: `IConnectorInstanceStore` (Task 2), `IConnectorFactory` (Task 1 revision), `ConnectorInstance`, `ConnectorRuntime`, `ConnectorStatus` (Core)
- Produces: `ConnectorManager` (registered as `IConnectorManager` in Task 6); corrected `IndexPipeline`

- [ ] **Step 1: Write failing `ConnectorManager` tests**

Create `tests/Ferret.ConnectorPlatform.Tests/ConnectorManagerTests.cs`:

```csharp
using Ferret.ConnectorPlatform;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class ConnectorManagerTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    public ConnectorManagerTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    [Fact]
    public async Task GetActiveConnectorsAsync_Returns_Empty_When_No_Instances()
    {
        var manager = new ConnectorManager(_store, [], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Empty(runtimes);
    }

    [Fact]
    public async Task GetActiveConnectorsAsync_Returns_Runtime_For_Enabled_Instance()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Default",
        };
        await _store.SaveAsync(_root, [instance]);

        var factory = new FakeConnectorManagerFactory("fake");
        var manager = new ConnectorManager(_store, [factory], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Single(runtimes);
        Assert.Equal("default", runtimes[0].Instance.Id.Value);
    }

    [Fact]
    public async Task GetActiveConnectorsAsync_Skips_Disabled_Instances()
    {
        var enabled = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("enabled"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Enabled",
            IsEnabled = true,
        };
        var disabled = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("disabled"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Disabled",
            IsEnabled = false,
        };
        await _store.SaveAsync(_root, [enabled, disabled]);

        var factory = new FakeConnectorManagerFactory("fake");
        var manager = new ConnectorManager(_store, [factory], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Single(runtimes);
        Assert.Equal("enabled", runtimes[0].Instance.Id.Value);
    }

    [Fact]
    public async Task GetActiveConnectorsAsync_Skips_Unknown_ConnectorType()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("x"),
            ConnectorType = new ConnectorId("unknown-type"),
            DisplayName = "X",
        };
        await _store.SaveAsync(_root, [instance]);
        var manager = new ConnectorManager(_store, [], _root); // no factories

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Empty(runtimes);
    }

    [Fact]
    public async Task GetActiveConnectorsAsync_Returns_Same_Cached_Runtime_On_Second_Call()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Default",
        };
        await _store.SaveAsync(_root, [instance]);

        var factory = new FakeConnectorManagerFactory("fake");
        var manager = new ConnectorManager(_store, [factory], _root);

        var first = await manager.GetActiveConnectorsAsync();
        var second = await manager.GetActiveConnectorsAsync();

        Assert.Same(first[0], second[0]);
    }

    [Fact]
    public async Task GetInstanceAsync_Returns_Null_For_Unknown_Id()
    {
        var manager = new ConnectorManager(_store, [], _root);

        var instance = await manager.GetInstanceAsync(new ConnectorInstanceId("nonexistent"));

        Assert.Null(instance);
    }

    [Fact]
    public async Task GetInstanceAsync_Returns_Instance_By_Id()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("my-instance"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Mine",
        };
        await _store.SaveAsync(_root, [instance]);
        var manager = new ConnectorManager(_store, [], _root);

        var loaded = await manager.GetInstanceAsync(new ConnectorInstanceId("my-instance"));

        Assert.NotNull(loaded);
        Assert.Equal("my-instance", loaded!.Id.Value);
    }

    [Fact]
    public void Dispose_Does_Not_Throw()
    {
        var manager = new ConnectorManager(_store, [], _root);
        var ex = Record.Exception(() => manager.Dispose());
        Assert.Null(ex);
    }

    // ---- Inner fake ----

    private sealed class FakeConnectorManagerFactory : IConnectorFactory
    {
        internal FakeConnectorManagerFactory(string connectorId)
        {
            ConnectorId = new ConnectorId(connectorId);
            Descriptor = new ConnectorDescriptor
            {
                Id = ConnectorId,
                Metadata = ConnectorMetadata.Create(
                    connectorId, connectorId, $"{connectorId} connector",
                    ConnectorType.Custom, "1.0"),
                Capabilities = [],
                SupportedPlatforms = [],
            };
        }

        public ConnectorId ConnectorId { get; }
        public ConnectorDescriptor Descriptor { get; }

        public IConnector Create(ConnectorInstance instance) =>
            new FakeConnector(instance.Id);
    }

    private sealed class FakeConnector : IConnector
    {
        internal FakeConnector(ConnectorInstanceId id)
        {
            ConnectorType = ConnectorType.Custom;
            Metadata = ConnectorMetadata.Create(
                id.Value, id.Value, "fake", ConnectorType.Custom, "1.0");
            Capabilities = ConnectorIoCapabilities.ReadOnly();
        }

        public ConnectorType ConnectorType { get; }
        public ConnectorMetadata Metadata { get; }
        public ConnectorIoCapabilities Capabilities { get; }

        public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));

        public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task DisconnectAsync(CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
```

Note: Read `ConnectorMetadata`, `ConnectorIoCapabilities`, and `ConnectorType` in `Ferret.Core.Connectors` before writing inner fakes — verify the factory static method signatures.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests --filter "ConnectorManagerTests"
```

Expected: FAIL — `ConnectorManager` not found.

- [ ] **Step 3: Create `ConnectorManager.cs`**

`src/Ferret.ConnectorPlatform/ConnectorManager.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform;

/// <summary>
/// Process-scoped lifecycle owner for connector runtimes.
/// Loads enabled instances from <see cref="IConnectorInstanceStore"/>, creates connectors
/// via registered <see cref="IConnectorFactory"/> instances, and caches the resulting
/// <see cref="ConnectorRuntime"/> objects for the lifetime of the process.
/// <para>Instances with an unregistered <see cref="ConnectorInstance.ConnectorType"/> are silently skipped.</para>
/// <para>Thread safety: protected by a <see cref="SemaphoreSlim"/>.</para>
/// </summary>
internal sealed class ConnectorManager : IConnectorManager, IDisposable
{
    private readonly IConnectorInstanceStore _store;
    private readonly IReadOnlyDictionary<ConnectorId, IConnectorFactory> _factories;
    private readonly WorkspacePath _rootPath;
    private readonly Dictionary<ConnectorInstanceId, ConnectorRuntime> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    /// <summary>Initializes a new <see cref="ConnectorManager"/>.</summary>
    /// <param name="store">The persistent instance store.</param>
    /// <param name="factories">All registered connector factories.</param>
    /// <param name="rootPath">The workspace root path.</param>
    public ConnectorManager(
        IConnectorInstanceStore store,
        IEnumerable<IConnectorFactory> factories,
        WorkspacePath rootPath)
    {
        _store = store;
        _factories = factories.ToDictionary(f => f.ConnectorId);
        _rootPath = rootPath;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(
        CancellationToken ct = default)
    {
        var instances = await _store.LoadAllAsync(_rootPath, ct).ConfigureAwait(false);

        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var result = new List<ConnectorRuntime>();
            foreach (var instance in instances.Where(i => i.IsEnabled))
            {
                if (!_factories.TryGetValue(instance.ConnectorType, out var factory))
                    continue; // Unknown connector type — skip silently

                if (!_cache.TryGetValue(instance.Id, out var runtime))
                {
                    var connector = factory.Create(instance);
                    runtime = new ConnectorRuntime
                    {
                        Instance = instance,
                        Connector = connector,
                        Status = new ConnectorStatus
                        {
                            ConnectorId = instance.ConnectorType,
                            InstanceId = instance.Id,
                            IsActive = true,
                            Health = ConnectorHealth.Connected(DateTimeOffset.UtcNow),
                        },
                    };
                    _cache[instance.Id] = runtime;
                }

                result.Add(runtime);
            }

            return result;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ConnectorInstance?> GetInstanceAsync(
        ConnectorInstanceId id,
        CancellationToken ct = default)
    {
        var instances = await _store.LoadAllAsync(_rootPath, ct).ConfigureAwait(false);
        return instances.FirstOrDefault(i => i.Id == id);
    }

    /// <inheritdoc/>
    public void Dispose() => _cacheLock.Dispose();
}
```

- [ ] **Step 4: Create `FakeConnectorManager.cs` in Indexing tests**

`tests/Ferret.Indexing.Tests/Fakes/FakeConnectorManager.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IConnectorManager. Returns a pre-configured list of runtimes.</summary>
internal sealed class FakeConnectorManager : IConnectorManager
{
    private readonly List<ConnectorRuntime> _runtimes;

    internal FakeConnectorManager(IEnumerable<ConnectorRuntime> runtimes)
    {
        _runtimes = runtimes.ToList();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ConnectorRuntime>>(_runtimes);

    /// <inheritdoc/>
    public Task<ConnectorInstance?> GetInstanceAsync(
        ConnectorInstanceId id,
        CancellationToken ct = default) =>
        Task.FromResult(_runtimes.FirstOrDefault(r => r.Instance.Id == id)?.Instance);
}
```

- [ ] **Step 5: Correct `IndexPipeline.cs`**

Read `src/Ferret.Indexing/IndexPipeline.cs` first. Replace `IConnectorRegistry` with `IConnectorManager`:

Constructor change:
```csharp
// Before:
public IndexPipeline(
    IConnectorRegistry registry,
    IParserDispatcher dispatcher,
    IIndexEngine engine,
    IEventBus bus,
    CorrelationId correlationId)

// After:
public IndexPipeline(
    IConnectorManager connectorManager,
    IParserDispatcher dispatcher,
    IIndexEngine engine,
    IEventBus bus,
    CorrelationId correlationId)
```

In `RunCoreAsync`, replace:
```csharp
// Before:
var connectors = _registry.GetEnabled();
foreach (var connector in connectors)
{
    if (connector is not IAssetSource assetSource) continue;
    // ...
    if (connector is not IAssetReader reader)
    // ...
}
```

With:
```csharp
// After:
var runtimes = await _connectorManager.GetActiveConnectorsAsync(ct).ConfigureAwait(false);
foreach (var runtime in runtimes)
{
    if (runtime.Connector is not IAssetSource assetSource) continue;
    // ...
    if (runtime.Connector is not IAssetReader reader)
    // ...
}
```

- [ ] **Step 6: Update `IndexPipelineTests.cs` to use `FakeConnectorManager`**

Read `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs` first. Replace `FakeConnectorRegistry` usage:

1. Update `BuildPipeline` helper to accept `FakeConnectorManager` instead of `FakeConnectorRegistry`:
   ```csharp
   private static (IndexPipeline pipeline, FakeIndexEngine engine, FakeEventBus bus)
       BuildPipeline(FakeConnectorManager manager, FakeParserDispatcher dispatcher)
   {
       var engine = new FakeIndexEngine();
       var bus = new FakeEventBus();
       var correlationId = new CorrelationId("test-run");
       var pipeline = new IndexPipeline(manager, dispatcher, engine, bus, correlationId);
       return (pipeline, engine, bus);
   }
   ```

2. In each test that previously created `FakeConnectorRegistry([fakeConnector])`, create a `ConnectorRuntime` and `FakeConnectorManager` instead:
   ```csharp
   // Pattern for tests using FakeConnectorWithReader:
   var runtime = new ConnectorRuntime
   {
       Instance = new ConnectorInstance
       {
           Id = new ConnectorInstanceId("test"),
           ConnectorType = new ConnectorId("filesystem"),
           DisplayName = "test",
       },
       Connector = fakeConnector,
       Status = new ConnectorStatus
       {
           ConnectorId = new ConnectorId("filesystem"),
           InstanceId = new ConnectorInstanceId("test"),
           IsActive = true,
           Health = ConnectorHealth.Connected(DateTimeOffset.UtcNow),
       },
   };
   var manager = new FakeConnectorManager([runtime]);
   ```

3. Delete `tests/Ferret.Indexing.Tests/Fakes/FakeConnectorRegistry.cs` — it is no longer used.

- [ ] **Step 7: Create `IndexPipelineConnectorManagerTests.cs`**

Create `tests/Ferret.Indexing.Tests/IndexPipelineConnectorManagerTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing.Tests.Fakes;
using Xunit;

namespace Ferret.Indexing.Tests;

/// <summary>Verifies IndexPipeline correctly uses IConnectorManager + ConnectorRuntime (S4 correction).</summary>
public sealed class IndexPipelineConnectorManagerTests
{
    private static ConnectorRuntime MakeRuntime(IConnector connector) =>
        new()
        {
            Instance = new ConnectorInstance
            {
                Id = new ConnectorInstanceId("test"),
                ConnectorType = new ConnectorId("filesystem"),
                DisplayName = "Test",
            },
            Connector = connector,
            Status = new ConnectorStatus
            {
                ConnectorId = new ConnectorId("filesystem"),
                InstanceId = new ConnectorInstanceId("test"),
                IsActive = true,
                Health = ConnectorHealth.Connected(DateTimeOffset.UtcNow),
            },
        };

    [Fact]
    public async Task Pipeline_Receives_FakeConnectorManager_And_Accesses_Connector()
    {
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(new Uri("filesystem:///src/a.txt")),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///src/a.txt"),
            DisplayName = "a.txt",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/plain",
        };
        var fakeConnector = new FakeAssetSourceReader(
            [asset],
            _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content")));
        var manager = new FakeConnectorManager([MakeRuntime(fakeConnector)]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(new Document
        {
            Id = DocumentId.From(a.Id),
            SourceAssetId = a.Id,
            ConnectorId = a.ConnectorId,
            InstanceId = a.InstanceId,
            MediaType = "text/plain",
            Kind = DocumentKind.Unknown,
            PlainText = "content",
            ProducedAt = DateTimeOffset.UtcNow,
        }));
        var engine = new FakeIndexEngine();
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(manager, dispatcher, engine, bus, new CorrelationId("run"));

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(1, result.DocumentsIndexed);
    }

    [Fact]
    public async Task Pipeline_Accesses_Runtime_Connector_As_IAssetSource()
    {
        // Connector that is IAssetSource but not IAssetReader — assets are skipped
        var sourceOnly = new FakeSourceOnlyConnector();
        var manager = new FakeConnectorManager([MakeRuntime(sourceOnly)]);
        var dispatcher = new FakeParserDispatcher();
        var engine = new FakeIndexEngine();
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(manager, dispatcher, engine, bus, new CorrelationId("run"));

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        // Asset discovered, skipped because no IAssetReader
        Assert.Equal(0, result.DocumentsIndexed);
    }

    [Fact]
    public async Task Pipeline_Skips_Runtime_Where_Connector_Is_Not_IAssetSource()
    {
        // Connector that is neither IAssetSource nor IAssetReader
        var plain = new FakePlainConnector();
        var manager = new FakeConnectorManager([MakeRuntime(plain)]);
        var dispatcher = new FakeParserDispatcher();
        var engine = new FakeIndexEngine();
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(manager, dispatcher, engine, bus, new CorrelationId("run"));

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(0, result.AssetsDiscovered);
    }

    // ---- Inner fakes ----

    private sealed class FakeSourceOnlyConnector : IConnector, IAssetSource
    {
        public ConnectorType ConnectorType => ConnectorType.Custom;
        public ConnectorMetadata Metadata => ConnectorMetadata.Create("fake", "fake", "fake", ConnectorType.Custom, "1.0");
        public ConnectorIoCapabilities Capabilities => ConnectorIoCapabilities.ReadOnly();

        public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new AssetDescriptor
            {
                Id = AssetId.From(new Uri("filesystem:///src/a.txt")),
                ConnectorId = new ConnectorId("fake"),
                InstanceId = new ConnectorInstanceId("fake"),
                Kind = AssetKind.File,
                CanonicalUri = new Uri("filesystem:///src/a.txt"),
                DisplayName = "a.txt",
                LastModified = DateTimeOffset.UtcNow,
            };
            await Task.Yield();
        }

        public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));
        public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();
        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePlainConnector : IConnector
    {
        public ConnectorType ConnectorType => ConnectorType.Custom;
        public ConnectorMetadata Metadata => ConnectorMetadata.Create("plain", "plain", "plain", ConnectorType.Custom, "1.0");
        public ConnectorIoCapabilities Capabilities => ConnectorIoCapabilities.ReadOnly();
        public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));
        public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();
        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
```

Note: Read `ConnectorMetadata`, `ConnectorIoCapabilities`, `ConnectorType` before writing inner fakes to confirm static method signatures.

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests --filter "ConnectorManagerTests"
dotnet test tests/Ferret.Indexing.Tests
dotnet test tests/Ferret.Core.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 4: `FilesystemConnectorFactory.Create(ConnectorInstance)`

**Why:** `ConnectorManager` calls `IConnectorFactory.Create(ConnectorInstance)` — the filesystem factory must be updated to read from `ConnectorInstance.Configuration` rather than a static `ConnectorInstanceId`. This is the concrete implementation of the breaking interface change from Task 1.

**Files:**
- Modify: `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs` (read file first — may not exist yet; check with Glob)
- Create: `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorFactoryTests.cs`

**Interfaces:**
- Consumes: `IConnectorFactory` (Task 1 revision), `ConnectorInstance`, `ConnectorConfiguration`, `FilesystemConnectorConfiguration`
- Produces: Updated `FilesystemConnectorFactory` — consumed by `ConnectorManager` (Task 3)

**Configuration key mapping:**

| `ConnectorInstance.Configuration` key | `FilesystemConnectorConfiguration` field |
|---|---|
| `rootPath` | `RootPath` (default `"."`) |
| `includeExtensions` | `IncludeExtensions` (comma-split, ensure `.` prefix) |
| `excludeExtensions` | `ExcludeExtensions` (comma-split, ensure `.` prefix) |

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorFactoryTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorFactoryTests
{
    private static ConnectorInstance MakeInstance(
        string id = "default",
        string? rootPath = null,
        string? include = null,
        string? exclude = null)
    {
        var dict = new Dictionary<string, string>();
        if (rootPath is not null) dict["rootPath"] = rootPath;
        if (include is not null) dict["includeExtensions"] = include;
        if (exclude is not null) dict["excludeExtensions"] = exclude;
        return new ConnectorInstance
        {
            Id = new ConnectorInstanceId(id),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = id,
            Configuration = new ConnectorConfiguration(dict),
        };
    }

    [Fact]
    public void Create_Returns_FilesystemConnector()
    {
        var factory = new FilesystemConnectorFactory();
        var instance = MakeInstance();

        var connector = factory.Create(instance);

        Assert.IsType<FilesystemConnector>(connector);
    }

    [Fact]
    public void Create_With_RootPath_Config_Uses_That_Path()
    {
        var factory = new FilesystemConnectorFactory();
        var instance = MakeInstance(rootPath: "./src");

        var connector = (FilesystemConnector)factory.Create(instance);

        // Access internal config via GetHealthAsync or a test-visible property
        // If config is not public, verify indirectly via GetHealthAsync on a known path
        // Adjust this assertion based on actual FilesystemConnector internals.
        Assert.NotNull(connector);
    }

    [Fact]
    public void Create_With_Missing_RootPath_Defaults_To_Dot()
    {
        var factory = new FilesystemConnectorFactory();
        var instance = MakeInstance(); // no rootPath key

        // Should not throw
        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    [Fact]
    public void Create_With_ExcludeExtensions_No_Dots_Adds_Dots()
    {
        var factory = new FilesystemConnectorFactory();
        var instance = MakeInstance(exclude: "dll,exe");

        // Should not throw — dots are normalised inside the factory
        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    [Fact]
    public void Create_With_ExcludeExtensions_Already_Dotted_Keeps_Dots()
    {
        var factory = new FilesystemConnectorFactory();
        var instance = MakeInstance(exclude: ".dll,.exe,.pdb");

        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    [Fact]
    public void ConnectorId_Returns_Filesystem()
    {
        var factory = new FilesystemConnectorFactory();

        Assert.Equal("filesystem", factory.ConnectorId.Value);
    }
}
```

Note: Read `FilesystemConnectorFactory.cs` with Glob first — it may not exist yet (Sprint 8 may have left it as a placeholder). Adjust the test to match the actual file location.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorFactoryTests"
```

Expected: FAIL — `FilesystemConnectorFactory.Create(ConnectorInstance)` not present.

- [ ] **Step 3: Create or update `FilesystemConnectorFactory.cs`**

Read the existing file first (Glob `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs`). If it exists, update the `Create` method. If it doesn't exist, create it:

`src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem;

/// <summary>Creates <see cref="FilesystemConnector"/> instances from stored <see cref="ConnectorInstance"/> records.</summary>
public sealed class FilesystemConnectorFactory : IConnectorFactory
{
    /// <inheritdoc/>
    public ConnectorId ConnectorId { get; } = new("filesystem");

    /// <inheritdoc/>
    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Id = new ConnectorId("filesystem"),
        Metadata = ConnectorMetadata.Create(
            "filesystem",
            "Filesystem Connector",
            "Discovers and reads files from the local filesystem.",
            ConnectorType.Filesystem,
            "1.0"),
        Capabilities = [ConnectorCapability.AssetDiscovery, ConnectorCapability.ContentReading],
        SupportedPlatforms = ["Linux", "macOS", "Windows"],
    };

    /// <inheritdoc/>
    public IConnector Create(ConnectorInstance instance)
    {
        var config = new FilesystemConnectorConfiguration
        {
            RootPath = instance.Configuration.GetValueOrDefault("rootPath", "."),
            IncludeExtensions = ParseExtensions(
                instance.Configuration.GetValue("includeExtensions")),
            ExcludeExtensions = ParseExtensions(
                instance.Configuration.GetValue("excludeExtensions")),
        };
        return new FilesystemConnector(config);
    }

    private static IReadOnlyList<string> ParseExtensions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToList();
    }
}
```

Note: Read `ConnectorCapability.cs` and `ConnectorMetadata.cs` in `Ferret.Core.Connectors` to confirm enum values and static factory method signature before writing.

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorFactoryTests"
dotnet test tests/Ferret.Connectors.Filesystem.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 5: CLI Command Handlers (5 handlers)

**Why:** These are the user-visible deliverables of Section 4. Each handler follows the same `ICommandHandler` pattern as `WorkspaceInitCommandHandler` (uses `IFerretContext`, returns `CommandResult`). All five handlers use `IConnectorInstanceStore` directly for atomic read-modify-write. `validate` also uses `IConnectorRegistry` to check that instance types are registered.

**Files:**
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorEnableCommandHandler.cs`
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorDisableCommandHandler.cs`
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorConfigureCommandHandler.cs`
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorInspectCommandHandler.cs`
- Create: `src/Ferret.ConnectorPlatform/Commands/ConnectorValidateCommandHandler.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorEnableCommandTests.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorConfigureCommandTests.cs`
- Create: `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorValidateCommandTests.cs`

**Interfaces:**
- Consumes: `IConnectorInstanceStore` (Task 2), `IConnectorRegistry` (existing in `ConnectorPlatform`), `IFerretContext`, `ICommandHandler`, `CommandResult` (from `Ferret.Cli`)
- Produces: 5 command handlers — registered in Task 6

Note: Command handlers live in `Ferret.ConnectorPlatform` but must reference `Ferret.Cli` types (`ICommandHandler`, `IFerretContext`, `CommandResult`). Read `src/Ferret.ConnectorPlatform/Ferret.ConnectorPlatform.csproj` first — if `Ferret.Cli` is not already referenced, add it. Alternatively, if command handlers should live in `Ferret.Cli/Commands/` instead (to avoid the reverse dependency), place them there and adjust the file locations. Check existing Sprint 8 CLI module placement before deciding. If `ConnectorCliModule` is already in `Ferret.ConnectorPlatform`, command handlers can go alongside it.

**Shared options resolution pattern** (read context options in handlers):
```csharp
// Example: get the instance name option from IFerretContext
var name = context.GetOption<string>("name") ?? "default";
var type = context.GetOption<string>("type") ?? string.Empty;
var path = context.GetOption<string>("path");
```

- [ ] **Step 1: Write failing tests for `enable` and `configure`**

Create `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorEnableCommandTests.cs`:

```csharp
using Ferret.ConnectorPlatform;
using Ferret.ConnectorPlatform.Commands;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests.Commands;

public sealed class ConnectorEnableCommandTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    public ConnectorEnableCommandTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, recursive: true);
    }

    [Fact]
    public async Task Enable_New_Connector_Creates_Instance_In_Store()
    {
        var handler = new ConnectorEnableCommandHandler(_store);
        var context = new FakeEnableContext(_tmpDir, type: "filesystem", name: "default", path: ".");

        await handler.ExecuteAsync(context);

        var instances = await _store.LoadAllAsync(_root);
        Assert.Single(instances);
        Assert.Equal("default", instances[0].Id.Value);
        Assert.Equal("filesystem", instances[0].ConnectorType.Value);
        Assert.True(instances[0].IsEnabled);
    }

    [Fact]
    public async Task Enable_Already_Enabled_Returns_Success_No_Write()
    {
        var existing = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            IsEnabled = true,
        };
        await _store.SaveAsync(_root, [existing]);
        var handler = new ConnectorEnableCommandHandler(_store);
        var context = new FakeEnableContext(_tmpDir, type: "filesystem", name: "default", path: null);

        // Should not throw and should return success
        await handler.ExecuteAsync(context);

        var instances = await _store.LoadAllAsync(_root);
        Assert.Single(instances);
    }

    [Fact]
    public async Task Enable_Disabled_Connector_Sets_IsEnabled_True()
    {
        var existing = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            IsEnabled = false,
        };
        await _store.SaveAsync(_root, [existing]);
        var handler = new ConnectorEnableCommandHandler(_store);
        var context = new FakeEnableContext(_tmpDir, type: "filesystem", name: "default", path: null);

        await handler.ExecuteAsync(context);

        var instances = await _store.LoadAllAsync(_root);
        Assert.True(instances[0].IsEnabled);
    }

    // ---- Fake context ----

    private sealed class FakeEnableContext : IFerretContext
    {
        private readonly string _cwd;
        private readonly string _type;
        private readonly string _name;
        private readonly string? _path;

        internal FakeEnableContext(string cwd, string type, string name, string? path)
        {
            _cwd = cwd;
            _type = type;
            _name = name;
            _path = path;
        }

        public CancellationToken CancellationToken => CancellationToken.None;
        public VerbosityLevel Verbosity => VerbosityLevel.Normal;
        public OutputFormat OutputFormat => OutputFormat.Text;
        public IFerretServices Services => throw new NotImplementedException();
        public string WorkingDirectory => _cwd;

        public T? GetOption<T>(string name) => name switch
        {
            "type" => (T?)(object?)_type,
            "name" => (T?)(object?)_name,
            "path" => (T?)(object?)_path,
            _ => default,
        };
    }
}
```

Note: Read `IFerretContext`, `VerbosityLevel`, `OutputFormat` from `Ferret.Cli.Cli` before writing test fakes. The `IFerretServices` property may need to return a stub — check `WorkspaceInitCommandHandler` tests for the existing pattern.

Create `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorConfigureCommandTests.cs`:

```csharp
using Ferret.ConnectorPlatform;
using Ferret.ConnectorPlatform.Commands;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests.Commands;

public sealed class ConnectorConfigureCommandTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    public ConnectorConfigureCommandTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, recursive: true);
    }

    [Fact]
    public async Task Configure_Path_Only_Changes_RootPath_Leaves_Exclude_Unchanged()
    {
        var existing = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            Configuration = new ConnectorConfiguration(new Dictionary<string, string>
            {
                ["rootPath"] = ".",
                ["excludeExtensions"] = ".dll,.exe",
            }),
        };
        await _store.SaveAsync(_root, [existing]);
        var handler = new ConnectorConfigureCommandHandler(_store);
        var context = new FakeConfigureContext(_tmpDir, name: "default", path: "./src", exclude: null, displayName: null);

        await handler.ExecuteAsync(context);

        var instances = await _store.LoadAllAsync(_root);
        Assert.Equal("./src", instances[0].Configuration.GetValue("rootPath"));
        Assert.Equal(".dll,.exe", instances[0].Configuration.GetValue("excludeExtensions"));
    }

    [Fact]
    public async Task Configure_Exclude_Only_Changes_Exclude_Leaves_RootPath_Unchanged()
    {
        var existing = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "default",
            Configuration = new ConnectorConfiguration(new Dictionary<string, string>
            {
                ["rootPath"] = "./src",
                ["excludeExtensions"] = ".dll",
            }),
        };
        await _store.SaveAsync(_root, [existing]);
        var handler = new ConnectorConfigureCommandHandler(_store);
        var context = new FakeConfigureContext(_tmpDir, name: "default", path: null, exclude: ".tmp,.log", displayName: null);

        await handler.ExecuteAsync(context);

        var instances = await _store.LoadAllAsync(_root);
        Assert.Equal("./src", instances[0].Configuration.GetValue("rootPath"));
        Assert.Equal(".tmp,.log", instances[0].Configuration.GetValue("excludeExtensions"));
    }

    // ---- Fake context ----

    private sealed class FakeConfigureContext : IFerretContext
    {
        private readonly string _cwd;
        private readonly string _name;
        private readonly string? _path;
        private readonly string? _exclude;
        private readonly string? _displayName;

        internal FakeConfigureContext(
            string cwd, string name, string? path, string? exclude, string? displayName)
        {
            _cwd = cwd; _name = name; _path = path; _exclude = exclude; _displayName = displayName;
        }

        public CancellationToken CancellationToken => CancellationToken.None;
        public VerbosityLevel Verbosity => VerbosityLevel.Normal;
        public OutputFormat OutputFormat => OutputFormat.Text;
        public IFerretServices Services => throw new NotImplementedException();
        public string WorkingDirectory => _cwd;

        public T? GetOption<T>(string name) => name switch
        {
            "name" => (T?)(object?)_name,
            "path" => (T?)(object?)_path,
            "exclude" => (T?)(object?)_exclude,
            "display-name" => (T?)(object?)_displayName,
            _ => default,
        };
    }
}
```

Create `tests/Ferret.ConnectorPlatform.Tests/Commands/ConnectorValidateCommandTests.cs`:

```csharp
using Ferret.ConnectorPlatform;
using Ferret.ConnectorPlatform.Commands;
using Ferret.ConnectorPlatform.Tests.Fakes;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests.Commands;

public sealed class ConnectorValidateCommandTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    public ConnectorValidateCommandTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, recursive: true);
    }

    [Fact]
    public async Task Validate_Known_Type_Returns_IsValid_True()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Default",
        };
        await _store.SaveAsync(_root, [instance]);
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        var handler = new ConnectorValidateCommandHandler(_store, registry);
        var context = new FakeValidateContext(_tmpDir);

        var result = await handler.ValidateAsync(_root);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_Unknown_Type_Returns_IsValid_False()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("unknown-type"),
            DisplayName = "Default",
        };
        await _store.SaveAsync(_root, [instance]);
        var registry = RegistryBuilder.Build([]); // no factories
        var handler = new ConnectorValidateCommandHandler(_store, registry);

        var result = await handler.ValidateAsync(_root);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.InstanceId == "default");
    }

    [Fact]
    public async Task Validate_No_File_Returns_Error()
    {
        var registry = RegistryBuilder.Build([]);
        var handler = new ConnectorValidateCommandHandler(_store, registry);

        var result = await handler.ValidateAsync(_root);

        // No connectors.json — no instances to validate, considered valid (empty OK)
        Assert.True(result.IsValid);
    }

    // Fake context (minimal)
    private sealed class FakeValidateContext : IFerretContext
    {
        private readonly string _cwd;
        internal FakeValidateContext(string cwd) { _cwd = cwd; }

        public CancellationToken CancellationToken => CancellationToken.None;
        public VerbosityLevel Verbosity => VerbosityLevel.Normal;
        public OutputFormat OutputFormat => OutputFormat.Text;
        public IFerretServices Services => throw new NotImplementedException();
        public string WorkingDirectory => _cwd;
        public T? GetOption<T>(string name) => default;
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests --filter "ConnectorEnableCommandTests|ConnectorConfigureCommandTests|ConnectorValidateCommandTests"
```

Expected: FAIL — handler types not found.

- [ ] **Step 3: Create `ConnectorEnableCommandHandler.cs`**

`src/Ferret.ConnectorPlatform/Commands/ConnectorEnableCommandHandler.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles <c>ferret connector enable &lt;type&gt;</c>.</summary>
internal sealed class ConnectorEnableCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of <see cref="ConnectorEnableCommandHandler"/>.</summary>
    public ConnectorEnableCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var type = context.GetOption<string>("type") ?? string.Empty;
        var name = context.GetOption<string>("name") ?? "default";
        var path = context.GetOption<string>("path");
        var include = context.GetOption<string>("include");
        var exclude = context.GetOption<string>("exclude");

        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = (await _store.LoadAllAsync(rootPath, context.CancellationToken)
            .ConfigureAwait(false)).ToList();

        var id = new ConnectorInstanceId(name);
        var existing = instances.FirstOrDefault(i => i.Id == id);

        if (existing is not null && existing.IsEnabled)
        {
            context.Services.Output.WriteLine($"Connector '{name}' is already enabled.");
            return CommandResult.Success;
        }

        ConnectorInstance updated;
        if (existing is not null)
        {
            var cfg = PatchConfiguration(existing.Configuration, path, include, exclude);
            updated = existing with { IsEnabled = true, Configuration = cfg };
            var idx = instances.IndexOf(existing);
            instances[idx] = updated;
        }
        else
        {
            var cfg = BuildConfiguration(path, include, exclude);
            updated = new ConnectorInstance
            {
                Id = id,
                ConnectorType = new ConnectorId(type),
                DisplayName = name,
                IsEnabled = true,
                Configuration = cfg,
            };
            instances.Add(updated);
        }

        await _store.SaveAsync(rootPath, instances, context.CancellationToken).ConfigureAwait(false);
        context.Services.Output.WriteLine(
            $"Enabled {type} connector '{name}' at '{updated.Configuration.GetValueOrDefault("rootPath", ".")}'.");
        return CommandResult.Success;
    }

    private static ConnectorConfiguration BuildConfiguration(
        string? path, string? include, string? exclude)
    {
        var cfg = ConnectorConfiguration.Empty;
        if (path is not null) cfg = cfg.With("rootPath", path);
        if (include is not null) cfg = cfg.With("includeExtensions", include);
        if (exclude is not null) cfg = cfg.With("excludeExtensions", exclude);
        return cfg;
    }

    private static ConnectorConfiguration PatchConfiguration(
        ConnectorConfiguration existing, string? path, string? include, string? exclude)
    {
        var cfg = existing;
        if (path is not null) cfg = cfg.With("rootPath", path);
        if (include is not null) cfg = cfg.With("includeExtensions", include);
        if (exclude is not null) cfg = cfg.With("excludeExtensions", exclude);
        return cfg;
    }
}
```

- [ ] **Step 4: Create `ConnectorDisableCommandHandler.cs`**

`src/Ferret.ConnectorPlatform/Commands/ConnectorDisableCommandHandler.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles <c>ferret connector disable &lt;name&gt;</c>.</summary>
internal sealed class ConnectorDisableCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of <see cref="ConnectorDisableCommandHandler"/>.</summary>
    public ConnectorDisableCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = (await _store.LoadAllAsync(rootPath, context.CancellationToken)
            .ConfigureAwait(false)).ToList();

        var id = new ConnectorInstanceId(name);
        var existing = instances.FirstOrDefault(i => i.Id == id);

        if (existing is null)
        {
            context.Services.Output.WriteLine($"Connector '{name}' not found.");
            return CommandResult.Failure;
        }

        if (!existing.IsEnabled)
        {
            context.Services.Output.WriteLine($"Connector '{name}' is already disabled.");
            return CommandResult.Success;
        }

        var idx = instances.IndexOf(existing);
        instances[idx] = existing with { IsEnabled = false };

        await _store.SaveAsync(rootPath, instances, context.CancellationToken).ConfigureAwait(false);
        context.Services.Output.WriteLine($"Disabled connector '{name}'.");
        return CommandResult.Success;
    }
}
```

- [ ] **Step 5: Create `ConnectorConfigureCommandHandler.cs`**

`src/Ferret.ConnectorPlatform/Commands/ConnectorConfigureCommandHandler.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles <c>ferret connector configure &lt;name&gt;</c>. Patch-based — only supplied flags update configuration.</summary>
internal sealed class ConnectorConfigureCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of <see cref="ConnectorConfigureCommandHandler"/>.</summary>
    public ConnectorConfigureCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var path = context.GetOption<string>("path");
        var exclude = context.GetOption<string>("exclude");
        var include = context.GetOption<string>("include");
        var displayName = context.GetOption<string>("display-name");

        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = (await _store.LoadAllAsync(rootPath, context.CancellationToken)
            .ConfigureAwait(false)).ToList();

        var id = new ConnectorInstanceId(name);
        var existing = instances.FirstOrDefault(i => i.Id == id);

        if (existing is null)
        {
            context.Services.Output.WriteLine($"Connector '{name}' not found.");
            return CommandResult.Failure;
        }

        var cfg = existing.Configuration;
        if (path is not null)
        {
            context.Services.Output.WriteLine(
                $"  rootPath: {cfg.GetValueOrDefault("rootPath", ".")} → {path}");
            cfg = cfg.With("rootPath", path);
        }

        if (exclude is not null)
        {
            context.Services.Output.WriteLine(
                $"  excludeExtensions: {cfg.GetValue("excludeExtensions") ?? "(none)"} → {exclude}");
            cfg = cfg.With("excludeExtensions", exclude);
        }

        if (include is not null)
        {
            context.Services.Output.WriteLine(
                $"  includeExtensions: {cfg.GetValue("includeExtensions") ?? "(none)"} → {include}");
            cfg = cfg.With("includeExtensions", include);
        }

        var updated = existing with
        {
            Configuration = cfg,
            DisplayName = displayName ?? existing.DisplayName,
        };

        var idx = instances.IndexOf(existing);
        instances[idx] = updated;

        await _store.SaveAsync(rootPath, instances, context.CancellationToken).ConfigureAwait(false);
        return CommandResult.Success;
    }
}
```

- [ ] **Step 6: Create `ConnectorInspectCommandHandler.cs`**

`src/Ferret.ConnectorPlatform/Commands/ConnectorInspectCommandHandler.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles <c>ferret connector inspect &lt;name&gt;</c>. Displays full instance configuration.</summary>
internal sealed class ConnectorInspectCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of <see cref="ConnectorInspectCommandHandler"/>.</summary>
    public ConnectorInspectCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = await _store.LoadAllAsync(rootPath, context.CancellationToken)
            .ConfigureAwait(false);

        var instance = instances.FirstOrDefault(i => i.Id.Value == name);
        if (instance is null)
        {
            context.Services.Output.WriteLine($"Connector '{name}' not found.");
            return CommandResult.Failure;
        }

        var out_ = context.Services.Output;
        out_.WriteLine($"Instance ID:     {instance.Id.Value}");
        out_.WriteLine($"Connector Type:  {instance.ConnectorType.Value}");
        out_.WriteLine($"Display Name:    {instance.DisplayName}");
        out_.WriteLine($"Enabled:         {instance.IsEnabled.ToString().ToLowerInvariant()}");
        out_.WriteLine($"Tags:            {(instance.Tags.Count > 0 ? string.Join(", ", instance.Tags) : "(none)")}");
        out_.WriteLine($"Schema Version:  {instance.SchemaVersion}");
        out_.WriteLine(string.Empty);
        out_.WriteLine("Configuration:");
        var dict = instance.Configuration.AsReadOnlyDictionary();
        if (dict.Count == 0)
        {
            out_.WriteLine("  (none)");
        }
        else
        {
            foreach (var kv in dict.OrderBy(k => k.Key))
                out_.WriteLine($"  {kv.Key,-24} {kv.Value}");
        }

        return CommandResult.Success;
    }
}
```

- [ ] **Step 7: Create `ConnectorValidateCommandHandler.cs`**

`src/Ferret.ConnectorPlatform/Commands/ConnectorValidateCommandHandler.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform.Commands;

/// <summary>Handles <c>ferret connector validate</c>. Checks all instances have a registered connector type.</summary>
internal sealed class ConnectorValidateCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;
    private readonly IConnectorRegistry _registry;

    /// <summary>Initializes a new instance of <see cref="ConnectorValidateCommandHandler"/>.</summary>
    public ConnectorValidateCommandHandler(IConnectorInstanceStore store, IConnectorRegistry registry)
    {
        _store = store;
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var result = await ValidateAsync(rootPath, context.CancellationToken).ConfigureAwait(false);

        var instances = await _store.LoadAllAsync(rootPath, context.CancellationToken)
            .ConfigureAwait(false);
        var errors = result.Issues.Count(i => i.Severity == ValidationSeverity.Error);
        var valid = instances.Count - errors;

        context.Services.Output.WriteLine(
            $"Validated {instances.Count} instance(s): {valid} valid, {errors} error(s).");

        foreach (var issue in result.Issues.Where(i => i.Severity == ValidationSeverity.Error))
            context.Services.Output.WriteLine(
                $"  [{issue.InstanceId ?? "?"}] {issue.Message}");

        return result.IsValid ? CommandResult.Success : CommandResult.Failure;
    }

    /// <summary>Validates all instances without requiring <see cref="IFerretContext"/>. Used in tests.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="ct">A cancellation token.</param>
    public async Task<ValidationResult> ValidateAsync(
        WorkspacePath rootPath, CancellationToken ct = default)
    {
        var instances = await _store.LoadAllAsync(rootPath, ct).ConfigureAwait(false);
        var results = instances.Select(i =>
            _registry.IsRegistered(i.ConnectorType)
                ? ValidationResult.Ok()
                : ValidationResult.WithError(
                    $"Connector type '{i.ConnectorType.Value}' is not registered.",
                    i.Id.Value));

        return ValidationResult.Combine(results);
    }
}
```

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.ConnectorPlatform.Tests --filter "ConnectorEnableCommandTests|ConnectorConfigureCommandTests|ConnectorValidateCommandTests"
dotnet test tests/Ferret.ConnectorPlatform.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 6: `ConnectorCliModule` Updates + Final Build

**Why last:** `ConnectorCliModule` ties together all Section 4 outputs. It registers `IConnectorInstanceStore`, `IConnectorManager`, and the 5 command handlers in DI, and contributes the 5 new subcommands to the `connector` group. `ConnectorManager` needs `WorkspacePath` resolved from `IFerretContext.WorkingDirectory` — this wiring lives in the DI registration lambda.

**Files:**
- Modify: existing `ConnectorCliModule` in `Ferret.ConnectorPlatform` or `Ferret.Cli`

Note: Before modifying, run `Glob src/Ferret.ConnectorPlatform/**/*.cs` and `Glob src/Ferret.Cli/Commands/**/*.cs` to find `ConnectorCliModule`. Read it to understand the current structure. The module may currently exist from Sprint 8 with 0 commands — this task adds the 5 new ones.

**Interfaces:**
- Consumes: all 5 command handlers (Task 5), `ConnectorInstanceStore` (Task 2), `ConnectorManager` (Task 3), `IConnectorFactory` implementations, `WorkspacePath`, `IFerretContext`
- Produces: fully wired `ConnectorCliModule` — consumed by `CoreCliModule` / `RootCommandFactory`

- [ ] **Step 1: Find `ConnectorCliModule`**

```
Glob src/Ferret.ConnectorPlatform/**/*.cs
Glob src/Ferret.Cli/Commands/**/*.cs
```

Read the file. Understand the current `GetCommands()` and `ConfigureServices()` implementations.

- [ ] **Step 2: Add command registrations to `GetCommands()`**

Pattern: follow exactly how `WorkspaceCliModule.GetCommands()` defines subcommands. Each new command is a child of the `connector` group.

Add to `GetCommands()`:

```csharp
yield return new CommandDefinition(
    new CommandMetadata("enable", "Enable a connector instance (creates it if it does not exist)."),
    typeof(ConnectorEnableCommandHandler),
    Group: "connector");

yield return new CommandDefinition(
    new CommandMetadata("disable", "Disable a connector instance."),
    typeof(ConnectorDisableCommandHandler),
    Group: "connector");

yield return new CommandDefinition(
    new CommandMetadata("configure", "Patch connector instance configuration."),
    typeof(ConnectorConfigureCommandHandler),
    Group: "connector");

yield return new CommandDefinition(
    new CommandMetadata("inspect", "Display full configuration for a connector instance."),
    typeof(ConnectorInspectCommandHandler),
    Group: "connector");

yield return new CommandDefinition(
    new CommandMetadata("validate", "Validate all connector instances against the registry."),
    typeof(ConnectorValidateCommandHandler),
    Group: "connector");

// Reserved: ferret connector doctor — health, permissions, credentials, connectivity (not implemented)
```

- [ ] **Step 3: Register services in `ConfigureServices()`**

Add to `ConfigureServices()`:

```csharp
// Store
services.AddSingleton<IConnectorInstanceStore, ConnectorInstanceStore>();

// Manager — needs WorkspacePath resolved at runtime from IFerretContext
// WorkspacePath is resolved from IFerretContext.WorkingDirectory at construction time.
// If IFerretContext is available in DI, resolve via factory lambda; otherwise use IHttpContextAccessor pattern.
// Simplest approach: register as Transient, resolve WorkspacePath from IFerretContext in the lambda.
services.AddSingleton<IConnectorManager>(sp =>
{
    var store = sp.GetRequiredService<IConnectorInstanceStore>();
    var factories = sp.GetServices<IConnectorFactory>();
    // WorkspacePath defaults to current directory at startup — adjusted per-command via context
    var rootPath = WorkspacePath.Create(Directory.GetCurrentDirectory());
    return new ConnectorManager(store, factories, rootPath);
});

// Command handlers
services.AddTransient<ConnectorEnableCommandHandler>();
services.AddTransient<ConnectorDisableCommandHandler>();
services.AddTransient<ConnectorConfigureCommandHandler>();
services.AddTransient<ConnectorInspectCommandHandler>();
services.AddTransient<ConnectorValidateCommandHandler>();
```

Note: `ConnectorManager` requires `WorkspacePath` at construction time. The CLI invocation sets the working directory before constructing handlers — `Directory.GetCurrentDirectory()` at DI build time is the workspace root. If a different pattern is used in the codebase (e.g. workspace path injected via `IOptions<>` or a wrapper service), match that pattern instead. Read `WorkspaceInitCommandHandler` and `CoreCliModule` to verify before writing.

- [ ] **Step 4: Final build and full test run**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Core.Tests
dotnet test tests/Ferret.ConnectorPlatform.Tests
dotnet test tests/Ferret.Connectors.Filesystem.Tests
dotnet test tests/Ferret.Indexing.Tests
```

Expected: 0 build errors, 0 warnings, all tests pass.

---

## Section 4 Complete

**Outputs of Section 4:**

- `ConnectorConfiguration` (Core) — future-proof, case-insensitive, immutable string dictionary abstraction
- `ConnectorRuntime` (Core) — encapsulates `ConnectorInstance` + live `IConnector` + `ConnectorStatus`
- `ConnectorInstance` (Core) — completes the Metadata → Descriptor → Instance → Status / Runtime pattern
- `ValidationResult` / `ValidationIssue` / `ValidationSeverity` (Core) — general-purpose validation result type
- `IConnectorInstanceStore` (Core) — interface for `.ferret/connectors.json` I/O
- `ConnectorInstanceStore` (ConnectorPlatform) — atomic `connectors.json` read/write with backup-before-overwrite
- `ConnectorManager` (ConnectorPlatform) — process-scoped cached lifecycle owner; only component that calls `IConnectorFactory.Create`
- `IConnectorFactory.Create(ConnectorInstance)` — configuration-aware factory (breaking change from Sprint 8 `Create(ConnectorInstanceId)`)
- `FilesystemConnectorFactory` updated — reads `rootPath`, `includeExtensions`, `excludeExtensions` from `ConnectorInstance.Configuration`
- `IndexPipeline` corrected — constructor takes `IConnectorManager`; iterates `ConnectorRuntime` list; accesses `runtime.Connector` as `IAssetSource`/`IAssetReader`
- 5 CLI commands: `enable`, `disable`, `configure`, `inspect`, `validate`
- ADR-0014 Principle 10 — only `ConnectorManager` creates connector runtime instances + `ConnectorPolicy`/`ConnectorProfile`/`doctor` reservations
- All existing tests still pass, `dotnet build src/Ferret.sln` clean

**What Section 5 (Wire-up) depends on from Section 4:**

- `IConnectorManager` — `ConnectorManager` implementation; `IndexingModule` wires this via DI
- `SqliteKeywordIndexEngine` — path resolved from `WorkspacePath` + `.ferret/indexes/keyword/keyword-index.db` (from S3)
- `IIndexPipeline` — `IndexPipeline` implementation (corrected in this section); S5 registers it via `IndexingModule`
- `IndexingModule.ConfigureServices` — registers `IIndexPipeline` (S3); S5 calls it after registering `IIndexEngine`
- `ParserPlatformModule.ConfigureServices` — registers `IParserDispatcher` (S2); S5 calls it before `IndexingModule`
- `ConnectorCliModule` — with 5 new commands now fully wired; S5 adds the `index` command alongside these
