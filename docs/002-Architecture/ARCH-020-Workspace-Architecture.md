# ARCH-020 — Workspace Architecture

> **Status:** Reserved — not yet authored  
> **Sprint:** 7+  
> **Owner:** TBD

## Purpose

Document the workspace lifecycle, on-disk layout, and runtime contracts that govern how Ferret creates, locates, loads, and validates workspace state.

## Topics to cover

- `.ferret/` directory layout and artifact responsibilities (`workspace.json`, `state.json`, `connectors/`, `indexes/`, `memory/`, `snapshots/`, `config/`)
- `WorkspacePath` → `WorkspaceContext` lifecycle (init → locate → load → health)
- `IWorkspaceEngine`, `IWorkspaceLocator` contracts and extension points
- Workspace versioning and upgrade path
- Error model: corrupted state detection, recovery guidance
- CLI surface: `workspace init`, `workspace status` command wiring

## Related

- ARCH-019 — Connector Platform Architecture
- ADR-0013 — Capability-Based Platform Architecture
