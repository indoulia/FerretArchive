# Sprint 3 Technology Evaluation

**Date:** 2026-06-27
**Sprint:** 3
**Author:** Pratap Singh
**Status:** Approved

## Summary

| Component | Decision | Package |
|---|---|---|
| SemanticVersion | Build | none |
| ContentHash | Build | none |
| Typed IDs (8x) | Build | none |
| Result Types | Build | none |
| Event Infrastructure | Build | none |
| Health Check Abstraction | Build | none |
| Exception Hierarchy | Build | none |
| Enumerations | Build | none |

## Decisions

### SemanticVersion

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | Implementation is ~70 lines — within the Build threshold. The `Semver` package (zero deps, MIT) was the only credible alternative, but adding it makes it a transitive dep for every consumer of `Ferret.Core`. Cross-assembly type agreement is required (plugins and modules pass `SemanticVersion` instances), and pre-release ordering per SemVer §11 is needed for plugin compatibility checks; both are trivially satisfied with a hand-rolled implementation at this scale. `NuGet.Versioning` rejected: pulls in Microsoft.Build transitive deps. `System.Version` rejected: four-part format, not SemVer. |
| **Package to add** | none |
| **Tasks affected** | Task 5 |

---

### ContentHash

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | `ContentHash` is a pure value object (~50 lines) — algorithm name string plus hex digest string plus structural equality. No NuGet package provides a typed hash-result value object; `System.Security.Cryptography` provides hashing algorithms but not a typed result carrier, and would require a wrapper anyway. Build is the only sensible answer. |
| **Package to add** | none |
| **Tasks affected** | Task 5 |

---

### Typed Identifiers

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | `StronglyTypedId` (compile-time source generator, `PrivateAssets="all"`, no runtime dep) was evaluated. It was ruled out for two reasons. First, the sprint plan has already committed a specific public contract for typed IDs: `sealed class` backing type, `static Create(string value)` factory that throws `ArgumentException` on null/whitespace, `IEquatable<T>`, and XML documentation on all public members. While `StronglyTypedId` v1.x does support class backing types and factory methods via templates, adopting it and confirming that its generated API exactly matches the committed contract would require a proof-of-concept — that is implementation work, not evaluation work, and is outside the scope of Task 0. Second, deviating from the committed contract (or verifying conformance) requires plan-level sign-off that Task 0 is not authorised to grant. `Vogen` was also rejected: its runtime package becomes a transitive dependency. Custom build (~200 lines, 8 files of identical pattern) is preferred. |
| **Package to add** | none |
| **Tasks affected** | Task 4 |

---

### Result Types

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | The platform requires domain-specific result shapes: `ValidationResult` carries `ValidationFailure` with Field/Constraint/Guidance fields; `DiscoveryResult` carries `IsComplete`; `ReviewResult`, `IndexResult`, and `ParseResult<T>` have Ferret-specific semantics. No ecosystem package (`FluentResults`, `ErrorOr`, `OneOf`, `LanguageExt`) provides these exact shapes. `FluentResults` and `ErrorOr` were assessed: both are zero-dep and well-maintained, but wrapping them would mean either (a) exposing their `IError` / `ISuccess` types on the public contract (Adopt, which requires ADR sign-off) or (b) hiding them behind facades that negate the benefit of using the library. Given the platform-specific shapes needed, Build is correct. |
| **Package to add** | none |
| **Tasks affected** | Task 6 |

---

### Event Infrastructure

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | `DomainEvent`, `IntegrationEvent`, `SystemEvent`, `EventEnvelope`, and `EventMetadata` are abstract base classes / value types carrying `EventId` (GUID string), `OccurredOn`, and `CorrelationId` — approximately 120 lines across 5 files. No dispatch, no bus, no serialization. `MediatR` was assessed: its `INotification` marker interface would be inappropriate to import into Core for marker types alone, and MediatR is a dispatch mechanism not a base-type library. `Dapr` and `MassTransit` are integration buses — far outside the scope of base types. ARCH-011 (telemetry contract) is a missing input listed in the plan; no approved event contract mandating a specific type was found. Build keeps Core free of infrastructure concerns. |
| **Package to add** | none |
| **Tasks affected** | Task 8 |

---

### Health Check Abstraction

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | `Microsoft.Extensions.Diagnostics.HealthChecks` was assessed: adding it (or its lighter `Abstractions` sub-package) to `Ferret.Core.csproj` pulls `Microsoft.Extensions.*` as a transitive dependency for every consumer of Core, including plugins and modules. The question of whether `Ferret.Core`'s `IHealthCheck` must be the same CLR type as `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck` was evaluated. The answer is no: the host layer (`Ferret.Runtime` or a future ASP.NET Core integration project) is the correct place for a bridge adapter. Core's `IHealthCheck` is one method (~40 lines including `HealthCheckResult`). Build keeps Core clean and dependency-free. |
| **Package to add** | none |
| **Tasks affected** | Task 7 |

---

### Exception Hierarchy

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | Domain exceptions are always project-specific. No ecosystem package provides Ferret-specific exception types (`FerretException`, `ValidationException`, `ConfigurationException`, `PlatformException`, `SecurityException`, `PermissionDeniedException`, 7 workspace exceptions). Build is the only option. |
| **Package to add** | none |
| **Tasks affected** | Task 3 |

---

### Enumerations

| | |
|---|---|
| **Decision** | Build |
| **Rationale** | Plain C# enums (`HealthStatus`, `Severity`, `ValidationSeverity`, `PluginState`, `SpecificationStatus`, `ReviewStatus`) are serialization-neutral, zero-dep, and correct for a contract-only kernel assembly. `Ardalis.SmartEnum` adds a runtime dependency and behaviour (display name, list, equality) that is overkill for pure contract enums. `Humanizer` is an infrastructure concern, not a Core type. |
| **Package to add** | none |
| **Tasks affected** | Task 2 |

---

## Outcome

All 8 components are **Build**. No NuGet packages are added to `Ferret.Core.csproj`. No changes to `Directory.Packages.props` or `src/Ferret.Core/Ferret.Core.csproj` are required. The File Structure and task code blocks in the sprint plan are unchanged. Task 1 may proceed.
