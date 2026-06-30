# ADR-0018 — Application Layer Reserved (Ferret.Application)

**Status:** Reserved  
**Date:** 2026-06-28

## Context

Multiple hosts (MCP, REST, future UI) may need shared orchestration — context assembly, cross-service queries, reusable platform concerns above individual service boundaries.

## Decision

`Ferret.Application` namespace is reserved. It will be introduced when a reusable platform concern is identified that multiple hosts need and that cannot be placed in an existing platform service.

**Trigger for introduction:** A feature or behavior is needed by ≥2 independent hosts and does not fit `Ferret.Core`, `Ferret.Search`, or any existing platform package.

## Consequences

- Sprint 11 MCP tools call platform services directly (no application layer).
- Premature introduction would add a layer with no distinct responsibility.
- This ADR is superseded when `Ferret.Application` is created.
