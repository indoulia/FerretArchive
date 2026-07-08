# Ferret Product Roadmap (FPR)

| Field | Value |
|---|---|
| **Status** | Living document |
| **Last Updated** | 2026-07-08 |
| **Authority** | Product strategy only — does not amend, supersede, or extend [FEP](../FEP/README.md) |

This document is the authoritative source for Ferret's future product direction. It exists outside the Ferret Engineering Program (FEP), which is complete and frozen at v1.0. Nothing here is an engineering commitment; it becomes one only when promoted through FEP vNext (see [README.md](README.md#promotion-lifecycle)).

Ferret's mission does not change across everything below: **a Context Operating System** — infrastructure that continuously acquires, organizes, maintains, assembles, and delivers trusted engineering context for humans, AI systems, and enterprise tools. It does not reason over that context, generate artefacts from it, or host AI model inference ([FEP-001 §1.3, §5.2](../FEP/FEP-001-Product-Architecture.md)).

---

## 1. Current Release

**Engineering Program:** FEP v1.0 — complete and frozen. Defines Ferret's product architecture as eleven capabilities across four groups (Foundation, Context Supply Chain, Trust, Platform) plus a Scale capability (Federation), and a five-generation maturity model (Generation 0–4). See [FEP-001](../FEP/FEP-001-Product-Architecture.md) and [FEP-002](../FEP/FEP-002-Capability-Catalog.md). Full detail: [CURRENT/CURRENT.md](CURRENT/CURRENT.md).

**Shipped product (separate track, pre-dates FEP):** v0.16.0 — Enterprise Content Pack 1. 7 parser formats, 79 recognized extensions, first OIDC-based npm publish. DOGFOOD-001 (real-repo dogfooding) in progress on the `dogfooding` branch.

**Strengths:**
- Capability boundaries are reviewed and non-overlapping (FEP-002's own review checklist).
- Provenance and access control are modeled as cross-cutting obligations, not bolted-on features.
- A zero-dependency Core is an enforced architectural invariant, not an aspiration.
- Real parser/connector breadth already shipped and dogfooded against external repos.

**Known limitations:**
- FEP is planning-only; execution is gated on AEF reaching General Availability ([FEP README](../FEP/README.md)).
- The workspace boundary is still single-repo in the shipped product; multi-workspace federation is approved but not yet released (see [NEXT/V2.md](NEXT/V2.md)).
- Full enterprise RBAC, audit logging, and cost/billing infrastructure are explicitly out of scope for the current and next milestone (see [§8](#8-deferred-ideas)).
- Two product narratives — FEP-001's Context OS and the historical/`FUTURE-002` broader "AI Workspace OS" — remain unreconciled ([FEP-001 §9 Q2](../FEP/FEP-001-Product-Architecture.md)).

---

## 2. Product Vision Evolution

Ferret's generational arc, expressed at the product level (see [FEP-001 §7](../FEP/FEP-001-Product-Architecture.md) for the underlying capability-maturity model this tracks):

- **Today → v2 (Workspace Intelligence).** The workspace boundary generalizes past "one repository." A workspace becomes a queryable unit that can reference other workspaces as read-only dependencies — the same relationship a package has to its dependencies. Repository boundaries become an implementation detail invisible to whoever is asking a question.
- **v3 (Trust & Governance).** Context stops being merely available and becomes *defensible*. Every consumer can see how trustworthy a specific answer is, not just assume it. Access control matures from "who can see this workspace" to "who can see this specific unit of context," with the audit trail to prove it.
- **v4 (Federation at Scale).** The same reference model that connects repositories within one organization extends across organizational boundaries — a vendor's public workspace, a partner's shared workspace, an open-source dependency's workspace — without any of them re-indexing the others' content.
- **Long-Term (Ecosystem).** The boundary of who can *extend* Ferret opens. New source types, new structure types, and new consumer types are added by third parties through a genuine extension surface, not exclusively by Ferret's own team.

Through every generation, the product boundary in [FEP-001 §5](../FEP/FEP-001-Product-Architecture.md) holds: Ferret gets better at acquiring, organizing, maintaining, assembling, and delivering context. It does not, at any generation, start reasoning over that context, generating artefacts, or becoming a system of record for what it observes.

---

## 3. Future Capability Evolution

Each item below is intentionally outside FEP v1 and not yet approved. None of these are engineered here.

**Multi-Source Enterprise Acquisition** — connectors for chat/collaboration platforms (Slack, Teams), wikis (Confluence), and email as Context Acquisition sources. *Business value:* removes the last major class of enterprise knowledge Ferret can't see. *User value:* answers that today require asking a colleague become queryable. *Relationship:* pure extension of Context Acquisition + Extensibility (FEP-002-CAP-02, CAP-09); no new capability required.

**Cross-Workspace Federation** — a workspace referencing other workspaces as read-only dependencies (already approved, see [NEXT/V2.md](NEXT/V2.md)). *Business value:* removes the duplication teams currently do to work around the single-repo boundary. *User value:* one query answers questions that today require knowing which of five repositories holds the answer. *Relationship:* directly implements FEP-002-CAP-11 Federation.

**Context Health & Trust Surfacing** — a delivery surface showing freshness, provenance completeness, and coverage gaps for a workspace, not just the answers themselves. *Business value:* makes "can I trust this answer" answerable before an incident, not after. *User value:* a consumer decides how much weight to give an answer without reading raw provenance records. *Relationship:* elevates Provenance & Attribution (CAP-07) and Observability & Health (CAP-10) from internal record-keeping to a first-class delivery surface.

**Enterprise Access Governance** — full RBAC (including an AI-agent principal type), SSO/identity-provider integration, and audit logging of access decisions. *Business value:* the compliance and procurement gate most enterprise deployments require before adoption. *User value:* administrators can answer "who accessed what" without instrumenting anything themselves. *Relationship:* matures Access Control & Policy (CAP-08) to Generation 2 per FEP-001 §7; already scoped as a deferred item ([Deferred-Scope.md](Future/Deferred-Scope.md)).

**Usage & Cost Analytics** — dashboards over the usage ledger: what's queried, by whom, how fresh, at what token cost. *Business value:* the data enterprises need to justify or optimize spend on context infrastructure. *User value:* a team lead sees where context gaps are actually costing time. *Relationship:* extends Observability & Health (CAP-10); builds on the Usage Ledger already designed for v2.0.

**Cross-Organization Federation ("Ferret Hub")** — the Federation capability extended past a single organization's registry, enabling a vendor's or partner's workspace to be referenced the same way an internal one is. *Business value:* opens federation network effects beyond one company's repositories. *User value:* a dependency's ADRs and docs become queryable the same way an internal shared library's are. *Relationship:* Generation 3 maturity of Federation (CAP-11); explicitly deferred from v2.0 ([Deferred-Scope.md](Future/Deferred-Scope.md)).

**Extension Marketplace** — a public registry of third-party source connectors, structure extractors, and delivery integrations. *Business value:* acquisition and delivery breadth grows without being bottlenecked on Ferret's own engineering capacity. *User value:* a niche source type (an internal ticketing system, a proprietary CAD format) gets a connector without waiting on Ferret's roadmap. *Relationship:* is FEP-001's Generation 4 — Extensibility (CAP-09) matured into "a genuine third-party extension surface."

**Subscription-Based Context Delivery** — consumers (human or system) subscribe to be notified when context relevant to them changes, rather than only pulling on request. *Business value:* turns Ferret from a lookup tool into ambient infrastructure teams build workflows on top of. *User value:* "tell me when the ADR I depend on changes" becomes possible without polling. *Relationship:* already anticipated by FEP-001 §6 (Consumer systems may subscribe); extends Context Delivery (CAP-06).

**Workspace Experience Surfaces** — an IDE-native delivery surface (editor extension) and a web dashboard, alongside the existing CLI/MCP surfaces. *Business value:* broadens the addressable set of consumers who'll actually use Ferret day-to-day. *User value:* context appears where the user already works, honoring Product Principle P4 (no privileged consumer) by adding surfaces rather than replacing the CLI. *Relationship:* Context Delivery (CAP-06) consumer-neutrality, extended to new surfaces.

---

## 4. Version Roadmap

| Generation | Theme | Status |
|---|---|---|
| **Ferret v2 — Workspace Intelligence Platform** | Multi-repository workspaces, cross-workspace reference (Federation, org-local) | Approved, in execution — see [NEXT/V2.md](NEXT/V2.md) |
| **Ferret v3 — Trust & Enterprise Governance** | Complete Provenance & Attribution, full Access Control & Policy maturity, cost/usage analytics | Roadmapped, not approved — see [FUTURE/V3.md](FUTURE/V3.md) |
| **Ferret v4 — Federation at Scale & Ecosystem Entry** | Cross-organization Federation, first Extensibility marketplace steps | Roadmapped, not approved — see [FUTURE/V4.md](FUTURE/V4.md) |
| **Long-Term — Ambient Context OS** | Full third-party ecosystem, subscription-driven delivery, ubiquitous workspace surfaces | Directional only — see [FUTURE/LONG-TERM.md](FUTURE/LONG-TERM.md) |

No dates or effort estimates are assigned to any generation, by design.

---

## 5. Strategic Themes

| Theme | Groups future capabilities around |
|---|---|
| [Context Intelligence](THEMES/Context-Intelligence.md) | Better organization, structuring, and retrieval of context (short of reasoning over it) |
| [Enterprise Knowledge](THEMES/Enterprise-Knowledge.md) | Broader acquisition — new source types, especially enterprise collaboration platforms |
| [Workspace Experience](THEMES/Workspace-Experience.md) | New delivery surfaces and consumer-facing ergonomics |
| [Federation](THEMES/Federation.md) | Cross-workspace and cross-organization composition |
| [Analytics](THEMES/Analytics.md) | Observability, usage, and cost visibility |
| [Collaboration](THEMES/Collaboration.md) | Multi-user and subscription-based context sharing |
| [Ecosystem](THEMES/Ecosystem.md) | Third-party extensibility and marketplace |

---

## 6. Research Candidates

- **Semantic retrieval feasibility.** Whether similarity-based retrieval can improve Context Organization/Assembly without crossing into reasoning, and at what token-cost trade-off. Must be resolved without adopting `FUTURE-002`'s Model Platform framing, which assumes embedded AI.
- **Workspace-count scaling.** 100,000+ workspaces in one registry is a different scaling problem than one large workspace (ARCH-001 already targets 500K+ files *per workspace*). Not designed until real usage data shows workspace *count* approaching a limit ([Deferred-Scope.md](Future/Deferred-Scope.md)).
- **Cross-organization identity and trust model.** What "the same workspace-reference model" means when the referenced workspace is outside the referencing organization's control.
- **FEP-001 / historical-docs reconciliation.** Whether the broader "AI Workspace OS" scope in `docs/000-Overview/` and `FUTURE-002` is permanently AEF's, latent for a future Ferret generation, or needs an explicit ADR ([FEP-001 §9 Q2](../FEP/FEP-001-Product-Architecture.md)).
- **Engineering-relevant source boundary.** Whether "any engineering-relevant source" (FEP-001 G1) needs an explicit, bounded taxonomy, or should stay open-ended ([FEP-001 §9 Q6](../FEP/FEP-001-Product-Architecture.md)).

Detail: [RESEARCH/](RESEARCH/)

---

## 7. Product Proposals

Unapproved ideas that may become future capabilities:

- **Ferret Hub** — a hosted service for cross-organization workspace sharing (name and shape carried over from `FUTURE-002`'s deferred concept, decoupled from that document's embedded-AI framing).
- **Context Health Dashboard** — a standalone delivery surface built on the Context Health & Trust Surfacing capability (§3).
- **IDE-native delivery surface** — VS Code / JetBrains extension surfacing context inline.
- **Workspace templates** — pre-configured scope/connector bundles for common organizational shapes (a service repo + its shared libraries + its infra repo, for example).
- **Change-subscription notifications** — Slack/Teams/email delivery when subscribed context changes, building on Subscription-Based Context Delivery (§3).

Detail: [PROPOSALS/](PROPOSALS/)

---

## 8. Deferred Ideas

Explicitly excluded from FEP v1 and the v2.0 milestone, carried forward from [`Future/Deferred-Scope.md`](Future/Deferred-Scope.md):

| Idea | Why deferred |
|---|---|
| Enterprise scale beyond current targets (100K workspaces, thousands of developers) | Registry/traversal at that workspace-*count* scale is a different problem than the per-workspace scale ARCH-001 already targets; no usage data yet justifies designing for it |
| Full sharing / RBAC model (AI Agent role, invitations, audit history, org-level sharing) | v1/v2.0 ships a 4-role subset (ADR-0029); the rest depends on unresolved `FUTURE-002` questions (org memory privacy, whether Ferret Hub is a separate product) |
| Cost / billing infrastructure | Blocked on `FUTURE-002` Q2 (how cost is attributed in multi-user deployments); the Usage Ledger is deliberately designed to accept a cost model later without a schema change |
| Cross-organization sharing / Ferret Hub | Deferred to V3+ per `FUTURE-002` §22; storage abstractions (`IWorkspaceRegistry`, `IUsageLedger`) are kept backend-swappable so this doesn't require migration when picked up |
| Cross-reference conflict resolution | v2.0 surfaces conflicting cross-workspace content without resolving it automatically; this is a product policy decision, not a technical gap |

Additionally, and permanently (not merely deferred) out of scope per FEP-001's frozen Non-Goals: reasoning over context, generating engineering artefacts, enforcing engineering process, executing changes to source systems, hosting AI model inference, and establishing identity. These are not future roadmap items — they are the product boundary itself.

---

## 9. Product Risks

- **Two concurrent, unreconciled product narratives.** `FUTURE-002` describes an embedded-AI "Enterprise Intelligence Platform" that conflicts with FEP-001's frozen Non-Goals. Every future roadmap decision risks silently re-opening this instead of resolving it explicitly (FEP-001 Risk 1, §9 Q2).
- **Scope regression toward reasoning/generation.** The historical pull toward a broader "AI Workspace OS" is real and already documented; maintaining the Context OS boundary requires active discipline, not a one-time decision.
- **Federation governance lagging federation capability.** Extending Federation across organizations (v4) raises identity, trust, and policy questions that don't exist at org-local scale; building the capability ahead of the governance model risks a retrofit.
- **Enterprise trust expectations outpacing Access Control maturity.** Enterprises evaluating v2.0's 4-role model may expect v3's full RBAC/audit capability before it exists, creating adoption friction in the gap.
- **Knowledge consistency across federated workspaces.** Deferring automatic conflict resolution (§8) is sound for v2.0's scale, but the same choice compounds as more workspaces are federated and cross-org federation (v4) begins.
- **Organizational complexity of a multi-tenant registry.** Every step toward Ferret Hub increases the operational and governance surface Ferret itself must be trustworthy about — the same G4 (trustworthy context) goal now applied to Ferret's own infrastructure.

---

## 10. Open Questions

- Does resolving the FEP-001/`FUTURE-002` narrative conflict belong to this roadmap, to FEP governance, or to a dedicated ADR — and who has authority to decide? (Carries forward [FEP-001 §9 Q2](../FEP/FEP-001-Product-Architecture.md).)
- What signal — adoption, workspace count, explicit customer demand — triggers moving Federation from "org-local registry" (v2.0/v3) to "Ferret Hub" (v4)?
- Should Ecosystem/Extensibility's Generation 4 maturity (§3, §5) involve a public marketplace, or stay enterprise-internal (private connector registries per organization)?
- How far does Federation extend in practice — is cross-workspace composition scoped to one organization, or does it need to anticipate a shared open-source dependency's workspace from the start? (Carries forward [FEP-001 §9 Q5](../FEP/FEP-001-Product-Architecture.md).)
- Where does "engineering-relevant source" stop, and does that boundary need to be explicit before Multi-Source Enterprise Acquisition (§3) is scoped? (Carries forward [FEP-001 §9 Q6](../FEP/FEP-001-Product-Architecture.md).)

---

## Cross References

| Document | Relationship |
|---|---|
| [FEP-001 Product Architecture](../FEP/FEP-001-Product-Architecture.md) | Frozen capability model and product boundary this roadmap must remain consistent with |
| [FEP-002 Capability Catalog](../FEP/FEP-002-Capability-Catalog.md) | Authoritative capability definitions — never redefined here |
| [NEXT/V2.md](NEXT/V2.md) | Approved next milestone, product-level summary |
| [Workspace-Intelligence/](Workspace-Intelligence/) | Full engineering detail for the v2.0 milestone |
| [`FUTURE-002-Enterprise-Intelligence-Vision.md`](../002-Architecture/FUTURE-002-Enterprise-Intelligence-Vision.md) | Source of several deferred/proposal items here; its embedded-AI thesis is explicitly not adopted |

## Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | 2026-07-08 | Initial Ferret Product Roadmap, established alongside the ROADMAP folder structure |
