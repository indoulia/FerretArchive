# Deferred Scope

Items from the original Workspace Intelligence Platform brief that are genuinely out of scope for this milestone. Each entry says why, and what would have to be true before it's picked up. Nothing here is designed in detail — that's the point of deferring it.

## Enterprise Scale Beyond Current Targets

**Ask:** 100,000 repositories, millions of documents/symbols, thousands of developers.

**Status:** Partially already covered, partially deferred. ARCH-001 §25 already targets 500K+ files and 5M+ nodes *per workspace* via the pluggable `IKnowledgeStore` backend. What's genuinely new and undesigned is the **repository/workspace-count** axis — 100,000 *workspaces* in one registry is a different scaling problem (registry lookup, reference-graph traversal at that scale) than one large workspace. Not designed here because nothing in this milestone's v1 usage pattern (a team with a handful of referenced workspaces) requires it, and designing for it now would be exactly the speculative work the Founder directive rules out.

**Pick this up when:** real usage data shows workspace *count* (not size) approaching a limit the identity-based registry (ADR-0026) can't serve efficiently.

## Full Sharing / RBAC Model

**Ask:** Owner/Admin/Developer/Viewer/AI Agent roles, invitation model, conflict handling, audit history, future organization support.

**Status:** v1 ships a 4-role subset (ADR-0029). Deferred: the AI Agent role, invitation flows beyond direct user-ID grants, audit history, and organization-level sharing. This tracks directly onto FUTURE-002's existing V3 deferral of "RBAC for knowledge graph and memory access," "audit logging for AI operations," and "multi-tenant enterprise deployment" — this milestone does not reopen that boundary, it works within it.

**Pick this up when:** FUTURE-002 Q5 (organisation memory privacy model) and Q8 (is Ferret Hub a separate product) are resolved — both are prerequisites for a real audit/AI-agent/cross-org model, not just this milestone's choice to defer.

## Cost / Billing Infrastructure

**Ask:** Estimated cost saved, usage-based billing groundwork.

**Status:** v1 reports token counts, not dollars (09-Analytics.md §3). Directly blocked on FUTURE-002 Q2 (how should model cost be attributed in enterprise multi-user deployments — per-user, per-team, per-project, or Hub-managed). The Usage Ledger (10-Usage-Ledger.md) is designed so a cost model can be layered on by adding a pricing-rate rollup over existing token events — no ledger schema change needed when this unblocks.

## Cross-Organisation Sharing / Ferret Hub

**Ask:** Cloud synchronization, workspace collections at organization scale, cross-company knowledge sharing.

**Status:** Deferred to V3 per FUTURE-002 §22 ("Ferret Hub," "cross-organisation knowledge sharing... deferred indefinitely, requires further validation"). This milestone's storage design (13-Storage.md) deliberately keeps the door open — the `IWorkspaceRegistry` and `IUsageLedger` abstractions are backend-swappable specifically so a hosted registry doesn't require a schema migration when/if this is picked up — but no hosted component is built now.

## Cross-Reference Conflict Resolution

**Ask (implicit in "no duplication, references only"):** what happens when two referenced workspaces disagree about the same symbol or decision.

**Status:** v1 surfaces both, tagged by source workspace, and resolves nothing automatically (03-Cross-Workspace-References.md §5). This mirrors FUTURE-002 Q4's identical unresolved question one layer up (connector conflicts) — it's a product policy decision, not a technical gap, and doesn't block anything in this milestone's Backlog.
