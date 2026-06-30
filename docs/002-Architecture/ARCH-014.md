# ARCH-014 — Platform Error Model

| Field | Value |
|---|---|
| **Document ID** | ARCH-014 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending Architecture Review |
| **Last Updated** | 2026-06-27 |
| **Parent Architecture** | ARCH-001 §10 — Cross-Cutting Concerns |

---

## Overview

The Platform Error Model defines the exception hierarchy used across all Ferret modules. Consistent exception types prevent every module from inventing its own error types and ensure that callers (the Application Layer, CLI entry point, and MCP handler) can handle errors predictably.

Core platform exceptions are declared in `Ferret.Core.Errors`. Workspace-domain exceptions follow the Module Ownership rule (ARCH-001 §3) and are declared in `Ferret.Core.Workspace.Errors`. Engines throw them; application layer handlers catch them and translate them into user-facing messages or exit codes.

---

## 1. Exception Hierarchy

```
FerretException                    // base for all platform exceptions
├── ValidationException             // input or state fails a validation rule
│   ├── SpecificationValidationException
│   └── WorkspaceValidationException
├── ConfigurationException          // configuration is invalid or missing
│   └── SecretResolutionException   // a ${ENV_VAR} reference could not be resolved
├── WorkspaceException              // workspace operation failed
│   ├── WorkspaceNotInitializedException
│   └── WorkspaceUpgradeException
├── IndexException                  // index pipeline failed
│   ├── IndexCorruptionException
│   └── IndexMigrationException
├── KnowledgeException              // knowledge query or context assembly failed
│   └── ContextBudgetExceededException
├── PluginException                 // plugin lifecycle or execution failed
│   ├── PluginActivationException
│   ├── PermissionDeniedException   // plugin requested a capability it did not declare
│   └── PluginContractException     // plugin violated its declared interface contract
├── SecurityException               // security policy violation
│   └── SensitiveFileViolationException
├── ReviewException                 // review lifecycle failure
│   └── ReviewGateException         // attempted to bypass the review gate
└── ArtifactException               // artefact provenance failure
    └── ProvenanceIncompleteException // artefact lacks required provenance fields
```

---

## 2. Exception Definitions

### FerretException

```
FerretException : Exception {
    ErrorCode       : string        // machine-readable error code (see §3)
    Guidance        : string        // actionable message for the developer or operator
    CorrelationId   : string?       // propagated from the triggering operation (if available)
}
```

Base class for all platform exceptions. Never throw `FerretException` directly — always throw a specific subclass.

---

### ValidationException

**Thrown by:** All engines, before committing a state transition that would violate a domain rule.

**Caught by:** Application Layer handlers — translate to structured validation error output.

```
ValidationException : FerretException {
    Field           : string        // dotted path to the field that failed (e.g. "spec.acceptanceCriteria")
    Constraint      : string        // human-readable description of the constraint
    ActualValue     : string?       // the value that was provided (redacted if sensitive)
}
```

#### SpecificationValidationException

Thrown when a specification fails completeness validation before submission for review.

```
SpecificationValidationException : ValidationException {
    SpecificationId : string
    FailedChecks    : string[]      // list of validation rule identifiers that failed
}
```

#### WorkspaceValidationException

Thrown when workspace configuration fails schema validation.

```
WorkspaceValidationException : ValidationException {
    SourceLayer     : string        // "WorkspaceConfig" | "UserConfig" | "EnvironmentVariable" | "CliFlag"
    SchemaPath      : string        // JSON Pointer path to the failing field
}
```

---

### ConfigurationException

**Thrown by:** `Ferret.Configuration` during startup configuration load.

**Caught by:** Composition root — terminates startup with a structured error diagnostic.

```
ConfigurationException : FerretException {
    SourceLayer     : string        // layer where the invalid value was found
    FieldPath       : string        // dotted field path
}
```

#### SecretResolutionException

Thrown when a `"${ENV_VAR}"` reference cannot be resolved.

```
SecretResolutionException : ConfigurationException {
    ReferenceName   : string        // the environment variable name that was not found
    FieldPath       : string        // the configuration field containing the unresolved reference
}
```

---

### WorkspaceException

**Namespace:** `Ferret.Core.Workspace.Errors` (Module Ownership rule — ARCH-001 §3)

**Thrown by:** Workspace Engine.

**Caught by:** CLI command handlers — translate to workspace error messages with remediation steps.

```
WorkspaceException : FerretException {
    WorkspaceRoot   : string?       // path to the workspace root (if determined)
}
```

Concrete subtypes implemented in Sprint 4: `WorkspaceNotFoundException`, `WorkspaceAlreadyExistsException`, `WorkspaceConfigurationException`, `WorkspaceSchemaVersionException`, `WorkspaceUpgradeRequiredException`, `WorkspaceUpgradeFailedException`, `WorkspacePathTraversalException`.

#### WorkspaceNotFoundException

Thrown when a workspace operation is attempted on a directory that has not been initialised with `Ferret init`, or a workspace cannot be located from the given path.

#### WorkspaceUpgradeFailedException

Thrown when a schema migration step fails. Includes the migration step identifier and the underlying cause.

---

### IndexException

**Thrown by:** Index Engine.

**Caught by:** Application Layer index handlers — log, report, and (where possible) continue.

#### IndexCorruptionException

Thrown when the index is detected to be in an inconsistent state that cannot be repaired incrementally. Resolution: `Ferret index build --full`.

#### IndexMigrationException

Thrown when an index schema migration fails during `Ferret workspace upgrade`.

---

### KnowledgeException

**Thrown by:** Knowledge Engine.

**Caught by:** Application Layer knowledge handlers and MCP tool handlers.

#### ContextBudgetExceededException

Thrown when a context assembly request cannot fit any useful content within the requested token budget. Callers should increase the budget or narrow the query scope.

```
ContextBudgetExceededException : KnowledgeException {
    RequestedBudget : int
    MinimumRequired : int           // minimum tokens needed for the smallest valid context
}
```

---

### PluginException

**Thrown by:** Plugin Host (`Ferret.Plugins`).

**Caught by:** Plugin Host itself for `PluginActivationException` (deactivates the plugin); Application Layer for others.

#### PluginActivationException

Thrown when a plugin's activation entry point throws. The plugin transitions to `Rejected` state.

```
PluginActivationException : PluginException {
    PluginId        : string
    PluginVersion   : string
    InnerException  : Exception     // the exception from the plugin's entry point
}
```

#### PermissionDeniedException

Thrown when a plugin calls an `IPluginContext` method for a capability it did not declare in its manifest.

```
PermissionDeniedException : PluginException {
    PluginId            : string
    RequestedPermission : string    // the permission namespace that was denied
    Operation           : string    // the specific operation that was blocked
}
```

#### PluginContractException

Thrown when a plugin's return value violates the interface contract (e.g., null where non-null is required, or a type mismatch).

---

### SecurityException

**Thrown by:** Any engine that enforces a security policy.

**Caught by:** Application Layer — surfaced as a security error with no sensitive detail leaked to the caller.

#### SensitiveFileViolationException

Thrown when a file matching a sensitive exclusion pattern is detected in a context that would expose its content (e.g., a parser result that escaped the exclusion guard).

---

### ReviewException

**Thrown by:** Review Engine.

#### ReviewGateException

Thrown when the Artifact Engine detects an attempt to mark an artefact as committed without a completed review record. This is the structural enforcement of AG-009.

---

### ArtifactException

**Thrown by:** Artifact Engine.

#### ProvenanceIncompleteException

Thrown when an artefact record is missing one or more required provenance fields (model ID, user ID, knowledge state hash, interaction ID).

---

## 3. Error Codes

Every `FerretException` carries a machine-readable `ErrorCode` string. Error codes are stable within a major version and are used by CI pipelines and tooling to identify specific failure modes.

| Error Code | Exception Type | Description |
|---|---|---|
| `AISP-001` | `WorkspaceNotFoundException` | Workspace not found or not initialised |
| `AISP-002` | `WorkspaceUpgradeFailedException` | Schema migration failed |
| `AISP-003` | `ConfigurationException` | Configuration invalid |
| `AISP-004` | `SecretResolutionException` | Unresolved secret reference |
| `AISP-005` | `ValidationException` | Input validation failed |
| `AISP-006` | `SpecificationValidationException` | Specification completeness check failed |
| `AISP-007` | `IndexCorruptionException` | Index corruption detected |
| `AISP-008` | `IndexMigrationException` | Index migration failed |
| `AISP-009` | `PermissionDeniedException` | Plugin permission denied |
| `AISP-010` | `PluginActivationException` | Plugin activation failed |
| `AISP-011` | `PluginContractException` | Plugin contract violated |
| `AISP-012` | `ReviewGateException` | Review gate enforced |
| `AISP-013` | `ContextBudgetExceededException` | Context budget insufficient |
| `AISP-014` | `SensitiveFileViolationException` | Sensitive file exclusion violation |
| `AISP-015` | `ProvenanceIncompleteException` | Artefact provenance incomplete |

---

## 4. Exception Propagation Rules

1. **Engines throw; handlers translate.** An engine throws a specific exception. The Application Layer handler catches it and translates it to a user-facing message or structured error response. The engine never formats user-facing messages.

2. **Platform exceptions only.** Engines do not let infrastructure exceptions (I/O, network, JSON parse errors) propagate to the Application Layer. Infrastructure exceptions are caught, wrapped in the appropriate `FerretException` subclass, and then thrown.

3. **Plugin exceptions are isolated.** A plugin exception is caught by the Plugin Host. If the plugin is in active operation, it transitions to `Failed` and the `PluginFailed` event is raised (see ARCH-013 §2.5). The exception does not propagate to the engine that invoked the plugin.

4. **Log before re-throw.** Every catch-and-rethrow in an engine logs the original exception at Error level before wrapping. This preserves the full stack trace in telemetry.

5. **No information leakage.** Exception messages must not include file contents, credential values, or user data. `SensitiveFileViolationException` includes only the file path pattern that was violated, not the content.

---

## 5. Design Rationale

A shared exception hierarchy prevents the common anti-pattern where every module defines its own `MyModuleException` base class that the Application Layer cannot handle generically. The hierarchy in §1 is intentionally narrow — the Application Layer handles six to eight distinct exception types and does not need to know about every possible failure mode in every engine.

**Why in `Ferret.Core`?** The exception types must be in Core because engines (which depend only on Core) need to throw them, and the Application Layer (which depends on Runtime and Core) needs to catch them. Placing them in Runtime would prevent Core-only tests from using them.

**Trade-offs:** A global exception hierarchy means it must be forward-compatible. New exception types can be added freely within a major version. Removing or renaming exception types in a minor version would break catch blocks in plugins.

---

## Traceability

| Input Document | Role |
|---|---|
| ARCH-001 §10 | Cross-cutting concerns that place exception handling in the Application Layer |
| ARCH-001 §11.5 | Plugin permission model — source of `PermissionDeniedException` |
| ARCH-001 §9 | AG-009 (Human Review Cannot Be Bypassed) — source of `ReviewGateException` |
| PRINCIPLES-001 §14 | Simplicity — drives keeping the hierarchy narrow |
