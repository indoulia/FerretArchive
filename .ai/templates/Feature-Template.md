# Feature: [Feature Name]

| Field | Value |
|---|---|
| **Feature ID** | FEAT-NNN |
| **Work Item** | WI-XYYY |
| **Module** | Ferret.[Module] |
| **Namespace** | Ferret.[Module].[SubNamespace] |
| **ARCH Reference** | ARCH-NNN §section |
| **Status** | Draft / Accepted |

---

## Description

[One paragraph. What this feature does from the user's or engine's perspective. Not how it works internally.]

---

## Interface Contracts

[The public interface(s) this feature implements or extends. Reference the ARCH-NNN interface definition. Do not duplicate the interface definition — reference it.]

**Implements:** `IEngineName` (ARCH-NNN §section)

**New interface members (if any):**
```csharp
// Interface additions go here
```

---

## Inputs and Outputs

| Input | Type | Source |
|---|---|---|
| [name] | [type] | [where it comes from] |

| Output | Type | Destination |
|---|---|---|
| [name] | [type] | [where it goes] |

---

## Error Handling

| Condition | Exception | Retryable |
|---|---|---|
| [condition] | [ExceptionType] | Yes / No |

---

## Domain Events Raised

| Event | When |
|---|---|
| [EventName] | [when it is raised] |

---

## Acceptance Criteria

- [ ] [Criterion 1 — corresponds to a unit test]
- [ ] [Criterion 2]

---

## Test Plan

**Unit tests** (`tests/Ferret.[Module].Tests/`):
- `[MethodName]_[State]_[Expected]`

**Integration tests** (`tests/Ferret.Integration.Tests/`):
- [scenario name] — [what it tests end-to-end]
