# 08 — Telemetry

**Status:** Ready for implementation
**Extends:** ARCH-001 §21 (Telemetry Architecture) — new metrics and one new sink on the existing pipeline

## 1. New Metrics (Metrics Pillar, §21.2)

Added to the existing named-metric set (`index.build.duration`, `knowledge.query.duration`, etc.) — same pillar, same `Meter` mechanism, no new instrumentation approach:

| Metric | Emitted by | Feeds |
|---|---|---|
| `workspace.federated_query.duration` | `Ferret.Knowledge.Federation` | 09-Analytics.md latency rollups |
| `workspace.reference.resolve.duration` | Workspace Graph topology resolution | Cache-effectiveness dashboards |
| `context.scope_narrowed.count` | Scope Classifier (05 §2) | "workspaces skipped" metric |
| `context.compression.tokens_saved` | Compressor (05 §3) | Estimated token/cost savings (Objective 7) |
| `cache.federation.hit` / `cache.federation.miss` | Federated query cache (07 §1) | Cache hit-rate dashboard |

## 2. New Sink: the Usage Ledger

§21.3's pipeline (`Engines → Ferret.Telemetry → {Console, File, OTEL}`) gains a fourth sink:

```
Engines → Ferret.Telemetry → {Console, File, OTEL, Ledger (NEW)}
```

The Ledger sink is the only telemetry consumer that persists structured, queryable *events* (not just metric samples or trace spans) — this is what 10-Usage-Ledger.md and 09-Analytics.md read from. It is additive: Console/File/OTEL behavior is unchanged, and telemetry export continues to have no observability-vendor dependency (§21.5), since the Ledger is a first-party sink, not a vendor plugin.

## 3. What Every Ledger Event Carries

Every event this milestone adds to the ledger includes the workspace ID and, where applicable, the knowledge state hash active at the time (§13.4) — this is what lets 10-Usage-Ledger.md answer "what did the system know when this happened," the same guarantee §13.4 already gives AI interactions.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| New metrics use the existing `Meter`-based mechanism; no new instrumentation library | Ready for implementation |
| Usage Ledger is a fourth sink on the existing telemetry pipeline | Ready for implementation |
| Every ledger event carries workspace ID + knowledge state hash | Ready for implementation |
