# Reviews

Architecture reviews, technical reviews, and audit records for Ferret.

---

## Index

| ID | Title | Type | Date | Status |
|---|---|---|---|---|
| [AR-001](AR-001.md) | Sprint 0 Repository Foundation Review | Architecture Review | 2026-06-27 | Accepted |
| [AGR-001](AGR-001.md) | Architecture Governance Review: Ferret V2 Foundation Series | Architecture Governance Review | 2026-07-03 | Accepted |
| [AGR-002](AGR-002.md) | Architecture Amendment Governance Review: ARCH-028 | Architecture Governance Review | 2026-07-03 | Accepted |
| [AGR-003](AGR-003.md) | Architecture Amendment Governance Review: ARCH-029 | Architecture Governance Review | 2026-07-03 | Accepted |
| [AGR-004](AGR-004.md) | Architecture Amendment Governance Review: ARCH-030 | Architecture Governance Review | 2026-07-03 | Accepted |

---

## Review Types

| Code | Type | Description |
|---|---|---|
| `AR-` | Architecture Review | Evaluates design decisions, structure, and patterns |
| `AGR-` | Architecture Governance Review | Governance checkpoint for a series of architecture documents treated as one system — records cross-document findings, mandatory corrections, deferred questions, and closed decisions; does not redesign |
| `SR-` | Security Review | Evaluates security posture and threat exposure |
| `PR-` | Performance Review | Evaluates performance characteristics and SLOs |
| `DR-` | Dependency Review | Evaluates third-party dependency choices |

---

## Process

1. Author drafts the review document using the relevant section of [docs/templates/architecture.md](../templates/architecture.md).
2. Reviewers add findings inline or as comments on the PR.
3. Author resolves or escalates each finding.
4. Review is marked **Accepted** when all critical/high findings are resolved.
