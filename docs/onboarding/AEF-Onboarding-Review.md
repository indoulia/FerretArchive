# AEF Onboarding Review — Evaluating the Ferret Onboarding Package

> This is a review of **AEF's onboarding capability**, not of Ferret. It evaluates the six-document package produced by the first-time Ferret onboarding (`AEF-Onboarding.md`, `AEF-Onboarding-Validation.md`, `Repository-Inventory.md`, `Architecture-Inventory.md`, `Technology-Inventory.md`, `Documentation-Inventory.md`) against the goal of becoming AEF's standard onboarding output for future repositories. No file in the reviewed package was modified to produce this review, and nothing has been committed.

## 1. Executive Summary

The package is substantively strong: every non-trivial claim is grounded in a cited file path or a `tokensave` code-graph query, Ferret-specific findings are cleanly separated from AEF-process findings, and the process correctly detected the repository's deliberately-staged validation fixture (the three-way "current sprint" contradiction) through an independent, generalizable technique (cross-checking self-declared state docs against `git log`) rather than by luck. That said, the package has real, fixable defects that disqualify it — as-is — from being frozen as the standard template: it contains at least one internal factual inconsistency (test-project count stated as both 31 and 34), no provenance/reproducibility metadata (which git ref and which `tokensave` index snapshot the findings were computed against — itself the exact failure mode the package criticizes in Ferret's own `PROJECT-STATE.md`), a file-naming scheme whose default sort order inverts the intended reading order, and significant unmanaged duplication of its own major findings across multiple files. None of these are fundamental — they are template-engineering gaps, not evidence that AEF's onboarding *capability* is unsound. Recommendation: **APPROVED WITH CHANGES**.

## 2. Strengths

- **Evidence discipline.** Nearly every claim across all six documents cites a specific file path, and often a line number or a `git log`/`tokensave` command output. This is the single most important property for both human trust and AI re-verifiability, and it holds consistently across ~580 lines of dense material.
- **Correctly separates "what AEF found" from "what AEF should change about itself."** `AEF-Onboarding-Validation.md` isolates a dedicated "Recommended Improvements for AEF Itself" section, distinct from the Ferret-specific findings tables. This is exactly the Ferret-vs-AEF separation this review was asked to check for, and it was already done well.
- **The central finding was derived, not handed to it.** The three-way "current sprint" contradiction (PROJECT-STATE.md vs ROADMAP-001.md vs `.ai/session.md`) was surfaced by independently running `git log`/`git merge-base` against the documents' claims — a technique that would work on a repository that does *not* narrate its own inconsistencies, unlike the confirming commit message this repo happened to contain (see §3 for the risk this still creates).
- **Tool skepticism was applied, not assumed.** Two `tokensave` outputs (a reported circular dependency, a reported coverage gap) were independently spot-checked against source and shown to be a false positive and a false negative respectively, before being reported with the correct caveat rather than at face value.
- **Honest confidence calibration.** The Validation report scores each area (6–9/10) with a stated justification rather than defaulting to uniform high confidence, and explicitly names what was *not* deep-read (e.g., `ARCH-001.md` §15+, `AGR-002`–`004`) rather than implying full coverage.
- **Human-relevant judgment calls are correctly routed to a human**, not resolved by the process itself (the 7-item "Human Intervention Required" list in the Validation report — e.g., ADR-0029's Founder decision, ADR-0025's stuck Proposed status).

## 3. Weaknesses

- **An internal factual inconsistency exists inside the package itself.** `Technology-Inventory.md` states "31 dedicated test projects (see `Repository-Inventory.md` for the list)," but `Repository-Inventory.md` and `Architecture-Inventory.md` both state 34 test projects (and 62 total `.csproj` files = 28 src + 34 tests). These cannot both be right, and no document flags or explains the discrepancy. This is a real accuracy defect in a package whose entire value proposition is precise, citable numbers.
- **No provenance/reproducibility metadata.** None of the six documents state which git commit/branch the analysis was run against, or that `tokensave`'s own `status` output returned `"branch_fallback": true, "branch_warning": "branch 'chore/aef-onboarding-cleanup' is not tracked — serving from 'main'"` during this run. Every `tokensave`-derived number in the package (module public-surface counts, coverage/risk percentages, circular-dependency and god-class rankings) was computed against whatever `main`'s index contained, not necessarily the checked-out branch — and a reader has no way to know this, or to know when these numbers would need re-verification. This is precisely the failure mode the package repeatedly criticizes in Ferret's own `PROJECT-STATE.md` (a document with no anchor to reality that nobody can tell is stale).
- **Filename sort order inverts the intended reading order.** A directory listing of `docs/onboarding/` sorts as: `AEF-Onboarding-Validation.md`, `AEF-Onboarding.md`, `Architecture-Inventory.md`, `Documentation-Inventory.md`, `Repository-Inventory.md`, `Technology-Inventory.md` (hyphen sorts before period in ASCII). A reader who browses the folder without already knowing to start with `AEF-Onboarding.md` will land on the audit/validation report first. There is no `README.md`/index file in the folder to correct this.
- **Major findings are narrated in full prose 3–6 separate times across the package** instead of being stated once and referenced by ID. Concretely: the three-way sprint contradiction appears in `AEF-Onboarding.md` (§11, §12, §14), `Documentation-Inventory.md`, and `AEF-Onboarding-Validation.md` (twice) — six restatements of one fact. The three-ADR-location finding, the four dangling links, and the security-scanning overstatement each appear 3+ times. This inflates the package's size and creates a maintenance hazard: if any of these facts changes, up to six locations need updating, with nothing forcing them to stay in sync (as the 31-vs-34 defect above demonstrates already happened once).
- **The package's confidence narrative leans partly on a lucky confession.** The roadmap finding's write-up (`AEF-Onboarding.md` §11, Validation R1) is correct and well-evidenced, but its framing draws reassurance from the fact that the repository's own commit message admitted the staging was deliberate. A real (non-fixture) repository will essentially never do this. The package doesn't clearly separate "what the cross-check technique found on its own" from "what the commit message additionally confirmed," which risks readers of a future onboarding overestimating how often this kind of confirmation will be available.
- **Optimized for audit thoroughness over a human's first hour.** `AEF-Onboarding.md` places "First-Day Setup" at §13 of 15, after nine sections of dense, heavily-caveated prose. A new human engineer wanting to get a build running has to read through governance/roadmap staleness analysis first.
- **No demonstration of the product actually working.** First-Day Setup covers `dotnet build`/`dotnet test`/`dotnet format`, but never walks through using Ferret itself (e.g., `ferret workspace init` → `ferret index` → `ferret search`). A new engineer finishes setup without having seen the thing the repository exists to build.
- **No "who do I ask" dimension.** None of the eight original research passes were scoped to find team/maintainer/communication-channel information beyond the single `@indoulia` CODEOWNERS entry noted under Governance. This is a standard onboarding expectation that fell through the cracks because it wasn't assigned to any research agent.

## 4. Missing Information

- **Provenance**: git commit SHA / branch, analysis date, and `tokensave` index snapshot state (see §3) — needed for any of the numeric findings to be reproducible or re-checkable later.
- **Team & communication channels**: who maintains this day-to-day beyond `@indoulia`, and where would a contributor ask a question (issue tracker only? Discussions? none of the above?).
- **A working "hello world" of the product itself**, distinct from building/testing the codebase.
- **Repo-at-a-glance scale metrics** in one place for a human skimmer (age, total commits, contributor count) — some numbers exist (1,189 indexed files, 16,178 graph nodes) but scattered, not summarized as a single orientation stat block.
- **An explicit note on which branch a new contributor should actually start from** — `main` holds the real merged state; the checked-out branch (`chore/aef-onboarding-cleanup`) is a cleanup/validation branch. The package never states this distinction plainly for someone about to `git checkout`.

## 5. Redundant Information

| Finding | Restated in | Count |
|---|---|---|
| Ferret-vs-tokensave relationship | `AEF-Onboarding.md` §2 (twice) + Glossary, `Repository-Inventory.md` | 3+ |
| Three ADR/decision locations | `AEF-Onboarding.md` §9, §14; `Architecture-Inventory.md`; `Documentation-Inventory.md`; `AEF-Onboarding-Validation.md` (A4) | 5 |
| Three-way "current sprint" contradiction | `AEF-Onboarding.md` §11, §12, §14; `Documentation-Inventory.md` item 9; `AEF-Onboarding-Validation.md` (R1, D1) | 6 |
| Four dangling links | `Documentation-Inventory.md`; `AEF-Onboarding.md` §14; `AEF-Onboarding-Validation.md` (D3) | 3 |
| Security-scanning overstatement | `Technology-Inventory.md`; `AEF-Onboarding.md` §14; `AEF-Onboarding-Validation.md` (DP2) | 3 |

Some cross-referencing is appropriate (each companion document is meant to stand alone), but full-prose repetition at this scale is a maintenance liability, not a feature — see Recommendation §10.

## 6. Sections to Standardize

Mandatory in every future onboarding, regardless of repository (structure, not content, generalizes):

- Executive Summary
- Repository Layout (top-level directory table)
- Technology Stack
- Build and Test Process
- Key Documents to Read
- First-Day Setup
- Common Pitfalls / "documents not to trust"
- The entire Validation report category: Findings table (PASS/WARNING/FAIL/UNKNOWN), Confidence by Area, Human Intervention Required (Y/N + list), AEF Improvement extraction
- Documentation freshness methodology (per-doc `git log -1`) as a required technique, not an incidental choice this run happened to make
- A cross-document consistency check as a synthesis step (see §10, Recommendation 3) — this run shows what happens without one

## 7. Sections to Parameterize

Ferret-specific content that a template should hold as placeholders, not fixed prose:

- Product Overview content (Ferret's specific identity, "ContextOS"/"AISpace" naming history, the tokensave-relationship digression) — the *slot* ("resolve any confusing tool/product name collisions found in the repo") is generic; the content is 100% Ferret-specific.
- Architecture Overview specifics (the 5-layer model, the seven "engines," which four are unimplemented) — the *pattern* ("compare the canonical architecture doc's claimed structure against the real code graph and report drift") is the reusable part.
- Governance vocabulary (ADR/AGR/Decision-Log/governance-gate terminology) — the *check* ("does this repo have a decision-record system? is it single- or multi-location? is it internally consistent?") generalizes; Ferret's specific term choices do not.
- The Glossary currently mixes two different kinds of entries in one table: generic AEF/process vocabulary (AEF, ADR, ARCH-NNN pattern) and Ferret-specific vocabulary (Ferret, ContextOS, tokensave, dogfooding). A template should split these into two glossary blocks — one pre-populated boilerplate block for AEF/process terms, one empty block for repo-specific terms — so template consumers aren't tempted to hand-copy Ferret's terms into an unrelated repo's onboarding doc.

## 8. Human-only Decisions

Already correctly identified in `AEF-Onboarding-Validation.md`'s "Human Intervention Required" list (reconciling stale state docs, ADR-0029's Founder decision, ADR-0025's status, `docs/002-Architecture/decisions/` disposition, dangling-link fixes, future-vision-doc precedence, CODEOWNERS bus-factor). This review adds one omitted from that list:

- **Whether AEF should treat a repository's self-declared "this inconsistency is intentional" narration (as found in commit `904d05f`) as authoritative going forward**, or should always independently re-derive severity regardless of what a commit message claims. This is a policy call for AEF's governance layer, not something an individual onboarding run should decide for itself (see Recommendation 9).
- **Whether onboarding missions are authorized to run safe, read-only smoke commands against a built artifact** (e.g., `ferret --help`) to produce a real usage demonstration, as opposed to being purely static/discovery. This trades off "discovery only, no side effects" against "show the product actually working" and is a scope decision, not something an agent should decide ad hoc mid-mission.

## 9. AI-only Responsibilities

- Cross-referencing hundreds of files, `git log`, and a code-intelligence graph within a single sitting at the depth this package demonstrates (e.g., resolving the Ferret-vs-tokensave question from indirect evidence across a dozen files, or reconciling `docs/adr/` against two other undocumented decision locations) is not practically achievable by a human doing the same task in comparable time — this is squarely AI-onboarding value, not a replacement for human review of the conclusions.
- Spot-verifying structural-analysis tool output (the confirmed `tokensave` false positive/false negative) against raw source is exactly the kind of high-volume cross-checking that should stay an automated/agent responsibility — a human reviewer should be able to trust that this checking happened rather than redoing it.
- Maintaining and checking a "distrust list" of known-stale documents against future `git log` state (Validation report's own Recommendation 4) is naturally an AI/tooling responsibility — humans should consume the list, not maintain it by memory.

## 10. AEF Improvement Recommendations

Each recommendation is evidence-backed from this review; each is classified into exactly one category.

1. **Add a mandatory Provenance block to every onboarding package** (git commit SHA, branch, analysis date, and any code-intelligence tool's own reported caveats such as `tokensave`'s `branch_fallback`/`branch_warning`). *Evidence*: `tokensave tool status` returned an explicit branch-fallback warning during this run that appears nowhere in the six delivered documents, making every `tokensave`-derived number's exact scope and reproducibility unverifiable. — **Template Improvement**

2. **Require agents to surface tool-reported caveats verbatim in their returned findings, not silently absorb them.** *Evidence*: same as above — the branch-fallback warning was visible in the raw tool output at the start of this session but did not survive into any research agent's report or the final synthesis. — **Agent Improvement**

3. **Add a mandatory cross-document numeric-consistency check as a synthesis step before publishing an onboarding package.** *Evidence*: `Technology-Inventory.md` states 31 test projects while `Repository-Inventory.md` and `Architecture-Inventory.md` state 34, for what is presented as the same fact — a defect that a single reconciliation pass over the eight raw agent reports would have caught before writing six final files. — **Workflow Improvement**

4. **Adopt a "state once, reference by ID" convention for major findings**, with `AEF-Onboarding-Validation.md`'s findings table as the system of record and other documents citing finding IDs (e.g. "see Validation D3") instead of re-narrating in full prose. *Evidence*: five distinct findings are each restated in full 3–6 times across the six documents (§5 table above). — **Template Improvement**

5. **Fix onboarding-package file naming so default alphabetical sort matches intended reading order** (e.g., numeric prefixes `00-`…`05-`, or an explicit `README.md` index inside `docs/onboarding/`). *Evidence*: directly confirmed directory listing sorts `AEF-Onboarding-Validation.md` before `AEF-Onboarding.md`. — **Template Improvement**

6. **Codify "cross-check self-declared current-state documents against `git log`/`git merge-base` ground truth" as a named, standing step in the onboarding skill**, not an emergent behavior of how one research agent happened to be prompted this run. *Evidence*: this technique produced the package's single most important finding (R1) and is explicitly recommended in the Validation report itself, but nothing outside that one paragraph guarantees a future onboarding run repeats it. — **Skill Improvement**

7. **Codify "verify surprising structural/code-graph tool output against source before reporting it" as a standing agent instruction**, not a one-off prompt choice. *Evidence*: this run's per-agent prompts happened to include verification instructions that caught a confirmed `tokensave` false positive (circular dependency) and false negative (coverage) — a future run without that specific prompt wording could silently report either as fact. — **Agent Improvement**

8. **Add a machine-readable findings manifest (JSON/YAML) alongside the prose Validation report**, keyed by finding ID, class (PASS/WARNING/FAIL/UNKNOWN), and area, so a future AEF session can diff against it instead of re-parsing prose. *Evidence*: the Validation report's own Recommendation 4 already calls for onboarding output to be "re-consumable" but proposes no concrete format; this review makes the format concrete. — **Platform Capability**

9. **Establish a governance policy on how much weight a repository's self-declared "this was intentional" narration should carry in AEF's confidence scoring**, since most real repositories will not narrate their own inconsistencies the way this validation fixture did. *Evidence*: §3/§8 above — the roadmap finding's cross-check methodology is sound and generalizes, but its write-up leans partly on a confirming commit message that is unlikely to be available in a typical onboarding. — **Governance (AER) Improvement**

10. **Decide, as policy, whether onboarding missions may execute safe read-only commands against a built artifact (e.g., `--help`/`--version`) to produce a genuine "first real usage" step**, distinct from the existing "avoid heavy/slow operations" guidance that currently blocks even light verification. *Evidence*: `AEF-Onboarding.md`'s First-Day Setup section stops at build/test/format and never shows Ferret doing the thing it exists to do, because the discovery-only mission scope was interpreted (reasonably, absent guidance) to exclude any execution at all. — **Governance (AER) Improvement**

11. **Add "Team & Communication Channels" as a required onboarding research dimension.** *Evidence*: none of the eight parallel research passes were scoped to this, and no document's own "Gaps/Unknowns" section flags its absence — it was missed silently rather than explicitly deferred. — **Workflow Improvement**

12. **No action required: the existing separation between Ferret-specific findings and "Recommended Improvements for AEF Itself" in `AEF-Onboarding-Validation.md`.** *Evidence*: reviewed and found already correctly isolated (§2 above) — this pattern should be preserved as-is in the template, not modified. — **No Action Required**

## 11. Proposed Updates to the AEF Onboarding Template

Based on §6, §7, and §10, the reusable template (to live in the AEF framework repository, not copied per-repo) should:

- Add a **Provenance** block at the top of the package (commit/branch/date/tool-index-state) — Recommendation 1.
- Add a **Findings Manifest** (structured, ID-keyed) as a required companion artifact, not just prose — Recommendation 8.
- Rename/number the six files for correct default sort order and/or ship a `docs/onboarding/README.md` index — Recommendation 5.
- Split the Glossary template into two blocks: fixed AEF/process boilerplate vs. empty repo-specific — §7.
- Add explicit template sections for **Team & Communication Channels** and a **Product Walkthrough / First Real Usage** step (contingent on Recommendation 10's scope decision) — Recommendations 10, 11.
- Add a template instruction that every major finding is written once (in the Validation findings table) and referenced by ID elsewhere, with a lint-style synthesis check for numeric consistency across documents — Recommendations 3, 4.
- Add a template caution note instructing future runs not to calibrate confidence based on a repository "confessing" its own inconsistencies, since this will be the exception, not the norm — Recommendation 9.
- Reorder `AEF-Onboarding.md`'s template so a short, human-first "Quickstart" (setup + key docs + one product usage example) appears before the deeper narrative/audit sections, without removing the deeper sections — §3, §6.

## 12. Readiness Assessment

The package demonstrates that AEF's onboarding *capability* — grounded, cross-checked, evidence-cited discovery of an unfamiliar repository's product, architecture, tech stack, testing, deployment, governance, roadmap, and documentation quality — works well, including on an adversarial fixture designed to test exactly this. It should **not**, however, be frozen as the standard template in its current form: it contains a genuine internal factual inconsistency, lacks the provenance metadata needed to make its own claims reproducible (the same defect class it flags in Ferret), has a navigational defect in its own file naming, and carries unmanaged duplication that will compound as a maintenance burden if reused as-is across many future repositories. All identified issues are template-engineering and process fixes, not evidence of a broken underlying method.

## Recommendation

**APPROVED WITH CHANGES**
