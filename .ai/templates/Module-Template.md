# Module: Ferret.[ModuleName]

| Field | Value |
|---|---|
| **Module** | Ferret.[ModuleName] |
| **Namespace** | Ferret.[ModuleName] |
| **ARCH Reference** | ARCH-NNN |
| **Layer** | Core / Runtime / Configuration / Telemetry / Plugins / Cli / Mcp / Sdk |
| **Status** | Planned / In Progress / Complete |

---

## Purpose

[One sentence. What this module does and why it exists as a separate project.]

---

## Project Dependencies

| Dependency | Type | Justification |
|---|---|---|
| Ferret.Core | ProjectReference | Required — interfaces and value objects |
| [NuGet Package] | PackageReference | [why this package is needed] |

**Must not reference:** [list modules this module is explicitly forbidden from referencing per ARCH-001 §8]

---

## Public Interfaces

[List the interfaces this module exposes. Do not define them here — define them in Ferret.Core. Reference them.]

| Interface | Defined In | Purpose |
|---|---|---|
| `IEngineName` | Ferret.Core.Engines | [purpose] |

---

## Key Types

| Type | Role |
|---|---|
| `EngineNameEngine` | Primary implementation of `IEngineName` |
| `EngineNameConfig` | Configuration record for this module |

---

## Configuration

[What configuration this module reads. Reference ARCH-011 for the config layer hierarchy.]

Config path in `workspace.json`: `[path]`
Config type: `[ClassName]` in namespace `Ferret.[Module]`

---

## Domain Events

| Raises | Handles |
|---|---|
| `[EventName]` | `[EventName]` |

---

## Error Handling

[Which exception types this module raises. Reference ARCH-014 for the exception hierarchy.]

| Condition | Exception |
|---|---|
| [condition] | `[ExceptionType]` |

---

## Test Requirements

- Unit tests in `tests/Ferret.[ModuleName].Tests/`
- Minimum coverage: all public methods on all public classes
- `IClock` injected — no real `DateTimeOffset.UtcNow` in tests
- `CancellationToken` tested: happy path and cancellation path for each async method
