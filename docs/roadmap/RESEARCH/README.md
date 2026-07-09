# Research Candidates

Open questions that require investigation before they can become a [Proposal](../PROPOSALS/README.md). Nothing here is resolved by this document — recording the question is the point.

## Open Research Items

**Semantic retrieval feasibility.** Whether similarity-based retrieval can improve Context Organization/Assembly without crossing into reasoning, and at what token-cost trade-off. Must be investigated without adopting `FUTURE-002`'s Model Platform framing, which assumes embedded AI — see the [guardrail](../THEMES/Context-Intelligence.md#guardrail).

**Workspace-count scaling.** 100,000+ workspaces in one registry is a different scaling problem than one large workspace — ARCH-001 already targets 500K+ files *per workspace*. Not worth designing until real usage data shows workspace *count* (not size) approaching a limit the identity-based registry can serve efficiently.

**Cross-organization identity and trust model.** What the workspace-reference model means when the referenced workspace is outside the referencing organization's control — a prerequisite for [FUTURE/V4.md](../Future/V4.md)'s cross-organization federation.

**FEP-001 / historical-docs reconciliation.** Whether the broader "AI Workspace OS" scope in `docs/000-Overview/` and `FUTURE-002` is permanently AEF's, latent for a future Ferret generation, or needs an explicit ADR. Carried from [FEP-001 §9 Q2](../../FEP/FEP-001-Product-Architecture.md) — this is a governance question, not a product-strategy one, but it blocks confidently roadmapping anything that touches reasoning-adjacent capability.

**Engineering-relevant source boundary.** Whether "any engineering-relevant source" (FEP-001 Goal G1) needs an explicit, bounded taxonomy before Enterprise Knowledge acquisition work (Slack, Teams, Confluence, email) is scoped, or should stay open-ended. Carried from [FEP-001 §9 Q6](../../FEP/FEP-001-Product-Architecture.md).

## How a Research Item Moves Forward

A research item becomes a Proposal once it has enough of an answer to describe a candidate shape — not a design, just enough to state what the idea would concretely be.
