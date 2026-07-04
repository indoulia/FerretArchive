# Ferret Dogfooding & Fix-Loop Plan — 2026-07-04 (v2, comprehensive)

This supersedes the v1 plan committed earlier today. v1's blocker (App1/App2 path) is resolved; scope has grown from "observe only" to "observe, file, fix, test, merge, repeat" — this version documents that whole loop before continuing execution, per explicit instruction to plan first.

## 0. Governing rules (recap, so this document is self-contained)

- **DOGFOOD-001 is active.** No new features, no architecture changes, no new platform layers. Bug fixes found *during* dogfooding are explicitly in scope per DOGFOOD-001's own text ("Work is limited to: using Ferret, recording evidence, and fixing bugs the dogfooding surfaces").
- **Evidence before architecture:** every fix in this loop is a behavioral/code-level fix, not a design change. If a bug turns out to need an architecture change, I stop and write a proposal instead of implementing it — I do not quietly reach for a bigger change than the evidence justifies.
- **No work-identifying names** (e.g. specific employer/product names found in sample files) in anything committed, logged, or filed as an issue — sample PDFs in App1/App2 contain such content; references to them in this repo's logs/issues are kept generic.
- **`main` is protected** — every change goes through a branch + PR, merged via `gh pr merge`, never a direct push. Confirmed this the hard way earlier today.
- **Cost discipline:** cheap CLI/timing work done directly; the one activity with real external $ cost (the live Claude-based A/B benchmark harness) is run with reduced trials, and its actual cost is reported afterward, not hidden.
- **Honesty over shortcuts:** false leads get ruled out and reported as ruled out (see Bug candidate #0 below), not silently dropped or padded into fake findings.

## 1. Status snapshot at the time this plan was written

| Item | Status |
|---|---|
| `.NET` test suite baseline | 0 failures, 652 passed / 5 skipped (pre-existing skips), 16 projects — see v1 plan for the full table |
| Bug candidate #0 — `ferret doctor` garbled icons | **Ruled out.** Confirmed via native PowerShell (UTF-8 codepage 65001) that icons render correctly; Git Bash's console encoding was the artifact. Not filed. |
| **Bug #1 — `ferret index` fails on every directory** | Confirmed, root-caused, GitHub issue pending (label was missing, fixed, refiling now). `FilesystemConnector.OpenAsync` (`src/Ferret.Connectors.Filesystem/FilesystemConnector.cs:76-84`) calls `File.OpenRead` on every discovered asset with no check for `AssetKind.Directory`, which `WalkDirectoryAsync` (lines 133-151) deliberately yields for every subdirectory. Severity: High. |
| Sample data | Located: `C:\POC\FerretTest\App1` and `App2`, 11 PDFs each (identical sets). A pre-existing, well-built A/B benchmark harness already lives at `C:\POC\FerretTest\Benchmarking\` (`Run-FerretBenchmark.ps1`) with dated results from 2026-07-01 already showing Ferret-connected sessions correctly answer PDF questions that non-Ferret sessions cannot. |
| Label hygiene | Repo's actual established label is `dogfooding` (not `dogfood` as my memory had it) — corrected; duplicate label removed. |

## 2. The fix loop

For every bug found from here on:

1. **Reproduce and verify** — trace to file:line, actively rule out false positives (environment/terminal artifacts, my own misconfiguration) before treating it as real.
2. **File a GitHub issue** on `indoulia/Ferret`, label `dogfooding`, DOGFOOD-001 severity rubric, with repro steps, root cause, and a suggested fix direction.
3. **Branch**: `fix/<short-slug>` off `main`.
4. **TDD**: write a failing test reproducing the bug at unit/component level → confirm red → implement the minimal behavioral fix → confirm green.
5. **Regression check**: full `dotnet test src/Ferret.sln`.
6. **PR**: push the branch, open a PR referencing `Fixes #N`.
7. **Merge**: via `gh pr merge` (protected branch requires this path regardless). Per your explicit "push and merge, start again" instruction, I will merge each PR in this loop without a manual review pause — flagging this plainly now since it's a deliberate, informed reading of that instruction, not a silent assumption.
8. **Close the loop**: confirm the issue closed (via `Fixes #N` or manually), then return to dogfooding for the next one.

One issue = one branch = one PR. No batching unrelated fixes together, so each merge stays independently reviewable and revertable.

## 3. Workstreams, in order, for the current unattended window

1. **Fix Bug #1** (index directory-handling) — TDD → test → PR → merge.
2. **Component/unit test-coverage gap survey** (TokenSave-based) — reported as a punch list. A coverage gap becomes a fix-loop item only if it's hiding an actual defect, not just "no test exists."
3. **Continue manual CLI dogfooding** on this repo (`ferret watch`, `ferret search` variations, `ferret ask`/`serve` if usable) — feeds the same fix loop with anything else found.
4. **Benchmark capture**:
   - No extra cost: Ferret's own DOGFOOD-001 Phase 6 metrics — index timing, search latency, cold-start, index size on disk — measured directly via CLI timing.
   - Real cost: re-run `Run-FerretBenchmark.ps1` with reduced trials (1, not the default 2) against the real App1/App2 PDFs for fresh warm/cold/capability data; actual dollar cost reported from the output afterward, not estimated in advance.
5. **App1/App2 PDF dogfooding** — index App1, run real search/ask queries, log quality and timing honestly; any reference to the sample PDFs' content stays generic (no work-identifying names).
6. **Daily log** (`docs/dogfood/2026-07-04-daily-log.md`) — updated continuously.
7. **End-of-session report** — all issues filed, all PRs merged, explicit **Went Well / Didn't Go Well / Needs Improvement / Missing** breakdown, waiting when you're back rather than needing you mid-flight.

## 4. Guardrails held throughout, unattended or not

- No architecture changes — confirmed-bug fixes only.
- No fabricated data — every logged number is something actually measured.
- No work-identifying names in anything committed or filed.
- No direct commits to `main` — branch + PR + merge, every time.
- One bug, one branch, one PR.
- A fix that turns out to need an architecture change gets written up as a proposal, not implemented ad hoc.
