# Architecture Document — [Component / Feature Name]

| Field | Value |
|---|---|
| **Status** | Draft \| Review \| Accepted \| Superseded |
| **Version** | 0.1 |
| **Author** | [name] |
| **Date** | YYYY-MM-DD |
| **Last Updated** | YYYY-MM-DD |
| **Related ADRs** | ADR-XXXX |
| **Related Spec** | [spec link] |

---

## Overview

<!--
2–3 sentences. What is this component/feature and what problem does it solve?
-->

## C2 — Container Diagram

```
[Paste or embed a Mermaid C4 container diagram here]

Example:
graph LR
  A[Consumer] -->|HTTP| B(Ferret API)
  B -->|gRPC| C(Agent Runtime)
  C -->|MCP| D[External Tool]
```

## C3 — Component Diagram

<!--
Key internal components and their relationships.
-->

## Data Flow

<!--
Sequence diagram for the primary happy path.
Use Mermaid sequenceDiagram syntax.
-->

## Key Design Decisions

| Decision | Rationale | ADR |
|---|---|---|
| | | |

## Interfaces and Contracts

### Public API Surface
<!--
List public types, interfaces, or endpoints exposed by this component.
-->

### Dependencies
| Dependency | Version | Purpose |
|---|---|---|
| | | |

## Configuration

| Key | Default | Description |
|---|---|---|
| | | |

## Error Handling

<!--
How errors propagate, retry policies, circuit breakers, etc.
-->

## Observability

| Signal | What is emitted |
|---|---|
| Logs | |
| Metrics | |
| Traces | |

## Security Considerations

<!--
Authentication, authorisation, secrets management, threat model notes.
-->

## Scalability and Performance

<!--
Expected load, bottlenecks, scaling strategy.
-->

## Open Questions

| # | Question | Owner |
|---|---|---|
| 1 | | |

---

_Template version: 1.0 — stored in `/templates/architecture.md`_
