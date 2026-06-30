> **Historical note:** This document was written when the product was named AISpace, which was renamed to Ferret during Sprint 5.

# Sprint 4 — Platform Foundation (Contracts + Architecture Completion)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the architecture documentation baseline, define the Runtime and Workspace public contracts in `Ferret.Core`, migrate workspace exceptions to their canonical namespace, and reach 100+ passing tests.

**Architecture:** Pure contracts — no implementations. All C# goes into `Ferret.Core` sub-namespaces (`Ferret.Core.Runtime`, `Ferret.Core.Workspace`, `Ferret.Core.Workspace.Errors`). Architecture documents go under `docs/`. No engine logic, no CLI wiring, no business rules.

**Tech Stack:** C# 13 / .NET 9, xunit 2.x, StyleCop.Analyzers, Markdown + Mermaid.

## Global Constraints

- `Ferret.Core` must have zero `<ProjectReference>` elements and zero NuGet runtime packages — enforced by the ARCH001 MSBuild target (build error on violation).
- `TreatWarningsAsErrors=true` — every StyleCop warning is a build error.
- `documentExposedElements=true` (StyleCop SA1600) — every `public` and `protected` member must carry an XML doc comment.
- `usingDirectivesPlacement=outsideNamespace` (StyleCop SA1200) — all `using` directives go before the `namespace` declaration.
- Value objects: `sealed class`, private constructor, `static Create(...)` factory, `IEquatable<T>`. No `record` — existing Sprint 3 value objects are `sealed class`.
- Domain events: inherit `DomainEvent` from `Ferret.Core.Events`.
- Test method naming: `MethodName_StateUnderTest_ExpectedBehaviour`.
- No business logic. No engine implementations. Contracts only.
- All changes build green before each commit. Never intentionally break the repository.
- `WorkspacePath` replaces raw `string` for paths wherever practical.
- Runtime contracts must not reference Workspace types.

---

## File Map

**Work Package A — Architecture Docs (execute from existing plan):**
- `docs/000-Overview/Vision.md` — rename DOC-001 → VISION-001
- `docs/000-Overview/Mission.md` — rename DOC-002 → MISSION-001
- `docs/000-Overview/Principles.md` — rename DOC-003 → PRINCIPLES-001
- `docs/000-Overview/Glossary.md` — rename DOC-004 → GLOSSARY-001
- `docs/001-Product/PRD-001.md` — fix DOC-00x references
- `docs/002-Architecture/ARCH-001.md` — fix refs; add §7.3, §8.6, §24; refactor §18
- `docs/002-Architecture/ARCH-003.md` — add ARCH-011 reference
- Create: `docs/002-Architecture/ARCH-011.md` — Configuration Architecture
- Create: `docs/002-Architecture/ARCH-013.md` — Event Architecture
- Create: `docs/002-Architecture/ARCH-014.md` — Platform Error Model
- Create: `docs/001-Product/ROADMAP-001.md` — Platform Roadmap
- `docs/002-Architecture/README.md` — update index

**Work Package B — Runtime Contracts:**
- Create: `src/Ferret.Core/Runtime/RuntimeState.cs`
- Create: `src/Ferret.Core/Runtime/ModuleState.cs`
- Create: `src/Ferret.Core/Runtime/ModuleCapability.cs`
- Create: `src/Ferret.Core/Runtime/ModuleMetadata.cs`
- Create: `src/Ferret.Core/Runtime/ILifecycleParticipant.cs`
- Create: `src/Ferret.Core/Runtime/IExecutionContext.cs`
- Create: `src/Ferret.Core/Runtime/IModuleDescriptor.cs`
- Create: `src/Ferret.Core/Runtime/IModuleRegistry.cs`
- Create: `src/Ferret.Core/Runtime/IModuleContext.cs`
- Create: `src/Ferret.Core/Runtime/IModule.cs`
- Create: `src/Ferret.Core/Runtime/IRuntimeService.cs`
- Create: `src/Ferret.Core/Runtime/IRuntimeHost.cs`
- Create: `src/Ferret.Core/Runtime/IRuntimeBuilder.cs`
- Create: `src/Ferret.Core/Runtime/Events/RuntimeStarted.cs`
- Create: `src/Ferret.Core/Runtime/Events/RuntimeStopped.cs`
- Create: `src/Ferret.Core/Runtime/Events/ModuleLoaded.cs`
- Create: `src/Ferret.Core/Runtime/Events/ModuleActivated.cs`
- Create: `src/Ferret.Core/Runtime/Events/ModuleStopped.cs`
- Create: `tests/Ferret.Core.Tests/Runtime/RuntimeEnumTests.cs`
- Create: `tests/Ferret.Core.Tests/Runtime/RuntimeContractTests.cs`

**Work Package C — Workspace Contracts:**
- Create: `src/Ferret.Core/Workspace/HealthCheckDepth.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceOptions.cs`
- Create: `src/Ferret.Core/Workspace/WorkspacePath.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceMetadata.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceCapabilities.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceStatistics.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceContext.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceHealthReport.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceInitResult.cs`
- Create: `src/Ferret.Core/Workspace/WorkspaceUpgradeResult.cs`
- Create: `src/Ferret.Core/Workspace/Changeset.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceEngine.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceHealthChecker.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceLocator.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceValidator.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceUpgradeManager.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceChangeDetector.cs`
- Create: `src/Ferret.Core/Workspace/IWorkspaceStateStore.cs`
- Create: `tests/Ferret.Core.Tests/Workspace/WorkspacePathTests.cs`
- Create: `tests/Ferret.Core.Tests/Workspace/WorkspaceContractTests.cs`

**Work Package D — Exception Migration:**
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceNotFoundException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceAlreadyExistsException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceConfigurationException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceSchemaVersionException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceUpgradeRequiredException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspaceUpgradeFailedException.cs`
- Create: `src/Ferret.Core/Workspace/Errors/WorkspacePathTraversalException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceNotFoundException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceAlreadyExistsException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceConfigurationException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceSchemaVersionException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceUpgradeRequiredException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspaceUpgradeFailedException.cs`
- Delete: `src/Ferret.Core/Errors/WorkspacePathTraversalException.cs`
- Create: `tests/Ferret.Core.Tests/Workspace/WorkspaceExceptionNamespaceTests.cs`

**Work Package F — Decision Record:**
- Create: `docs/002-Architecture/decisions/ADR-004-runtime-engine-container.md`

---

## Task A: Execute Architecture Improvements Plan

The full content for Tasks A1-A8 is defined in `docs/superpowers/plans/2026-06-27-architecture-improvements.md`. Execute its Tasks 1 through 8 in order. Each task in that plan ends with its own `git commit`. The summary of each task:

| Arch-Plan Task | What it does |
|---|---|
| Task 1 | Rename DOC-001..004 → VISION-001, MISSION-001, PRINCIPLES-001, GLOSSARY-001 in all files |
| Task 2 | Add ARCH-001 §7.3 Engine Capability Matrix |
| Task 3 | Add ARCH-001 §8.6 Architecture Fitness Functions |
| Task 4 | Create `docs/002-Architecture/ARCH-013.md` — Event Architecture |
| Task 5 | Create `docs/002-Architecture/ARCH-011.md` — Configuration Architecture; refactor ARCH-001 §18 |
| Task 6 | Create `docs/002-Architecture/ARCH-014.md` — Platform Error Model |
| Task 7 | Add ARCH-001 §24 Domain Architecture |
| Task 8 | Update `docs/002-Architecture/README.md` index |

After those 8 tasks complete, execute the additional steps below.

### Task A-extra: Create ROADMAP-001

**Files:**
- Create: `docs/001-Product/ROADMAP-001.md`
- Modify: `docs/001-Product/README.md` (add row)

- [ ] **Step 1: Create ROADMAP-001.md**

```markdown
# ROADMAP-001 — Platform Roadmap

| Field | Value |
|---|---|
| **Document ID** | ROADMAP-001 |
| **Version** | 1.0 |
| **Status** | Living Document |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

---

## Overview

This document is the authoritative roadmap for the Ferret platform. It records completed sprints, the current sprint, and the planned direction for upcoming work. Milestone and sprint scopes evolve; this document reflects the latest approved plan.

---

## Completed Milestones

### M1 — Project Foundation (Sprints 1–2)

**Delivered:**
- Sprint 1: Repository initialisation — git, licence, solution skeleton, CI pipeline bootstrap
- Sprint 2: Repository bootstrap — 17 compilable projects (8 source + 9 test), StyleCop enforcement, ARCH-001 dependency rules as MSBuild targets, sample plugin

**Status:** Done

---

### M2 — Core Kernel (Sprint 3)

**Delivered:**
- Sprint 3: `Ferret.Core` platform kernel — enumerations, typed IDs (10 value objects), result types (7), base interfaces (9), domain/integration/system events, `WorkspaceException` hierarchy root

**Status:** Done

---

## Current Milestone

### M3 — Platform Foundation (Sprint 4)

**Sprint 4 goal:** Architecture documentation baseline + public contracts for Runtime and Workspace modules.

**Scope:**
- Architecture document baseline: ARCH-011, ARCH-013, ARCH-014; ARCH-001 sections §7.3, §8.6, §24; document ID normalisation
- Runtime Foundation Contracts: `IRuntimeHost`, `IRuntimeBuilder`, `IModule`, `IModuleDescriptor`, `IModuleRegistry`, `IModuleContext`, `IExecutionContext`, `ILifecycleParticipant`, `IRuntimeService`, enums, `ModuleMetadata`, 5 runtime domain events
- Workspace Public Contracts: `WorkspacePath`, `WorkspaceContext`, `WorkspaceMetadata`, `WorkspaceCapabilities`, `WorkspaceStatistics`, `WorkspaceHealthReport`, `WorkspaceInitResult`, `WorkspaceUpgradeResult`, `Changeset`, `WorkspaceOptions`, 7 workspace interfaces
- Exception namespace migration: workspace exceptions moved to `Ferret.Core.Workspace.Errors`
- 100+ passing tests

**Status:** In Progress

---

## Planned Milestones

### M4 — Runtime Implementation (Sprint 5, planned)

- Implement `IRuntimeHost` and `IRuntimeBuilder` as `Ferret.Runtime` concrete types
- Implement `IWorkspaceEngine` — full workspace lifecycle (init, load, health, upgrade, validate)
- Implement `IWorkspaceLocator` — `.ai/` directory discovery
- Composition root wiring
- 150+ tests

### M5 — Index Pipeline (Sprint 6, planned)

- `IIndexEngine` contracts and implementation
- File system scanner, change detection
- Parser plugin dispatch
- Knowledge graph write path (stub)

### M6 — Knowledge & Context (Sprint 7, planned)

- `IKnowledgeEngine` contracts and implementation
- Graph query model
- Context assembly — token-bounded, profile-driven

### M7 — CLI & MCP Entry Points (Sprint 8, planned)

- `Ferret init`, `Ferret workspace`, `Ferret index` commands
- MCP server stub
- End-to-end smoke test

---

## Traceability

| Input Document | Role |
|---|---|
| VISION-001 | Long-term vision this roadmap advances |
| MISSION-001 | Success criteria that bound the scope of each milestone |
| PRD-001 | Product requirements driving milestone ordering |
```

- [ ] **Step 2: Update docs/001-Product/README.md**

Read `docs/001-Product/README.md` and add a row for ROADMAP-001 to the index table.

- [ ] **Step 3: Commit**

```powershell
git add docs/001-Product/ROADMAP-001.md docs/001-Product/README.md
git commit -m "docs: add ROADMAP-001 — Platform Roadmap with completed and planned milestones"
```

---

## Task B: Runtime Foundation Contracts

**Files:**
- Create 13 files in `src/Ferret.Core/Runtime/`
- Create 5 files in `src/Ferret.Core/Runtime/Events/`
- Create 2 test files in `tests/Ferret.Core.Tests/Runtime/`

### Task B1: Runtime Enums and ModuleMetadata

- [ ] **Step 1: Write failing tests for enums**

Create `tests/Ferret.Core.Tests/Runtime/RuntimeEnumTests.cs`:

```csharp
using Ferret.Core.Runtime;
using Xunit;

namespace Ferret.Core.Tests.Runtime;

public sealed class RuntimeEnumTests
{
    [Fact]
    public void RuntimeState_HasExpectedValues()
    {
        Assert.Equal(0, (int)RuntimeState.Stopped);
        Assert.Equal(1, (int)RuntimeState.Starting);
        Assert.Equal(2, (int)RuntimeState.Running);
        Assert.Equal(3, (int)RuntimeState.Stopping);
        Assert.Equal(4, (int)RuntimeState.Faulted);
    }

    [Fact]
    public void ModuleState_HasExpectedValues()
    {
        Assert.Equal(0, (int)ModuleState.Unloaded);
        Assert.Equal(1, (int)ModuleState.Loading);
        Assert.Equal(2, (int)ModuleState.Active);
        Assert.Equal(3, (int)ModuleState.Deactivating);
        Assert.Equal(4, (int)ModuleState.Stopped);
        Assert.Equal(5, (int)ModuleState.Faulted);
    }

    [Fact]
    public void ModuleCapability_IsFlags()
    {
        Assert.Equal(0, (int)ModuleCapability.None);
        Assert.Equal(1, (int)ModuleCapability.Indexing);
        Assert.Equal(2, (int)ModuleCapability.Knowledge);
        Assert.Equal(4, (int)ModuleCapability.Review);
        Assert.Equal(8, (int)ModuleCapability.Specification);
        Assert.Equal(16, (int)ModuleCapability.Memory);
        Assert.Equal(32, (int)ModuleCapability.Workspace);
        Assert.Equal(64, (int)ModuleCapability.Artifact);
    }

    [Fact]
    public void ModuleCapability_CanCombineFlags()
    {
        var combined = ModuleCapability.Indexing | ModuleCapability.Knowledge;
        Assert.True(combined.HasFlag(ModuleCapability.Indexing));
        Assert.True(combined.HasFlag(ModuleCapability.Knowledge));
        Assert.False(combined.HasFlag(ModuleCapability.Review));
    }

    [Fact]
    public void ModuleCapability_NoneIsZero()
    {
        Assert.Equal(ModuleCapability.None, (ModuleCapability)0);
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure (types not yet defined)**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~RuntimeEnumTests"
```

Expected: compile errors — `Ferret.Core.Runtime` namespace not found.

- [ ] **Step 3: Create RuntimeState.cs**

Create `src/Ferret.Core/Runtime/RuntimeState.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Represents the lifecycle state of the Ferret runtime host.</summary>
public enum RuntimeState
{
    /// <summary>The runtime is stopped and no modules are active.</summary>
    Stopped = 0,

    /// <summary>The runtime is in the process of starting up.</summary>
    Starting = 1,

    /// <summary>The runtime is fully started and all modules are active.</summary>
    Running = 2,

    /// <summary>The runtime is in the process of stopping.</summary>
    Stopping = 3,

    /// <summary>The runtime has encountered an unrecoverable error.</summary>
    Faulted = 4,
}
```

- [ ] **Step 4: Create ModuleState.cs**

Create `src/Ferret.Core/Runtime/ModuleState.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Represents the lifecycle state of a platform module.</summary>
public enum ModuleState
{
    /// <summary>The module has not been loaded.</summary>
    Unloaded = 0,

    /// <summary>The module is currently loading.</summary>
    Loading = 1,

    /// <summary>The module is loaded and active.</summary>
    Active = 2,

    /// <summary>The module is in the process of deactivating.</summary>
    Deactivating = 3,

    /// <summary>The module has been stopped cleanly.</summary>
    Stopped = 4,

    /// <summary>The module has encountered an unrecoverable error.</summary>
    Faulted = 5,
}
```

- [ ] **Step 5: Create ModuleCapability.cs**

Create `src/Ferret.Core/Runtime/ModuleCapability.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Defines the capabilities that a module can declare.</summary>
[Flags]
public enum ModuleCapability
{
    /// <summary>No capabilities declared.</summary>
    None = 0,

    /// <summary>The module provides file-indexing capability.</summary>
    Indexing = 1 << 0,

    /// <summary>The module provides knowledge-graph query capability.</summary>
    Knowledge = 1 << 1,

    /// <summary>The module provides AI-assisted review capability.</summary>
    Review = 1 << 2,

    /// <summary>The module provides specification management capability.</summary>
    Specification = 1 << 3,

    /// <summary>The module provides session memory capability.</summary>
    Memory = 1 << 4,

    /// <summary>The module provides workspace lifecycle capability.</summary>
    Workspace = 1 << 5,

    /// <summary>The module provides artefact provenance capability.</summary>
    Artifact = 1 << 6,
}
```

- [ ] **Step 6: Write failing test for ModuleMetadata**

Add to `tests/Ferret.Core.Tests/Runtime/RuntimeEnumTests.cs` a new test class in the same file:

```csharp
// Add after the closing brace of RuntimeEnumTests:

public sealed class ModuleMetadataTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsMetadata()
    {
        var version = SemanticVersion.Create(1, 0, 0);
        var caps = new[] { ModuleCapability.Workspace };

        var metadata = ModuleMetadata.Create("workspace", "Workspace Module", version, caps, "Manages workspace lifecycle.", "Ferret Core Team");

        Assert.Equal("workspace", metadata.Id);
        Assert.Equal("Workspace Module", metadata.Name);
        Assert.Equal(version, metadata.Version);
        Assert.Contains(ModuleCapability.Workspace, metadata.Capabilities);
        Assert.Equal("Manages workspace lifecycle.", metadata.Description);
        Assert.Equal("Ferret Core Team", metadata.Author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankId_ThrowsArgumentException(string id)
    {
        var version = SemanticVersion.Create(1, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            ModuleMetadata.Create(id, "Name", version, Array.Empty<ModuleCapability>(), string.Empty, string.Empty));
    }
}
```

Note: `using Ferret.Core.Primitives;` needs to be added at the top for `SemanticVersion`.

Replace the full file with:

```csharp
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Xunit;

namespace Ferret.Core.Tests.Runtime;

public sealed class RuntimeEnumTests
{
    [Fact]
    public void RuntimeState_HasExpectedValues()
    {
        Assert.Equal(0, (int)RuntimeState.Stopped);
        Assert.Equal(1, (int)RuntimeState.Starting);
        Assert.Equal(2, (int)RuntimeState.Running);
        Assert.Equal(3, (int)RuntimeState.Stopping);
        Assert.Equal(4, (int)RuntimeState.Faulted);
    }

    [Fact]
    public void ModuleState_HasExpectedValues()
    {
        Assert.Equal(0, (int)ModuleState.Unloaded);
        Assert.Equal(1, (int)ModuleState.Loading);
        Assert.Equal(2, (int)ModuleState.Active);
        Assert.Equal(3, (int)ModuleState.Deactivating);
        Assert.Equal(4, (int)ModuleState.Stopped);
        Assert.Equal(5, (int)ModuleState.Faulted);
    }

    [Fact]
    public void ModuleCapability_IsFlags()
    {
        Assert.Equal(0, (int)ModuleCapability.None);
        Assert.Equal(1, (int)ModuleCapability.Indexing);
        Assert.Equal(2, (int)ModuleCapability.Knowledge);
        Assert.Equal(4, (int)ModuleCapability.Review);
        Assert.Equal(8, (int)ModuleCapability.Specification);
        Assert.Equal(16, (int)ModuleCapability.Memory);
        Assert.Equal(32, (int)ModuleCapability.Workspace);
        Assert.Equal(64, (int)ModuleCapability.Artifact);
    }

    [Fact]
    public void ModuleCapability_CanCombineFlags()
    {
        var combined = ModuleCapability.Indexing | ModuleCapability.Knowledge;
        Assert.True(combined.HasFlag(ModuleCapability.Indexing));
        Assert.True(combined.HasFlag(ModuleCapability.Knowledge));
        Assert.False(combined.HasFlag(ModuleCapability.Review));
    }

    [Fact]
    public void ModuleCapability_NoneIsZero()
    {
        Assert.Equal(ModuleCapability.None, (ModuleCapability)0);
    }
}

public sealed class ModuleMetadataTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsMetadata()
    {
        var version = SemanticVersion.Create(1, 0, 0);
        var caps = new[] { ModuleCapability.Workspace };

        var metadata = ModuleMetadata.Create("workspace", "Workspace Module", version, caps, "Manages workspace lifecycle.", "Ferret Core Team");

        Assert.Equal("workspace", metadata.Id);
        Assert.Equal("Workspace Module", metadata.Name);
        Assert.Equal(version, metadata.Version);
        Assert.Contains(ModuleCapability.Workspace, metadata.Capabilities);
        Assert.Equal("Manages workspace lifecycle.", metadata.Description);
        Assert.Equal("Ferret Core Team", metadata.Author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankId_ThrowsArgumentException(string id)
    {
        var version = SemanticVersion.Create(1, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            ModuleMetadata.Create(id, "Name", version, Array.Empty<ModuleCapability>(), string.Empty, string.Empty));
    }
}
```

- [ ] **Step 7: Create ModuleMetadata.cs**

Create `src/Ferret.Core/Runtime/ModuleMetadata.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime;

/// <summary>Describes a platform module's identity and capabilities.</summary>
public sealed class ModuleMetadata : IEquatable<ModuleMetadata>
{
    private ModuleMetadata(
        string id,
        string name,
        SemanticVersion version,
        IReadOnlyCollection<ModuleCapability> capabilities,
        string description,
        string author)
    {
        Id = id;
        Name = name;
        Version = version;
        Capabilities = capabilities;
        Description = description;
        Author = author;
    }

    /// <summary>Gets the unique module identifier (e.g. "workspace").</summary>
    public string Id { get; }

    /// <summary>Gets the human-readable module name.</summary>
    public string Name { get; }

    /// <summary>Gets the module version.</summary>
    public SemanticVersion Version { get; }

    /// <summary>Gets the capabilities this module declares.</summary>
    public IReadOnlyCollection<ModuleCapability> Capabilities { get; }

    /// <summary>Gets a short description of the module's purpose.</summary>
    public string Description { get; }

    /// <summary>Gets the module author or team name.</summary>
    public string Author { get; }

    /// <summary>Creates a new <see cref="ModuleMetadata"/> instance.</summary>
    /// <param name="id">The module identifier. Must not be blank.</param>
    /// <param name="name">The human-readable name.</param>
    /// <param name="version">The module version.</param>
    /// <param name="capabilities">The capabilities this module declares.</param>
    /// <param name="description">A short description of the module.</param>
    /// <param name="author">The author or team name.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is blank.</exception>
    public static ModuleMetadata Create(
        string id,
        string name,
        SemanticVersion version,
        IEnumerable<ModuleCapability> capabilities,
        string description,
        string author)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Module ID must not be blank.", nameof(id));
        }

        return new ModuleMetadata(
            id,
            name ?? string.Empty,
            version,
            capabilities?.ToList().AsReadOnly() ?? new List<ModuleCapability>().AsReadOnly(),
            description ?? string.Empty,
            author ?? string.Empty);
    }

    /// <inheritdoc />
    public bool Equals(ModuleMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id && Version.Equals(other.Version);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ModuleMetadata);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Version);

    /// <summary>Returns the module identifier and version as a string.</summary>
    public override string ToString() => $"{Id} v{Version}";
}
```

- [ ] **Step 8: Run tests — verify pass**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~RuntimeEnumTests|FullyQualifiedName~ModuleMetadataTests"
```

Expected: 7 tests pass.

- [ ] **Step 9: Commit**

```powershell
git add src/Ferret.Core/Runtime/RuntimeState.cs src/Ferret.Core/Runtime/ModuleState.cs src/Ferret.Core/Runtime/ModuleCapability.cs src/Ferret.Core/Runtime/ModuleMetadata.cs tests/Ferret.Core.Tests/Runtime/RuntimeEnumTests.cs
git commit -m "feat(sprint-4): add runtime enums (RuntimeState, ModuleState, ModuleCapability) and ModuleMetadata value object"
```

---

### Task B2: Runtime Interfaces

- [ ] **Step 1: Write failing contract smoke test**

Create `tests/Ferret.Core.Tests/Runtime/RuntimeContractTests.cs`:

```csharp
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Xunit;

namespace Ferret.Core.Tests.Runtime;

public sealed class RuntimeContractTests
{
    [Fact]
    public void IRuntimeHost_ExistsAsInterface()
    {
        Assert.True(typeof(IRuntimeHost).IsInterface);
    }

    [Fact]
    public void IRuntimeBuilder_ExistsAsInterface()
    {
        Assert.True(typeof(IRuntimeBuilder).IsInterface);
    }

    [Fact]
    public void IModule_ExtendsILifecycleParticipant()
    {
        Assert.True(typeof(ILifecycleParticipant).IsAssignableFrom(typeof(IModule)));
    }

    [Fact]
    public void IModuleDescriptor_ExistsAsInterface()
    {
        Assert.True(typeof(IModuleDescriptor).IsInterface);
    }

    [Fact]
    public void IModuleRegistry_ExistsAsInterface()
    {
        Assert.True(typeof(IModuleRegistry).IsInterface);
    }

    [Fact]
    public void IModuleContext_ExistsAsInterface()
    {
        Assert.True(typeof(IModuleContext).IsInterface);
    }

    [Fact]
    public void IExecutionContext_ExistsAsInterface()
    {
        Assert.True(typeof(IExecutionContext).IsInterface);
    }

    [Fact]
    public void IRuntimeService_ExistsAsInterface()
    {
        Assert.True(typeof(IRuntimeService).IsInterface);
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: compile errors — interface types not yet defined.

- [ ] **Step 3: Create ILifecycleParticipant.cs**

Create `src/Ferret.Core/Runtime/ILifecycleParticipant.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Implemented by types that participate in the module lifecycle.</summary>
public interface ILifecycleParticipant
{
    /// <summary>Called before the module starts up. Use for pre-start validation or resource acquisition.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>Called after the module has fully started.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>Called before the module shuts down. Use for graceful termination of in-flight work.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>Called after the module has fully stopped.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Create IExecutionContext.cs**

Create `src/Ferret.Core/Runtime/IExecutionContext.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime;

/// <summary>Carries the correlation and execution identifiers for a single platform operation.</summary>
public interface IExecutionContext
{
    /// <summary>Gets the correlation identifier propagated from the triggering CLI invocation or MCP call.</summary>
    CorrelationId CorrelationId { get; }

    /// <summary>Gets the unique identifier for this execution instance.</summary>
    ExecutionId ExecutionId { get; }

    /// <summary>Gets a cancellation token that signals the operation should be cancelled.</summary>
    CancellationToken CancellationToken { get; }
}
```

- [ ] **Step 5: Create IModuleDescriptor.cs**

Create `src/Ferret.Core/Runtime/IModuleDescriptor.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime;

/// <summary>Describes a module for registration with the runtime builder before the module is activated.</summary>
public interface IModuleDescriptor
{
    /// <summary>Gets the unique module identifier.</summary>
    string Id { get; }

    /// <summary>Gets the human-readable module name.</summary>
    string Name { get; }

    /// <summary>Gets the module version.</summary>
    SemanticVersion Version { get; }

    /// <summary>Gets the capabilities this module declares.</summary>
    IReadOnlyCollection<ModuleCapability> Capabilities { get; }
}
```

- [ ] **Step 6: Create IModuleRegistry.cs**

Create `src/Ferret.Core/Runtime/IModuleRegistry.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Provides read access to the set of modules registered with the runtime host.</summary>
public interface IModuleRegistry
{
    /// <summary>Gets all active modules.</summary>
    IReadOnlyCollection<IModule> Modules { get; }

    /// <summary>Attempts to retrieve a module by its identifier.</summary>
    /// <param name="moduleId">The identifier of the module to retrieve.</param>
    /// <param name="module">When this method returns, contains the module if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the module was found; otherwise <see langword="false"/>.</returns>
    bool TryGet(string moduleId, out IModule? module);

    /// <summary>Retrieves a module by its identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="moduleId">The identifier of the module to retrieve.</param>
    IModule? GetById(string moduleId);
}
```

- [ ] **Step 7: Create IModuleContext.cs**

Create `src/Ferret.Core/Runtime/IModuleContext.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Provides a module with access to its execution context and the module registry.</summary>
public interface IModuleContext
{
    /// <summary>Gets the identifier of the module this context belongs to.</summary>
    string ModuleId { get; }

    /// <summary>Gets the execution context for the current operation.</summary>
    IExecutionContext ExecutionContext { get; }

    /// <summary>Gets the module registry, allowing this module to discover peer modules.</summary>
    IModuleRegistry Registry { get; }
}
```

- [ ] **Step 8: Create IModule.cs**

Create `src/Ferret.Core/Runtime/IModule.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Represents a platform module managed by the runtime host.</summary>
public interface IModule : ILifecycleParticipant
{
    /// <summary>Gets the metadata describing this module.</summary>
    ModuleMetadata Metadata { get; }

    /// <summary>Gets the current lifecycle state of this module.</summary>
    ModuleState State { get; }
}
```

- [ ] **Step 9: Create IRuntimeService.cs**

Create `src/Ferret.Core/Runtime/IRuntimeService.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Marker interface for services provided by the runtime and resolvable by modules.</summary>
public interface IRuntimeService
{
    /// <summary>Gets the unique identifier for this runtime service.</summary>
    string ServiceId { get; }
}
```

- [ ] **Step 10: Create IRuntimeHost.cs**

Create `src/Ferret.Core/Runtime/IRuntimeHost.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Manages the platform module lifecycle from startup through shutdown.</summary>
public interface IRuntimeHost
{
    /// <summary>Gets the current state of the runtime.</summary>
    RuntimeState State { get; }

    /// <summary>Gets the module registry for the active runtime.</summary>
    IModuleRegistry Modules { get; }

    /// <summary>Starts the runtime and activates all registered modules.</summary>
    /// <param name="cancellationToken">A token to cancel the startup sequence.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the runtime and deactivates all active modules.</summary>
    /// <param name="cancellationToken">A token to cancel the shutdown sequence.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 11: Create IRuntimeBuilder.cs**

Create `src/Ferret.Core/Runtime/IRuntimeBuilder.cs`:

```csharp
namespace Ferret.Core.Runtime;

/// <summary>Configures and builds a runtime host from registered module descriptors.</summary>
public interface IRuntimeBuilder
{
    /// <summary>Registers a module descriptor with the builder.</summary>
    /// <param name="descriptor">The module descriptor to register.</param>
    /// <returns>The same builder instance, to allow call chaining.</returns>
    IRuntimeBuilder AddModule(IModuleDescriptor descriptor);

    /// <summary>Constructs the configured runtime host.</summary>
    IRuntimeHost Build();
}
```

- [ ] **Step 12: Build and run tests**

```powershell
dotnet build src/Ferret.sln --configuration Release
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: build succeeds, 8 contract tests pass.

- [ ] **Step 13: Commit**

```powershell
git add src/Ferret.Core/Runtime/
git commit -m "feat(sprint-4): add runtime foundation contracts — IRuntimeHost, IRuntimeBuilder, IModule, IModuleDescriptor, IModuleRegistry, IModuleContext, IExecutionContext, ILifecycleParticipant, IRuntimeService"
```

---

### Task B3: Runtime Domain Events

- [ ] **Step 1: Write failing event tests**

Create `tests/Ferret.Core.Tests/Runtime/RuntimeEventTests.cs`:

```csharp
using Ferret.Core.Events;
using Ferret.Core.Runtime.Events;
using Xunit;

namespace Ferret.Core.Tests.Runtime;

public sealed class RuntimeEventTests
{
    [Fact]
    public void RuntimeStarted_InheritsDomainEvent()
    {
        Assert.True(typeof(DomainEvent).IsAssignableFrom(typeof(RuntimeStarted)));
    }

    [Fact]
    public void RuntimeStarted_CarriesRuntimeVersion()
    {
        var evt = new RuntimeStarted("1.0.0");
        Assert.Equal("1.0.0", evt.RuntimeVersion);
    }

    [Fact]
    public void RuntimeStopped_CarriesRuntimeVersionAndModuleCount()
    {
        var evt = new RuntimeStopped("1.0.0", modulesActive: 3);
        Assert.Equal("1.0.0", evt.RuntimeVersion);
        Assert.Equal(3, evt.ModulesActive);
    }

    [Fact]
    public void ModuleLoaded_CarriesModuleInfo()
    {
        var evt = new ModuleLoaded("workspace", "Workspace Module", "1.0.0");
        Assert.Equal("workspace", evt.ModuleId);
        Assert.Equal("Workspace Module", evt.ModuleName);
        Assert.Equal("1.0.0", evt.Version);
    }

    [Fact]
    public void ModuleActivated_CarriesModuleInfo()
    {
        var evt = new ModuleActivated("workspace", "Workspace Module");
        Assert.Equal("workspace", evt.ModuleId);
        Assert.Equal("Workspace Module", evt.ModuleName);
    }

    [Fact]
    public void ModuleStopped_CarriesModuleInfo()
    {
        var evt = new ModuleStopped("workspace", "Workspace Module");
        Assert.Equal("workspace", evt.ModuleId);
        Assert.Equal("Workspace Module", evt.ModuleName);
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~RuntimeEventTests"
```

Expected: compile errors — event types not defined.

- [ ] **Step 3: Create RuntimeStarted.cs**

Create `src/Ferret.Core/Runtime/Events/RuntimeStarted.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when the runtime host has fully started and all modules are active.</summary>
public sealed class RuntimeStarted : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="RuntimeStarted"/> class.</summary>
    /// <param name="runtimeVersion">The version of the runtime that started.</param>
    public RuntimeStarted(string runtimeVersion)
    {
        RuntimeVersion = runtimeVersion ?? string.Empty;
    }

    /// <summary>Gets the version of the runtime host that started.</summary>
    public string RuntimeVersion { get; }
}
```

- [ ] **Step 4: Create RuntimeStopped.cs**

Create `src/Ferret.Core/Runtime/Events/RuntimeStopped.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when the runtime host has fully stopped.</summary>
public sealed class RuntimeStopped : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="RuntimeStopped"/> class.</summary>
    /// <param name="runtimeVersion">The version of the runtime that stopped.</param>
    /// <param name="modulesActive">The number of modules that were active at the time of shutdown.</param>
    public RuntimeStopped(string runtimeVersion, int modulesActive)
    {
        RuntimeVersion = runtimeVersion ?? string.Empty;
        ModulesActive = modulesActive;
    }

    /// <summary>Gets the version of the runtime host that stopped.</summary>
    public string RuntimeVersion { get; }

    /// <summary>Gets the number of modules that were active at the time of shutdown.</summary>
    public int ModulesActive { get; }
}
```

- [ ] **Step 5: Create ModuleLoaded.cs**

Create `src/Ferret.Core/Runtime/Events/ModuleLoaded.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when a module has been loaded into the runtime registry.</summary>
public sealed class ModuleLoaded : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="ModuleLoaded"/> class.</summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="moduleName">The human-readable module name.</param>
    /// <param name="version">The module version string.</param>
    public ModuleLoaded(string moduleId, string moduleName, string version)
    {
        ModuleId = moduleId ?? string.Empty;
        ModuleName = moduleName ?? string.Empty;
        Version = version ?? string.Empty;
    }

    /// <summary>Gets the unique identifier of the loaded module.</summary>
    public string ModuleId { get; }

    /// <summary>Gets the human-readable name of the loaded module.</summary>
    public string ModuleName { get; }

    /// <summary>Gets the version of the loaded module.</summary>
    public string Version { get; }
}
```

- [ ] **Step 6: Create ModuleActivated.cs**

Create `src/Ferret.Core/Runtime/Events/ModuleActivated.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when a module has completed its startup sequence and is active.</summary>
public sealed class ModuleActivated : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="ModuleActivated"/> class.</summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="moduleName">The human-readable module name.</param>
    public ModuleActivated(string moduleId, string moduleName)
    {
        ModuleId = moduleId ?? string.Empty;
        ModuleName = moduleName ?? string.Empty;
    }

    /// <summary>Gets the unique identifier of the activated module.</summary>
    public string ModuleId { get; }

    /// <summary>Gets the human-readable name of the activated module.</summary>
    public string ModuleName { get; }
}
```

- [ ] **Step 7: Create ModuleStopped.cs**

Create `src/Ferret.Core/Runtime/Events/ModuleStopped.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when a module has completed its shutdown sequence.</summary>
public sealed class ModuleStopped : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="ModuleStopped"/> class.</summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="moduleName">The human-readable module name.</param>
    public ModuleStopped(string moduleId, string moduleName)
    {
        ModuleId = moduleId ?? string.Empty;
        ModuleName = moduleName ?? string.Empty;
    }

    /// <summary>Gets the unique identifier of the stopped module.</summary>
    public string ModuleId { get; }

    /// <summary>Gets the human-readable name of the stopped module.</summary>
    public string ModuleName { get; }
}
```

- [ ] **Step 8: Run all runtime tests**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Runtime"
```

Expected: 21 tests pass (7 enum/metadata + 8 contract + 6 event).

- [ ] **Step 9: Commit**

```powershell
git add src/Ferret.Core/Runtime/Events/ tests/Ferret.Core.Tests/Runtime/RuntimeEventTests.cs
git commit -m "feat(sprint-4): add runtime domain events — RuntimeStarted, RuntimeStopped, ModuleLoaded, ModuleActivated, ModuleStopped"
```

---

## Task C: Workspace Contracts

### Task C1: WorkspacePath Value Object

- [ ] **Step 1: Write failing WorkspacePath tests**

Create `tests/Ferret.Core.Tests/Workspace/WorkspacePathTests.cs`:

```csharp
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspacePathTests
{
    [Fact]
    public void Create_WithValidPath_ReturnsInstance()
    {
        var path = WorkspacePath.Create(@"C:\repos\myproject");
        Assert.Equal(@"C:\repos\myproject", path.FullPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankPath_ThrowsArgumentException(string? path)
    {
        Assert.Throws<ArgumentException>(() => WorkspacePath.Create(path!));
    }

    [Fact]
    public void Combine_WithRelativePath_ReturnsCombinedPath()
    {
        var root = WorkspacePath.Create(@"C:\repos\myproject");
        var combined = root.Combine(".ai");
        Assert.Equal(@"C:\repos\myproject\.ai", combined.FullPath);
    }

    [Fact]
    public void IsUnder_WhenChildIsUnderParent_ReturnsTrue()
    {
        var parent = WorkspacePath.Create(@"C:\repos\myproject");
        var child = WorkspacePath.Create(@"C:\repos\myproject\src\file.cs");
        Assert.True(child.IsUnder(parent));
    }

    [Fact]
    public void IsUnder_WhenPathIsSameAsParent_ReturnsFalse()
    {
        var path = WorkspacePath.Create(@"C:\repos\myproject");
        Assert.False(path.IsUnder(path));
    }

    [Fact]
    public void IsUnder_WhenPathIsNotUnderParent_ReturnsFalse()
    {
        var parent = WorkspacePath.Create(@"C:\repos\myproject");
        var other = WorkspacePath.Create(@"C:\repos\otherproject");
        Assert.False(other.IsUnder(parent));
    }

    [Fact]
    public void Equality_SameFullPath_AreEqual()
    {
        var a = WorkspacePath.Create(@"C:\repos\project");
        var b = WorkspacePath.Create(@"C:\repos\project");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentFullPath_AreNotEqual()
    {
        var a = WorkspacePath.Create(@"C:\repos\projectA");
        var b = WorkspacePath.Create(@"C:\repos\projectB");
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void ToString_ReturnsFullPath()
    {
        var path = WorkspacePath.Create(@"C:\repos\project");
        Assert.Equal(@"C:\repos\project", path.ToString());
    }

    [Fact]
    public void GetHashCode_EqualPaths_HaveSameHashCode()
    {
        var a = WorkspacePath.Create(@"C:\repos\project");
        var b = WorkspacePath.Create(@"C:\repos\project");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~WorkspacePathTests"
```

Expected: compile errors — `Ferret.Core.Workspace` not found.

- [ ] **Step 3: Create WorkspacePath.cs**

Create `src/Ferret.Core/Workspace/WorkspacePath.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Represents an absolute file system path within or referring to a workspace root.</summary>
public sealed class WorkspacePath : IEquatable<WorkspacePath>
{
    private WorkspacePath(string fullPath)
    {
        FullPath = fullPath;
    }

    /// <summary>Gets the absolute path string.</summary>
    public string FullPath { get; }

    /// <summary>Creates a new <see cref="WorkspacePath"/> from an absolute path string.</summary>
    /// <param name="path">The absolute path. Must not be blank.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    public static WorkspacePath Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Workspace path must not be blank.", nameof(path));
        }

        return new WorkspacePath(path);
    }

    /// <summary>Combines this path with a relative segment and returns a new <see cref="WorkspacePath"/>.</summary>
    /// <param name="relative">The relative path segment to append.</param>
    public WorkspacePath Combine(string relative)
    {
        return new WorkspacePath(Path.Combine(FullPath, relative));
    }

    /// <summary>Returns <see langword="true"/> if this path is located under <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent path to test against.</param>
    public bool IsUnder(WorkspacePath parent)
    {
        if (parent is null)
        {
            return false;
        }

        var parentNormalised = parent.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return FullPath.StartsWith(parentNormalised, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool Equals(WorkspacePath? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as WorkspacePath);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FullPath);

    /// <summary>Returns the full path string.</summary>
    public override string ToString() => FullPath;

    /// <summary>Returns <see langword="true"/> if both paths are equal.</summary>
    public static bool operator ==(WorkspacePath? left, WorkspacePath? right)
        => left?.Equals(right) ?? right is null;

    /// <summary>Returns <see langword="true"/> if the paths are not equal.</summary>
    public static bool operator !=(WorkspacePath? left, WorkspacePath? right)
        => !(left == right);
}
```

- [ ] **Step 4: Run WorkspacePath tests**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~WorkspacePathTests"
```

Expected: 10 tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/Ferret.Core/Workspace/WorkspacePath.cs tests/Ferret.Core.Tests/Workspace/WorkspacePathTests.cs
git commit -m "feat(sprint-4): add WorkspacePath value object"
```

---

### Task C2: Workspace Enum, Options, and Data Objects

- [ ] **Step 1: Write failing workspace contract tests**

Create `tests/Ferret.Core.Tests/Workspace/WorkspaceContractTests.cs`:

```csharp
using Ferret.Core.Abstractions;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspaceContractTests
{
    [Fact]
    public void HealthCheckDepth_HasExpectedValues()
    {
        Assert.Equal(0, (int)HealthCheckDepth.Quick);
        Assert.Equal(1, (int)HealthCheckDepth.Deep);
    }

    [Fact]
    public void WorkspaceOptions_DefaultIsReadOnly_False()
    {
        var options = new WorkspaceOptions();
        Assert.False(options.ReadOnly);
    }

    [Fact]
    public void WorkspaceMetadata_Create_StoresValues()
    {
        var meta = WorkspaceMetadata.Create("My Project", "A test project", "1.0", DateTimeOffset.UtcNow);
        Assert.Equal("My Project", meta.Name);
        Assert.Equal("A test project", meta.Description);
        Assert.Equal("1.0", meta.SchemaVersion);
    }

    [Fact]
    public void WorkspaceCapabilities_Create_StoresValues()
    {
        var caps = WorkspaceCapabilities.Create(readOnly: false, pluginCount: 2, indexedFileCount: 150);
        Assert.False(caps.ReadOnly);
        Assert.Equal(2, caps.PluginCount);
        Assert.Equal(150, caps.IndexedFileCount);
    }

    [Fact]
    public void WorkspaceStatistics_Create_StoresValues()
    {
        var stats = WorkspaceStatistics.Create(totalFiles: 500, indexedFiles: 450, lastIndexed: DateTimeOffset.UtcNow, schemaVersion: "1.0");
        Assert.Equal(500, stats.TotalFiles);
        Assert.Equal(450, stats.IndexedFiles);
        Assert.Equal("1.0", stats.SchemaVersion);
    }

    [Fact]
    public void WorkspaceContext_Create_StoresPath()
    {
        var path = WorkspacePath.Create(@"C:\repos\project");
        var id = WorkspaceId.Create("ws-001");
        var meta = WorkspaceMetadata.Create("Project", string.Empty, "1.0", DateTimeOffset.UtcNow);
        var caps = WorkspaceCapabilities.Create(false, 0, 0);

        var ctx = WorkspaceContext.Create(path, id, meta, caps);

        Assert.Equal(path, ctx.RootPath);
        Assert.Equal(id, ctx.Id);
    }

    [Fact]
    public void Changeset_Create_StoreCounts()
    {
        var added = new[] { "file1.cs" };
        var modified = new[] { "file2.cs" };
        var deleted = new[] { "file3.cs" };

        var changeset = Changeset.Create(added, modified, deleted, DateTimeOffset.UtcNow);

        Assert.Single(changeset.Added);
        Assert.Single(changeset.Modified);
        Assert.Single(changeset.Deleted);
    }

    [Fact]
    public void WorkspaceInitResult_Succeeded_HasContext()
    {
        var path = WorkspacePath.Create(@"C:\repos\project");
        var id = WorkspaceId.Create("ws-001");
        var meta = WorkspaceMetadata.Create("Project", string.Empty, "1.0", DateTimeOffset.UtcNow);
        var caps = WorkspaceCapabilities.Create(false, 0, 0);
        var ctx = WorkspaceContext.Create(path, id, meta, caps);

        var result = WorkspaceInitResult.Success(ctx);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Context);
    }

    [Fact]
    public void WorkspaceInitResult_Failed_HasErrorMessage()
    {
        var result = WorkspaceInitResult.Failure("Workspace already exists.");
        Assert.False(result.Succeeded);
        Assert.Equal("Workspace already exists.", result.ErrorMessage);
        Assert.Null(result.Context);
    }

    [Fact]
    public void WorkspaceUpgradeResult_Succeeded_HasVersions()
    {
        var result = WorkspaceUpgradeResult.Success("1.0", "2.0", new[] { "step-001" });
        Assert.True(result.Succeeded);
        Assert.Equal("1.0", result.FromVersion);
        Assert.Equal("2.0", result.ToVersion);
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~WorkspaceContractTests"
```

- [ ] **Step 3: Create HealthCheckDepth.cs**

Create `src/Ferret.Core/Workspace/HealthCheckDepth.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Controls how thorough a workspace health check is.</summary>
public enum HealthCheckDepth
{
    /// <summary>A fast structural check — verifies that required files are present and readable.</summary>
    Quick = 0,

    /// <summary>A full semantic check — validates file contents, schema consistency, and index integrity.</summary>
    Deep = 1,
}
```

- [ ] **Step 4: Create WorkspaceOptions.cs**

Create `src/Ferret.Core/Workspace/WorkspaceOptions.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Options that influence workspace engine operations.</summary>
public sealed class WorkspaceOptions
{
    /// <summary>Gets or sets a value indicating whether the workspace is opened in read-only mode.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>Gets or sets the list of plugin identifiers to activate for this workspace. An empty list activates all configured plugins.</summary>
    public IReadOnlyList<string> PluginIds { get; set; } = Array.Empty<string>();
}
```

- [ ] **Step 5: Create WorkspaceMetadata.cs**

Create `src/Ferret.Core/Workspace/WorkspaceMetadata.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Descriptive metadata about a workspace.</summary>
public sealed class WorkspaceMetadata
{
    private WorkspaceMetadata(string name, string description, string schemaVersion, DateTimeOffset createdAt, DateTimeOffset? lastIndexedAt)
    {
        Name = name;
        Description = description;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        LastIndexedAt = lastIndexedAt;
    }

    /// <summary>Gets the human-readable workspace name.</summary>
    public string Name { get; }

    /// <summary>Gets the workspace description.</summary>
    public string Description { get; }

    /// <summary>Gets the workspace configuration schema version (e.g. "1.0").</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the UTC timestamp when the workspace was first initialised.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the UTC timestamp of the last successful index build, or <see langword="null"/> if never indexed.</summary>
    public DateTimeOffset? LastIndexedAt { get; }

    /// <summary>Creates a new <see cref="WorkspaceMetadata"/> instance.</summary>
    /// <param name="name">The workspace name.</param>
    /// <param name="description">The workspace description.</param>
    /// <param name="schemaVersion">The schema version string.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="lastIndexedAt">The last index timestamp, or <see langword="null"/>.</param>
    public static WorkspaceMetadata Create(string name, string description, string schemaVersion, DateTimeOffset createdAt, DateTimeOffset? lastIndexedAt = null)
    {
        return new WorkspaceMetadata(
            name ?? string.Empty,
            description ?? string.Empty,
            schemaVersion ?? string.Empty,
            createdAt,
            lastIndexedAt);
    }
}
```

- [ ] **Step 6: Create WorkspaceCapabilities.cs**

Create `src/Ferret.Core/Workspace/WorkspaceCapabilities.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Describes the runtime capabilities of an open workspace.</summary>
public sealed class WorkspaceCapabilities
{
    private WorkspaceCapabilities(bool readOnly, int pluginCount, int indexedFileCount)
    {
        ReadOnly = readOnly;
        PluginCount = pluginCount;
        IndexedFileCount = indexedFileCount;
    }

    /// <summary>Gets a value indicating whether this workspace was opened in read-only mode.</summary>
    public bool ReadOnly { get; }

    /// <summary>Gets the number of active plugins in this workspace.</summary>
    public int PluginCount { get; }

    /// <summary>Gets the number of files in the current index.</summary>
    public int IndexedFileCount { get; }

    /// <summary>Creates a new <see cref="WorkspaceCapabilities"/> instance.</summary>
    /// <param name="readOnly">Whether the workspace is read-only.</param>
    /// <param name="pluginCount">Number of active plugins.</param>
    /// <param name="indexedFileCount">Number of indexed files.</param>
    public static WorkspaceCapabilities Create(bool readOnly, int pluginCount, int indexedFileCount)
    {
        return new WorkspaceCapabilities(readOnly, pluginCount, indexedFileCount);
    }
}
```

- [ ] **Step 7: Create WorkspaceStatistics.cs**

Create `src/Ferret.Core/Workspace/WorkspaceStatistics.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Quantitative statistics about a workspace's index and file state.</summary>
public sealed class WorkspaceStatistics
{
    private WorkspaceStatistics(int totalFiles, int indexedFiles, DateTimeOffset lastIndexed, string schemaVersion)
    {
        TotalFiles = totalFiles;
        IndexedFiles = indexedFiles;
        LastIndexed = lastIndexed;
        SchemaVersion = schemaVersion;
    }

    /// <summary>Gets the total number of files in the workspace.</summary>
    public int TotalFiles { get; }

    /// <summary>Gets the number of files currently in the index.</summary>
    public int IndexedFiles { get; }

    /// <summary>Gets the UTC timestamp of the last successful index operation.</summary>
    public DateTimeOffset LastIndexed { get; }

    /// <summary>Gets the workspace schema version at the time these statistics were recorded.</summary>
    public string SchemaVersion { get; }

    /// <summary>Creates a new <see cref="WorkspaceStatistics"/> instance.</summary>
    /// <param name="totalFiles">Total file count.</param>
    /// <param name="indexedFiles">Indexed file count.</param>
    /// <param name="lastIndexed">Last index timestamp.</param>
    /// <param name="schemaVersion">Schema version string.</param>
    public static WorkspaceStatistics Create(int totalFiles, int indexedFiles, DateTimeOffset lastIndexed, string schemaVersion)
    {
        return new WorkspaceStatistics(totalFiles, indexedFiles, lastIndexed, schemaVersion ?? string.Empty);
    }
}
```

- [ ] **Step 8: Create WorkspaceContext.cs**

Create `src/Ferret.Core/Workspace/WorkspaceContext.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Workspace;

/// <summary>Represents an open workspace — the root path, identity, metadata, and runtime capabilities.</summary>
public sealed class WorkspaceContext
{
    private WorkspaceContext(WorkspacePath rootPath, WorkspaceId id, WorkspaceMetadata metadata, WorkspaceCapabilities capabilities)
    {
        RootPath = rootPath;
        Id = id;
        Metadata = metadata;
        Capabilities = capabilities;
    }

    /// <summary>Gets the absolute path to the workspace root directory.</summary>
    public WorkspacePath RootPath { get; }

    /// <summary>Gets the unique workspace identifier.</summary>
    public WorkspaceId Id { get; }

    /// <summary>Gets the workspace metadata.</summary>
    public WorkspaceMetadata Metadata { get; }

    /// <summary>Gets the runtime capabilities of this workspace.</summary>
    public WorkspaceCapabilities Capabilities { get; }

    /// <summary>Creates a new <see cref="WorkspaceContext"/>.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="id">The workspace identifier.</param>
    /// <param name="metadata">The workspace metadata.</param>
    /// <param name="capabilities">The workspace runtime capabilities.</param>
    public static WorkspaceContext Create(WorkspacePath rootPath, WorkspaceId id, WorkspaceMetadata metadata, WorkspaceCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(capabilities);

        return new WorkspaceContext(rootPath, id, metadata, capabilities);
    }
}
```

- [ ] **Step 9: Create WorkspaceHealthReport.cs**

Create `src/Ferret.Core/Workspace/WorkspaceHealthReport.cs`:

```csharp
using Ferret.Core.Abstractions;
using Ferret.Core.Enumerations;

namespace Ferret.Core.Workspace;

/// <summary>The result of a workspace health check, containing an overall status and per-checker results.</summary>
public sealed class WorkspaceHealthReport
{
    private WorkspaceHealthReport(WorkspaceContext context, HealthCheckDepth depth, HealthStatus overall, IReadOnlyList<HealthCheckResult> checks, DateTimeOffset checkedAt)
    {
        Context = context;
        Depth = depth;
        Overall = overall;
        Checks = checks;
        CheckedAt = checkedAt;
    }

    /// <summary>Gets the workspace context that was checked.</summary>
    public WorkspaceContext Context { get; }

    /// <summary>Gets the depth at which the health check was performed.</summary>
    public HealthCheckDepth Depth { get; }

    /// <summary>Gets the overall health status — the worst status across all individual checks.</summary>
    public HealthStatus Overall { get; }

    /// <summary>Gets the individual health check results.</summary>
    public IReadOnlyList<HealthCheckResult> Checks { get; }

    /// <summary>Gets the UTC timestamp when the health check was performed.</summary>
    public DateTimeOffset CheckedAt { get; }

    /// <summary>Creates a new <see cref="WorkspaceHealthReport"/>.</summary>
    /// <param name="context">The workspace context that was checked.</param>
    /// <param name="depth">The depth of the check.</param>
    /// <param name="overall">The overall health status.</param>
    /// <param name="checks">The individual check results.</param>
    /// <param name="checkedAt">The time the check was performed.</param>
    public static WorkspaceHealthReport Create(WorkspaceContext context, HealthCheckDepth depth, HealthStatus overall, IEnumerable<HealthCheckResult> checks, DateTimeOffset checkedAt)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(checks);

        return new WorkspaceHealthReport(context, depth, overall, checks.ToList().AsReadOnly(), checkedAt);
    }
}
```

- [ ] **Step 10: Create Changeset.cs**

Create `src/Ferret.Core/Workspace/Changeset.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Represents the set of file changes detected since the last index operation.</summary>
public sealed class Changeset
{
    private Changeset(IReadOnlyList<string> added, IReadOnlyList<string> modified, IReadOnlyList<string> deleted, DateTimeOffset detectedAt)
    {
        Added = added;
        Modified = modified;
        Deleted = deleted;
        DetectedAt = detectedAt;
    }

    /// <summary>Gets the paths of files added since the last index.</summary>
    public IReadOnlyList<string> Added { get; }

    /// <summary>Gets the paths of files modified since the last index.</summary>
    public IReadOnlyList<string> Modified { get; }

    /// <summary>Gets the paths of files deleted since the last index.</summary>
    public IReadOnlyList<string> Deleted { get; }

    /// <summary>Gets the UTC timestamp when this changeset was detected.</summary>
    public DateTimeOffset DetectedAt { get; }

    /// <summary>Gets a value indicating whether there are any changes in this changeset.</summary>
    public bool HasChanges => Added.Count > 0 || Modified.Count > 0 || Deleted.Count > 0;

    /// <summary>Creates a new <see cref="Changeset"/>.</summary>
    /// <param name="added">Added file paths.</param>
    /// <param name="modified">Modified file paths.</param>
    /// <param name="deleted">Deleted file paths.</param>
    /// <param name="detectedAt">When the changeset was detected.</param>
    public static Changeset Create(IEnumerable<string> added, IEnumerable<string> modified, IEnumerable<string> deleted, DateTimeOffset detectedAt)
    {
        return new Changeset(
            (added ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
            (modified ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
            (deleted ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
            detectedAt);
    }

    /// <summary>Creates an empty changeset with no changes.</summary>
    /// <param name="detectedAt">When the (empty) changeset was detected.</param>
    public static Changeset Empty(DateTimeOffset detectedAt)
        => Create(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), detectedAt);
}
```

- [ ] **Step 11: Create WorkspaceInitResult.cs**

Create `src/Ferret.Core/Workspace/WorkspaceInitResult.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>The result of a workspace initialisation operation.</summary>
public sealed class WorkspaceInitResult
{
    private WorkspaceInitResult(bool succeeded, WorkspaceContext? context, string? errorMessage)
    {
        Succeeded = succeeded;
        Context = context;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the initialisation succeeded.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets the workspace context created by the initialisation, or <see langword="null"/> if it failed.</summary>
    public WorkspaceContext? Context { get; }

    /// <summary>Gets the error message if the initialisation failed, or <see langword="null"/> if it succeeded.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful initialisation result.</summary>
    /// <param name="context">The workspace context that was created.</param>
    public static WorkspaceInitResult Success(WorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WorkspaceInitResult(true, context, null);
    }

    /// <summary>Creates a failed initialisation result.</summary>
    /// <param name="errorMessage">A message describing the failure.</param>
    public static WorkspaceInitResult Failure(string errorMessage)
    {
        return new WorkspaceInitResult(false, null, errorMessage ?? "Initialisation failed.");
    }
}
```

- [ ] **Step 12: Create WorkspaceUpgradeResult.cs**

Create `src/Ferret.Core/Workspace/WorkspaceUpgradeResult.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>The result of a workspace schema upgrade operation.</summary>
public sealed class WorkspaceUpgradeResult
{
    private WorkspaceUpgradeResult(bool succeeded, string? fromVersion, string? toVersion, IReadOnlyList<string> stepsApplied, string? errorMessage)
    {
        Succeeded = succeeded;
        FromVersion = fromVersion;
        ToVersion = toVersion;
        StepsApplied = stepsApplied;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the upgrade succeeded.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets the schema version before the upgrade, or <see langword="null"/> if the upgrade was not attempted.</summary>
    public string? FromVersion { get; }

    /// <summary>Gets the schema version after the upgrade, or <see langword="null"/> if the upgrade failed.</summary>
    public string? ToVersion { get; }

    /// <summary>Gets the ordered list of migration step identifiers that were applied.</summary>
    public IReadOnlyList<string> StepsApplied { get; }

    /// <summary>Gets the error message if the upgrade failed, or <see langword="null"/> if it succeeded.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful upgrade result.</summary>
    /// <param name="fromVersion">The version before the upgrade.</param>
    /// <param name="toVersion">The version after the upgrade.</param>
    /// <param name="stepsApplied">The migration steps that were applied.</param>
    public static WorkspaceUpgradeResult Success(string fromVersion, string toVersion, IEnumerable<string> stepsApplied)
    {
        return new WorkspaceUpgradeResult(true, fromVersion, toVersion, (stepsApplied ?? Enumerable.Empty<string>()).ToList().AsReadOnly(), null);
    }

    /// <summary>Creates a failed upgrade result.</summary>
    /// <param name="errorMessage">A message describing the failure.</param>
    /// <param name="fromVersion">The version that was being upgraded from, if known.</param>
    public static WorkspaceUpgradeResult Failure(string errorMessage, string? fromVersion = null)
    {
        return new WorkspaceUpgradeResult(false, fromVersion, null, Array.Empty<string>(), errorMessage ?? "Upgrade failed.");
    }
}
```

- [ ] **Step 13: Run workspace data object tests**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~WorkspaceContractTests"
```

Expected: 10 tests pass.

- [ ] **Step 14: Commit**

```powershell
git add src/Ferret.Core/Workspace/ tests/Ferret.Core.Tests/Workspace/WorkspaceContractTests.cs
git commit -m "feat(sprint-4): add workspace data contracts — HealthCheckDepth, WorkspaceOptions, WorkspacePath, WorkspaceMetadata, WorkspaceCapabilities, WorkspaceStatistics, WorkspaceContext, WorkspaceHealthReport, WorkspaceInitResult, WorkspaceUpgradeResult, Changeset"
```

---

### Task C3: Workspace Interfaces

- [ ] **Step 1: Create IWorkspaceEngine.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceEngine.cs`:

```csharp
using Ferret.Core.Results;

namespace Ferret.Core.Workspace;

/// <summary>Provides the primary workspace lifecycle operations: initialise, load, health-check, upgrade, validate, and change-detect.</summary>
public interface IWorkspaceEngine
{
    /// <summary>Initialises a new workspace at the given root path.</summary>
    /// <param name="rootPath">The directory to initialise as a workspace.</param>
    /// <param name="options">Optional options for the initialisation.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkspaceInitResult> InitialiseAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default);

    /// <summary>Loads an existing workspace from the given root path.</summary>
    /// <param name="rootPath">The workspace root directory.</param>
    /// <param name="options">Optional options for the load.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkspaceContext> LoadAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default);

    /// <summary>Runs a health check against the given workspace context.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="depth">The depth of the health check.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkspaceHealthReport> GetHealthAsync(WorkspaceContext context, HealthCheckDepth depth = HealthCheckDepth.Quick, CancellationToken ct = default);

    /// <summary>Detects files changed since the last index operation.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Changeset> GetChangesetAsync(WorkspaceContext context, CancellationToken ct = default);

    /// <summary>Upgrades the workspace schema to the current platform version.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext context, CancellationToken ct = default);

    /// <summary>Validates the workspace configuration and structure.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<ValidationResult> ValidateAsync(WorkspaceContext context, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create IWorkspaceHealthChecker.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceHealthChecker.cs`:

```csharp
using Ferret.Core.Abstractions;

namespace Ferret.Core.Workspace;

/// <summary>Performs a single named health check against an open workspace.</summary>
public interface IWorkspaceHealthChecker
{
    /// <summary>Gets the unique name of this health checker.</summary>
    string Name { get; }

    /// <summary>Gets the minimum check depth at which this checker runs.</summary>
    HealthCheckDepth Depth { get; }

    /// <summary>Runs the health check against the given workspace context.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<HealthCheckResult> CheckAsync(WorkspaceContext context, CancellationToken ct = default);
}
```

- [ ] **Step 3: Create IWorkspaceLocator.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceLocator.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Locates a workspace root by searching from a given starting path.</summary>
public interface IWorkspaceLocator
{
    /// <summary>Searches for a workspace root starting at <paramref name="searchPath"/> and walking up the directory tree.</summary>
    /// <param name="searchPath">The path from which to begin the search.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The workspace root path if found; otherwise <see langword="null"/>.</returns>
    Task<WorkspacePath?> LocateAsync(WorkspacePath searchPath, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if <paramref name="rootPath"/> is an initialised workspace root.</summary>
    /// <param name="rootPath">The path to test.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> ExistsAsync(WorkspacePath rootPath, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create IWorkspaceValidator.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceValidator.cs`:

```csharp
using Ferret.Core.Results;

namespace Ferret.Core.Workspace;

/// <summary>Validates the configuration and structural integrity of an open workspace.</summary>
public interface IWorkspaceValidator
{
    /// <summary>Validates the workspace and returns a result containing any validation failures.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<ValidationResult> ValidateAsync(WorkspaceContext context, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create IWorkspaceUpgradeManager.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceUpgradeManager.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Manages workspace schema upgrades between platform versions.</summary>
public interface IWorkspaceUpgradeManager
{
    /// <summary>Returns <see langword="true"/> if the workspace at <paramref name="context"/> requires a schema upgrade.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> IsUpgradeRequiredAsync(WorkspaceContext context, CancellationToken ct = default);

    /// <summary>Applies any pending schema migration steps and returns the upgrade result.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext context, CancellationToken ct = default);
}
```

- [ ] **Step 6: Create IWorkspaceChangeDetector.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceChangeDetector.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Detects files changed since the last successful index operation.</summary>
public interface IWorkspaceChangeDetector
{
    /// <summary>Computes the set of changes since the last index for the given workspace context.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Changeset> DetectChangesAsync(WorkspaceContext context, CancellationToken ct = default);
}
```

- [ ] **Step 7: Create IWorkspaceStateStore.cs**

Create `src/Ferret.Core/Workspace/IWorkspaceStateStore.cs`:

```csharp
namespace Ferret.Core.Workspace;

/// <summary>Persists and retrieves workspace state statistics between platform invocations.</summary>
public interface IWorkspaceStateStore
{
    /// <summary>Reads the stored statistics for the workspace at <paramref name="rootPath"/>.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkspaceStatistics> ReadStatisticsAsync(WorkspacePath rootPath, CancellationToken ct = default);

    /// <summary>Persists updated statistics for the workspace at <paramref name="rootPath"/>.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="statistics">The statistics to write.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task WriteStatisticsAsync(WorkspacePath rootPath, WorkspaceStatistics statistics, CancellationToken ct = default);
}
```

- [ ] **Step 8: Build and run all workspace tests**

```powershell
dotnet build src/Ferret.sln --configuration Release
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~Ferret.Core.Tests.Workspace"
```

Expected: build succeeds, 20 workspace tests pass (10 path + 10 contract).

- [ ] **Step 9: Commit**

```powershell
git add src/Ferret.Core/Workspace/
git commit -m "feat(sprint-4): add workspace interfaces — IWorkspaceEngine, IWorkspaceHealthChecker, IWorkspaceLocator, IWorkspaceValidator, IWorkspaceUpgradeManager, IWorkspaceChangeDetector, IWorkspaceStateStore"
```

---

## Task D: Migrate Workspace Exceptions

The workspace exception types currently live in `Ferret.Core.Errors` with "temporary residency" documentation. This task moves them to their permanent home in `Ferret.Core.Workspace.Errors`.

**Important:** Create all new files first, build to verify, then delete the old files.

### Task D1: Create exceptions in new namespace

- [ ] **Step 1: Write failing namespace test**

Create `tests/Ferret.Core.Tests/Workspace/WorkspaceExceptionNamespaceTests.cs`:

```csharp
using Ferret.Core.Workspace.Errors;
using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspaceExceptionNamespaceTests
{
    [Fact]
    public void WorkspaceException_IsInWorkspaceErrorsNamespace()
    {
        Assert.Equal("Ferret.Core.Workspace.Errors", typeof(WorkspaceException).Namespace);
    }

    [Fact]
    public void WorkspaceNotFoundException_IsInWorkspaceErrorsNamespace()
    {
        Assert.Equal("Ferret.Core.Workspace.Errors", typeof(WorkspaceNotFoundException).Namespace);
    }

    [Fact]
    public void WorkspaceAlreadyExistsException_DerivesFromWorkspaceException()
    {
        Assert.True(typeof(WorkspaceException).IsAssignableFrom(typeof(WorkspaceAlreadyExistsException)));
    }

    [Fact]
    public void WorkspacePathTraversalException_IsInWorkspaceErrorsNamespace()
    {
        Assert.Equal("Ferret.Core.Workspace.Errors", typeof(WorkspacePathTraversalException).Namespace);
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~WorkspaceExceptionNamespaceTests"
```

Expected: compile errors — `Ferret.Core.Workspace.Errors` not found.

- [ ] **Step 3: Create WorkspaceException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceException.cs`:

```csharp
using Ferret.Core.Errors;

namespace Ferret.Core.Workspace.Errors;

/// <summary>Base class for all workspace-related platform exceptions.</summary>
public abstract class WorkspaceException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceException"/> class.</summary>
    protected WorkspaceException()
        : base("A workspace error occurred.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceException"/> class with a message.</summary>
    /// <param name="message">A message describing the workspace error.</param>
    protected WorkspaceException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the workspace error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected WorkspaceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 4: Create WorkspaceNotFoundException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceNotFoundException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when a workspace cannot be found by its identifier or path.</summary>
public sealed class WorkspaceNotFoundException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotFoundException"/> class.</summary>
    public WorkspaceNotFoundException()
        : base("The workspace was not found.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotFoundException"/> class for a specific workspace identifier.</summary>
    /// <param name="workspaceId">The identifier of the workspace that could not be found.</param>
    public WorkspaceNotFoundException(string workspaceId)
        : base($"Workspace '{workspaceId}' was not found.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotFoundException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Gets the identifier of the workspace that could not be found.</summary>
    public string WorkspaceId { get; }
}
```

- [ ] **Step 5: Create WorkspaceAlreadyExistsException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceAlreadyExistsException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when an attempt is made to create a workspace that already exists.</summary>
public sealed class WorkspaceAlreadyExistsException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceAlreadyExistsException"/> class.</summary>
    public WorkspaceAlreadyExistsException()
        : base("The workspace already exists.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAlreadyExistsException"/> class for a specific workspace identifier.</summary>
    /// <param name="workspaceId">The identifier of the workspace that already exists.</param>
    public WorkspaceAlreadyExistsException(string workspaceId)
        : base($"Workspace '{workspaceId}' already exists.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAlreadyExistsException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Gets the identifier of the workspace that already exists.</summary>
    public string WorkspaceId { get; }
}
```

- [ ] **Step 6: Create WorkspaceConfigurationException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceConfigurationException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when workspace configuration is invalid or cannot be loaded.</summary>
public sealed class WorkspaceConfigurationException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationException"/> class.</summary>
    public WorkspaceConfigurationException()
        : base("Workspace configuration is invalid or cannot be loaded.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationException"/> class with a message.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    public WorkspaceConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 7: Create WorkspaceSchemaVersionException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceSchemaVersionException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when the workspace schema version is incompatible with the current platform version.</summary>
public sealed class WorkspaceSchemaVersionException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class.</summary>
    public WorkspaceSchemaVersionException()
        : base("The workspace schema version is incompatible.")
    {
        WorkspaceId = string.Empty;
        SchemaVersion = string.Empty;
        RequiredVersion = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class with a message.</summary>
    /// <param name="message">A message describing the schema version incompatibility.</param>
    public WorkspaceSchemaVersionException(string message)
        : base(message)
    {
        WorkspaceId = string.Empty;
        SchemaVersion = string.Empty;
        RequiredVersion = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the schema version incompatibility.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceSchemaVersionException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
        SchemaVersion = string.Empty;
        RequiredVersion = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class with workspace and version details.</summary>
    /// <param name="workspaceId">The identifier of the workspace with the incompatible schema.</param>
    /// <param name="schemaVersion">The schema version found in the workspace.</param>
    /// <param name="requiredVersion">The schema version required by the platform.</param>
    public WorkspaceSchemaVersionException(string workspaceId, string schemaVersion, string requiredVersion)
        : base($"Workspace '{workspaceId}' has schema version '{schemaVersion}' but version '{requiredVersion}' is required.")
    {
        WorkspaceId = workspaceId;
        SchemaVersion = schemaVersion;
        RequiredVersion = requiredVersion;
    }

    /// <summary>Gets the identifier of the workspace.</summary>
    public string WorkspaceId { get; }

    /// <summary>Gets the schema version found in the workspace.</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the schema version required by the platform.</summary>
    public string RequiredVersion { get; }
}
```

- [ ] **Step 8: Create WorkspaceUpgradeRequiredException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceUpgradeRequiredException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when a workspace must be upgraded before it can be used.</summary>
public sealed class WorkspaceUpgradeRequiredException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeRequiredException"/> class.</summary>
    public WorkspaceUpgradeRequiredException()
        : base("The workspace must be upgraded before use.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeRequiredException"/> class for a specific workspace.</summary>
    /// <param name="workspaceId">The identifier of the workspace that requires upgrading.</param>
    public WorkspaceUpgradeRequiredException(string workspaceId)
        : base($"Workspace '{workspaceId}' must be upgraded before use.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeRequiredException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceUpgradeRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Gets the identifier of the workspace that requires upgrading.</summary>
    public string WorkspaceId { get; }
}
```

- [ ] **Step 9: Create WorkspaceUpgradeFailedException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspaceUpgradeFailedException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when a workspace upgrade attempt fails.</summary>
public sealed class WorkspaceUpgradeFailedException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeFailedException"/> class.</summary>
    public WorkspaceUpgradeFailedException()
        : base("Workspace upgrade failed.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeFailedException"/> class with a message.</summary>
    /// <param name="message">A message describing the upgrade failure.</param>
    public WorkspaceUpgradeFailedException(string message)
        : base(message)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeFailedException"/> class with workspace and inner exception.</summary>
    /// <param name="workspaceId">The identifier of the workspace whose upgrade failed.</param>
    /// <param name="innerException">The exception that caused the upgrade to fail.</param>
    public WorkspaceUpgradeFailedException(string workspaceId, Exception innerException)
        : base($"Upgrade failed for workspace '{workspaceId}'.", innerException)
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Gets the identifier of the workspace whose upgrade failed.</summary>
    public string WorkspaceId { get; }
}
```

- [ ] **Step 10: Create WorkspacePathTraversalException.cs (new namespace)**

Create `src/Ferret.Core/Workspace/Errors/WorkspacePathTraversalException.cs`:

```csharp
namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when an operation attempts to access a path outside the workspace root.</summary>
public sealed class WorkspacePathTraversalException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspacePathTraversalException"/> class.</summary>
    public WorkspacePathTraversalException()
        : base("A path traversal attempt was detected.")
    {
        AttemptedPath = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspacePathTraversalException"/> class for a specific attempted path.</summary>
    /// <param name="attemptedPath">The path string that was attempted.</param>
    public WorkspacePathTraversalException(string attemptedPath)
        : base($"Path traversal attempt detected: '{attemptedPath}' is outside the workspace root.")
    {
        AttemptedPath = attemptedPath;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspacePathTraversalException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the path traversal attempt.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspacePathTraversalException(string message, Exception innerException)
        : base(message, innerException)
    {
        AttemptedPath = string.Empty;
    }

    /// <summary>Gets the path string that was attempted.</summary>
    public string AttemptedPath { get; }
}
```

- [ ] **Step 11: Build with both old and new files present (dual-namespace compile check)**

```powershell
dotnet build src/Ferret.sln --configuration Release
```

Expected: build succeeds. At this point both old (`Ferret.Core.Errors.WorkspaceException`) and new (`Ferret.Core.Workspace.Errors.WorkspaceException`) exist — this is temporary and expected.

- [ ] **Step 12: Run namespace tests**

```powershell
dotnet test tests/Ferret.Core.Tests/ --filter "FullyQualifiedName~WorkspaceExceptionNamespaceTests"
```

Expected: 4 tests pass.

### Task D2: Remove old exception files

- [ ] **Step 1: Delete old workspace exception files from `Ferret.Core.Errors`**

```powershell
Remove-Item src/Ferret.Core/Errors/WorkspaceException.cs
Remove-Item src/Ferret.Core/Errors/WorkspaceNotFoundException.cs
Remove-Item src/Ferret.Core/Errors/WorkspaceAlreadyExistsException.cs
Remove-Item src/Ferret.Core/Errors/WorkspaceConfigurationException.cs
Remove-Item src/Ferret.Core/Errors/WorkspaceSchemaVersionException.cs
Remove-Item src/Ferret.Core/Errors/WorkspaceUpgradeRequiredException.cs
Remove-Item src/Ferret.Core/Errors/WorkspaceUpgradeFailedException.cs
Remove-Item src/Ferret.Core/Errors/WorkspacePathTraversalException.cs
```

- [ ] **Step 2: Update existing workspace exception tests to use new namespace**

Read `tests/Ferret.Core.Tests/Errors/WorkspaceExceptionTests.cs`. Replace the `using Ferret.Core.Errors;` line with:

```csharp
using Ferret.Core.Workspace.Errors;
```

- [ ] **Step 3: Build and run all tests**

```powershell
dotnet build src/Ferret.sln --configuration Release
dotnet test tests/Ferret.Core.Tests/
```

Expected: build succeeds, all tests pass (no regressions).

- [ ] **Step 4: Commit**

```powershell
git add src/Ferret.Core/Workspace/Errors/ tests/Ferret.Core.Tests/Workspace/WorkspaceExceptionNamespaceTests.cs tests/Ferret.Core.Tests/Errors/WorkspaceExceptionTests.cs
git rm src/Ferret.Core/Errors/WorkspaceException.cs src/Ferret.Core/Errors/WorkspaceNotFoundException.cs src/Ferret.Core/Errors/WorkspaceAlreadyExistsException.cs src/Ferret.Core/Errors/WorkspaceConfigurationException.cs src/Ferret.Core/Errors/WorkspaceSchemaVersionException.cs src/Ferret.Core/Errors/WorkspaceUpgradeRequiredException.cs src/Ferret.Core/Errors/WorkspaceUpgradeFailedException.cs src/Ferret.Core/Errors/WorkspacePathTraversalException.cs
git commit -m "refactor(sprint-4): migrate workspace exceptions from Ferret.Core.Errors to Ferret.Core.Workspace.Errors — permanent namespace (WP-D)"
```

---

## Task E: Verify Test Count ≥ 100

- [ ] **Step 1: Run full test suite and count**

```powershell
dotnet test tests/Ferret.Core.Tests/ --verbosity normal
```

Expected: ≥ 100 tests pass. The breakdown:
- Sprint 3 baseline: 71 tests
- Task B1 (RuntimeEnumTests): 5 tests
- Task B1 (ModuleMetadataTests): 2 tests  
- Task B2 (RuntimeContractTests): 8 tests
- Task B3 (RuntimeEventTests): 6 tests
- Task C1 (WorkspacePathTests): 10 tests
- Task C2 (WorkspaceContractTests): 10 tests
- Task D (WorkspaceExceptionNamespaceTests): 4 tests
- **Total: 116 tests**

If the count is below 100, check which test files are not being picked up and rerun with `--verbosity detailed`.

---

## Task F: Runtime Decision Record

**Files:**
- Create: `docs/002-Architecture/decisions/ADR-004-runtime-engine-container.md`

- [ ] **Step 1: Create ADR-004**

Create `docs/002-Architecture/decisions/ADR-004-runtime-engine-container.md`:

```markdown
# ADR-004 — Runtime Engine Container vs Composition Root

| Field | Value |
|---|---|
| **ADR ID** | ADR-004 |
| **Sprint** | Sprint 4 |
| **Status** | Accepted |
| **Decision** | Composition Root |
| **Date** | 2026-06-28 |
| **Author** | Ferret Core Team |

---

## Context

Sprint 4 defines the `IRuntimeHost` and `IRuntimeBuilder` contracts in `Ferret.Core.Runtime`. Before implementing these contracts in Sprint 5, the team must decide whether the runtime should act as a **DI container** (responsible for resolving engine dependencies) or as a **composition root** (responsible only for module lifecycle, with dependency resolution delegated to an external DI container).

Two concrete architectures were evaluated:

### Option A: Runtime as Engine Container

The `IRuntimeHost` implementation provides a service locator or DI container directly. Engines request dependencies from the host using `runtimeHost.GetService<T>()`. The runtime is responsible for knowing which concrete type to provide for each interface.

**Advantages:**
- Single object graph managed in one place
- Modules cannot depend on the external DI container framework
- Simpler startup: one registration API

**Disadvantages:**
- `IRuntimeHost` violates the Interface Segregation Principle — it becomes responsible for both lifecycle and dependency resolution
- The runtime must understand every module's dependencies, creating implicit coupling
- Difficult to test: replacing one engine type requires understanding the full dependency graph
- `Ferret.Core` remains zero-dependency, so the container cannot live there; it must live in `Ferret.Runtime`, creating a hard dependency on a DI framework in the platform's primary assembly

### Option B: Runtime as Composition Root (Chosen)

The `IRuntimeHost` implementation is responsible **only** for module lifecycle (starting and stopping modules in order). Dependency injection is performed by an external composition root (in `Ferret.Runtime`) that wires the DI container before passing built modules to the host.

`IRuntimeBuilder` is the composition root's entry point: it accepts module descriptors and returns a configured `IRuntimeHost`. The `IRuntimeBuilder` implementation (in Sprint 5) is responsible for registering module services in the DI container and constructing each module's concrete type.

**Advantages:**
- `IRuntimeHost` has a narrow, lifecycle-only contract — fully defined by its interface
- `Ferret.Core` stays zero-dependency: no DI framework reference needed
- Each module's dependencies are registered in its own descriptor, keeping coupling local
- Testable: modules can be tested with fakes injected at the composition root level, without the full host
- Aligns with the dependency inversion principle — upper layers (composition root) depend on lower interfaces (`IModule`), not the reverse

**Disadvantages:**
- Requires a DI container in `Ferret.Runtime` (acceptable — `Ferret.Runtime` is a platform assembly, not a kernel)
- Two concepts (`IRuntimeBuilder` and the DI container registration) must be kept consistent

---

## Decision

**Option B: Composition Root.**

The runtime host manages lifecycle only. The composition root (`IRuntimeBuilder` implementation) is responsible for wiring dependencies via a DI container in `Ferret.Runtime`. This decision aligns with the zero-dependency constraint on `Ferret.Core` and keeps the `IRuntimeHost` contract narrow and testable.

---

## Consequences

1. `IRuntimeBuilder.AddModule(IModuleDescriptor)` accepts a descriptor that knows how to register its own services (the descriptor carries a `RegisterServices(IServiceCollection)` method or equivalent, defined in Sprint 5).
2. The DI container implementation is selected in Sprint 5. Microsoft.Extensions.DependencyInjection is the default candidate (already used by the .NET ecosystem; lightweight; does not require NuGet packages in `Ferret.Core`).
3. `IRuntimeHost` and `IRuntimeBuilder` contracts (defined in Sprint 4) do not change as a result of this decision. The implementation details are deferred to Sprint 5.

---

## Traceability

| Input | Role |
|---|---|
| ARCH-001 §5 | Layered architecture — upper layers depend on lower interfaces |
| ARCH-001 §8 | Dependency rules — `Ferret.Core` has zero project references |
| Sprint 4 contracts | `IRuntimeHost`, `IRuntimeBuilder` interfaces that this decision contextualises |
```

- [ ] **Step 2: Commit**

```powershell
git add docs/002-Architecture/decisions/ADR-004-runtime-engine-container.md
git commit -m "docs(sprint-4): add ADR-004 — Runtime Engine Container vs Composition Root (Work Package F)"
```

---

## Task G: Sprint Tag and Final Verification

- [ ] **Step 1: Run full solution build and test suite**

```powershell
dotnet build src/Ferret.sln --configuration Release
dotnet test tests/Ferret.Core.Tests/ --verbosity normal
```

Expected:
- 0 build errors, 0 warnings
- ≥ 100 tests pass, 0 failed

- [ ] **Step 2: Verify no DOC-00x references remain**

```powershell
Select-String -Path "docs\**\*.md" -Pattern "DOC-00[1-4]" -Recurse
```

Expected: zero results.

- [ ] **Step 3: Verify no workspace exceptions in old namespace**

```powershell
Select-String -Path "src\Ferret.Core\Errors\*.cs" -Pattern "WorkspaceException"
```

Expected: zero results (all workspace exception files deleted).

- [ ] **Step 4: Tag the sprint**

```powershell
git tag v0.4.0-sprint4
```

---

## Self-Review

### Spec Coverage

| Requirement | Task |
|---|---|
| A1 — Rename DOC-001..004 | Task A (arch improvements plan Task 1) |
| A2 — Update ARCH-001 with Capability Matrix, Fitness Functions, Domain Architecture | Task A (arch improvements plan Tasks 2, 3, 7) |
| A3 — Create ARCH-011 Configuration Architecture | Task A (arch improvements plan Task 5) |
| A4 — Create ARCH-013 Event Architecture | Task A (arch improvements plan Task 4) |
| A5 — Create ARCH-014 Platform Error Model | Task A (arch improvements plan Task 6) |
| A6 — Create ROADMAP-001 | Task A-extra |
| A7 — Update README indexes | Task A (arch improvements plan Task 8) |
| B — Runtime Foundation Contracts | Tasks B1, B2, B3 |
| C — Workspace Public Contracts | Tasks C1, C2, C3 |
| D — Exception migration | Tasks D1, D2 |
| E — 100+ passing tests | Task E (116 projected) |
| F — Runtime Decision Record | Task F |

### Constraint Verification

- `Ferret.Core` zero project references: no `<ProjectReference>` added to `Ferret.Core.csproj`
- No business logic: all types are interfaces, enums, value objects, or thin result wrappers
- Runtime contracts do not reference Workspace types: `Ferret.Core.Runtime.*` has no `using Ferret.Core.Workspace`
- XML docs on every public member: all code blocks include `/// <summary>` on every public member
- `WorkspacePath` used for path parameters throughout workspace interfaces
- `CancellationToken` on all async interface methods
