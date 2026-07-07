> **Historical note:** This document was written when the product was named AISpace, which was renamed to Ferret during Sprint 5.

# Sprint 3 – Platform Kernel (Ferret.Core) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish Ferret.Core as the stable platform kernel — defining enumerations, typed IDs, value objects, result types, base interfaces, exception hierarchy, and domain event infrastructure that all future modules will depend on.

**Architecture:** All types live in `Ferret.Core`. The assembly has zero project references (enforced by MSBuild ARCH001 target — a build error fires if a `<ProjectReference>` is added). Public contracts created this sprint are long-term API surface: every public member must carry XML documentation. No business logic, no I/O, no infrastructure concerns.

**Tech Stack:** .NET 9 · C# 13 · xUnit 2.9.2 · StyleCop.Analyzers 1.2.0-beta.556 · Central Package Management

## Global Constraints

- TFM: `net9.0`
- C# language version: determined by SDK defaults for .NET 9 (C# 13)
- `TreatWarningsAsErrors=true` — every warning is a build failure
- StyleCop `documentExposedElements: true` — every `public` member needs `<summary>`, `<param>`, `<returns>`, `<exception>` where applicable
- `usingDirectivesPlacement: outsideNamespace` — all `using` directives go **before** the `namespace` declaration
- No `Version` attribute in `.csproj` — Central Package Management owns all versions
- No `<ProjectReference>` in `Ferret.Core.csproj` — ARCH001 MSBuild target fires a build error
- Value objects: `sealed class`, private constructor, `static Create(string)` factory, `IEquatable<T>`, XML docs on every public member
- Exception hierarchy root: `FerretException` (abstract) — all custom exceptions extend it
- Tests: `xUnit`, `global using Xunit;` already in `GlobalUsings.cs`, `public sealed` test classes
- Commit after every task; no batch commits

## Missing Inputs

The following documents are referenced in ARCH-001, ARCH-003, and SDK-001 but do not exist in this repository. They are recorded as backlog items; no architecture was invented to substitute for them:

| Document | Referenced As | Backlog Item |
|---|---|---|
| ROADMAP-001 | Sprint ordering, feature gates | Create ROADMAP-001 |
| ARCH-011 | Telemetry contract | Create ARCH-011 |
| ARCH-012 | Configuration contract | Create ARCH-012 |
| ARCH-013 | CLI contract | Create ARCH-013 |
| ARCH-014 | MCP contract | Create ARCH-014 |
| STD-005 §12–17 | Additional coding standards | Create STD-005 |
| Decision Register | Formal decision log | Create decision register |

---

## File Structure

### New files to create

```
src/Ferret.Core/
  Enumerations/
    HealthStatus.cs
    Severity.cs
    ValidationSeverity.cs
    PluginState.cs
    SpecificationStatus.cs
    ReviewStatus.cs
  Errors/
    FerretException.cs          ← abstract base
    ValidationException.cs
    ConfigurationException.cs
    PlatformException.cs
    SecurityException.cs
    PermissionDeniedException.cs
    WorkspaceNotFoundException.cs
    WorkspaceAlreadyExistsException.cs
    WorkspaceConfigurationException.cs
    WorkspaceSchemaVersionException.cs
    WorkspaceUpgradeRequiredException.cs
    WorkspaceUpgradeFailedException.cs
    WorkspacePathTraversalException.cs
  Primitives/
    WorkspaceId.cs
    DocumentId.cs
    SpecificationId.cs
    ReviewId.cs
    PluginId.cs
    ArtifactId.cs
    CorrelationId.cs
    ExecutionId.cs
    ContentHash.cs
    SemanticVersion.cs
  Results/
    OperationResult.cs
    ValidationResult.cs
    ValidationFailure.cs
    DiscoveryResult.cs
    ParseResult.cs
    ReviewResult.cs
    IndexResult.cs
  Abstractions/
    IIdentifiable.cs
    IVersioned.cs
    IValidatable.cs
    IInitializable.cs
    IConfiguration.cs
    IHealthCheck.cs
    IMetadata.cs
    IClock.cs
    ICorrelationContext.cs
    HealthCheckResult.cs
  Events/
    EventMetadata.cs
    DomainEvent.cs
    IntegrationEvent.cs
    SystemEvent.cs
    EventEnvelope.cs

tests/Ferret.Core.Tests/
  Enumerations/
    EnumerationTests.cs
  Errors/
    ExceptionHierarchyTests.cs
    WorkspaceExceptionTests.cs
  Primitives/
    TypedIdTests.cs
    ContentHashTests.cs
    SemanticVersionTests.cs
  Results/
    ResultTypeTests.cs
  Abstractions/
    HealthCheckResultTests.cs
  Events/
    EventBaseTests.cs
```

### Files to modify

- `src/Ferret.Core/CoreModule.cs` — remove `internal` marker comment; becomes the assembly anchor only
- `tests/Ferret.Core.Tests/CoreModuleTests.cs` — replace placeholder test with meaningful smoke test

---

## Task 0: Technology Evaluation (Mandatory Gate)

> **Do not proceed to Task 1 until every decision in this task is documented and committed.**
> The File Structure section above assumes a "Build all" outcome. If any component is resolved as **Wrap** or **Adopt**, update the File Structure and affected task code before starting Task 1.

**Files:**
- Create: `docs/002-Architecture/decisions/sprint-3-technology-evaluation.md`

**Interfaces:**
- Produces: A signed-off decision record. Every subsequent task depends on these decisions.

**Decision criteria:**

| Label | Meaning |
|---|---|
| **Build** | Implement from scratch inside Ferret.Core. Justified when the abstraction is small, the contract must be fully platform-owned, or a package would add a transitive dependency to every consumer of Core. |
| **Wrap** | A mature package is adopted internally; Ferret.Core exposes its own public contract backed by that package. The package is a private implementation detail. |
| **Adopt** | A package's public types are used directly as the platform contract (no wrapper). Only valid when the package is stable, widely used, and its removal would be a breaking change — i.e., you are intentionally tying the platform's public surface to it. |

**Adoption constraint for Ferret.Core:** Adding any NuGet package to `Ferret.Core.csproj` makes it a transitive dependency for every module, plugin, and test project in the solution. Prefer **Build** for abstractions under ~100 lines. Only choose **Wrap** or **Adopt** when the package delivers capabilities that are genuinely non-trivial to replicate correctly.

---

- [ ] **Step 1: Research and decide — Semantic Versioning**

Component: `SemanticVersion` (parse, compare, stringify SemVer 2.0.0 strings)

Ecosystem survey:

| Package | Author | Latest | Notes |
|---|---|---|---|
| `NuGet.Versioning` | Microsoft | 6.x | Full SemVer 2.0.0 + NuGet-specific extensions. Pulls in several Microsoft.Build transitive deps. |
| `Semver` | Max Hauser | 3.x | Pure SemVer 2.0.0 (no extras), zero deps, MIT. |
| BCL `System.Version` | Microsoft | built-in | Four-part (`Major.Minor.Build.Revision`). Not SemVer — rejects pre-release strings. Reject. |
| **Build custom** | — | — | ~70 lines. Covers `Major.Minor.Patch[-pre-release]` parse, compare, stringify. |

Decision questions to answer before filling the table:
1. Does any other module need to pass `SemanticVersion` instances across assembly boundaries where both sides must agree on the same CLR type?
2. Is pre-release ordering (SemVer §11) required beyond simple string comparison?

Fill in the decision table in `sprint-3-technology-evaluation.md`:

```markdown
## SemanticVersion

| | |
|---|---|
| **Decision** | Build / Wrap (`Semver`) / Adopt (`Semver`) |
| **Rationale** | |
| **Package to add** | none / `Semver` |
| **Tasks affected** | Task 5 |
```

If **Wrap** or **Adopt**: add `<PackageVersion Include="Semver" Version="3.*" />` to `Directory.Packages.props` and rewrite Task 5 to use the package type directly or behind a thin adapter.

---

- [ ] **Step 2: Research and decide — Content Hashing**

Component: `ContentHash` (a value object holding an algorithm name + hex digest string; does NOT perform hashing itself)

Ecosystem survey:

| Option | Notes |
|---|---|
| **Build custom** | ~50 lines. A pure value object — algorithm string + hex string + equality. No hashing logic; callers supply pre-computed digests. |
| `System.Security.Cryptography` | BCL, built-in — provides the hashing algorithms but not a typed hash-result value object. Would need wrapping anyway. |

Decision: `ContentHash` is a dumb value holder. No package does this for you. The correct answer is almost certainly **Build** — but confirm there is no project-wide hash-result abstraction already approved in a missing architecture doc before committing.

```markdown
## ContentHash

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | |
| **Package to add** | none |
| **Tasks affected** | Task 5 |
```

---

- [ ] **Step 3: Research and decide — Typed Identifiers**

Component: 8 typed ID value objects (`WorkspaceId`, `DocumentId`, `SpecificationId`, `ReviewId`, `PluginId`, `ArtifactId`, `CorrelationId`, `ExecutionId`)

Each ID is ~25 lines of identical boilerplate (private constructor, `Create` factory, `IEquatable<T>`, `ToString`, `GetHashCode`). 8 IDs = ~200 lines.

Ecosystem survey:

| Package | Author | Latest | Notes |
|---|---|---|---|
| `StronglyTypedId` | Andrew Lock | 1.x | Roslyn source generator. Attribute-driven (`[StronglyTypedId]`). Generates the boilerplate at compile time. Backing type configurable (string, Guid, int, etc.). Zero runtime dep — generator only. |
| `Vogen` | Steve Dunn | 5.x | Source generator + validation (`[ValueObject]`). Richer features (normalization, validation). Pulls in `Vogen` runtime package. |
| **Build custom** | — | — | ~200 lines total. Identical pattern repeated 8 times. Straightforward but verbose. |

Decision questions:
1. Will more typed IDs be added in future sprints? (If yes, a generator pays off quickly.)
2. Is a source generator in `Ferret.Core.csproj` acceptable? (Analyzers/generators don't become runtime transitive deps — `PrivateAssets="all"` scopes them to the project.)

For `StronglyTypedId` specifically: the generator package is `StronglyTypedId` (compile-time only); if using the string backing, no runtime package is needed. Typical `.csproj` entry:

```xml
<PackageReference Include="StronglyTypedId" PrivateAssets="all" />
```

And usage:

```csharp
[StronglyTypedId(backingType: StronglyTypedIdBackingType.String)]
public partial struct WorkspaceId { }
```

Fill in:

```markdown
## Typed Identifiers

| | |
|---|---|
| **Decision** | Build / Wrap (`StronglyTypedId`) / Wrap (`Vogen`) |
| **Rationale** | |
| **Package to add** | none / `StronglyTypedId` (PrivateAssets=all) / `Vogen` |
| **Tasks affected** | Task 4 |
```

If **Wrap (`StronglyTypedId`)**:
- Add `<PackageVersion Include="StronglyTypedId" Version="1.*" />` to `Directory.Packages.props`.
- Task 4 becomes: add the attribute to 8 `partial struct` declarations instead of writing 200 lines of boilerplate.
- Rewrite Task 4 code blocks accordingly.

---

- [ ] **Step 4: Research and decide — Result / Error types**

Component: `OperationResult<T>`, `ValidationResult`, `ParseResult<T>`, `DiscoveryResult<T>`, `ReviewResult`, `IndexResult`

Ecosystem survey:

| Package | Author | Notes |
|---|---|---|
| `FluentResults` | Altinn | `Result<T>` with multiple errors, reasons, and success types. Zero deps. Actively maintained. |
| `ErrorOr` | Amichai Mantinband | Discriminated union (`ErrorOr<T>`). Single or multiple `Error` values. Zero deps. |
| `OneOf` | Harry McIntyre | General-purpose discriminated union. Not result-specific. |
| `LanguageExt` | Paul Louth | Full FP library (`Either<L,R>`, `Option<T>`). Heavy dependency. Reject for Core. |
| **Build custom** | — | Platform-specific shapes (`ValidationResult` carries `ValidationFailure` with Field/Constraint/Guidance; `DiscoveryResult` carries `IsComplete`). Ecosystem packages don't match these exact shapes. |

Decision questions:
1. Would callers benefit from `FluentResults`' chaining API (`.Bind()`, `.Map()`, `.OnFailure()`)? Or are simple `IsSuccess` + `ErrorMessage` checks sufficient?
2. Is it acceptable for the platform's public error-result contract to be bound to `FluentResults`' `IError` type?

```markdown
## Result Types

| | |
|---|---|
| **Decision** | Build / Wrap (`FluentResults`) / Wrap (`ErrorOr`) |
| **Rationale** | |
| **Package to add** | none / `FluentResults` / `ErrorOr` |
| **Tasks affected** | Task 6 |
```

If **Wrap (`FluentResults`)**: replace `OperationResult` with `FluentResults.Result<T>` (or a thin facade). Add `<PackageVersion Include="FluentResults" Version="3.*" />` to `Directory.Packages.props`. Rewrite Task 6 code blocks to use `FluentResults` types.

---

- [ ] **Step 5: Research and decide — Domain / Integration / System Events**

Component: `DomainEvent`, `IntegrationEvent`, `SystemEvent`, `EventEnvelope`, `EventMetadata` — base classes only; no dispatch, no bus, no handlers.

Ecosystem survey:

| Package | Notes |
|---|---|
| `MediatR` | Notification dispatch infrastructure. Its `INotification` marker interface could serve as event base, but MediatR is a dispatch mechanism — importing it into Core for its marker types is inappropriate. |
| `Dapr` / `MassTransit` | Integration event buses. Far outside the scope of base types. |
| **Build custom** | ~120 lines across 5 files. Pure value types carrying `EventId` (GUID string), `OccurredOn`, `CorrelationId`. No dispatch, no serialization. |

Decision: The correct answer here is almost certainly **Build**. These are abstract base classes; the event bus belongs in `Ferret.Runtime` or a later sprint. Confirm there is no approved event contract in ARCH-011 (missing input) that mandates a specific type.

```markdown
## Event Infrastructure

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | |
| **Package to add** | none |
| **Tasks affected** | Task 8 |
```

---

- [ ] **Step 6: Research and decide — Health Check abstraction**

Component: `IHealthCheck`, `HealthCheckResult` — platform-defined health contract.

Ecosystem survey:

| Package | Notes |
|---|---|
| `Microsoft.Extensions.Diagnostics.HealthChecks` | Provides `IHealthCheck`, `HealthCheckResult`, `HealthStatus`. Adding this to Ferret.Core would pull `Microsoft.Extensions.Diagnostics.Abstractions` as a transitive dep for every consumer. |
| `Microsoft.Extensions.Diagnostics.Abstractions` | Lightweight subset — still pulls `Microsoft.Extensions.*`. |
| **Build custom** | `IHealthCheck` is one method; `HealthCheckResult` is a factory-method value type with three states. ~40 lines. |

Decision question: Should Ferret.Core's `IHealthCheck` be the same CLR type as `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck`, so that hosts can register Core components directly with ASP.NET Core's health-check middleware without adapters? If yes, **Wrap** is justified. If the host layer is responsible for bridging, **Build** keeps Core clean.

```markdown
## Health Check Abstraction

| | |
|---|---|
| **Decision** | Build / Wrap (`Microsoft.Extensions.Diagnostics.Abstractions`) |
| **Rationale** | |
| **Package to add** | none / `Microsoft.Extensions.Diagnostics.Abstractions` |
| **Tasks affected** | Task 7 |
```

---

- [ ] **Step 7: Research and decide — Exception hierarchy**

Component: `FerretException` (abstract), `ValidationException`, `ConfigurationException`, `PlatformException`, `SecurityException`, `PermissionDeniedException`, 7 workspace exceptions.

Ecosystem survey: No relevant package. Custom domain exception hierarchies are always built; no ecosystem library provides these. Decision is **Build** — confirm and document.

```markdown
## Exception Hierarchy

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | Domain exceptions are always project-specific. No ecosystem package provides Ferret-specific types. |
| **Package to add** | none |
| **Tasks affected** | Tasks 3 |
```

---

- [ ] **Step 8: Research and decide — Enumerations**

Component: `HealthStatus`, `Severity`, `ValidationSeverity`, `PluginState`, `SpecificationStatus`, `ReviewStatus` — contract-level enums with no behaviour.

Ecosystem survey:

| Package | Notes |
|---|---|
| `Ardalis.SmartEnum` | Adds behaviour (display name, list, equality) to enums via a base class pattern. Overkill for pure contract enums. |
| `Humanizer` | Enum description display. Infrastructure concern, not Core. |
| **Plain C# enums** | Zero deps. Values are integer-backed. Serialization-neutral. |

Decision: **Build** using plain C# `enum` declarations. Smart-enum patterns add runtime deps and are inappropriate for a contract-only kernel assembly.

```markdown
## Enumerations

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | Plain C# enums are serialization-neutral, zero-dep, and appropriate for contract-only types. |
| **Package to add** | none |
| **Tasks affected** | Task 2 |
```

---

- [ ] **Step 9: Write the decision record**

Create `docs/002-Architecture/decisions/sprint-3-technology-evaluation.md` and paste in the completed decision tables from Steps 1–8.

The file must include a header section:

```markdown
# Sprint 3 Technology Evaluation

**Date:** 2026-06-27
**Sprint:** 3
**Author:** [your name]
**Status:** Approved

## Summary

| Component | Decision | Package |
|---|---|---|
| SemanticVersion | | |
| ContentHash | | |
| Typed IDs (8x) | | |
| Result Types | | |
| Event Infrastructure | | |
| Health Check Abstraction | | |
| Exception Hierarchy | | |
| Enumerations | | |

## Decisions

[Paste completed tables from each step here]
```

- [ ] **Step 10: If any "Wrap" or "Adopt" decisions were made — update the plan**

For each package to be added:

1. Add the version pin to `Directory.Packages.props`:

```xml
<!-- Example — only add what was actually decided -->
<PackageVersion Include="StronglyTypedId" Version="1.*" />
<PackageVersion Include="FluentResults" Version="3.*" />
<PackageVersion Include="Semver" Version="3.*" />
```

2. Add the `<PackageReference>` to `src/Ferret.Core/Ferret.Core.csproj` (source generators with `PrivateAssets="all"` do not become transitive deps):

```xml
<!-- Source generators — PrivateAssets="all" keeps them out of the transitive closure -->
<PackageReference Include="StronglyTypedId" PrivateAssets="all" />

<!-- Runtime packages — becomes transitive for all consumers of Core -->
<PackageReference Include="FluentResults" />
```

3. Edit the code blocks in the affected tasks in **this plan file** to reflect the adopted types before starting Task 1.

- [ ] **Step 11: Verify Directory.Packages.props compiles with any new pins**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings. (No new source files yet — this verifies the package version pins are syntactically valid.)

- [ ] **Step 12: Commit the decision record**

```powershell
git add docs/002-Architecture/decisions/sprint-3-technology-evaluation.md
git add Directory.Packages.props  # only if packages were added
git add src/Ferret.Core/Ferret.Core.csproj  # only if package references were added
git add docs/superpowers/plans/2026-06-27-sprint-3-platform-kernel.md  # if task code was updated
git commit -m "docs(sprint-3): technology evaluation — build/wrap/adopt decisions for all Core components"
```

**Gate check:** All 8 components have a documented decision. Any "Wrap" or "Adopt" choices have been reflected in the File Structure above and in the affected task code blocks. Only then proceed to Task 1.

---

## Task 1: Folder Scaffolding

**Files:**
- Modify: `src/Ferret.Core/CoreModule.cs`

**Interfaces:**
- Produces: Clean folder structure; CoreModule available as assembly anchor

> `CoreModuleTests.cs` is **not** touched in this task. The smoke test that references `HealthStatus` is deferred to Task 2, which creates the type. This keeps the repository green after Task 1.

- [ ] **Step 1: Create source folders**

```powershell
New-Item -ItemType Directory src/Ferret.Core/Enumerations
New-Item -ItemType Directory src/Ferret.Core/Errors
New-Item -ItemType Directory src/Ferret.Core/Primitives
New-Item -ItemType Directory src/Ferret.Core/Results
New-Item -ItemType Directory src/Ferret.Core/Abstractions
New-Item -ItemType Directory src/Ferret.Core/Events
New-Item -ItemType Directory tests/Ferret.Core.Tests/Enumerations
New-Item -ItemType Directory tests/Ferret.Core.Tests/Errors
New-Item -ItemType Directory tests/Ferret.Core.Tests/Primitives
New-Item -ItemType Directory tests/Ferret.Core.Tests/Results
New-Item -ItemType Directory tests/Ferret.Core.Tests/Abstractions
New-Item -ItemType Directory tests/Ferret.Core.Tests/Events
```

- [ ] **Step 2: Update CoreModule.cs**

```csharp
namespace Ferret.Core;

/// <summary>Assembly anchor for Ferret.Core. Contains no business logic.</summary>
internal static class CoreModule
{
}
```

- [ ] **Step 3: Build and test — expect all green**

```powershell
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: 0 errors, 0 warnings. 1 test passes (`Placeholder_ScaffoldVerification_Passes`).

- [ ] **Step 4: Commit**

```powershell
git add src/Ferret.Core/CoreModule.cs
git commit -m "chore(sprint-3): scaffold Ferret.Core folder structure"
```

---

## Task 2: Enumerations

**Files:**
- Create: `src/Ferret.Core/Enumerations/HealthStatus.cs`
- Create: `src/Ferret.Core/Enumerations/Severity.cs`
- Create: `src/Ferret.Core/Enumerations/ValidationSeverity.cs`
- Create: `src/Ferret.Core/Enumerations/PluginState.cs`
- Create: `src/Ferret.Core/Enumerations/SpecificationStatus.cs`
- Create: `src/Ferret.Core/Enumerations/ReviewStatus.cs`
- Create: `tests/Ferret.Core.Tests/Enumerations/EnumerationTests.cs`
- Modify: `tests/Ferret.Core.Tests/CoreModuleTests.cs`

**Interfaces:**
- Produces: `Ferret.Core.Enumerations.HealthStatus`, `Severity`, `ValidationSeverity`, `PluginState`, `SpecificationStatus`, `ReviewStatus`

- [ ] **Step 0: Update CoreModuleTests.cs smoke test (now that HealthStatus will exist after this task)**

Replace the placeholder in `tests/Ferret.Core.Tests/CoreModuleTests.cs` with:

```csharp
namespace Ferret.Core.Tests;

public sealed class CoreModuleTests
{
    [Fact]
    public void Core_Assembly_Loads() =>
        Assert.NotNull(typeof(Ferret.Core.Enumerations.HealthStatus).Assembly);
}
```

This test is intentionally written red first — it will compile only once Step 3 (HealthStatus.cs) is created.

- [ ] **Step 1: Write the failing enumeration tests (EnumerationTests.cs)**

Create `tests/Ferret.Core.Tests/Enumerations/EnumerationTests.cs`:

```csharp
using Ferret.Core.Enumerations;

namespace Ferret.Core.Tests.Enumerations;

public sealed class EnumerationTests
{
    [Fact]
    public void HealthStatus_Has_Expected_Values()
    {
        Assert.Equal(0, (int)HealthStatus.Unknown);
        Assert.Equal(1, (int)HealthStatus.Healthy);
        Assert.Equal(2, (int)HealthStatus.Degraded);
        Assert.Equal(3, (int)HealthStatus.Unhealthy);
    }

    [Fact]
    public void Severity_Has_Expected_Values()
    {
        Assert.Equal(0, (int)Severity.None);
        Assert.Equal(1, (int)Severity.Low);
        Assert.Equal(2, (int)Severity.Medium);
        Assert.Equal(3, (int)Severity.High);
        Assert.Equal(4, (int)Severity.Critical);
    }

    [Fact]
    public void ValidationSeverity_Has_Expected_Values()
    {
        Assert.Equal(0, (int)ValidationSeverity.Info);
        Assert.Equal(1, (int)ValidationSeverity.Warning);
        Assert.Equal(2, (int)ValidationSeverity.Error);
    }

    [Fact]
    public void PluginState_Has_Expected_Values()
    {
        Assert.Equal(0, (int)PluginState.Unloaded);
        Assert.Equal(1, (int)PluginState.Loading);
        Assert.Equal(2, (int)PluginState.Active);
        Assert.Equal(3, (int)PluginState.Faulted);
        Assert.Equal(4, (int)PluginState.Unloading);
    }

    [Fact]
    public void SpecificationStatus_Has_Expected_Values()
    {
        Assert.Equal(0, (int)SpecificationStatus.Draft);
        Assert.Equal(1, (int)SpecificationStatus.UnderReview);
        Assert.Equal(2, (int)SpecificationStatus.Approved);
        Assert.Equal(3, (int)SpecificationStatus.Rejected);
        Assert.Equal(4, (int)SpecificationStatus.Superseded);
    }

    [Fact]
    public void ReviewStatus_Has_Expected_Values()
    {
        Assert.Equal(0, (int)ReviewStatus.Pending);
        Assert.Equal(1, (int)ReviewStatus.InProgress);
        Assert.Equal(2, (int)ReviewStatus.Complete);
        Assert.Equal(3, (int)ReviewStatus.Abandoned);
    }
}
```

- [ ] **Step 2: Run test to confirm red**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj --no-build 2>&1 | Select-String "error|fail|Error"
```

Expected: compile errors — enum types not found.

- [ ] **Step 3: Implement HealthStatus.cs**

```csharp
namespace Ferret.Core.Enumerations;

/// <summary>Represents the health state of a platform component or subsystem.</summary>
public enum HealthStatus
{
    /// <summary>Health state has not been determined.</summary>
    Unknown = 0,

    /// <summary>The component is operating normally.</summary>
    Healthy = 1,

    /// <summary>The component is operational but degraded.</summary>
    Degraded = 2,

    /// <summary>The component has failed and is not operational.</summary>
    Unhealthy = 3,
}
```

- [ ] **Step 4: Implement Severity.cs**

```csharp
namespace Ferret.Core.Enumerations;

/// <summary>Represents the severity level of a finding, issue, or event.</summary>
public enum Severity
{
    /// <summary>No severity — informational only.</summary>
    None = 0,

    /// <summary>Low severity — minor issue with minimal impact.</summary>
    Low = 1,

    /// <summary>Medium severity — notable issue requiring attention.</summary>
    Medium = 2,

    /// <summary>High severity — significant issue requiring prompt action.</summary>
    High = 3,

    /// <summary>Critical severity — blocking issue requiring immediate action.</summary>
    Critical = 4,
}
```

- [ ] **Step 5: Implement ValidationSeverity.cs**

```csharp
namespace Ferret.Core.Enumerations;

/// <summary>Represents the severity of a validation finding.</summary>
public enum ValidationSeverity
{
    /// <summary>Informational message — no action required.</summary>
    Info = 0,

    /// <summary>Warning — the input is valid but may cause issues.</summary>
    Warning = 1,

    /// <summary>Error — the input is invalid and must be corrected.</summary>
    Error = 2,
}
```

- [ ] **Step 6: Implement PluginState.cs**

```csharp
namespace Ferret.Core.Enumerations;

/// <summary>Represents the lifecycle state of a plugin within the platform.</summary>
public enum PluginState
{
    /// <summary>The plugin has not been loaded.</summary>
    Unloaded = 0,

    /// <summary>The plugin is in the process of loading.</summary>
    Loading = 1,

    /// <summary>The plugin is loaded and active.</summary>
    Active = 2,

    /// <summary>The plugin encountered an error and is in a faulted state.</summary>
    Faulted = 3,

    /// <summary>The plugin is in the process of unloading.</summary>
    Unloading = 4,
}
```

- [ ] **Step 7: Implement SpecificationStatus.cs**

```csharp
namespace Ferret.Core.Enumerations;

/// <summary>Represents the review lifecycle state of a specification document.</summary>
public enum SpecificationStatus
{
    /// <summary>The specification is in draft state and not yet under review.</summary>
    Draft = 0,

    /// <summary>The specification has been submitted and is under review.</summary>
    UnderReview = 1,

    /// <summary>The specification has been approved.</summary>
    Approved = 2,

    /// <summary>The specification has been rejected.</summary>
    Rejected = 3,

    /// <summary>The specification has been superseded by a newer version.</summary>
    Superseded = 4,
}
```

- [ ] **Step 8: Implement ReviewStatus.cs**

```csharp
namespace Ferret.Core.Enumerations;

/// <summary>Represents the execution state of a review workflow.</summary>
public enum ReviewStatus
{
    /// <summary>The review has been created but not yet started.</summary>
    Pending = 0,

    /// <summary>The review is actively in progress.</summary>
    InProgress = 1,

    /// <summary>The review has been completed.</summary>
    Complete = 2,

    /// <summary>The review was abandoned before completion.</summary>
    Abandoned = 3,
}
```

- [ ] **Step 9: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All 7 tests pass (6 enumeration tests + CoreModuleTests.Core_Assembly_Loads).

- [ ] **Step 10: Verify build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 11: Commit**

```powershell
git add src/Ferret.Core/Enumerations/ tests/Ferret.Core.Tests/Enumerations/ tests/Ferret.Core.Tests/CoreModuleTests.cs
git commit -m "feat(sprint-3): add Core enumerations — HealthStatus, Severity, ValidationSeverity, PluginState, SpecificationStatus, ReviewStatus"
```

---

## Task 3: Exception Hierarchy

**Files:**
- Create: `src/Ferret.Core/Errors/FerretException.cs`
- Create: `src/Ferret.Core/Errors/ValidationException.cs`
- Create: `src/Ferret.Core/Errors/ConfigurationException.cs`
- Create: `src/Ferret.Core/Errors/PlatformException.cs`
- Create: `src/Ferret.Core/Errors/SecurityException.cs`
- Create: `src/Ferret.Core/Errors/PermissionDeniedException.cs`
- Create: `src/Ferret.Core/Errors/WorkspaceNotFoundException.cs`
- Create: `src/Ferret.Core/Errors/WorkspaceAlreadyExistsException.cs`
- Create: `src/Ferret.Core/Errors/WorkspaceConfigurationException.cs`
- Create: `src/Ferret.Core/Errors/WorkspaceSchemaVersionException.cs`
- Create: `src/Ferret.Core/Errors/WorkspaceUpgradeRequiredException.cs`
- Create: `src/Ferret.Core/Errors/WorkspaceUpgradeFailedException.cs`
- Create: `src/Ferret.Core/Errors/WorkspacePathTraversalException.cs`
- Create: `tests/Ferret.Core.Tests/Errors/ExceptionHierarchyTests.cs`
- Create: `tests/Ferret.Core.Tests/Errors/WorkspaceExceptionTests.cs`

**Interfaces:**
- Consumes: `Ferret.Core.Enumerations.Severity` (from Task 2)
- Produces: All exception types in `Ferret.Core.Errors`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Errors/ExceptionHierarchyTests.cs`:

```csharp
using Ferret.Core.Errors;

namespace Ferret.Core.Tests.Errors;

public sealed class ExceptionHierarchyTests
{
    [Fact]
    public void ValidationException_Is_FerretException() =>
        Assert.True(typeof(ValidationException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void ConfigurationException_Is_FerretException() =>
        Assert.True(typeof(ConfigurationException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void PlatformException_Is_FerretException() =>
        Assert.True(typeof(PlatformException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void SecurityException_Is_FerretException() =>
        Assert.True(typeof(SecurityException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void PermissionDeniedException_Is_SecurityException() =>
        Assert.True(typeof(PermissionDeniedException).IsSubclassOf(typeof(SecurityException)));

    [Fact]
    public void ValidationException_Stores_Field_And_Constraint()
    {
        var ex = new ValidationException("name", "required", "Provide a name.");
        Assert.Equal("name", ex.Field);
        Assert.Equal("required", ex.Constraint);
        Assert.Equal("Provide a name.", ex.Guidance);
    }

    [Fact]
    public void PermissionDeniedException_Stores_Permission()
    {
        var ex = new PermissionDeniedException("workspace:read");
        Assert.Equal("workspace:read", ex.Permission);
    }
}
```

Create `tests/Ferret.Core.Tests/Errors/WorkspaceExceptionTests.cs`:

```csharp
using Ferret.Core.Errors;

namespace Ferret.Core.Tests.Errors;

public sealed class WorkspaceExceptionTests
{
    [Fact]
    public void WorkspaceNotFoundException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceNotFoundException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceAlreadyExistsException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceAlreadyExistsException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceConfigurationException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceConfigurationException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceSchemaVersionException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceSchemaVersionException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceUpgradeRequiredException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceUpgradeRequiredException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceUpgradeFailedException_Is_FerretException() =>
        Assert.True(typeof(WorkspaceUpgradeFailedException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspacePathTraversalException_Is_FerretException() =>
        Assert.True(typeof(WorkspacePathTraversalException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void WorkspaceNotFoundException_Stores_WorkspaceId()
    {
        var ex = new WorkspaceNotFoundException("ws-123");
        Assert.Equal("ws-123", ex.WorkspaceId);
        Assert.Contains("ws-123", ex.Message);
    }

    [Fact]
    public void WorkspacePathTraversalException_Stores_Path()
    {
        var ex = new WorkspacePathTraversalException("../../../etc/passwd");
        Assert.Equal("../../../etc/passwd", ex.AttemptedPath);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet build tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj 2>&1 | Select-String "error CS"
```

Expected: compile errors for missing exception types.

- [ ] **Step 3: Implement FerretException.cs**

```csharp
namespace Ferret.Core.Errors;

/// <summary>Base class for all Ferret platform exceptions.</summary>
public abstract class FerretException : Exception
{
    /// <summary>Initializes a new instance of <see cref="FerretException"/> with a message.</summary>
    /// <param name="message">The exception message.</param>
    protected FerretException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="FerretException"/> with a message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected FerretException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 4: Implement ValidationException.cs**

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when input validation fails for a specific field or constraint.</summary>
public sealed class ValidationException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="ValidationException"/>.</summary>
    /// <param name="field">The name of the field that failed validation.</param>
    /// <param name="constraint">The constraint that was violated.</param>
    /// <param name="guidance">Human-readable guidance for resolving the validation failure.</param>
    public ValidationException(string field, string constraint, string guidance)
        : base($"Validation failed for field '{field}': {constraint}. {guidance}")
    {
        Field = field;
        Constraint = constraint;
        Guidance = guidance;
    }

    /// <summary>Gets the name of the field that failed validation.</summary>
    public string Field { get; }

    /// <summary>Gets the constraint that was violated.</summary>
    public string Constraint { get; }

    /// <summary>Gets human-readable guidance for resolving the failure.</summary>
    public string Guidance { get; }
}
```

- [ ] **Step 5: Implement ConfigurationException.cs**

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when a configuration value is missing, malformed, or invalid.</summary>
public sealed class ConfigurationException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="ConfigurationException"/>.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ConfigurationException"/> with an inner exception.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 6: Implement PlatformException.cs**

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when an unrecoverable platform-level error occurs.</summary>
public sealed class PlatformException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="PlatformException"/>.</summary>
    /// <param name="message">A message describing the platform error.</param>
    public PlatformException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="PlatformException"/> with an inner exception.</summary>
    /// <param name="message">A message describing the platform error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public PlatformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 7: Implement SecurityException.cs**

```csharp
namespace Ferret.Core.Errors;

/// <summary>Base class for security-related platform exceptions.</summary>
public class SecurityException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="SecurityException"/>.</summary>
    /// <param name="message">A message describing the security violation.</param>
    public SecurityException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="SecurityException"/> with an inner exception.</summary>
    /// <param name="message">A message describing the security violation.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SecurityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 8: Implement PermissionDeniedException.cs**

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when an operation is denied because the caller lacks the required permission.</summary>
public sealed class PermissionDeniedException : SecurityException
{
    /// <summary>Initializes a new instance of <see cref="PermissionDeniedException"/>.</summary>
    /// <param name="permission">The permission that was required but not held.</param>
    public PermissionDeniedException(string permission)
        : base($"Permission denied: '{permission}' is required to perform this operation.")
    {
        Permission = permission;
    }

    /// <summary>Gets the permission identifier that was required but not held.</summary>
    public string Permission { get; }
}
```

- [ ] **Step 9: Implement workspace exceptions**

Create `src/Ferret.Core/Errors/WorkspaceNotFoundException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when a workspace cannot be found by its identifier or path.</summary>
public sealed class WorkspaceNotFoundException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspaceNotFoundException"/>.</summary>
    /// <param name="workspaceId">The identifier of the workspace that could not be found.</param>
    public WorkspaceNotFoundException(string workspaceId)
        : base($"Workspace '{workspaceId}' was not found.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Gets the identifier of the workspace that could not be found.</summary>
    public string WorkspaceId { get; }
}
```

Create `src/Ferret.Core/Errors/WorkspaceAlreadyExistsException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when an attempt is made to create a workspace that already exists.</summary>
public sealed class WorkspaceAlreadyExistsException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspaceAlreadyExistsException"/>.</summary>
    /// <param name="workspaceId">The identifier of the workspace that already exists.</param>
    public WorkspaceAlreadyExistsException(string workspaceId)
        : base($"Workspace '{workspaceId}' already exists.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Gets the identifier of the workspace that already exists.</summary>
    public string WorkspaceId { get; }
}
```

Create `src/Ferret.Core/Errors/WorkspaceConfigurationException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when workspace configuration is invalid or cannot be loaded.</summary>
public sealed class WorkspaceConfigurationException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspaceConfigurationException"/>.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    public WorkspaceConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="WorkspaceConfigurationException"/> with an inner exception.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

Create `src/Ferret.Core/Errors/WorkspaceSchemaVersionException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when the workspace schema version is incompatible with the current platform version.</summary>
public sealed class WorkspaceSchemaVersionException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspaceSchemaVersionException"/>.</summary>
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

Create `src/Ferret.Core/Errors/WorkspaceUpgradeRequiredException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when a workspace must be upgraded before it can be used.</summary>
public sealed class WorkspaceUpgradeRequiredException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspaceUpgradeRequiredException"/>.</summary>
    /// <param name="workspaceId">The identifier of the workspace that requires upgrading.</param>
    public WorkspaceUpgradeRequiredException(string workspaceId)
        : base($"Workspace '{workspaceId}' must be upgraded before use.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Gets the identifier of the workspace that requires upgrading.</summary>
    public string WorkspaceId { get; }
}
```

Create `src/Ferret.Core/Errors/WorkspaceUpgradeFailedException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when a workspace upgrade attempt fails.</summary>
public sealed class WorkspaceUpgradeFailedException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspaceUpgradeFailedException"/>.</summary>
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

Create `src/Ferret.Core/Errors/WorkspacePathTraversalException.cs`:

```csharp
namespace Ferret.Core.Errors;

/// <summary>Thrown when an operation attempts to access a path outside the workspace root.</summary>
public sealed class WorkspacePathTraversalException : FerretException
{
    /// <summary>Initializes a new instance of <see cref="WorkspacePathTraversalException"/>.</summary>
    /// <param name="attemptedPath">The path that was attempted.</param>
    public WorkspacePathTraversalException(string attemptedPath)
        : base($"Path traversal attempt detected: '{attemptedPath}' is outside the workspace root.")
    {
        AttemptedPath = attemptedPath;
    }

    /// <summary>Gets the path that was attempted.</summary>
    public string AttemptedPath { get; }
}
```

- [ ] **Step 10: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All tests pass (previous + 17 new exception tests).

- [ ] **Step 11: Verify build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 12: Commit**

```powershell
git add src/Ferret.Core/Errors/ tests/Ferret.Core.Tests/Errors/
git commit -m "feat(sprint-3): add Core exception hierarchy — FerretException and 12 concrete types"
```

---

## Task 4: Typed ID Value Objects

**Files:**
- Create: `src/Ferret.Core/Primitives/WorkspaceId.cs`
- Create: `src/Ferret.Core/Primitives/DocumentId.cs`
- Create: `src/Ferret.Core/Primitives/SpecificationId.cs`
- Create: `src/Ferret.Core/Primitives/ReviewId.cs`
- Create: `src/Ferret.Core/Primitives/PluginId.cs`
- Create: `src/Ferret.Core/Primitives/ArtifactId.cs`
- Create: `src/Ferret.Core/Primitives/CorrelationId.cs`
- Create: `src/Ferret.Core/Primitives/ExecutionId.cs`
- Create: `tests/Ferret.Core.Tests/Primitives/TypedIdTests.cs`

**Interfaces:**
- Produces: All 8 typed ID types in `Ferret.Core.Primitives`, each implementing `IEquatable<T>`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Primitives/TypedIdTests.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Primitives;

public sealed class TypedIdTests
{
    // WorkspaceId
    [Fact]
    public void WorkspaceId_Create_ReturnsInstance() =>
        Assert.Equal("ws-1", WorkspaceId.Create("ws-1").Value);

    [Fact]
    public void WorkspaceId_Create_ThrowsOnEmpty() =>
        Assert.Throws<ArgumentException>(() => WorkspaceId.Create(string.Empty));

    [Fact]
    public void WorkspaceId_Equality_SameValue_IsEqual()
    {
        var a = WorkspaceId.Create("ws-1");
        var b = WorkspaceId.Create("ws-1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void WorkspaceId_Equality_DifferentValue_IsNotEqual()
    {
        var a = WorkspaceId.Create("ws-1");
        var b = WorkspaceId.Create("ws-2");
        Assert.NotEqual(a, b);
    }

    // DocumentId
    [Fact]
    public void DocumentId_Create_ReturnsInstance() =>
        Assert.Equal("doc-1", DocumentId.Create("doc-1").Value);

    [Fact]
    public void DocumentId_Create_ThrowsOnWhiteSpace() =>
        Assert.Throws<ArgumentException>(() => DocumentId.Create("   "));

    // SpecificationId
    [Fact]
    public void SpecificationId_Create_ReturnsInstance() =>
        Assert.Equal("spec-1", SpecificationId.Create("spec-1").Value);

    // ReviewId
    [Fact]
    public void ReviewId_Create_ReturnsInstance() =>
        Assert.Equal("rv-1", ReviewId.Create("rv-1").Value);

    // PluginId
    [Fact]
    public void PluginId_Create_ReturnsInstance() =>
        Assert.Equal("plugin-foo", PluginId.Create("plugin-foo").Value);

    // ArtifactId
    [Fact]
    public void ArtifactId_Create_ReturnsInstance() =>
        Assert.Equal("art-1", ArtifactId.Create("art-1").Value);

    // CorrelationId
    [Fact]
    public void CorrelationId_Create_ReturnsInstance() =>
        Assert.Equal("corr-abc", CorrelationId.Create("corr-abc").Value);

    // ExecutionId
    [Fact]
    public void ExecutionId_Create_ReturnsInstance() =>
        Assert.Equal("exec-1", ExecutionId.Create("exec-1").Value);

    // ToString
    [Fact]
    public void WorkspaceId_ToString_ReturnsValue() =>
        Assert.Equal("ws-42", WorkspaceId.Create("ws-42").ToString());

    // GetHashCode consistency
    [Fact]
    public void WorkspaceId_SameValue_SameHashCode()
    {
        var a = WorkspaceId.Create("ws-1");
        var b = WorkspaceId.Create("ws-1");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet build tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj 2>&1 | Select-String "error CS"
```

Expected: compile errors for missing typed ID types.

- [ ] **Step 3: Implement WorkspaceId.cs**

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a workspace.</summary>
public sealed class WorkspaceId : IEquatable<WorkspaceId>
{
    private WorkspaceId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="WorkspaceId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="WorkspaceId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static WorkspaceId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new WorkspaceId(value);
    }

    /// <inheritdoc/>
    public bool Equals(WorkspaceId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is WorkspaceId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

- [ ] **Step 4: Implement the remaining 7 typed IDs**

Each file follows the exact same pattern. Create these 7 files:

`src/Ferret.Core/Primitives/DocumentId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a document.</summary>
public sealed class DocumentId : IEquatable<DocumentId>
{
    private DocumentId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="DocumentId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="DocumentId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static DocumentId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new DocumentId(value);
    }

    /// <inheritdoc/>
    public bool Equals(DocumentId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DocumentId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Primitives/SpecificationId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a specification.</summary>
public sealed class SpecificationId : IEquatable<SpecificationId>
{
    private SpecificationId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="SpecificationId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="SpecificationId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static SpecificationId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new SpecificationId(value);
    }

    /// <inheritdoc/>
    public bool Equals(SpecificationId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SpecificationId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Primitives/ReviewId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a review.</summary>
public sealed class ReviewId : IEquatable<ReviewId>
{
    private ReviewId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="ReviewId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="ReviewId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ReviewId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ReviewId(value);
    }

    /// <inheritdoc/>
    public bool Equals(ReviewId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ReviewId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Primitives/PluginId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a plugin.</summary>
public sealed class PluginId : IEquatable<PluginId>
{
    private PluginId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="PluginId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="PluginId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static PluginId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new PluginId(value);
    }

    /// <inheritdoc/>
    public bool Equals(PluginId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PluginId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Primitives/ArtifactId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for an artifact.</summary>
public sealed class ArtifactId : IEquatable<ArtifactId>
{
    private ArtifactId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="ArtifactId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="ArtifactId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ArtifactId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ArtifactId(value);
    }

    /// <inheritdoc/>
    public bool Equals(ArtifactId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ArtifactId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Primitives/CorrelationId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for correlating operations across module boundaries.</summary>
public sealed class CorrelationId : IEquatable<CorrelationId>
{
    private CorrelationId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="CorrelationId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="CorrelationId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static CorrelationId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new CorrelationId(value);
    }

    /// <inheritdoc/>
    public bool Equals(CorrelationId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CorrelationId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

`src/Ferret.Core/Primitives/ExecutionId.cs`:

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a single execution or run.</summary>
public sealed class ExecutionId : IEquatable<ExecutionId>
{
    private ExecutionId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="ExecutionId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="ExecutionId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ExecutionId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ExecutionId(value);
    }

    /// <inheritdoc/>
    public bool Equals(ExecutionId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ExecutionId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

- [ ] **Step 5: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All previous tests + 15 new typed ID tests pass.

- [ ] **Step 6: Verify build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 7: Commit**

```powershell
git add src/Ferret.Core/Primitives/ tests/Ferret.Core.Tests/Primitives/TypedIdTests.cs
git commit -m "feat(sprint-3): add 8 typed ID value objects — WorkspaceId, DocumentId, SpecificationId, ReviewId, PluginId, ArtifactId, CorrelationId, ExecutionId"
```

---

## Task 5: ContentHash and SemanticVersion

**Files:**
- Create: `src/Ferret.Core/Primitives/ContentHash.cs`
- Create: `src/Ferret.Core/Primitives/SemanticVersion.cs`
- Create: `tests/Ferret.Core.Tests/Primitives/ContentHashTests.cs`
- Create: `tests/Ferret.Core.Tests/Primitives/SemanticVersionTests.cs`

**Interfaces:**
- Produces: `Ferret.Core.Primitives.ContentHash`, `Ferret.Core.Primitives.SemanticVersion`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Primitives/ContentHashTests.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Primitives;

public sealed class ContentHashTests
{
    [Fact]
    public void ContentHash_Create_ReturnsInstance()
    {
        var hash = ContentHash.Create("sha256", "abc123");
        Assert.Equal("sha256", hash.Algorithm);
        Assert.Equal("abc123", hash.Hex);
    }

    [Fact]
    public void ContentHash_Create_ThrowsOnEmptyAlgorithm() =>
        Assert.Throws<ArgumentException>(() => ContentHash.Create(string.Empty, "abc"));

    [Fact]
    public void ContentHash_Create_ThrowsOnEmptyHex() =>
        Assert.Throws<ArgumentException>(() => ContentHash.Create("sha256", string.Empty));

    [Fact]
    public void ContentHash_Equality_SameValues_IsEqual()
    {
        var a = ContentHash.Create("sha256", "abc");
        var b = ContentHash.Create("sha256", "abc");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ContentHash_Equality_DifferentHex_IsNotEqual()
    {
        var a = ContentHash.Create("sha256", "abc");
        var b = ContentHash.Create("sha256", "def");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ContentHash_ToString_ReturnsCombined() =>
        Assert.Equal("sha256:abc123", ContentHash.Create("sha256", "abc123").ToString());
}
```

Create `tests/Ferret.Core.Tests/Primitives/SemanticVersionTests.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Primitives;

public sealed class SemanticVersionTests
{
    [Fact]
    public void SemanticVersion_Create_ParsesCorrectly()
    {
        var v = SemanticVersion.Parse("1.2.3");
        Assert.Equal(1, v.Major);
        Assert.Equal(2, v.Minor);
        Assert.Equal(3, v.Patch);
        Assert.Null(v.PreRelease);
    }

    [Fact]
    public void SemanticVersion_Create_WithPreRelease()
    {
        var v = SemanticVersion.Parse("1.0.0-beta.1");
        Assert.Equal(1, v.Major);
        Assert.Equal(0, v.Minor);
        Assert.Equal(0, v.Patch);
        Assert.Equal("beta.1", v.PreRelease);
    }

    [Fact]
    public void SemanticVersion_Parse_ThrowsOnInvalidFormat() =>
        Assert.Throws<FormatException>(() => SemanticVersion.Parse("not-a-version"));

    [Fact]
    public void SemanticVersion_Equality_SameVersion_IsEqual()
    {
        var a = SemanticVersion.Parse("2.0.0");
        var b = SemanticVersion.Parse("2.0.0");
        Assert.Equal(a, b);
    }

    [Fact]
    public void SemanticVersion_ToString_ReturnsString()
    {
        Assert.Equal("1.2.3", SemanticVersion.Parse("1.2.3").ToString());
        Assert.Equal("1.0.0-beta.1", SemanticVersion.Parse("1.0.0-beta.1").ToString());
    }

    [Fact]
    public void SemanticVersion_Comparison_OlderIsLess()
    {
        var older = SemanticVersion.Parse("1.0.0");
        var newer = SemanticVersion.Parse("2.0.0");
        Assert.True(older.CompareTo(newer) < 0);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet build tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj 2>&1 | Select-String "error CS"
```

Expected: compile errors for missing `ContentHash` and `SemanticVersion`.

- [ ] **Step 3: Implement ContentHash.cs**

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Represents the cryptographic hash of content, identified by algorithm and hex digest.</summary>
public sealed class ContentHash : IEquatable<ContentHash>
{
    private ContentHash(string algorithm, string hex)
    {
        Algorithm = algorithm;
        Hex = hex;
    }

    /// <summary>Gets the name of the hashing algorithm (e.g. "sha256").</summary>
    public string Algorithm { get; }

    /// <summary>Gets the hexadecimal digest string.</summary>
    public string Hex { get; }

    /// <summary>Creates a new <see cref="ContentHash"/> instance.</summary>
    /// <param name="algorithm">The hashing algorithm name.</param>
    /// <param name="hex">The hexadecimal digest string.</param>
    /// <returns>A new <see cref="ContentHash"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="algorithm"/> or <paramref name="hex"/> is null or whitespace.</exception>
    public static ContentHash Create(string algorithm, string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        return new ContentHash(algorithm, hex);
    }

    /// <inheritdoc/>
    public bool Equals(ContentHash? other) =>
        other is not null &&
        string.Equals(Algorithm, other.Algorithm, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Hex, other.Hex, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ContentHash other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(
            Algorithm.ToUpperInvariant().GetHashCode(StringComparison.Ordinal),
            Hex.GetHashCode(StringComparison.Ordinal));

    /// <summary>Returns the hash in <c>algorithm:hex</c> format.</summary>
    /// <returns>A string representation of the content hash.</returns>
    public override string ToString() => $"{Algorithm}:{Hex}";
}
```

- [ ] **Step 4: Implement SemanticVersion.cs**

```csharp
namespace Ferret.Core.Primitives;

/// <summary>Represents a semantic version following the SemVer 2.0.0 specification.</summary>
public sealed class SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
{
    private SemanticVersion(int major, int minor, int patch, string? preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    /// <summary>Gets the major version number.</summary>
    public int Major { get; }

    /// <summary>Gets the minor version number.</summary>
    public int Minor { get; }

    /// <summary>Gets the patch version number.</summary>
    public int Patch { get; }

    /// <summary>Gets the pre-release label, or <see langword="null"/> if this is a stable release.</summary>
    public string? PreRelease { get; }

    /// <summary>Parses a semantic version string in the form <c>MAJOR.MINOR.PATCH[-pre-release]</c>.</summary>
    /// <param name="value">The version string to parse.</param>
    /// <returns>A parsed <see cref="SemanticVersion"/> instance.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not a valid semantic version.</exception>
    public static SemanticVersion Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var dashIndex = value.IndexOf('-', StringComparison.Ordinal);
        var corePart = dashIndex >= 0 ? value[..dashIndex] : value;
        var preRelease = dashIndex >= 0 ? value[(dashIndex + 1)..] : null;

        var segments = corePart.Split('.');
        if (segments.Length != 3 ||
            !int.TryParse(segments[0], out var major) ||
            !int.TryParse(segments[1], out var minor) ||
            !int.TryParse(segments[2], out var patch) ||
            major < 0 || minor < 0 || patch < 0)
        {
            throw new FormatException($"'{value}' is not a valid semantic version. Expected MAJOR.MINOR.PATCH[-pre-release].");
        }

        return new SemanticVersion(major, minor, patch, preRelease);
    }

    /// <inheritdoc/>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        var cmp = Major.CompareTo(other.Major);
        if (cmp != 0) return cmp;
        cmp = Minor.CompareTo(other.Minor);
        if (cmp != 0) return cmp;
        cmp = Patch.CompareTo(other.Patch);
        if (cmp != 0) return cmp;

        // Stable > pre-release
        if (PreRelease is null && other.PreRelease is not null) return 1;
        if (PreRelease is not null && other.PreRelease is null) return -1;
        return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public bool Equals(SemanticVersion? other) => other is not null && CompareTo(other) == 0;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    /// <summary>Returns the version string in <c>MAJOR.MINOR.PATCH[-pre-release]</c> format.</summary>
    /// <returns>The semantic version string.</returns>
    public override string ToString() =>
        PreRelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{PreRelease}";
}
```

- [ ] **Step 5: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All previous tests + 12 new tests pass.

- [ ] **Step 6: Verify build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 7: Commit**

```powershell
git add src/Ferret.Core/Primitives/ContentHash.cs src/Ferret.Core/Primitives/SemanticVersion.cs tests/Ferret.Core.Tests/Primitives/ContentHashTests.cs tests/Ferret.Core.Tests/Primitives/SemanticVersionTests.cs
git commit -m "feat(sprint-3): add ContentHash and SemanticVersion value objects"
```

---

## Task 6: Result Types

**Files:**
- Create: `src/Ferret.Core/Results/OperationResult.cs`
- Create: `src/Ferret.Core/Results/ValidationFailure.cs`
- Create: `src/Ferret.Core/Results/ValidationResult.cs`
- Create: `src/Ferret.Core/Results/DiscoveryResult.cs`
- Create: `src/Ferret.Core/Results/ParseResult.cs`
- Create: `src/Ferret.Core/Results/ReviewResult.cs`
- Create: `src/Ferret.Core/Results/IndexResult.cs`
- Create: `tests/Ferret.Core.Tests/Results/ResultTypeTests.cs`

**Interfaces:**
- Consumes: `Ferret.Core.Enumerations.ValidationSeverity`, `Ferret.Core.Enumerations.ReviewStatus` (Tasks 2)
- Produces: All result types in `Ferret.Core.Results`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Results/ResultTypeTests.cs`:

```csharp
using Ferret.Core.Enumerations;
using Ferret.Core.Results;

namespace Ferret.Core.Tests.Results;

public sealed class ResultTypeTests
{
    [Fact]
    public void OperationResult_Success_IsSuccessful()
    {
        var r = OperationResult.Success();
        Assert.True(r.IsSuccess);
        Assert.Null(r.ErrorMessage);
    }

    [Fact]
    public void OperationResult_Failure_IsNotSuccessful()
    {
        var r = OperationResult.Failure("Something went wrong.");
        Assert.False(r.IsSuccess);
        Assert.Equal("Something went wrong.", r.ErrorMessage);
    }

    [Fact]
    public void OperationResult_Generic_Success_HasValue()
    {
        var r = OperationResult<int>.Success(42);
        Assert.True(r.IsSuccess);
        Assert.Equal(42, r.Value);
    }

    [Fact]
    public void OperationResult_Generic_Failure_HasNoValue()
    {
        var r = OperationResult<int>.Failure("error");
        Assert.False(r.IsSuccess);
        Assert.Equal(default, r.Value);
    }

    [Fact]
    public void ValidationFailure_Properties_AreStored()
    {
        var f = new ValidationFailure("field", "required", "Provide a value.", ValidationSeverity.Error);
        Assert.Equal("field", f.Field);
        Assert.Equal("required", f.Constraint);
        Assert.Equal("Provide a value.", f.Guidance);
        Assert.Equal(ValidationSeverity.Error, f.Severity);
    }

    [Fact]
    public void ValidationResult_Valid_HasNoFailures()
    {
        var r = ValidationResult.Valid();
        Assert.True(r.IsValid);
        Assert.Empty(r.Failures);
    }

    [Fact]
    public void ValidationResult_Invalid_HasFailures()
    {
        var f = new ValidationFailure("name", "required", "Provide a name.", ValidationSeverity.Error);
        var r = ValidationResult.Invalid([f]);
        Assert.False(r.IsValid);
        Assert.Single(r.Failures);
    }

    [Fact]
    public void DiscoveryResult_Stores_Items()
    {
        var r = new DiscoveryResult<string>(["a", "b", "c"], true);
        Assert.Equal(3, r.Items.Count);
        Assert.True(r.IsComplete);
    }

    [Fact]
    public void ParseResult_Success_HasValue()
    {
        var r = ParseResult<int>.Success(99);
        Assert.True(r.IsSuccess);
        Assert.Equal(99, r.Value);
    }

    [Fact]
    public void ParseResult_Failure_HasMessage()
    {
        var r = ParseResult<int>.Failure("parse error");
        Assert.False(r.IsSuccess);
        Assert.Equal("parse error", r.ErrorMessage);
    }

    [Fact]
    public void ReviewResult_Stores_Status()
    {
        var r = new ReviewResult(ReviewStatus.Complete, [], "All good.");
        Assert.Equal(ReviewStatus.Complete, r.Status);
        Assert.Equal("All good.", r.Summary);
    }

    [Fact]
    public void IndexResult_Stores_Count()
    {
        var r = new IndexResult(42, 2, false);
        Assert.Equal(42, r.IndexedCount);
        Assert.Equal(2, r.FailedCount);
        Assert.False(r.IsComplete);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet build tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj 2>&1 | Select-String "error CS"
```

Expected: compile errors for missing result types.

- [ ] **Step 3: Implement OperationResult.cs**

```csharp
namespace Ferret.Core.Results;

/// <summary>Represents the outcome of an operation that produces no value.</summary>
public sealed class OperationResult
{
    private OperationResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error message when the operation failed, or <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful operation result.</summary>
    /// <returns>A successful <see cref="OperationResult"/>.</returns>
    public static OperationResult Success() => new(true, null);

    /// <summary>Creates a failed operation result with a message.</summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult"/>.</returns>
    public static OperationResult Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>Represents the outcome of an operation that produces a value of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of the produced value.</typeparam>
public sealed class OperationResult<T>
{
    private OperationResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the produced value on success, or the default of <typeparamref name="T"/> on failure.</summary>
    public T? Value { get; }

    /// <summary>Gets the error message when the operation failed, or <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful result carrying a value.</summary>
    /// <param name="value">The produced value.</param>
    /// <returns>A successful <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result with an error message.</summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
```

- [ ] **Step 4: Implement ValidationFailure.cs**

```csharp
using Ferret.Core.Enumerations;

namespace Ferret.Core.Results;

/// <summary>Describes a single validation failure for a field or constraint.</summary>
public sealed class ValidationFailure
{
    /// <summary>Initializes a new instance of <see cref="ValidationFailure"/>.</summary>
    /// <param name="field">The name of the field that failed validation.</param>
    /// <param name="constraint">The constraint that was violated.</param>
    /// <param name="guidance">Human-readable guidance for resolving the failure.</param>
    /// <param name="severity">The severity of the failure.</param>
    public ValidationFailure(string field, string constraint, string guidance, ValidationSeverity severity)
    {
        Field = field;
        Constraint = constraint;
        Guidance = guidance;
        Severity = severity;
    }

    /// <summary>Gets the name of the field that failed validation.</summary>
    public string Field { get; }

    /// <summary>Gets the constraint that was violated.</summary>
    public string Constraint { get; }

    /// <summary>Gets human-readable guidance for resolving the failure.</summary>
    public string Guidance { get; }

    /// <summary>Gets the severity of the validation failure.</summary>
    public ValidationSeverity Severity { get; }
}
```

- [ ] **Step 5: Implement ValidationResult.cs**

```csharp
namespace Ferret.Core.Results;

/// <summary>Represents the outcome of a validation operation, including all failures.</summary>
public sealed class ValidationResult
{
    private ValidationResult(bool isValid, IReadOnlyList<ValidationFailure> failures)
    {
        IsValid = isValid;
        Failures = failures;
    }

    /// <summary>Gets a value indicating whether validation passed with no errors.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the collection of validation failures, empty when valid.</summary>
    public IReadOnlyList<ValidationFailure> Failures { get; }

    /// <summary>Creates a valid result with no failures.</summary>
    /// <returns>A valid <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Valid() => new(true, []);

    /// <summary>Creates an invalid result from a list of failures.</summary>
    /// <param name="failures">The validation failures.</param>
    /// <returns>An invalid <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Invalid(IReadOnlyList<ValidationFailure> failures) =>
        new(false, failures);
}
```

- [ ] **Step 6: Implement DiscoveryResult.cs**

```csharp
namespace Ferret.Core.Results;

/// <summary>Represents the result of a discovery operation that finds items of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of items discovered.</typeparam>
public sealed class DiscoveryResult<T>
{
    /// <summary>Initializes a new instance of <see cref="DiscoveryResult{T}"/>.</summary>
    /// <param name="items">The discovered items.</param>
    /// <param name="isComplete">Indicates whether discovery is complete or was truncated.</param>
    public DiscoveryResult(IReadOnlyList<T> items, bool isComplete)
    {
        Items = items;
        IsComplete = isComplete;
    }

    /// <summary>Gets the discovered items.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets a value indicating whether discovery completed without truncation.</summary>
    public bool IsComplete { get; }
}
```

- [ ] **Step 7: Implement ParseResult.cs**

```csharp
namespace Ferret.Core.Results;

/// <summary>Represents the result of a parse operation that produces a value of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
public sealed class ParseResult<T>
{
    private ParseResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether parsing succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the parsed value on success, or the default of <typeparamref name="T"/> on failure.</summary>
    public T? Value { get; }

    /// <summary>Gets the error message when parsing failed, or <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful parse result carrying a value.</summary>
    /// <param name="value">The parsed value.</param>
    /// <returns>A successful <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed parse result with an error message.</summary>
    /// <param name="errorMessage">The error message describing the parse failure.</param>
    /// <returns>A failed <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
```

- [ ] **Step 8: Implement ReviewResult.cs**

```csharp
using Ferret.Core.Enumerations;

namespace Ferret.Core.Results;

/// <summary>Represents the outcome of a review workflow.</summary>
public sealed class ReviewResult
{
    /// <summary>Initializes a new instance of <see cref="ReviewResult"/>.</summary>
    /// <param name="status">The final status of the review.</param>
    /// <param name="findings">The findings produced by the review.</param>
    /// <param name="summary">A human-readable summary of the review outcome.</param>
    public ReviewResult(ReviewStatus status, IReadOnlyList<string> findings, string summary)
    {
        Status = status;
        Findings = findings;
        Summary = summary;
    }

    /// <summary>Gets the final status of the review.</summary>
    public ReviewStatus Status { get; }

    /// <summary>Gets the findings produced by the review.</summary>
    public IReadOnlyList<string> Findings { get; }

    /// <summary>Gets a human-readable summary of the review outcome.</summary>
    public string Summary { get; }
}
```

- [ ] **Step 9: Implement IndexResult.cs**

```csharp
namespace Ferret.Core.Results;

/// <summary>Represents the outcome of an indexing operation.</summary>
public sealed class IndexResult
{
    /// <summary>Initializes a new instance of <see cref="IndexResult"/>.</summary>
    /// <param name="indexedCount">The number of items successfully indexed.</param>
    /// <param name="failedCount">The number of items that failed to index.</param>
    /// <param name="isComplete">Indicates whether indexing completed without truncation.</param>
    public IndexResult(int indexedCount, int failedCount, bool isComplete)
    {
        IndexedCount = indexedCount;
        FailedCount = failedCount;
        IsComplete = isComplete;
    }

    /// <summary>Gets the number of items successfully indexed.</summary>
    public int IndexedCount { get; }

    /// <summary>Gets the number of items that failed to index.</summary>
    public int FailedCount { get; }

    /// <summary>Gets a value indicating whether indexing completed without truncation.</summary>
    public bool IsComplete { get; }
}
```

- [ ] **Step 10: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All previous tests + 12 new result type tests pass.

- [ ] **Step 11: Verify build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 12: Commit**

```powershell
git add src/Ferret.Core/Results/ tests/Ferret.Core.Tests/Results/
git commit -m "feat(sprint-3): add result types — OperationResult, ValidationResult, DiscoveryResult, ParseResult, ReviewResult, IndexResult"
```

---

## Task 7: Base Interfaces and HealthCheckResult

**Files:**
- Create: `src/Ferret.Core/Abstractions/IIdentifiable.cs`
- Create: `src/Ferret.Core/Abstractions/IVersioned.cs`
- Create: `src/Ferret.Core/Abstractions/IValidatable.cs`
- Create: `src/Ferret.Core/Abstractions/IInitializable.cs`
- Create: `src/Ferret.Core/Abstractions/IConfiguration.cs`
- Create: `src/Ferret.Core/Abstractions/IHealthCheck.cs`
- Create: `src/Ferret.Core/Abstractions/IMetadata.cs`
- Create: `src/Ferret.Core/Abstractions/IClock.cs`
- Create: `src/Ferret.Core/Abstractions/ICorrelationContext.cs`
- Create: `src/Ferret.Core/Abstractions/HealthCheckResult.cs`
- Create: `tests/Ferret.Core.Tests/Abstractions/HealthCheckResultTests.cs`

**Interfaces:**
- Consumes: `HealthStatus` (Task 2), `ValidationResult` (Task 6), `SemanticVersion` (Task 5), `CorrelationId` (Task 4)
- Produces: All abstraction interfaces and `HealthCheckResult`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Abstractions/HealthCheckResultTests.cs`:

```csharp
using Ferret.Core.Abstractions;
using Ferret.Core.Enumerations;

namespace Ferret.Core.Tests.Abstractions;

public sealed class HealthCheckResultTests
{
    [Fact]
    public void HealthCheckResult_Healthy_IsHealthy()
    {
        var r = HealthCheckResult.Healthy("All OK");
        Assert.Equal(HealthStatus.Healthy, r.Status);
        Assert.Equal("All OK", r.Description);
        Assert.Null(r.Exception);
    }

    [Fact]
    public void HealthCheckResult_Degraded_IsDegraded()
    {
        var r = HealthCheckResult.Degraded("Slow response");
        Assert.Equal(HealthStatus.Degraded, r.Status);
    }

    [Fact]
    public void HealthCheckResult_Unhealthy_WithException()
    {
        var ex = new InvalidOperationException("broken");
        var r = HealthCheckResult.Unhealthy("Connection failed", ex);
        Assert.Equal(HealthStatus.Unhealthy, r.Status);
        Assert.Equal(ex, r.Exception);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet build tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj 2>&1 | Select-String "error CS"
```

Expected: compile errors for missing `HealthCheckResult`.

- [ ] **Step 3: Implement IIdentifiable.cs**

```csharp
namespace Ferret.Core.Abstractions;

/// <summary>Marks an entity as having a stable string identifier.</summary>
public interface IIdentifiable
{
    /// <summary>Gets the unique identifier of this entity.</summary>
    string Id { get; }
}
```

- [ ] **Step 4: Implement IVersioned.cs**

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Abstractions;

/// <summary>Marks a component or artifact as carrying a semantic version.</summary>
public interface IVersioned
{
    /// <summary>Gets the semantic version of this component.</summary>
    SemanticVersion Version { get; }
}
```

- [ ] **Step 5: Implement IValidatable.cs**

```csharp
using Ferret.Core.Results;

namespace Ferret.Core.Abstractions;

/// <summary>Allows a type to validate its own state and return structured failures.</summary>
public interface IValidatable
{
    /// <summary>Validates the current state of this instance.</summary>
    /// <returns>A <see cref="ValidationResult"/> describing any validation failures.</returns>
    ValidationResult Validate();
}
```

- [ ] **Step 6: Implement IInitializable.cs**

```csharp
namespace Ferret.Core.Abstractions;

/// <summary>Represents a component that requires explicit asynchronous initialization before use.</summary>
public interface IInitializable
{
    /// <summary>Initializes this component asynchronously.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that represents the initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 7: Implement IConfiguration.cs**

```csharp
namespace Ferret.Core.Abstractions;

/// <summary>Provides read access to a typed configuration value.</summary>
/// <typeparam name="T">The type of the configuration value.</typeparam>
public interface IConfiguration<out T>
{
    /// <summary>Gets the configuration value.</summary>
    T Value { get; }
}
```

- [ ] **Step 8: Implement HealthCheckResult.cs**

```csharp
using Ferret.Core.Enumerations;

namespace Ferret.Core.Abstractions;

/// <summary>Represents the result of a health check evaluation.</summary>
public sealed class HealthCheckResult
{
    private HealthCheckResult(HealthStatus status, string description, Exception? exception)
    {
        Status = status;
        Description = description;
        Exception = exception;
    }

    /// <summary>Gets the health status reported by the check.</summary>
    public HealthStatus Status { get; }

    /// <summary>Gets a human-readable description of the health check outcome.</summary>
    public string Description { get; }

    /// <summary>Gets the exception that caused an unhealthy state, or <see langword="null"/>.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a healthy result.</summary>
    /// <param name="description">A description of the healthy state.</param>
    /// <returns>A healthy <see cref="HealthCheckResult"/>.</returns>
    public static HealthCheckResult Healthy(string description) =>
        new(HealthStatus.Healthy, description, null);

    /// <summary>Creates a degraded result.</summary>
    /// <param name="description">A description of the degraded state.</param>
    /// <returns>A degraded <see cref="HealthCheckResult"/>.</returns>
    public static HealthCheckResult Degraded(string description) =>
        new(HealthStatus.Degraded, description, null);

    /// <summary>Creates an unhealthy result.</summary>
    /// <param name="description">A description of the unhealthy state.</param>
    /// <param name="exception">The exception that caused the unhealthy state.</param>
    /// <returns>An unhealthy <see cref="HealthCheckResult"/>.</returns>
    public static HealthCheckResult Unhealthy(string description, Exception? exception = null) =>
        new(HealthStatus.Unhealthy, description, exception);
}
```

- [ ] **Step 9: Implement IHealthCheck.cs**

```csharp
namespace Ferret.Core.Abstractions;

/// <summary>Enables a component to report its own health status.</summary>
public interface IHealthCheck
{
    /// <summary>Checks the health of this component asynchronously.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that resolves to a <see cref="HealthCheckResult"/>.</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 10: Implement IMetadata.cs**

```csharp
namespace Ferret.Core.Abstractions;

/// <summary>Provides access to arbitrary string metadata associated with an entity.</summary>
public interface IMetadata
{
    /// <summary>Gets the metadata dictionary for this entity.</summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
```

- [ ] **Step 11: Implement IClock.cs**

```csharp
namespace Ferret.Core.Abstractions;

/// <summary>Abstracts the system clock to enable deterministic testing of time-dependent logic.</summary>
public interface IClock
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTimeOffset UtcNow { get; }
}
```

- [ ] **Step 12: Implement ICorrelationContext.cs**

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Abstractions;

/// <summary>Provides access to the correlation identifier for the current operation scope.</summary>
public interface ICorrelationContext
{
    /// <summary>Gets the correlation identifier for the current operation.</summary>
    CorrelationId CorrelationId { get; }
}
```

- [ ] **Step 13: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All previous tests + 3 new health check result tests pass.

- [ ] **Step 14: Verify build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 15: Commit**

```powershell
git add src/Ferret.Core/Abstractions/ tests/Ferret.Core.Tests/Abstractions/
git commit -m "feat(sprint-3): add base interfaces — IIdentifiable, IVersioned, IValidatable, IInitializable, IConfiguration, IHealthCheck, IMetadata, IClock, ICorrelationContext, HealthCheckResult"
```

---

## Task 8: Domain Event Infrastructure

**Files:**
- Create: `src/Ferret.Core/Events/EventMetadata.cs`
- Create: `src/Ferret.Core/Events/DomainEvent.cs`
- Create: `src/Ferret.Core/Events/IntegrationEvent.cs`
- Create: `src/Ferret.Core/Events/SystemEvent.cs`
- Create: `src/Ferret.Core/Events/EventEnvelope.cs`
- Create: `tests/Ferret.Core.Tests/Events/EventBaseTests.cs`

**Interfaces:**
- Consumes: `CorrelationId` (Task 4)
- Produces: All event base types in `Ferret.Core.Events`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Events/EventBaseTests.cs`:

```csharp
using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Events;

public sealed class EventBaseTests
{
    private sealed class TestDomainEvent : DomainEvent
    {
        public TestDomainEvent(string aggregateId, CorrelationId correlationId)
            : base(aggregateId, correlationId)
        {
        }
    }

    private sealed class TestIntegrationEvent : IntegrationEvent
    {
        public TestIntegrationEvent(string source, CorrelationId correlationId)
            : base(source, correlationId)
        {
        }
    }

    private sealed class TestSystemEvent : SystemEvent
    {
        public TestSystemEvent(string component, CorrelationId correlationId)
            : base(component, correlationId)
        {
        }
    }

    [Fact]
    public void DomainEvent_Has_EventId_And_OccurredOn()
    {
        var ev = new TestDomainEvent("agg-1", CorrelationId.Create("corr-1"));
        Assert.NotEmpty(ev.EventId);
        Assert.True(ev.OccurredOn > DateTimeOffset.MinValue);
        Assert.Equal("agg-1", ev.AggregateId);
    }

    [Fact]
    public void DomainEvent_CorrelationId_IsPreserved()
    {
        var corr = CorrelationId.Create("corr-abc");
        var ev = new TestDomainEvent("agg-1", corr);
        Assert.Equal("corr-abc", ev.CorrelationId.Value);
    }

    [Fact]
    public void IntegrationEvent_Has_Source()
    {
        var ev = new TestIntegrationEvent("module.workspace", CorrelationId.Create("c-1"));
        Assert.Equal("module.workspace", ev.Source);
        Assert.NotEmpty(ev.EventId);
    }

    [Fact]
    public void SystemEvent_Has_Component()
    {
        var ev = new TestSystemEvent("platform.boot", CorrelationId.Create("c-2"));
        Assert.Equal("platform.boot", ev.Component);
    }

    [Fact]
    public void EventEnvelope_Wraps_Event()
    {
        var ev = new TestDomainEvent("agg-1", CorrelationId.Create("c-3"));
        var envelope = new EventEnvelope(ev, "v1");
        Assert.Equal(ev, envelope.Payload);
        Assert.Equal("v1", envelope.SchemaVersion);
        Assert.NotEmpty(envelope.EnvelopeId);
    }

    [Fact]
    public void EventMetadata_Stores_Properties()
    {
        var meta = new EventMetadata("source.module", "v1");
        Assert.Equal("source.module", meta.Source);
        Assert.Equal("v1", meta.SchemaVersion);
    }
}
```

- [ ] **Step 2: Run tests to confirm red**

```powershell
dotnet build tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj 2>&1 | Select-String "error CS"
```

Expected: compile errors for missing event types.

- [ ] **Step 3: Implement EventMetadata.cs**

```csharp
namespace Ferret.Core.Events;

/// <summary>Carries source and schema information attached to an event.</summary>
public sealed class EventMetadata
{
    /// <summary>Initializes a new instance of <see cref="EventMetadata"/>.</summary>
    /// <param name="source">The module or component that emitted the event.</param>
    /// <param name="schemaVersion">The schema version of the event payload.</param>
    public EventMetadata(string source, string schemaVersion)
    {
        Source = source;
        SchemaVersion = schemaVersion;
    }

    /// <summary>Gets the module or component that emitted the event.</summary>
    public string Source { get; }

    /// <summary>Gets the schema version of the event payload.</summary>
    public string SchemaVersion { get; }
}
```

- [ ] **Step 4: Implement DomainEvent.cs**

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Events;

/// <summary>Base class for domain events that occur within an aggregate boundary.</summary>
public abstract class DomainEvent
{
    /// <summary>Initializes a new instance of <see cref="DomainEvent"/>.</summary>
    /// <param name="aggregateId">The identifier of the aggregate that raised this event.</param>
    /// <param name="correlationId">The correlation identifier for the operation that caused this event.</param>
    protected DomainEvent(string aggregateId, CorrelationId correlationId)
    {
        EventId = Guid.NewGuid().ToString("N");
        OccurredOn = DateTimeOffset.UtcNow;
        AggregateId = aggregateId;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public string EventId { get; }

    /// <summary>Gets the UTC timestamp at which this event occurred.</summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>Gets the identifier of the aggregate that raised this event.</summary>
    public string AggregateId { get; }

    /// <summary>Gets the correlation identifier for the operation that caused this event.</summary>
    public CorrelationId CorrelationId { get; }
}
```

- [ ] **Step 5: Implement IntegrationEvent.cs**

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Events;

/// <summary>Base class for integration events that cross module or service boundaries.</summary>
public abstract class IntegrationEvent
{
    /// <summary>Initializes a new instance of <see cref="IntegrationEvent"/>.</summary>
    /// <param name="source">The module or component that emitted this event.</param>
    /// <param name="correlationId">The correlation identifier for the operation that caused this event.</param>
    protected IntegrationEvent(string source, CorrelationId correlationId)
    {
        EventId = Guid.NewGuid().ToString("N");
        OccurredOn = DateTimeOffset.UtcNow;
        Source = source;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public string EventId { get; }

    /// <summary>Gets the UTC timestamp at which this event occurred.</summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>Gets the module or component that emitted this event.</summary>
    public string Source { get; }

    /// <summary>Gets the correlation identifier for the operation that caused this event.</summary>
    public CorrelationId CorrelationId { get; }
}
```

- [ ] **Step 6: Implement SystemEvent.cs**

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Events;

/// <summary>Base class for platform-level system events, such as startup and shutdown notifications.</summary>
public abstract class SystemEvent
{
    /// <summary>Initializes a new instance of <see cref="SystemEvent"/>.</summary>
    /// <param name="component">The platform component that emitted this event.</param>
    /// <param name="correlationId">The correlation identifier for the operation that caused this event.</param>
    protected SystemEvent(string component, CorrelationId correlationId)
    {
        EventId = Guid.NewGuid().ToString("N");
        OccurredOn = DateTimeOffset.UtcNow;
        Component = component;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public string EventId { get; }

    /// <summary>Gets the UTC timestamp at which this event occurred.</summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>Gets the platform component that emitted this event.</summary>
    public string Component { get; }

    /// <summary>Gets the correlation identifier for the operation that caused this event.</summary>
    public CorrelationId CorrelationId { get; }
}
```

- [ ] **Step 7: Implement EventEnvelope.cs**

```csharp
namespace Ferret.Core.Events;

/// <summary>Wraps an event payload with routing and versioning metadata.</summary>
public sealed class EventEnvelope
{
    /// <summary>Initializes a new instance of <see cref="EventEnvelope"/>.</summary>
    /// <param name="payload">The event payload to wrap.</param>
    /// <param name="schemaVersion">The schema version of the payload.</param>
    public EventEnvelope(object payload, string schemaVersion)
    {
        EnvelopeId = Guid.NewGuid().ToString("N");
        Payload = payload;
        SchemaVersion = schemaVersion;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the unique identifier of this envelope.</summary>
    public string EnvelopeId { get; }

    /// <summary>Gets the event payload.</summary>
    public object Payload { get; }

    /// <summary>Gets the schema version of the payload.</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the UTC timestamp at which this envelope was created.</summary>
    public DateTimeOffset CreatedOn { get; }
}
```

- [ ] **Step 8: Run tests to confirm green**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: All previous tests + 6 new event infrastructure tests pass.

- [ ] **Step 9: Verify full solution build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 10: Run dotnet format verification**

```powershell
dotnet format src/Ferret.sln --verify-no-changes
```

Expected: No changes reported.

- [ ] **Step 11: Commit**

```powershell
git add src/Ferret.Core/Events/ tests/Ferret.Core.Tests/Events/
git commit -m "feat(sprint-3): add event infrastructure — DomainEvent, IntegrationEvent, SystemEvent, EventEnvelope, EventMetadata"
```

---

## Task 9: Sprint Completion — Update Context and Session Files

**Files:**
- Modify: `.ai/current-context.json`
- Modify: `.ai/session.md`

**Interfaces:**
- Produces: Updated context reflecting Sprint 3 complete and In Review

- [ ] **Step 1: Final full test run**

```powershell
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj --verbosity normal
```

Expected: All tests pass. Record final test count.

- [ ] **Step 2: Final solution build**

```powershell
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Update .ai/current-context.json**

Set `sprint` to `3`, `task` to `"Sprint 3 — Platform Kernel (complete, In Review)"`, update `activeFiles` to list the new Core source files, update `recentDecisions` with Sprint 3 decisions, clear `openQuestions` and `pendingADRs`.

- [ ] **Step 4: Update .ai/session.md**

Record Sprint 3 completion: test count, 0 warnings, 0 errors, `dotnet format --verify-no-changes` passes. Set Next Steps to Sprint 4.

- [ ] **Step 5: Commit context files**

```powershell
git add .ai/current-context.json .ai/session.md
git commit -m "chore(sprint-3): mark sprint complete — In Review"
```

---

## Self-Review Checklist

_Run after plan is complete — fix inline, no re-review needed._

- [ ] Every task has complete code (no TBD, no "similar to Task N")
- [ ] Every public member in every file has `<summary>` and where applicable `<param>`, `<returns>`, `<exception>`
- [ ] All `using` directives are outside the namespace block
- [ ] No `Version` attribute in any `.csproj` changes
- [ ] No `<ProjectReference>` added to `Ferret.Core.csproj`
- [ ] All 8 typed IDs follow the same pattern (private constructor, `Create` factory, `IEquatable<T>`)
- [ ] `ValidationException` (in `Errors/`) matches SDK-001 §20 (Field, Constraint, Guidance properties)
- [ ] All workspace exceptions match ARCH-003 (7 types, all extend `FerretException`)
- [ ] `PermissionDeniedException` extends `SecurityException` per ARCH-001 §11.5
- [ ] Result types are technology-neutral (no infrastructure imports)
- [ ] Event base types carry `EventId`, `OccurredOn`, `CorrelationId` consistently
- [ ] No git tag task — tagging happens post-approval and post-Completed status only
- [ ] Missing inputs are listed (ROADMAP-001, ARCH-011–014, STD-005, Decision Register)
