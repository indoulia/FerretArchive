# Immediate Product Roadmap

**Purpose:** bridge between the current Dogfooding milestone and the start of the Workspace Intelligence Platform milestone. Short by design — the deep plan lives in `Workspace-Intelligence/15-Execution-Plan.md`.

## Where We Are

- v0.16.0 shipped: Enterprise Content Pack 1 (PDF/DOCX/XLSX parsers), first OIDC npm publish.
- DOGFOOD-001 is in progress on the `dogfooding` branch: using Ferret on external/real repos, logging and fixing bugs found along the way (most recent: URI-unescaping fix, hyphenated-keyword FTS5 quoting fix, connectors directory-skip-list fix).

## What Finishes Before Workspace Intelligence Platform Starts

1. **DOGFOOD-001 closes out.** No new feature work starts until the current dogfooding pass's open findings are triaged (fixed or filed as GitHub issues per the existing bug-tracking convention) and the branch is in a mergeable state.
2. **Trusted Publishing follow-up.** Set npm "disallow tokens" and delete the `NPM_TOKEN` secret — small, already-scoped, should not be allowed to bleed into the next milestone's timeline.

Neither item blocks *starting* Workspace Intelligence Platform's Phase 0 (Founder Gate, ADR-0026/0029) — those are decisions, not code, and can close in parallel. They block Phase 1 (Foundation) implementation work, which needs engineering capacity currently on DOGFOOD-001.

## What Starts Immediately After

Workspace Intelligence Platform, Phase 0 (`Workspace-Intelligence/15-Execution-Plan.md` §1) — this is the Founder decision gate (ADR-0026, ADR-0029), which can run concurrently with DOGFOOD-001 close-out since it requires Founder time, not engineering time. Phase 1 implementation begins once both DOGFOOD-001 closes and Phase 0 is decided.

## Decision Log

| Decision | Outcome |
|---|---|
| Workspace Intelligence Platform replaces all previously planned feature work as the next milestone | Ready — confirmed by Founder directive |
| Phase 0 (ADR review) runs concurrently with DOGFOOD-001 close-out | Ready for implementation |
| DOGFOOD-001's open findings must be triaged before Phase 1 engineering work starts | Ready — capacity constraint, not a design gate |
