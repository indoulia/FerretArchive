# 004 — Database

Data model, schema, and migration documentation for Ferret.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added)_ | | |

---

## Conventions

- Schemas documented using [docs/templates/database.md](../templates/database.md)
- Entity-relationship diagrams use Mermaid `erDiagram` syntax
- All schema changes delivered as migrations — no manual DDL in production
- Column naming: `snake_case` — Table naming: `snake_case`, plural
- Every table has `id` (UUID PK), `created_at`, and `updated_at` columns

---

## Storage Backend

The storage backend has not been decided yet.  
See ADR backlog — an ADR will be written in Sprint 1 to record the choice.
