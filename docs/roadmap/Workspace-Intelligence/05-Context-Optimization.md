# 05 — Context Optimization Engine

**Status:** Ready for implementation
**Extends:** ARCH-001 §13.3 (Context Assembly)
**Why this doc exists:** this is the milestone's primary differentiator — the founder brief names it explicitly as the thing that must make repository/workspace size stop mattering to AI performance.

## 1. Starting Point: This Already Exists

ARCH-001 §13.3 already implements the shape the founder described (Question → Intent → Symbols → Dependencies → Context → LLM):

```
Request → Planner (token budget by category) → {Spec Query, Knowledge Query, Memory Query}
        → Relevance Scorer → Token Packer (greedy, budget-bounded) → Exclusion Guard → Context
```

The risk this milestone has to manage is federation making that pipeline slower or more expensive, not building a new pipeline. Two additions close that gap.

## 2. Addition 1 — Scope Narrowing Before Fan-Out

Without this, every query against a workspace with N references fans out to N+1 stores even when the answer clearly lives in one. Before the existing Planner step runs:

```
Request → Scope Classifier (NEW) → Planner → ... (unchanged)
```

The Scope Classifier is a cheap, local step (keyword/symbol-name match against each workspace's index manifest — not an LLM call) that ranks referenced workspaces by likelihood of relevance and drops fan-out to workspaces below a relevance floor. This is the mechanism behind the Vision doc's latency target (00-Vision.md §4: p95 ≤ 2x single-repo baseline regardless of reference count) — cost should scale with *relevant* references, not *total* references.

## 3. Addition 2 — Compression Before Packing

The existing Token Packer (§13.3) packs whole scored entries. For federated results specifically, add a compression step between Scorer and Packer:

```
... → Relevance Scorer → Compressor (NEW, federated results only) → Token Packer → ...
```

For a cross-workspace `SourceSymbol` result, the Compressor replaces the full body with signature + docstring + direct dependency list unless the query's intent classification (already computed by the Scope Classifier) indicates full-body detail is needed. This is the direct mechanism behind 00-Vision.md's token-cost target.

## 4. What Is Explicitly Not Being Built

- **No semantic/vector layer.** FUTURE-002 §22 defers the semantic index to "keyword-first, semantic second," and nothing here requires it — the Scope Classifier and Compressor both work on the existing graph/keyword index.
- **No LLM call inside the optimization pipeline itself.** Scope Classifier and Compressor are both deterministic, so the whole pipeline keeps the determinism property in §13.4 (same inputs + state → same knowledge state hash → same assembled context).

## 5. Measurement

Both additions emit metrics on the existing telemetry pipeline (§21.2), not a new one — `context.scope_narrowed.count`, `context.compression.tokens_saved` — which is what 09-Analytics.md aggregates into the "estimated tokens saved" dashboard metric from the founder's brief.

## 6. Decision Log

| Decision | Outcome |
|---|---|
| Scope narrowing and compression are additions to the existing pipeline, not a new one | Ready for implementation |
| Both stages are deterministic, no LLM call in the optimization path | Ready for implementation |
| Semantic/vector retrieval | Deferred — tracked as a FUTURE-002 item, not this milestone's problem |
