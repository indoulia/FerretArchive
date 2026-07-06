# 24 — WIP-033 Discovery: Scope Classifier Feasibility

**Status:** Complete — analysis only, no implementation, no architecture change.
**Purpose:** Verify `20-Phase-3-Priority-Assessment.md` §4/§5's proposal — reuse the existing SQLite
FTS5 vocabulary (`fts5vocab`) as WIP-033's scope signal, "no new persistence" — against the actual
implementation and real, dogfooded index files, not just the design doc's premise. No code was
changed to produce this document; all numbers below come from direct SQL against the two real,
live-dogfooded indexes (`C:\POC\Ferret`, 2,542 docs / 18,040 vocab terms; `C:\POC\indoulia-foundation`,
386 docs / 8,716 vocab terms) via a throwaway, uncommitted Python probe (`sqlite3` stdlib), mirroring
this codebase's established "direct-instantiation-and-timing" benchmark style (`21`, `22`, `23`).

---

## 1. Investigation Question 1 — FTS Vocabulary

**Is `fts5vocab` already usable? Yes, with a caveat the design doc doesn't mention.**

`SqliteKeywordIndexEngine.CreateFtsSql` (`src/Ferret.Indexing/SqliteKeywordIndexEngine.cs:28-34`)
creates `documents_fts` as a plain, non-`content=`-backed FTS5 table — an ordinary standalone virtual
table, not a contentless or external-content one. `fts5vocab` requires exactly this shape and works
against it unmodified: `CREATE VIRTUAL TABLE temp.vocab USING fts5vocab(main, documents_fts, row)`
succeeded against both real databases in 0.08–2.3 ms, zero schema change, zero rebuild.

**Caveat found empirically, not in the doc:** the 2-argument form implied by `20`'s wording
(`fts5vocab('documents_fts', 'row')`) fails with `no such fts5 table: temp.documents_fts` — the
2-arg form assumes the vocab table and the source table live in the *same* schema. Since a
classifier would create this table ad hoc (not as a permanent schema object), it must use the
3-argument form with an explicit schema name (`fts5vocab(main, documents_fts, row)`). Small, but a
real implementation detail that "no schema change required" doesn't fully capture — it's "no schema
*change*," but the *creation statement* itself needs the schema-qualified form to work from an
external caller.

No index rebuild is required in any case — `fts5vocab` reads the existing index's shadow tables live.

## 2. Investigation Question 2 — Query Cost

**Can the classifier stay cheaper than the search it's avoiding? Only modestly — the premise that
BM25 search itself is expensive doesn't hold on real, dogfooded data.**

| Operation (real `ferret-platform` index, 2,542 docs) | Cost |
|---|---|
| Vocabulary membership check (`SELECT 1 FROM vocab WHERE term = ?`), reused connection | 0.12 ms/term |
| Real BM25 `MATCH` query (full `SELECT ... JOIN ... WHERE documents_fts MATCH ? ORDER BY rank LIMIT 10`) | 0.49 ms/term |
| **Ratio** | **vocab check is ~4x cheaper**, not orders of magnitude |
| Fresh connection + vocab table creation + 1 lookup (worst case, no reuse) | 0.85 ms |
| Reused connection, 1 lookup | 0.16 ms |

`indoulia-foundation` (386 docs) shows the same shape: 0.09 ms vocab vs 0.34 ms `MATCH` (~3.7x).

This directly contradicts the implicit framing in `05-Context-Optimization.md` §2 and `20`'s §4
("far cheaper than a real BM25 query"): on real, small-to-medium dogfooded repos, FTS5's own inverted
index makes a single `MATCH` query already sub-millisecond. The vocabulary check is cheaper, but by a
small, not dramatic, margin — because there was very little to save in the first place at this scale.

**More importantly, the two most expensive costs `20-Phase-3-Priority-Assessment.md` §2 identified are
already fixed by prior work**, which changes WIP-033's cost/benefit case materially:
- Bottleneck #1 (`WorkspaceStateFingerprintProvider` full re-hash per query) — fixed by P3-001
  (`21-P3-001-Fingerprint-Optimization.md`).
- Bottleneck #2 (per-query registry file I/O) — fixed by WIP-032 (`22`).
- Bottleneck #4 ("possible per-query connection/service construction," flagged **unconfirmed** in
  `20`) — **now confirmed**: `RepoSearchServiceFactory.CreateForRepo` (`src/Ferret.Cli/Commands/Workspaces/RepoSearchServiceFactory.cs:27`)
  constructs a fresh `Bm25SearchProvider` per call (cheap, no I/O), but `Bm25SearchProvider.ExecuteAsync`
  (`src/Ferret.Search/Providers/Bm25/Bm25SearchProvider.cs:66-116`) opens a **fresh `SqliteConnection`
  per search, per source, every query** — real, but per this session's own connection-cost measurement
  (~0.2–0.85 ms), cheap in absolute terms, not the dominant cost.
- What's left uncached is bottleneck #3 (unconditional full fan-out, cost scales with reference count)
  — exactly what WIP-033 targets. But with #1 and #2 already amortized to near-zero on a warm process,
  and #4 confirmed cheap, the remaining avoidable cost *per skipped reference* is small: one connection
  open (~0.2–0.85 ms) plus one `MATCH` query (~0.1–0.5 ms) plus result marshaling — a low single-digit
  millisecond figure per source on real data at this scale, not the hundreds of milliseconds P3-001 was
  built to eliminate.

**Conclusion:** the classifier is cheaper than what it avoids, but the gap has shrunk since `20` was
written, because the two other Phase 3 fixes already closed most of the cost the Scope Classifier was
originally justified against. It is still net-positive, just smaller than the design doc implies.

## 3. Investigation Question 3 — Accuracy

**Vocabulary membership alone would produce useful routing for distinctive identifiers, and
near-useless routing for common words — confirmed by inspecting the real vocabulary, not assumed.**

- **No stemming, no stopword removal.** `CreateFtsSql` specifies no tokenizer option, so FTS5's
  default (`unicode61`) applies — pure case-insensitive/Unicode-aware tokenization only. Confirmed
  directly: `cache`, `cached`, and `caching` all exist as three *separate* vocabulary entries;
  `workspace` and `workspaces` are separate entries. A classifier checking for `"caching"` would
  produce a **false negative** against a workspace that only contains the word `"cached"`.
- **Common words dominate by document frequency and would produce false positives almost everywhere.**
  Real top-15-by-document-frequency terms in `ferret-platform`: `the` (1,806/2,542 docs), `a` (1,730),
  `and` (1,252), `is` (1,236), `to` (1,288) — plus generic code vocabulary any C#/.NET repo shares:
  `namespace` (1,750), `public` (1,742), `class` (1,423), `using` (1,336), `summary` (1,368). 40 terms
  appear in over 30% of all documents. A naive membership check on any of these terms would vote
  "relevant" for nearly every referenced workspace, defeating the classifier's purpose.
- **Distinctive identifiers/symbols are exactly where the signal is real.** `workspaceclimodule`
  appears in only 39/2,542 docs; `iworkspaceengine` in 34; `workspacelayout` in 59 — these are the
  terms a scope classifier would correctly narrow on. 1,971 of 18,040 total vocabulary terms
  (~11%) appear in exactly one document — maximally specific, maximally useful for narrowing.
- **Implication for the "smallest implementation":** a naive membership check as sketched in `20`
  §4 ("run a cheap vocabulary-membership check... for the query's terms") needs at minimum a
  stopword/high-document-frequency filter to avoid the common-word false-positive case identified
  above — this is a small addition, not a redesign, but it is not in the doc's sketch and would be a
  real correctness gap if skipped. Likely candidates already exist for free: SQLite's `fts5vocab`
  'row'/'col' modes both expose `doc` (document frequency) directly, so "skip terms with `doc` above
  some fraction of total document count" is a one-line addition using data already being read.

## 4. Investigation Question 4 — Workspace Scale

**Behavior at 2 / 10 / 50 / 100 references was not directly measurable — no dogfooded workspace has
more than one reference — but the cost shape is now well-characterized from measured primitives.**

Per-reference classifier cost (confirmed above): ~0.2–0.85 ms connection + vocab-table setup, plus
~0.1–0.5 ms per queried term. For R references and T query terms, that's roughly `R × (0.5 + 0.3×T)`
ms — at R=100, T=3, that's on the order of 140 ms, all sequential unless parallelized (the classifier
as sketched has no stated concurrency model; `FederatedKnowledgeStore`'s own fan-out already uses
`Task.WhenAll`, and a classifier pre-pass would presumably do the same). This scales *linearly* with
reference count, same as the fan-out it's trying to avoid — it does not change the asymptotic
complexity, only the constant-factor cost per source (a cheap vocabulary check instead of a full
search+merge). At small R (1–2, today's dogfooded reality) the classifier's own overhead is not
obviously worth paying. At larger R (50–100, hypothetical) linear-in-R connection overhead for the
classifier itself becomes non-trivial unless connections are pooled/reused across the classifier and
the real query — an optimization not in `20`'s sketch and not yet needed at any scale actually
observed.

## 5. Investigation Question 5 — Existing Metadata

**No existing artifact provides a scope signal — confirmed again this session, independently of `20`
§4's own finding.**

- `WorkspaceReference` (`src/Ferret.Workspace.Graph/WorkspaceReference.cs`) carries only `WorkspaceId`,
  `Mode`, `PinnedStateHash` — no content signal.
- `IndexStats` (`src/Ferret.Core/Indexing/IndexStats.cs`) carries `DocumentCount`, `TotalChars`,
  `LastIndexedAt`, `IndexSizeBytes` — none correlate with "does this workspace mention X."
- `ContentHash.cs` (`src/Ferret.Core/Primitives/ContentHash.cs`) exists but has **zero references
  anywhere under `src/Ferret.Indexing`** (confirmed via search this session) — `20`'s "unverified this
  pass" flag on this is now resolved: it is unused, not a hidden existing signal to reuse.
- `fts5vocab` remains the only real, already-materialized, per-workspace signal that exists without
  inventing new persistence — `20`'s conclusion holds.

## 6. Investigation Question 6 — Failure Behaviour

**Fail-open (search everything) remains correct, and is consistent with every other fail-safe decision
already made in this codebase for federation.**

`FederatedKnowledgeStore.ResolveSourcesAsync` and `CachingFederatedKnowledgeStore.TryBuildCacheKeyAsync`
both establish the same pattern repeatedly: when a signal *can't* be computed (corrupt registry entry,
unreachable workspace, unverifiable fingerprint), the code degrades to including/running the real path
rather than guessing — "fail closed" is reserved specifically for *pinned-reference content
verification* (ADR-0027 Amendment: never serve stale pinned content), not for auxiliary optimization
signals. A Scope Classifier failure (can't open a reference's index, vocab table creation fails,
timeout) is architecturally the same class of situation as "fingerprint can't be computed" in
`CachingFederatedKnowledgeStore` — and that path already fails open (bypasses the optimization, runs
the real query) by design. Failing closed on a classifier error — i.e., silently excluding a workspace
because its scope couldn't be checked — would reintroduce exactly the "silent, undetectable
degradation" class of defect that `17-Dogfooding-Sprint-1.md` Friction #5 and `18`'s Gap #2 already
identified and fixed as Critical/High. Fail-open is correct and is the only option consistent with
that precedent.

## 7. Investigation Question 7 — User Value

**No dogfooding evidence exists yet at any reference count above 1–2.** `17-Dogfooding-Sprint-1.md`
and `20`'s own §1/§5 both state real dogfooded workspaces have 1–2 references. Nothing in this
session's investigation found newer evidence changing that — `22` and `23`'s dogfooding (most recent)
still used the same single `ferret-platform` → `indoulia-foundation` reference pair. There is no
empirical basis yet for naming a specific reference count where Scope Classification "becomes
beneficial" — any number offered would be speculative, which the mission's constraints explicitly
disallow. What can be said with evidence: at R=1–2, a classifier's own overhead (§4) is comparable to
or larger than the per-source cost it would save, so it has no demonstrated value at the only scale
actually dogfooded.

---

## Deliverable 1 — Feasibility Report

**Can WIP-033 be implemented exactly as envisioned? Mostly, with two real corrections found this
session:**
1. The `fts5vocab` creation statement needs the 3-argument, schema-qualified form
   (`fts5vocab(main, documents_fts, row)`), not the 2-argument form the doc's phrasing implies —
   confirmed by reproducing the failure directly.
2. A naive membership check needs a document-frequency filter (already-available data, `fts5vocab`'s
   own `doc` column) to avoid false positives on stopwords and generic code vocabulary
   (`the`, `public`, `namespace`, ...) that appear in nearly every workspace regardless of relevance —
   not in the doc's sketch, needed for the classifier to do anything useful.

Everything else in `20-Phase-3-Priority-Assessment.md` §4's premise holds: no new persistence, no
schema change, no index rebuild, `fts5vocab` is real and queryable today against the exact schema
`SqliteKeywordIndexEngine` already produces.

## Deliverable 2 — Implementation Plan (smallest version, not built this session)

1. A `ScopeClassifier` type taking a query's extracted terms and a referenced workspace's DB path;
   opens a connection, creates the schema-qualified `fts5vocab` table, and for each term either finds
   it absent (workspace excluded) or present with `doc` below a frequency threshold (workspace
   included) — absent entirely defaults to include (fail-open, per Q6).
2. The document-frequency threshold (§3) as a constant, not a config surface — no evidence yet for
   what value is right, and no user-facing tuning has been requested anywhere in the roadmap.
3. Wire it into `FederatedKnowledgeStore.ResolveSourcesAsync` as a pre-filter on `entry.References`
   before `AddRepos` is called for each reference — the one place references are already walked.
4. No parallelism beyond what's already proven safe (`Task.WhenAll`, matching the existing fan-out
   pattern) — do not add a new concurrency primitive for this.

This is smaller than `20`'s own estimate implied "Medium effort" it as — the FTS5-reuse mechanism
needs no new infrastructure; the frequency-filter is the only design element not already specified.

## Deliverable 3 — Risk Assessment

- **Correctness risk (confirmed, Medium):** false negatives from missing stemming (§3) — a query for
  `"caching"` misses a workspace containing only `"cached"`. No mitigation exists in FTS5's default
  tokenizer without introducing a `porter` tokenizer, which is a schema change WIP-033 doesn't
  currently plan and this session doesn't recommend adding speculatively.
- **Correctness risk (confirmed, Medium):** false positives from stopword/generic-term flooding (§3) —
  mitigated by the document-frequency filter above, but the *threshold value* is unvalidated; wrong
  threshold either lets the false-positive problem back in (too high) or produces false negatives on
  legitimately common-but-relevant terms (too low).
- **Performance risk (confirmed, Low at current scale):** classifier overhead scales linearly with
  reference count, same as the fan-out it replaces (§4) — no asymptotic improvement, only a
  constant-factor one, and that factor is smaller than `20` implied (~4x, not orders of magnitude) now
  that P3-001/WIP-032 already removed the two dominant costs.
- **Maintenance risk (Low):** no new persistence, no new interface beyond what `20` already scoped;
  the frequency-filter constant is the only new tunable surface, and it has no user-facing exposure.

## Deliverable 4 — Validation Plan

Prefer real repositories and dogfooding over synthetic tests, consistent with `21`/`22`/`23`'s own
precedent:
1. Reuse the same `ferret-platform` / `indoulia-foundation` pair already registered from prior
   sessions — add a query term known (from this session's actual vocabulary dump) to exist only in one
   side (e.g. `iworkspaceengine`, present only in `ferret-platform`) and confirm the classifier excludes
   the other.
2. Add 3–5 more throwaway reference workspaces from other real local repos (if available under
   `C:\POC`) specifically to get above the 1–2-reference ceiling this project has never dogfooded, and
   measure real classifier overhead at that count directly rather than extrapolating from §4's formula.
3. A regression test asserting fail-open behavior when a referenced workspace's index file is missing
   or locked — mirroring the exact failure-injection style that caught the real bug in `23`'s dogfooding
   pass (`SearchAsync_WhenComputingTheFingerprintThrows_...`).
4. Do not rely on synthetic vocabularies for the accuracy question (§3) — this session's findings on
   stopword/stemming behavior came from real indexed content and would not have been visible against
   a small synthetic fixture with a handpicked vocabulary.

## Deliverable 5 — Recommendation

**Implement with minor adjustments** (of the four options offered) — not "exactly as planned" (two
real corrections were found, §Feasibility), not "re-sequence" (no dependency or blocking issue was
found — the fingerprint and registry prerequisites are already shipped), not "reject" (the mechanism
is real, works against the actual schema, and costs less than the query it replaces, just not
dramatically less).

**Why not a stronger recommendation either way:** the cost side of the case is solid — `fts5vocab`
works today, costs less than a `MATCH` query, and requires no new persistence. The *value* side is
unverified — every dogfooding session to date (`17`, `22`, `23`) has exercised exactly one reference,
never enough to observe the fan-out cost WIP-033 exists to avoid. Building it now would be implementing
against a real, working mechanism but an unvalidated payoff scale — the same category of risk `20`
itself flagged ("Medium risk — a false-negative silently excludes a relevant workspace"). The
adjustment this session recommends before committing engineering time: dogfood past 2 references
first (Validation Plan step 2) to get real fan-out-cost evidence at a scale where WIP-033 would
actually matter, before or alongside building the classifier — consistent with this project's own
established discipline (validate assumptions → implement narrowly → benchmark → dogfood → merge) and
with `20-Phase-3-Priority-Assessment.md` §5's own explicit conclusion that Scope Classifier's benefit
is real "at scale" but not yet demonstrated at the scale actually in use.
