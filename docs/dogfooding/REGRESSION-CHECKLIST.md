ID: N/A
Title: Dogfooding Regression Checklist
Type: Reference
Status: Active
Version: 1.0
Owner: TODO
Approved By: TODO
Related Documents: [2026-07-04-daily-log.md](2026-07-04-daily-log.md), [2026-07-05-daily-log.md](2026-07-05-daily-log.md)
Last Updated: 2026-07-05

---

# Dogfooding Regression Checklist

Run this at the **start** of every new dogfooding session, before exploring new territory. It captures every real check performed across the 2026-07-04/05 sessions, so a future session can quickly confirm nothing has regressed before moving on to new ground (a "Dogfooding 2" pass).

Each item: the command, what a healthy result looks like, and the issue it would reopen if it regresses.

## 1. Test suite baseline

```
dotnet test src/Ferret.sln
```
Expect: 0 failures. (Pre-existing skips are fine: 1 perf-benchmark opt-out, 4 OpenAI live-API tests needing credentials.)

## 2. Basic CLI health

| Command | Healthy result |
|---|---|
| `ferret --version` | Prints version string, exits 0 |
| `ferret about` | Prints name/tagline/version/runtime, exits 0 |
| `ferret doctor` | All checks pass; icons render correctly in **PowerShell** (Git Bash mangles them — known terminal artifact, not a bug) |
| `ferret doctor --verbose` | Full parser platform report, no errors |
| `ferret workspace status` | Correct workspace ID/root/created date |

## 3. Index / search core (regression-sensitive — issues #13, #14, #15, #16, #22, #28)

```
ferret index
ferret search "<any real identifier from the repo>"
ferret search "some-hyphenated-term"          # issue #15
ferret search "<term>" --format json          # inspect canonicalUri — issue #22
```
Expect:
- `Discovered`/`Indexed`/`Failed` counts are non-zero and sane for the repo size (a full-repo run reporting `Discovered: 0` is the #27 zero-config trap — see §5).
- Hyphenated queries return results, not `Invalid query: empty or whitespace` (#15).
- `canonicalUri` in JSON output is a single, valid `filesystem:///...` URI, never double-wrapped like `file:///filesystem:///...` (#22).
- No `packages/` or `node_modules/`/`bin/`/`obj/` content polluting results (#16, and the still-open release-lag #9/#13 for directory-open failures — expected until a new npm release ships).

## 4. Rename/delete hygiene (issue #28)

```
echo "content-marker-<random>" > .tmp-regression-marker.md   # marker MUST be in the file
ferret index                                                  # CONTENT, not just the filename --
ferret search "<random>"                       # a filename-only marker won't be found by
mv .tmp-regression-marker.md .tmp-regression-marker-renamed.md   # full-text search and gives a
ferret index                                                  # false "0 results" that looks like
ferret search "<random>" --format json         # a bug but isn't (learned the hard way, 2026-07-05)
rm .tmp-regression-marker-renamed.md
ferret index                                  # clean up the index entry too
```
Expect: after rename, exactly one hit, at the new path. Two hits (old + new) is a #28 regression.

## 5. Connector lifecycle (issues #26, #27 — handle with care, see warning)

```
ferret connector list                         # CONFIGURED: no (nothing configured yet)
ferret connector enable --type filesystem --name regression-test --path <subfolder>
ferret connector list                         # CONFIGURED: yes  -- issue #26
ferret connector inspect --name regression-test
ferret connector disable --name regression-test
ferret index                                  # MUST still discover the whole workspace -- issue #27
```
**Warning:** before #27's fix, this exact sequence permanently disabled all indexing. If step 6 reports `Discovered: 0`, that is a #27 regression — do not proceed with other dogfooding until resolved (check `.ferret/connectors.json` and `ConnectorManager.GetActiveConnectorsAsync`'s synthesis condition first).

## 6. Config path consistency (issue #24)

```
ferret config validate                        # with no ferret.json present
```
Expect: `Config file not found: ferret.json` (not `ferret.config.json` or `.ferret/config.json`). If any of Ferret's own messages or `docs/` disagree on the config filename, that's a #24 regression.

## 7. Watch (issue #17 — known, unresolved)

```
ferret watch &
echo marker > .tmp-watch-test.md
# poll: ferret search "<marker text>" every ~1s
```
Expect (currently): may take well over the documented ≤5s target before the change is searchable. This is **known and open** (#17) — not yet fixed. Re-test only to see if it's gotten better or worse, not as a pass/fail gate.

## 8. Status / diagnostics honesty (issue #25 — known, unresolved)

```
ferret status                                  # always says "not running" -- known stub, #25
ferret start &
ferret status                                  # still says "not running" -- expected until #25 is implemented
```

## 9. MCP tool layer (issue #20, #21)

Exercise `search`, `context`(`ferret_context`), `read_document`, `workspace_status` tools directly if an MCP host is available. Confirm:
- `search` returns a real error (not "no results") for an invalid/failing query (#20, fixed).
- `context`/`ferret_context` on a failing search still silently reports 0 documents with no error — **known, open** (#21).

## 10. `--passages` (issue #23 — known, unresolved)

```
ferret search "<term>" --format json > a.json
ferret search "<term>" --passages --format json > b.json
diff a.json b.json
```
Expect (currently): **no diff** — the flag is a documented no-op. Known and open (#23). A fix here is the one item on this list that would represent genuinely new functionality landing, not a regression check.

## Known-open items (not regressions — check status before re-testing)

| Issue | What | Status |
|---|---|---|
| #9 / #13 | Directory-open failures during `ferret index` | Fixed on `main`, pending npm release |
| #17 | `ferret watch` visibility latency | Open, no confident root cause yet |
| #18 | `ferret doctor` freshness has no git-branch awareness | Open, needs design decision |
| #21 | `ContextAssembler` swallows search failures silently | Open, needs public-contract decision |
| #23 | `--passages` flag is a no-op | Open, real feature work needed |
| #25 | `ferret status` is a hardcoded stub | Open, needs real IPC design |

## Fixed this session (verify these hold, don't re-litigate them)

#14, #15, #16, #19, #20, #22, #24, #26, #27, #28 — all fixed via TDD on the `dogfooding` branch, all confirmed via full-solution-green at time of fix. If any of §3-§6 above regress, check these issues' original repro steps (linked from `2026-07-04-daily-log.md` / `2026-07-05-daily-log.md`) before re-diagnosing from scratch.
