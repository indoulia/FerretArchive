# FEP-002-CAP-06 — Context Delivery

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-06 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.6 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

An assembled answer that never reaches its consumer, or that reaches them altered, has failed regardless of how well it was assembled. Context Delivery exists to get assembled context into the hands of the consumer that asked for it, through a surface that fits how that consumer actually receives things, without changing what was assembled.

## 2. Responsibilities

- Present assembled context to the requesting consumer through a surface appropriate to that consumer's shape of interaction — human-facing, tool-facing, or agent-facing, described conceptually.
- Preserve the substance and completeness indications of the assembled context through to the consumer.
- Support consumers being notified of context relevant to a standing interest they have expressed, not only responding to one-off requests.
- Respect Access Control & Policy's determination of what a given consumer may receive.

## 3. Non-Responsibilities

- Must never decide what context to include — that belongs entirely to Context Assembly.
- Must never reason about, act on, or modify the context it delivers.
- Must never grant access beyond what Access Control & Policy has determined.
- Must never treat delivery to one consumer type as more complete or authoritative than delivery of the same assembled result to another.

## 4. Inputs

- Assembled context and its completeness indications from Context Assembly.
- The requesting consumer's declared shape of interaction — for example, wanting a direct answer, a standing subscription, or a browsable view.
- Access permission from Access Control & Policy.

## 5. Outputs

- Delivered context that faithfully represents what Assembly produced, in a form fit for the receiving consumer.
- Delivery outcome facts — delivered, partially delivered, or denied — for Provenance & Attribution and Observability & Health.

## 6. Context Objects

- **Delivery Surface** — a conceptual channel through which a consumer receives context: the concept of how this consumer receives things, not a protocol.
- **Delivery Record** — the conceptual fact that a specific assembled result was, or was not, delivered to a specific consumer, and when.
- **Subscription** — a conceptual standing interest a consumer has expressed, entitling them to notification of relevant new or changed context.

## 7. Relationships

Consumes from Context Assembly. Enforces determinations from Access Control & Policy. Reports to Provenance & Attribution and Observability & Health. Is the point at which Extensibility's "new consumer" extension point attaches.

## 8. Constraints

- **Business.** Delivery must not create a de facto privileged consumer by virtue of a richer delivery surface being built for it first, per Product Principle P4 — the underlying content available to any permitted consumer must be equivalent regardless of surface.
- **Product.** Delivery must preserve fidelity — what reaches the consumer must be what Assembly produced, not a lossy or reinterpreted version of it.
- **Context integrity.** Denials and partial deliveries must be distinguishable from the simple absence of relevant context, so a consumer is not misled into thinking nothing existed when something existed but was restricted.

## 9. Success Criteria

- A consumer receives what was assembled for them, faithfully, via a fitting surface.
- Access restrictions are enforced without exception, and are distinguishable from "no relevant context existed."
- New consumer shapes can be supported without altering what Assembly produces.

## 10. Failure Modes

- **Fidelity loss** — delivery reformats or reinterprets assembled context in a way that changes its meaning.
- **Access leakage** — a consumer receives context Access Control & Policy had not permitted, due to a gap between Assembly and Delivery.
- **Ambiguous denial** — a consumer cannot tell whether they were denied context or whether none existed, undermining trust in the whole system, in tension with Product Principle P5.
- **De facto privileged consumer** — one consumer's delivery surface becomes so much richer than others' that it functionally receives a better product, contradicting P4.

## 11. Future Evolution

Growth in the diversity of delivery surfaces as new classes of human, agent, and tool consumers emerge. Maturation of subscription-based delivery alongside one-off request/response delivery. Delivery patterns spanning federated workspaces, once Federation matures, while preserving the same fidelity and access guarantees within each workspace.
