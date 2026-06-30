# Test Strategy — [Component / Feature Name]

| Field | Value |
|---|---|
| **Status** | Draft \| Review \| Accepted |
| **Author** | [name] |
| **Date** | YYYY-MM-DD |
| **Related Spec** | [spec link] |

---

## Overview

<!--
What is being tested? What risks are being mitigated?
-->

## Test Pyramid

```
        /\
       /E2E\         Few — full system, slow
      /------\
     /  Integ  \     Some — real I/O, moderate speed
    /------------\
   /    Unit      \  Many — isolated, fast
  /__________________\
```

## Coverage Targets

| Layer | Line Coverage | Branch Coverage |
|---|---|---|
| Core / Domain | ≥ 90 % | ≥ 85 % |
| Application | ≥ 80 % | ≥ 75 % |
| Infrastructure | ≥ 70 % | ≥ 65 % |
| API controllers | ≥ 80 % | ≥ 75 % |

## Test Scenarios

### Happy Path

| ID | Scenario | Layer | Automated |
|---|---|---|---|
| T-001 | | Unit | Yes |

### Edge Cases

| ID | Scenario | Layer | Automated |
|---|---|---|---|
| T-100 | | Unit | Yes |

### Error / Failure Cases

| ID | Scenario | Expected Behaviour | Layer |
|---|---|---|---|
| T-200 | | | Unit |

## Test Data Strategy

<!--
Seed data, test fixtures, factories, snapshot testing approach.
-->

## Non-Functional Tests

| Type | Tool | Threshold | Frequency |
|---|---|---|---|
| Performance | k6 | P99 < 500 ms @ 100 RPS | Per release |
| Load | k6 | No errors @ 500 RPS for 5 min | Per release |
| Security | CodeQL + OWASP ZAP | 0 High findings | Per PR |

## Test Environment

| Environment | Purpose | Infrastructure |
|---|---|---|
| Local | Developer inner loop | Docker Compose |
| CI | PR validation | GitHub Actions |
| Staging | Pre-release validation | Kubernetes |

---

_Template version: 1.0 — stored in `/templates/testing.md`_
