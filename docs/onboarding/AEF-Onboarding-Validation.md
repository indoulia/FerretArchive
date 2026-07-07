# AEF Onboarding Validation Report — Ferret

> Validation report for the first-time AEF onboarding of the Ferret repository, performed 2026-07-07 with no prior context and no human guidance beyond the repository itself. This report documents what was discovered, what could not be determined, and where a human decision is needed. Nothing described here has been fixed, redesigned, or reconciled — per mission scope, this is discovery only.

## Method

Eight independent research passes (product/repo-layout, architecture/ADRs, tech stack/build, testing, deployment/security/releases, governance, roadmap/current-state, documentation quality) were run in parallel, each grounded in direct file reads plus the `tokensave` code-intelligence CLI (already indexed for this repo) for structural/code-graph claims. Findings below are synthesized from those eight reports; every claim in the companion inventory documents cites a specific file path.

## Findings

Classified PASS (verified working as documented), WARNING (real but non-blocking drift/gap), FAIL (confirmed contradiction or broken reference), UNKNOWN (could not be determined from available evidence).

### Architecture

| # | Class | Finding |
|---|---|---|
| A1 | WARNING | `ARCH-001.md` describes seven "engines" inside `Ferret.Runtime`; four (Artifact, Memory, Review, Specification) have no implementation anywhere in `src/`. Confirmed against code via `tokensave` and independently stated in `ARCH-024-Artifact-Inventory.md` itself. |
| A2 | WARNING | `Ferret.Knowledge.Federation` and `Ferret.Workspace.Graph` are real, populated projects not reflected anywhere in ARCH-001's module list (they postdate its baseline). |
| A3 | WARNING | `docs/002-Architecture/README.md`'s index table omits ARCH-017, 018, 019, 020, 021, 022, and 037, all present as files in the same folder. |
| A4 | FAIL | `docs/002-Architecture/decisions/` holds two real decisions (including an Accepted ADR under a different ID format) that are invisible from the canonical `docs/adr/README.md` index — a discoverability break, not a content conflict. |
| A5 | PASS | Architecture-fitness rules (`Ferret.Core` zero-dependency, `Ferret.Runtime` cannot reference `Cli`/`Mcp`) are enforced as **build errors** (`Directory.Build.targets`) and covered by `Ferret.Architecture.Tests` — this is real, working, code-level governance, not just a paper policy. |
| A6 | PASS | Code graph is structurally healthy: sparse/well-partitioned (DSM density 0.002), no severe god classes, no confirmed circular dependencies (one candidate was investigated and shown to be a tool false positive). |

### Documentation

| # | Class | Finding |
|---|---|---|
| D1 | FAIL | `docs/000-Overview/PROJECT-STATE.md` self-declares it must be kept current but is severely stale (references Sprint 13/v0.12.0/ADR-0020 as current, while `main` is well past ADR-0030 and an entire merged v2 platform). |
| D2 | FAIL | `docs/012-Releases/README.md`'s own release index says "no releases yet" while the same folder contains three real, dated release docs. |
| D3 | WARNING | Four confirmed dangling links left by the most recent cleanup commit (`SECURITY.md`, `templates/README.md`, `templates/release.md`, `docs/002-Architecture/README.md` — see `Documentation-Inventory.md` for exact lines). |
| D4 | WARNING | Four overlapping "future vision" documents across two doc trees, with no stated precedence or cross-reference. |
| D5 | WARNING | Archived docs (`docs/archive/`) don't self-identify as archived at the file level; only a folder-level note in `docs/README.md` marks the tree as historical. |
| D6 | UNKNOWN | Whether ~10 "Planned"/unlinked ARCH-* documents represent intentional sequencing or abandoned scope. |

### Roadmap

| # | Class | Finding |
|---|---|---|
| R1 | FAIL — **by design** | `PROJECT-STATE.md`, `docs/001-Product/ROADMAP-001.md`, and `.ai/session.md`/`current-context.json` each claim a different, and each wrong, "current sprint" — none reflects the real, merged Workspace Intelligence Platform / Epic 5 work visible in `git log`. The repository's own most recent commit message states this was **left in place intentionally as a validation signal for this onboarding exercise**. Classified FAIL because the documents are, as written, false; flagged "by design" because detecting this was evidently the point, not a genuine incident requiring urgent human triage — see "Human Intervention Required" below for the reconciliation decision this still leaves open. |
| R2 | PASS | The `v2/workspace-intelligence-platform` branch and its worktree were correctly identified, via `git merge-base`/ancestor checks, as a fully-merged fossil rather than an active parallel initiative. |
| R3 | PASS | The roadmap's own self-correction chain (docs 27→28→29→30 in `docs/roadmap/Workspace-Intelligence/`) shows genuine evidence-driven correction — a later doc caught and fixed a stale instruction in an earlier one before it caused harm. |
| R4 | WARNING | `CHANGELOG.md` has no entry for the entire Workspace Intelligence Platform milestone, which has already shipped to `main`. |

### Governance

| # | Class | Finding |
|---|---|---|
| G1 | WARNING | ADR-0025 (governance-gate rule) remains Status: Proposed in the ADR index, despite being cited and applied as settled practice in later Decision-Log entries. |
| G2 | WARNING | `.ai/workflows/CreateADR.md` instructs checking a `docs/013-Governance/Decision-Register.md` file that does not exist (only `DECISION-LOG.md` exists) — the workflow doc and the actual governance folder are out of sync. |
| G3 | WARNING | `.github/CODEOWNERS` names a single owner (`@indoulia`) on nearly every path pattern, including ones structured to suggest differentiated review (security paths, docs, CI) — a single-maintainer model in practice today. |
| G4 | PASS | The ADR → AGR → Decision-Log chain is real, internally consistent, and traceable: 18 ADRs, 4 AGRs, and a running decision log all cross-reference coherently, and the "closed decision requires a new governance review to reopen" rule (AGR-001) is applied consistently in later documents (e.g. ADR-0030). |
| G5 | UNKNOWN | Whether the `.ai/agents/*.md` role-authority model (e.g. "ChiefArchitect can block a sprint") is enforced by any tooling, or is a prose convention followed manually. |

### Testing

| # | Class | Finding |
|---|---|---|
| T1 | WARNING | Stated coverage targets (Core ≥90%/85% line/branch, etc.) are not evidenced as met, and CI collects coverage without gating on any threshold. |
| T2 | WARNING | `CONTRIBUTING.md` describes Docker-Compose-backed integration tests and category-filtered CI test runs; neither exists in the actual repo/CI configuration. |
| T3 | FAIL (tooling limitation, logged as an AEF improvement item — see below) | The `tokensave` call-graph coverage tools were confirmed, by direct comparison against a real test file, to under-report coverage (a directly-tested method was reported as uncovered). Coverage percentages in `Technology-Inventory.md` should be read as a floor, not ground truth. |
| T4 | UNKNOWN | True test pass/fail rate and real (execution-based, not call-graph-proxy) coverage — the test suite was deliberately not executed during this discovery pass, per the mission's "discovery only" / avoid-heavy-operations guidance. |

### Build

| # | Class | Finding |
|---|---|---|
| B1 | PASS | Build system is fully coherent and verified: single solution (`src/Ferret.sln`), Central Package Management, build-time architecture-fitness gates, cross-platform CI matrix (Ubuntu + Windows), and a `dotnet format` gate — all real and consistent with documentation. |
| B2 | WARNING | `docs/templates/versioning.md`, referenced by `docs/012-Releases/README.md` as the authoritative SemVer policy, does not exist. |

### Deployment

| # | Class | Finding |
|---|---|---|
| DP1 | PASS | The full release pipeline (tag → `release.yml` build/manifest → draft GitHub Release → public mirror for anonymous access → `npm-publish.yml` OIDC publish) is internally consistent between workflow YAML and release-process documentation. |
| DP2 | FAIL | `docs/010-Security/README.md`'s "Automated Scanning" table claims CodeQL, OWASP Dependency Check, and GitHub Dependabot are all active. None are: CodeQL/Dependency Review were explicitly removed (documented in an inline `security.yml` comment — GitHub Advanced Security requires a paid plan on a private repo), no OWASP job exists, and no `.github/dependabot.yml` exists. |
| DP3 | WARNING | `SECURITY.md` links to a hardening guide (`docs/guides/security-hardening.md`) that does not exist — no `docs/guides/` directory at all. |
| DP4 | PASS | macOS binaries being unsigned/unnotarized is an explicitly documented, accepted, known limitation (not a hidden gap). |

### Product Understanding

| # | Class | Finding |
|---|---|---|
| P1 | PASS | Product purpose, mission, and target users are clearly stated and internally consistent across `Vision.md`, `Mission.md`, and `PRD-001.md`. |
| P2 | PASS (high confidence, indirect evidence) | The relationship between Ferret and the separately-installed `tokensave` tool was resolved with strong circumstantial evidence (rename history, dogfooding-log mentions, absence from Ferret's own competitive-landscape doc) even though no single document states it directly. See `AEF-Onboarding.md` §2 and Glossary. |
| P3 | WARNING | `docs/001-Product/PRD-001.md`, the authoritative product-requirements document, is itself Status: Draft, Pending Architecture Review — not a formally approved reference. |
| P4 | UNKNOWN | Business/monetization model beyond "no commercial entity requires monetization" (`Mission.md` §8.3) — no revenue model, pricing, or funding source is documented for a project with stated "enterprise" ambitions. |

## Confidence by Area

| Area | Confidence | Justification |
|---|---|---|
| Architecture | **8/10 — High** | Claims cross-checked directly against the code graph (`tokensave module_api`, `circular`, `god_class`, `dsm`), not just docs. One structural anomaly (a reported circular dependency) was investigated and correctly shown to be a false positive rather than taken at face value. Residual uncertainty: `ARCH-001.md` §15+ (of 2,148 lines) and the newest ARCH-037 were not deep-read. |
| Documentation | **8/10 — High** | Broad inventory (every `docs/` subfolder counted and sampled) plus targeted freshness checks (`git log -1` per key doc) and active inconsistency-hunting. Residual uncertainty: no full repo-wide link-checker pass was run, only a sample of the most structurally important documents. |
| Roadmap | **9/10 — High** | Cross-checked against `git log`/`git merge-base` ground truth, not just documents — this is what surfaced the central finding (§R1) and correctly resolved the `v2/...` branch/worktree question (§R2) with direct evidence rather than assumption. |
| Governance | **7/10 — Medium-High** | The ADR/AGR/Decision-Log workflow was read and cross-referenced thoroughly. Residual uncertainty: `docs/Reviews/AGR-002`–`AGR-004` were not deep-read (only their existence and outcome status), and whether the `.ai/agents/` role-authority model has any tooling enforcement (G5) is genuinely unknown, not just under-researched. |
| Testing | **6/10 — Medium** | Real coverage/risk numbers were obtained, but the measurement tool itself was shown to produce false negatives, and the test suite was not executed (by design, to avoid a slow/heavy operation during discovery). The qualitative findings (stated-vs-actual mismatches in test organization and CI behavior) are high-confidence; the quantitative coverage numbers are a low-confidence floor. |
| Build | **8/10 — High** | Verified directly against `.csproj`/`Directory.Build.*`/CI YAML content, not inferred from docs. |
| Deployment | **8/10 — High** | Verified directly against workflow YAML, release-process runbook, and dated release notes, all mutually consistent. The security-scanning overstatement (DP2) was caught by directly reading the workflow file's own inline comment explaining the removal, not by inference. |
| Product Understanding | **7/10 — Medium-High** | Mission/Vision/PRD are clear and mutually consistent, and the non-obvious Ferret-vs-tokensave question was resolved with real (if indirect) evidence. Residual uncertainty: the PRD's own Draft status, and no documented business model despite stated enterprise ambitions. |

## Human Intervention Required: **YES**

Not because AEF failed to understand the repository, but because several of the findings above are genuinely decisions only a maintainer/Founder can make, not facts a discovery pass can resolve:

1. **Reconcile or deliberately re-scope** `PROJECT-STATE.md`, `ROADMAP-001.md`, and `.ai/session.md` against the real current state (R1) — or make an explicit call that a different mechanism should own "current state" going forward.
2. **ADR-0029** (v2 sharing/RBAC scope) explicitly awaits a Founder decision — this onboarding pass cannot and should not make it.
3. **ADR-0025's Proposed status** should be formally resolved (Accept, revise, or reject) given it is already being treated as binding (G1).
4. **Decide the disposition of `docs/002-Architecture/decisions/`** — migrate its two files into `docs/adr/`, or explicitly document it as a legacy/archival location (A4).
5. **Fix the four confirmed dangling links** left by the last cleanup pass (D3) — small, but they affect `SECURITY.md`, a document users may rely on during an incident.
6. **Decide precedence among the four overlapping future-vision documents** (D4) so contributors don't have to guess which is authoritative.
7. **Confirm whether the single-owner `CODEOWNERS` pattern (G3) is intentional** (solo maintainer today) or should be broadened before it becomes a bus-factor risk.

None of these require urgent action to keep the repository functioning — the build, tests, and release pipeline all work as documented (B1, DP1) — but all seven require a human (or a future, explicitly-authorized AEF work item) to resolve, not another discovery pass.

## Recommended Improvements for AEF Itself

Only improvements surfaced directly by this onboarding run:

1. **Cross-check "current state" documents against `git log` ground truth as a standard onboarding step, not an ad hoc one.** This run's single most important finding (R1) only surfaced because the roadmap research pass independently ran `git log`/`git merge-base` instead of trusting `PROJECT-STATE.md`/`ROADMAP-001.md` at face value. AEF's onboarding process should make "diff self-declared current-state docs against git history" a required, named step for every onboarding, since self-declared "must stay current" documents are exactly the ones most likely to silently drift (they create false confidence precisely because they claim authority).
2. **Don't trust a single canonical-location claim for decision records — sweep for ADR-shaped documents repository-wide.** This run found a third, undocumented ADR location (`docs/002-Architecture/decisions/`, A4) only because one research agent happened to go looking past the documented `docs/adr/` folder. A systematic grep for ADR-shaped filenames/frontmatter across the whole repo (not just the folder the README says to look in) should be a standard onboarding step for any project claiming to use ADRs.
3. **Treat code-graph/structural-analysis tool output as a hypothesis to verify, not a fact to report.** `tokensave`'s coverage tools produced a confirmed false negative (T3) and its circular-dependency detector produced one confirmed false positive (A6) in this run. Both were caught because the synthesis step spot-checked surprising results against source before reporting them. AEF should codify "verify surprising structural-tool findings against source before including them in a report" as a standing instruction, since these tools are being relied on specifically to avoid full-repo reads — the failure mode (silently trusting a wrong tool output) is otherwise invisible.
4. **Make onboarding output re-consumable, not just a point-in-time report.** This run produced a specific, actionable "don't trust these documents" list (§14 of `AEF-Onboarding.md`). A future AEF session working in this repo should be able to load that list directly rather than re-deriving it from scratch — onboarding packages should be structured so a later AEF session can cheaply check "has anything in the distrust-list changed?" rather than repeating the full discovery.
