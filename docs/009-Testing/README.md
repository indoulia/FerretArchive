# 009 — Testing

Test strategy, plans, and quality documentation for Ferret.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added)_ | | |

---

## Testing Philosophy

- **Test pyramid:** many unit → some integration → few E2E
- **TDD:** failing test → red → fix → green — always
- **No mocking the database** in integration tests — use real infrastructure via Docker

---

## Coverage Targets

| Layer | Line | Branch |
|---|---|---|
| Core / Domain | ≥ 90 % | ≥ 85 % |
| Application | ≥ 80 % | ≥ 75 % |
| Infrastructure | ≥ 70 % | ≥ 65 % |
| API controllers | ≥ 80 % | ≥ 75 % |

---

## Test Categories

| Category | Location | Infrastructure |
|---|---|---|
| Unit | `src/**/Tests/` | None |
| Integration | `tests/Ferret.IntegrationTests/` | Docker Compose |
| E2E | `tests/Ferret.E2ETests/` | Full stack |

---

## Template

Use [docs/templates/testing.md](../templates/testing.md) for test strategy documents.
