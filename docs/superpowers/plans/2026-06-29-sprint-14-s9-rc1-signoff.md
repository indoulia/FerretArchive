# Sprint 14 S9: RC1 Sign-off Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to walk this plan step-by-step. Each task is a distinct verification pass; do not skip any step. Log failures as GitHub issues immediately — do not batch them. This plan is the gate: Sprint 14 is declared complete only when all 77 checklist items are verified green.

**Goal:** Walk every item on the RC1 Readiness Checklist (77 items across 11 categories), verify pass/fail with a concrete command or procedure, open a GitHub issue for every failure, write the CHANGELOG.md entry, create the DOGFOOD.md template, and publish the `v0.14.0-sprint14` release tag.

**Architecture (walk-and-verify):** S9 is a verification sprint, not an implementation sprint. The worker runs commands, inspects output, and makes a binary pass/fail decision for each checklist item. When a check fails: file a GitHub issue, record the failure, continue. Do not fix inline — fixes belong on a separate branch linked to the issue. At the end, if any P0 (blocking) failures remain unfixed, do not tag.

**Tech Stack:** .NET 9, `dotnet CLI`, `gh CLI` (GitHub), `ferret` binary (self-contained release build), PowerShell / bash for scripts, xUnit for test execution confirmation.

## Global Constraints

- Commit prefix: `chore(sprint-14):`
- Sprint tag: `v0.14.0-sprint14`
- All `gh issue create` calls use `--label "rc1-gap"` and `--milestone "RC1"`
- All verification commands run against the Release build (`-c Release`), not Debug
- Verification environment: a fresh checkout of `master` with no `.ferret/` directory seeded — simulate a new user
- Log output is compared to stderr (not stdout) per the logging contract
- If more than 5 P0 gaps are found, escalate to a sprint extension before tagging

## File Structure

**Files written by this plan:**
- `CHANGELOG.md` — RC1 entry covering Sprints 13 and 14
- `docs/DOGFOOD.md` — 25-task dogfood log template
- `.ferret/` — initialised index (committed for dogfood setup item)

**Files read (not modified):**
- `docs/superpowers/specs/2026-06-29-sprint-14-rc1-checklist.md`
- All `docs/` documentation files (existence checks)
- CI workflow files in `.github/workflows/`

---

## Task 1: Walk the Correctness Checklist (10 items)

**Checklist section:** Correctness

- [ ] **Step 1: Build the release binary**

  ```powershell
  dotnet publish src/Ferret.Cli/Ferret.Cli.csproj -r win-x64 --self-contained -c Release -o out/win-x64
  ```
  Expected: `Build succeeded.` No errors. Binary `out/win-x64/ferret.exe` exists.

- [ ] **Step 2: Initialise a fresh workspace from the samples directory**

  ```powershell
  cd samples
  ../out/win-x64/ferret.exe init
  ../out/win-x64/ferret.exe index
  ```
  Expected: `Indexed N files, skipped M files, 0 errors in X.Xs.` where N > 0.
  PASS = non-empty index, correct document count visible in log.

- [ ] **Step 3: Verify `ferret search` returns results containing the search term**

  ```powershell
  ../out/win-x64/ferret.exe search "workspace"
  ```
  Expected: top-10 results all contain the word "workspace" in their content or filename.
  PASS = zero false positives in the result set (inspect each returned path manually or via grep).

- [ ] **Step 4: Verify `ferret search` returns empty on a term not in the index**

  ```powershell
  ../out/win-x64/ferret.exe search "xyzzy_nonexistent_term_9f3a2"
  ```
  Expected: `No results found.` (or empty JSON array if `--format json`). Exit code 0.

- [ ] **Step 5: Verify MCP `search` tool is consistent with CLI search**

  Start `ferret serve` in background. In a second shell, invoke the MCP `search` tool directly via the in-process E2E test harness or via `ferret serve --test-mode`:
  ```powershell
  ../out/win-x64/ferret.exe serve &
  # use the E2E MCP client helper (see Task 8, Step 3) to call search("workspace")
  ```
  Expected: result set matches `ferret search "workspace"` result set (same document IDs).

- [ ] **Step 6: Verify MCP `read_document` returns full file content**

  Using the MCP test client, call `read_document` with a known path from the `samples/` workspace.
  Expected: response body equals `Get-Content <path>` verbatim (no truncation, no encoding artifacts).

- [ ] **Step 7: Verify MCP `ferret_context` returns a non-empty structured package**

  Call `ferret_context` with query `"workspace indexing"`.
  Expected: JSON response with at least one `documentExcerpt` item whose `content` is non-empty and relevant.

- [ ] **Step 8: Verify delete removes document from index**

  ```powershell
  $uniqueTerm = "ferret_delete_sentinel_$(Get-Random)"
  "content: $uniqueTerm" | Out-File samples/delete-test.md -Encoding utf8
  ../out/win-x64/ferret.exe index
  ../out/win-x64/ferret.exe search $uniqueTerm   # must return delete-test.md
  Remove-Item samples/delete-test.md
  ../out/win-x64/ferret.exe index
  ../out/win-x64/ferret.exe search $uniqueTerm   # must return zero results
  ```
  PASS = zero results after delete + reindex.

- [ ] **Step 9: Verify rename indexes under new path and removes old path**

  ```powershell
  $term = "ferret_rename_sentinel_$(Get-Random)"
  "content: $term" | Out-File samples/rename-before.md -Encoding utf8
  ../out/win-x64/ferret.exe index
  Rename-Item samples/rename-before.md samples/rename-after.md
  ../out/win-x64/ferret.exe index
  ../out/win-x64/ferret.exe search $term
  ```
  PASS = results contain `rename-after.md`, do not contain `rename-before.md`.

- [ ] **Step 10: Verify `--rebuild` produces identical results to fresh index**

  ```powershell
  ../out/win-x64/ferret.exe index --rebuild
  $countRebuild = (../out/win-x64/ferret.exe search "*" --format json | ConvertFrom-Json).Count
  Remove-Item -Recurse .ferret
  ../out/win-x64/ferret.exe index
  $countFresh = (../out/win-x64/ferret.exe search "*" --format json | ConvertFrom-Json).Count
  ```
  PASS = `$countRebuild -eq $countFresh`. Also verify double-run produces no errors:
  ```powershell
  ../out/win-x64/ferret.exe index; ../out/win-x64/ferret.exe index
  ```
  PASS = exit code 0 both times, no error output.

- [ ] **Step 11: Record failures** — for any step above that fails, run:
  ```powershell
  gh issue create --title "RC1 gap: [describe failing item]" `
    --body "**Checklist item:** Correctness > [item text]`n`n**Reproduction:**`n\`\`\`powershell`n[paste failing command]`n\`\`\``n`n**Actual output:**`n[paste output]`n`n**Expected output:**`n[paste expectation]" `
    --label "rc1-gap" --milestone "RC1"
  ```

---

## Task 2: Walk the File Watching Checklist (10 items)

**Checklist section:** File Watching

- [ ] **Step 1: Verify `ferret watch` command exists**

  ```powershell
  ../out/win-x64/ferret.exe watch --help
  ```
  PASS = exit code 0, help text printed describing the watch command.

- [ ] **Step 2: Verify `ferret index --watch` is accepted as alias**

  ```powershell
  $proc = Start-Process -FilePath ../out/win-x64/ferret.exe -ArgumentList "index","--watch" -PassThru -NoNewWindow
  Start-Sleep -Seconds 2
  $proc.HasExited | Should -Be $false
  Stop-Process -InputObject $proc
  ```
  PASS = process starts without error and stays alive (not exiting immediately).

- [ ] **Step 3: Verify new file triggers reindex within 2 seconds**

  ```powershell
  $proc = Start-Process -FilePath ../out/win-x64/ferret.exe -ArgumentList "watch" -PassThru -NoNewWindow -RedirectStandardError watch.log
  Start-Sleep -Seconds 1
  $sentinel = "watch_new_$(Get-Random)"
  "content: $sentinel" | Out-File samples/watch-new.md -Encoding utf8
  Start-Sleep -Seconds 3
  $result = ../out/win-x64/ferret.exe search $sentinel --format json | ConvertFrom-Json
  Stop-Process -InputObject $proc
  Remove-Item samples/watch-new.md -ErrorAction SilentlyContinue
  ```
  PASS = `$result.Count -gt 0` within 3 seconds.

- [ ] **Step 4: Verify modified file triggers reindex within 2 seconds**

  ```powershell
  "initial content" | Out-File samples/watch-modify.md -Encoding utf8
  ../out/win-x64/ferret.exe index
  $proc = Start-Process -FilePath ../out/win-x64/ferret.exe -ArgumentList "watch" -PassThru -NoNewWindow
  Start-Sleep -Seconds 1
  $sentinel = "modified_$(Get-Random)"
  "content: $sentinel" | Out-File samples/watch-modify.md -Encoding utf8
  Start-Sleep -Seconds 3
  $result = ../out/win-x64/ferret.exe search $sentinel --format json | ConvertFrom-Json
  Stop-Process -InputObject $proc
  Remove-Item samples/watch-modify.md -ErrorAction SilentlyContinue
  ```
  PASS = `$result.Count -gt 0`.

- [ ] **Step 5: Verify deleted file is removed from index within 2 seconds**

  ```powershell
  $sentinel = "delete_watch_$(Get-Random)"
  "content: $sentinel" | Out-File samples/watch-delete.md -Encoding utf8
  ../out/win-x64/ferret.exe index
  $proc = Start-Process -FilePath ../out/win-x64/ferret.exe -ArgumentList "watch" -PassThru -NoNewWindow
  Start-Sleep -Seconds 1
  Remove-Item samples/watch-delete.md
  Start-Sleep -Seconds 3
  $result = ../out/win-x64/ferret.exe search $sentinel --format json | ConvertFrom-Json
  Stop-Process -InputObject $proc
  ```
  PASS = `$result.Count -eq 0`.

- [ ] **Step 6: Verify watch respects `.ferretignore`**

  Add `*.ignored` to `.ferretignore`. Create `samples/test.ignored`. Wait 3 seconds.
  PASS = `ferret search` finds no content from `test.ignored`. Check `watch.log` shows file was not indexed (no "indexed" log line for that file).

- [ ] **Step 7: Verify watch logs file change events**

  Inspect `watch.log` from Step 3.
  PASS = log contains a line with the filename and action (e.g., `[FileWatcher] indexed samples/watch-new.md`).

- [ ] **Step 8: Verify debounce under rapid modification**

  ```powershell
  $proc = Start-Process -FilePath ../out/win-x64/ferret.exe -ArgumentList "watch" -PassThru -NoNewWindow -RedirectStandardError rapid.log
  1..20 | ForEach-Object { "content $_" | Out-File samples/rapid.md -Encoding utf8 }
  Start-Sleep -Seconds 3
  Stop-Process -InputObject $proc
  ```
  PASS = process did not crash (exit code from `$proc.ExitCode` is null / process still ran). Log does not show more than 2-3 reindex events for `rapid.md` (debounce coalesced them).

- [ ] **Step 9: Verify Ctrl+C exits with code 0**

  Start `ferret watch` in a terminal. Press Ctrl+C.
  PASS = `$LASTEXITCODE -eq 0`, no exception stack trace printed.

- [ ] **Step 10: Verify transient I/O error does not crash watcher**

  Lock a file in the watched directory (open with exclusive FileShare.None in PowerShell):
  ```powershell
  $stream = [System.IO.File]::Open("samples/locked.md", "OpenOrCreate", "ReadWrite", "None")
  # watcher is running; attempt to index locked.md should log warning and continue
  Start-Sleep -Seconds 2
  $stream.Close()
  ```
  PASS = watcher still running after the lock period; `ferret watch` log contains `WARN` for `locked.md`.

- [ ] **Step 11: Record failures** — use `gh issue create` template from Task 1 Step 11 for any failing item.

---

## Task 3: Walk the Incremental Indexing Checklist (8 items)

**Checklist section:** Incremental Indexing

- [ ] **Step 1: Verify single changed file triggers single file reindex**

  ```powershell
  ../out/win-x64/ferret.exe index   # establish baseline
  "modified" | Out-File samples/one-change.md -Encoding utf8
  ../out/win-x64/ferret.exe index --verbose 2>&1 | Tee-Object incremental.log
  Select-String "files changed" incremental.log
  ```
  PASS = log contains `1 file changed` (or equivalent wording).

- [ ] **Step 2: Verify unchanged workspace indexes 0 files**

  ```powershell
  ../out/win-x64/ferret.exe index 2>&1 | Tee-Object unchanged.log
  Select-String "files changed" unchanged.log
  ```
  PASS = `0 files changed`.

- [ ] **Step 3: Verify incremental reindex completes in under 2 seconds**

  ```powershell
  "changed" | Out-File samples/one-change.md -Encoding utf8
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  ../out/win-x64/ferret.exe index
  $sw.Stop()
  ```
  PASS = `$sw.Elapsed.TotalSeconds -lt 2`.

- [ ] **Step 4: Verify mtime is the primary change signal**

  Inspect source: `grep -r "LastWriteTime\|mtime" src/ --include="*.cs"` — must find references in the incremental fingerprinting code.
  PASS = `LastWriteTimeUtc` (or equivalent) is read and compared per-file in the index pipeline.

- [ ] **Step 5: Verify `--hash` fallback option is accepted**

  ```powershell
  ../out/win-x64/ferret.exe index --hash --verbose
  ```
  PASS = exit code 0, log indicates hash-based change detection was used (look for "hash" in log output).

- [ ] **Step 6: Verify `--rebuild` bypasses incremental logic**

  ```powershell
  ../out/win-x64/ferret.exe index   # set all files as up-to-date
  ../out/win-x64/ferret.exe index --rebuild --verbose 2>&1 | Tee-Object rebuild.log
  Select-String "files changed\|reindexing\|rebuild" rebuild.log
  ```
  PASS = log shows all N files reindexed (not 0).

- [ ] **Step 7: Verify incremental state survives restart**

  ```powershell
  ../out/win-x64/ferret.exe index
  # verify .ferret/ contains state file
  Test-Path .ferret/index-state.json  # or equivalent state file name
  # restart (new process) — verify 0 files changed on next run
  ../out/win-x64/ferret.exe index --verbose 2>&1 | Select-String "0 files changed"
  ```
  PASS = state file exists and 0 files changed after restart.

- [ ] **Step 8: Verify corrupted state falls back to full reindex**

  ```powershell
  "corrupted json {{{" | Out-File .ferret/index-state.json -Encoding utf8
  ../out/win-x64/ferret.exe index 2>&1 | Tee-Object corrupt.log
  Select-String "warn\|fallback\|corrupt\|full reindex" corrupt.log -SimpleMatch
  ```
  PASS = exit code 0, WARN logged, full reindex performed (N files indexed, not 0).

- [ ] **Step 9: Record failures** — use `gh issue create` template for any failing item.

---

## Task 4: Walk the Performance Checklist (8 items)

**Checklist section:** Performance

- [ ] **Step 1: Verify 10,000-file index under 60 seconds**

  Use the performance test workspace generator (or the benchmark test in CI):
  ```powershell
  dotnet test tests/ -c Release --filter "Category=Benchmark" --logger "console;verbosity=normal"
  ```
  PASS = benchmark test `IndexTenThousandFilesUnder60Seconds` passes (green).

- [ ] **Step 2: Verify 1,000-file index under 10 seconds**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=Benchmark&DisplayName~1000"
  ```
  PASS = test passes.

- [ ] **Step 3: Verify search returns in under 200 ms at 10,000 documents**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=Benchmark&DisplayName~SearchPerf"
  ```
  PASS = `SearchTenThousandDocumentsUnder200Ms` test passes.

- [ ] **Step 4: Verify `ferret serve` cold-start under 3 seconds**

  ```powershell
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  $proc = Start-Process -FilePath ../out/win-x64/ferret.exe -ArgumentList "serve" -PassThru -NoNewWindow -RedirectStandardError serve-start.log
  # wait for "MCP server ready" line in log
  $deadline = [DateTime]::Now.AddSeconds(5)
  while (-not (Select-String "MCP server ready\|ready\|listening" serve-start.log -Quiet) -and [DateTime]::Now -lt $deadline) {
      Start-Sleep -Milliseconds 100
  }
  $sw.Stop()
  Stop-Process -InputObject $proc
  ```
  PASS = `$sw.Elapsed.TotalSeconds -lt 3`.

- [ ] **Step 5: Verify peak memory during 10k-file index under 512 MB**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=Benchmark&DisplayName~MemoryIndex"
  ```
  PASS = `IndexMemoryUnder512MB` test passes; or manually measure via Process.PeakWorkingSet64 during benchmark.

- [ ] **Step 6: Verify idle `ferret serve` memory under 100 MB after 10k index**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=Benchmark&DisplayName~MemoryServe"
  ```
  PASS = `ServeIdleMemoryUnder100MB` test passes.

- [ ] **Step 7: Verify 10k index benchmark test exists in CI**

  ```powershell
  Get-ChildItem tests/ -Recurse -Filter "*.cs" | Select-String "IndexTenThousandFiles\|10.*000.*file\|Benchmark" | Select-Object -First 5
  ```
  PASS = at least one test file contains a benchmark with the 60s assertion.

- [ ] **Step 8: Verify search 200ms benchmark test exists in CI**

  ```powershell
  Get-ChildItem tests/ -Recurse -Filter "*.cs" | Select-String "SearchTenThousand\|200.*ms\|SearchPerf" | Select-Object -First 5
  ```
  PASS = at least one test file contains a search benchmark with the 200ms assertion.

- [ ] **Step 9: Record failures** — use `gh issue create` template for any failing item.

---

## Task 5: Walk the Reliability Checklist (8 items)

**Checklist section:** Reliability

- [ ] **Step 1: Verify non-workspace directory prints clear error**

  ```powershell
  $tmp = New-TemporaryFile | Split-Path
  $tmpDir = Join-Path $tmp "ferret-nonws-$(Get-Random)"
  New-Item -ItemType Directory $tmpDir
  Push-Location $tmpDir
  ferret.exe index
  $exitCode = $LASTEXITCODE
  Pop-Location
  Remove-Item -Recurse $tmpDir
  ```
  PASS = `$exitCode -ne 0`, stderr contains a human-readable message (not a stack trace).

- [ ] **Step 2: Verify `ferret search` with no index prints remediation message**

  ```powershell
  $tmp = New-TemporaryFile | Split-Path
  $tmpDir = Join-Path $tmp "ferret-noidx-$(Get-Random)"
  New-Item -ItemType Directory $tmpDir
  Push-Location $tmpDir
  ferret.exe init
  ferret.exe search "test"
  $exitCode = $LASTEXITCODE; Pop-Location; Remove-Item -Recurse $tmpDir
  ```
  PASS = stderr contains `No index found. Run \`ferret index\` first.`, exit code 1.

- [ ] **Step 3: Verify `ferret serve` with no index starts and returns structured errors**

  ```powershell
  $proc = Start-Process -FilePath ferret.exe -ArgumentList "serve" -PassThru -NoNewWindow -RedirectStandardError serve-noindex.log
  Start-Sleep -Seconds 2
  # call MCP search tool; expect structured JSON error, not crash
  Stop-Process -InputObject $proc
  ```
  PASS = process started, logged a warning about missing index, and responded to MCP tool call with a JSON error body (not an unhandled exception).

- [ ] **Step 4: Verify locked file is skipped gracefully**

  ```powershell
  $stream = [System.IO.File]::Open("samples/locked.md", "OpenOrCreate", "ReadWrite", "None")
  ferret.exe index 2>&1 | Tee-Object locked.log
  $stream.Close()
  Select-String "warn\|skip\|locked" locked.log -SimpleMatch
  ```
  PASS = exit code 0, locked file logged as skipped/warning, other files indexed normally.

- [ ] **Step 5: Verify binary file does not crash indexer**

  Copy a `.png` or `.exe` into `samples/`. Run `ferret index`.
  PASS = exit code 0, binary file either skipped (WARN logged) or metadata-only indexed, no stack trace.

- [ ] **Step 6: Verify malformed MCP JSON does not crash server**

  ```powershell
  # send malformed JSON over stdio to ferret serve
  $proc = Start-Process -FilePath ferret.exe -ArgumentList "serve" -PassThru -NoNewWindow -RedirectStandardInput -RedirectStandardOutput -RedirectStandardError
  $proc.StandardInput.WriteLine("{this is not json}")
  Start-Sleep -Seconds 1
  $alive = -not $proc.HasExited
  Stop-Process -InputObject $proc -ErrorAction SilentlyContinue
  ```
  PASS = `$alive -eq $true` (server did not crash); stderr contains an error response JSON.

- [ ] **Step 7: Verify all commands exit 0 on success, non-zero on failure**

  ```powershell
  ferret.exe --version; $ver = $LASTEXITCODE
  ferret.exe index;     $idx = $LASTEXITCODE
  ferret.exe search "workspace"; $srch = $LASTEXITCODE
  @($ver, $idx, $srch) | ForEach-Object { $_ | Should -Be 0 }
  # failure case
  ferret.exe search "xyzzy_nonexistent_9f3a2"; $fail = $LASTEXITCODE
  ```
  PASS = success commands return 0; `ferret doctor` on bad config returns non-zero.

- [ ] **Step 8: Verify no unhandled exception stack traces on stderr in normal error scenarios**

  Review stderr output from Steps 1-7. Search for `Unhandled exception`, `System.Exception`, `at Ferret.` stack trace lines.
  PASS = zero occurrences of raw stack traces in any normal error scenario.

- [ ] **Step 9: Record failures** — use `gh issue create` template for any failing item.

---

## Task 6: Walk the Diagnostics Checklist (9 items)

**Checklist section:** Diagnostics

- [ ] **Step 1: Verify `ferret doctor` reports all five check categories**

  ```powershell
  ferret.exe doctor 2>&1 | Tee-Object doctor.log
  ```
  PASS = output contains: workspace existence check, index existence check, index freshness (last-indexed timestamp), MCP server reachability check, .NET runtime version check — all labeled PASS or FAIL.

- [ ] **Step 2: Verify `ferret doctor` exit codes**

  On a fully configured workspace: `ferret doctor` exits 0.
  On a workspace with no index: `ferret doctor` exits 1.
  ```powershell
  ferret.exe doctor; $okCode = $LASTEXITCODE   # should be 0 on good setup
  # remove index and re-run
  Remove-Item -Recurse .ferret/index -ErrorAction SilentlyContinue
  ferret.exe doctor; $failCode = $LASTEXITCODE  # should be 1
  ```
  PASS = `$okCode -eq 0`, `$failCode -eq 1`.

- [ ] **Step 3: Verify `ferret doctor` FAIL includes remediation hint**

  Review doctor.log from Step 1 for any FAIL lines.
  PASS = each FAIL line is followed (same line or next line) by a hint starting with "Run " or "Set " or similar actionable text.

- [ ] **Step 4: Verify log line format (timestamp, level, component, message)**

  ```powershell
  ferret.exe index --verbose 2>&1 | Select-Object -First 5
  ```
  PASS = each log line matches pattern: `\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}` (ISO 8601) + `ERROR|WARN|INFO|DEBUG` + `\[[\w]+\]` component + message.

- [ ] **Step 5: Verify `--log-level debug` global flag works**

  ```powershell
  ferret.exe --log-level debug index 2>&1 | Select-String "DEBUG"
  ```
  PASS = at least one DEBUG-level line appears in output that does not appear without the flag.

- [ ] **Step 6: Verify `ferret index --verbose` prints per-file log lines**

  ```powershell
  ferret.exe index --verbose --rebuild 2>&1 | Select-Object -First 20
  ```
  PASS = each file appears in a log line showing filename, parser used, and document ID.

- [ ] **Step 7: Verify `ferret index` summary line**

  ```powershell
  ferret.exe index 2>&1 | Select-String "Indexed"
  ```
  PASS = line matching `Indexed \d+ files, skipped \d+ files, \d+ errors in \d+\.\d+s` appears.

- [ ] **Step 8: Verify `ferret watch` startup banner**

  ```powershell
  $proc = Start-Process -FilePath ferret.exe -ArgumentList "watch" -PassThru -NoNewWindow -RedirectStandardError watch-banner.log
  Start-Sleep -Seconds 1
  Stop-Process -InputObject $proc
  Get-Content watch-banner.log | Select-Object -First 5
  ```
  PASS = first few lines contain the workspace path and directory count, e.g., `Watching /path/to/samples (3 directories)`.

- [ ] **Step 9: Verify log output goes to stderr, not stdout**

  ```powershell
  ferret.exe index --verbose > stdout.log 2> stderr.log
  (Get-Content stdout.log).Count   # should be 0 or only machine-readable output
  (Get-Content stderr.log).Count   # should contain all log lines
  ```
  PASS = log lines (timestamps, levels, components) appear only in `stderr.log`, not `stdout.log`. Structured output (e.g., JSON search results) appears only on stdout.

- [ ] **Step 10: Record failures** — use `gh issue create` template for any failing item.

---

## Task 7: Walk the Configuration Checklist (7 items)

**Checklist section:** Configuration

- [ ] **Step 1: Verify config validation on missing required fields**

  ```powershell
  '{"workspacePath": null}' | Out-File .ferret/config.json -Encoding utf8
  ferret.exe index 2>&1 | Tee-Object config-missing.log
  Select-String "workspacePath\|required\|missing" config-missing.log -SimpleMatch
  ```
  PASS = error names the specific field, exit code non-zero, no stack trace.

- [ ] **Step 2: Verify unknown config key produces warning (not crash)**

  ```powershell
  '{"unknownFutureSetting": 42}' | ConvertFrom-Json | Add-Member ... # merge into valid config
  # or write a config with one extra unknown key
  ferret.exe index 2>&1 | Select-String "unknown\|unrecognised\|unrecognized"
  ```
  PASS = WARN logged naming the unknown key, exit code 0, indexing proceeds.

- [ ] **Step 3: Verify `ferret config validate` command**

  ```powershell
  ferret.exe config validate; $ok = $LASTEXITCODE
  # write invalid config and re-test
  '{"bad": true}' | Out-File .ferret/config.json
  ferret.exe config validate 2>&1; $bad = $LASTEXITCODE
  ```
  PASS = `$ok -eq 0`, `$bad -eq 1` with diagnostic message.

- [ ] **Step 4: Verify `.ferretignore` excludes files**

  Add `*.log` to `.ferretignore`. Run `ferret index`. Search for content from a `.log` file.
  PASS = `.log` files absent from index results. `ferret index --verbose` log shows them as "ignored".

- [ ] **Step 5: Verify environment variable overrides config values**

  ```powershell
  $env:FERRET_OLLAMA_BASE_URL = "http://localhost:9999"
  ferret.exe --log-level debug index 2>&1 | Select-String "9999\|FERRET_OLLAMA_BASE_URL"
  $env:FERRET_OLLAMA_BASE_URL = $null
  ```
  PASS = debug log shows the env-var value was used, not the config file default.

- [ ] **Step 6: Verify `ferret --version` output format**

  ```powershell
  ferret.exe --version
  ```
  PASS = output matches `ferret 0\.14\.0` (or current release version). Exit code 0.

- [ ] **Step 7: Verify version string matches assembly version**

  ```powershell
  $fileVersion = (Get-Item out/win-x64/ferret.exe).VersionInfo.FileVersion
  $cliVersion = ferret.exe --version
  ```
  PASS = `$cliVersion` contains the same version numbers as `$fileVersion` (e.g., both contain `0.14.0`).

- [ ] **Step 8: Record failures** — use `gh issue create` template for any failing item.

---

## Task 8: Walk the Installation Checklist (9 items)

**Checklist section:** Installation

- [ ] **Step 1: Build win-x64 self-contained binary and verify it runs**

  ```powershell
  dotnet publish src/Ferret.Cli/Ferret.Cli.csproj -r win-x64 --self-contained -c Release -p:PublishTrimmed=true -o out/win-x64
  out/win-x64/ferret.exe --version
  ```
  PASS = binary produced, `--version` works without .NET SDK on PATH.

- [ ] **Step 2: Build osx-arm64 self-contained binary (on Apple Silicon or via cross-compile)**

  ```powershell
  dotnet publish src/Ferret.Cli/Ferret.Cli.csproj -r osx-arm64 --self-contained -c Release -p:PublishTrimmed=true -o out/osx-arm64
  Test-Path out/osx-arm64/ferret
  ```
  PASS = binary produced without build errors. (Runtime verification deferred to macOS CI runner.)

- [ ] **Step 3: Build osx-x64 self-contained binary**

  ```powershell
  dotnet publish src/Ferret.Cli/Ferret.Cli.csproj -r osx-x64 --self-contained -c Release -p:PublishTrimmed=true -o out/osx-x64
  Test-Path out/osx-x64/ferret
  ```
  PASS = binary produced without build errors.

- [ ] **Step 4: Build linux-x64 self-contained binary**

  ```powershell
  dotnet publish src/Ferret.Cli/Ferret.Cli.csproj -r linux-x64 --self-contained -c Release -p:PublishTrimmed=true -o out/linux-x64
  Test-Path out/linux-x64/ferret
  ```
  PASS = binary produced without build errors. (Runtime on Ubuntu 22.04 verified in CI.)

- [ ] **Step 5: Verify each platform binary is under 100 MB after trimming**

  ```powershell
  @("win-x64/ferret.exe","osx-arm64/ferret","osx-x64/ferret","linux-x64/ferret") | ForEach-Object {
      $size = (Get-Item "out/$_").Length / 1MB
      "$_ : $([math]::Round($size,1)) MB"
  }
  ```
  PASS = all sizes under 100 MB.

- [ ] **Step 6: Verify install scripts exist**

  ```powershell
  Test-Path scripts/install.sh
  Test-Path scripts/install.ps1
  ```
  PASS = both files exist.

- [ ] **Step 7: Verify install scripts are idempotent**

  ```powershell
  pwsh scripts/install.ps1
  pwsh scripts/install.ps1   # second run
  ferret --version
  ```
  PASS = second run produces no error, `ferret --version` works after both runs.

- [ ] **Step 8: Verify `ferret --version` works from a fresh shell after install**

  Open a new PowerShell terminal (to clear PATH cache). Run `ferret --version`.
  PASS = version string printed, exit code 0. No "command not found" error.

- [ ] **Step 9: Verify GitHub Actions CI publishes binaries on tagged commits**

  ```powershell
  gh workflow list
  gh workflow view release.yml   # or equivalent workflow file name
  ```
  PASS = workflow exists, triggers on `v*` tags, has publish steps for all four platforms.
  Alternatively check the last release run:
  ```powershell
  gh release list --limit 3
  gh release view v0.14.0-sprint14 --json assets
  ```
  PASS = four platform assets attached to the release (win-x64, osx-arm64, osx-x64, linux-x64).

- [ ] **Step 10: Record failures** — use `gh issue create` template for any failing item.

---

## Task 9: Walk the Documentation Checklist (9 items)

**Checklist section:** Documentation

- [ ] **Step 1: Verify QUICKSTART.md exists and covers all required sections**

  ```powershell
  Test-Path docs/QUICKSTART.md
  Select-String "ferret init|ferret index|ferret serve|Claude Desktop|install" docs/QUICKSTART.md
  ```
  PASS = file exists, all five topics covered. Estimated reading time: count words / 200 WPM < 5 minutes.

- [ ] **Step 2: Verify QUICKSTART.md includes Claude Desktop JSON config snippet**

  ```powershell
  Select-String "mcpServers\|ferret serve\|claude_desktop_config" docs/QUICKSTART.md
  ```
  PASS = JSON snippet with `mcpServers` key present.

- [ ] **Step 3: Verify CLI-REFERENCE.md exists and documents every command**

  ```powershell
  Test-Path docs/CLI-REFERENCE.md
  @("ferret init","ferret index","ferret search","ferret serve","ferret watch","ferret doctor","ferret config","ferret --version") | ForEach-Object {
      Select-String $_ docs/CLI-REFERENCE.md -Quiet | Should -Be $true
  }
  ```
  PASS = all shipped commands documented with flags, arguments, exit codes, and example.

- [ ] **Step 4: Verify CONFIGURATION.md exists and documents every config field**

  ```powershell
  Test-Path docs/CONFIGURATION.md
  Select-String "workspacePath\|type\|default\|description" docs/CONFIGURATION.md
  ```
  PASS = each field documented with type, default, and description.

- [ ] **Step 5: Verify CONFIGURATION.md documents environment variable overrides**

  ```powershell
  Select-String "FERRET_\|environment variable" docs/CONFIGURATION.md
  ```
  PASS = environment variable overrides section present.

- [ ] **Step 6: Verify MCP-TOOLS.md exists and documents all four tools**

  ```powershell
  Test-Path docs/MCP-TOOLS.md
  @("search","read_document","workspace_status","ferret_context") | ForEach-Object {
      Select-String $_ docs/MCP-TOOLS.md -Quiet | Should -Be $true
  }
  ```
  PASS = each tool has input schema, output schema, and example.

- [ ] **Step 7: Verify TROUBLESHOOTING.md exists and covers five common errors**

  ```powershell
  Test-Path docs/TROUBLESHOOTING.md
  (Select-String "FAIL\|## " docs/TROUBLESHOOTING.md).Count
  ```
  PASS = file exists, at least five distinct error scenarios with remediation steps.

- [ ] **Step 8: Verify samples/ directory exists with usable content**

  ```powershell
  Test-Path samples/
  (Get-ChildItem samples/ -Recurse -File).Count
  ```
  PASS = directory exists, contains at least one markdown file and one code file.

- [ ] **Step 9: Verify README.md is updated for RC1**

  ```powershell
  Select-String "RC1\|0\.14\.0\|QUICKSTART\|install" README.md
  ```
  PASS = README mentions RC1 release, install instructions present, link to QUICKSTART.md present.

- [ ] **Step 10: Record failures** — use `gh issue create` template for any failing item.

---

## Task 10: Walk the Testing Checklist (11 items)

**Checklist section:** Testing

- [ ] **Step 1: Run E2E test — index samples, search known term, assert top-3 result**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~IndexAndSearch" --logger "console;verbosity=normal"
  ```
  PASS = test passes. Test names the file that must appear in top-3 results.

- [ ] **Step 2: Run E2E test — delete file, reindex, assert zero results**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~DeleteAndSearch"
  ```
  PASS = test passes.

- [ ] **Step 3: Run E2E test — start serve, call MCP search, assert results**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~ServeAndSearch"
  ```
  PASS = test passes. Subprocess ferret serve is started and torn down by the test.

- [ ] **Step 4: Run E2E test — call `ferret_context`, assert non-empty package**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~FerretContext"
  ```
  PASS = test passes.

- [ ] **Step 5: Run E2E test — watch, create file, assert indexed within 3 seconds**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~WatchAndIndex"
  ```
  PASS = test passes. Uses 3-second timeout with polling.

- [ ] **Step 6: Run E2E test — `ferret doctor` on good workspace, assert exit 0 + all PASS**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~DoctorPass"
  ```
  PASS = test passes.

- [ ] **Step 7: Run E2E test — `ferret doctor` on workspace with no index, assert exit 1**

  ```powershell
  dotnet test tests/ -c Release --filter "Category=E2E&DisplayName~DoctorFail"
  ```
  PASS = test passes.

- [ ] **Step 8: Run full unit test suite on current platform**

  ```powershell
  dotnet test tests/ -c Release --logger "trx;LogFileName=rc1-test-results.trx"
  ```
  PASS = all tests pass (zero failures, zero errors). No test is skipped without a linked issue.

- [ ] **Step 9: Verify CI passes on all required platforms (check CI status)**

  ```powershell
  gh run list --workflow "ci.yml" --limit 5 --json status,conclusion,headBranch
  ```
  PASS = most recent run on `master` shows success on Windows x64 and Linux x64 runners.

- [ ] **Step 10: Verify test count has not decreased from Sprint 13**

  ```powershell
  # compare current test count with Sprint 13 tag
  git stash
  git checkout v0.13.0-sprint13
  $s13Count = (dotnet test tests/ -c Release --no-build --list-tests 2>&1 | Select-String "^\s+").Count
  git checkout master
  git stash pop
  $s14Count = (dotnet test tests/ -c Release --no-build --list-tests 2>&1 | Select-String "^\s+").Count
  "$s13Count tests at Sprint 13, $s14Count tests now"
  ```
  PASS = `$s14Count -ge $s13Count`.

- [ ] **Step 11: Verify no `[Ignore]` or `[Skip]` without linked issue**

  ```powershell
  Get-ChildItem tests/ -Recurse -Filter "*.cs" | Select-String "\[Ignore\]|\[Skip\]" | ForEach-Object {
      # each match: verify nearby comment contains a GitHub issue URL
      $_.Line
  }
  ```
  PASS = zero `[Ignore]`/`[Skip]` found, or every found instance has a GitHub issue URL in a comment on the same or adjacent line.

- [ ] **Step 12: Record failures** — use `gh issue create` template for any failing item.

---

## Task 11: Walk the Dogfooding Checklist (5 items)

**Checklist section:** Dogfooding

- [ ] **Step 1: Verify Ferret repo itself is indexed**

  ```powershell
  Test-Path .ferret/
  ferret.exe search "IIndexPipeline" --format json | ConvertFrom-Json | Select-Object -First 3
  ```
  PASS = `.ferret/` directory exists, search returns results from the Ferret source tree.

- [ ] **Step 2: Verify Claude Desktop / Claude Code is connected to `ferret serve`**

  ```powershell
  ferret.exe serve &
  ferret.exe doctor 2>&1 | Select-String "MCP server"
  ```
  PASS = `ferret doctor` shows MCP server reachability as PASS.

- [ ] **Step 3: Verify DOGFOOD.md exists with at least 25 completed task entries**

  ```powershell
  Test-Path docs/DOGFOOD.md
  (Select-String "^\| \d+\|" docs/DOGFOOD.md).Count
  ```
  PASS = file exists, at least 25 rows in the task table.

- [ ] **Step 4: Verify all high-impact failures in DOGFOOD.md have fixes or linked issues**

  Manually review DOGFOOD.md for rows where `result` contains "workaround" or "failed".
  PASS = each such row has a non-empty `issue` column with a `#NNN` GitHub issue reference.

- [ ] **Step 5: Verify 25th task was completed after the last high-impact fix merge**

  Check the `date` field on task row 25 in DOGFOOD.md. Compare against the merge date of the last high-impact fix (visible via `gh pr list --state merged --label "rc1-gap" --limit 5`).
  PASS = task 25 date is after the last fix merge date.

- [ ] **Step 6: Record failures** — use `gh issue create` template for any failing item.

---

## Task 12: Write the CHANGELOG.md Entry

- [ ] **Step 1: Write CHANGELOG.md**

  Write (or prepend to) `CHANGELOG.md` at the repository root with the RC1 release entry shown below. If the file already exists, prepend the new entry before any existing entries.

```markdown
# Changelog

All notable changes to Ferret (ContextOS) are recorded in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [0.14.0] — RC1 — 2026-06-29

### Summary

Ferret RC1 is the first production-ready release. After Sprints 8–13 delivered the
core platform (workspace engine, document parsing, keyword search, MCP server, context
assembly), Sprint 14 hardened it: file watching, incremental indexing, performance
benchmarks, diagnostics, a cross-platform installer, documentation, end-to-end tests,
and mandatory dogfooding. A developer can install Ferret, index a workspace, and have
a Claude Desktop integration running in under five minutes.

---

### Added — Sprint 14

**File Watching**
- `ferret watch` command: monitors workspace for file changes using `FileSystemWatcher`
  with 500 ms debounce, triggers incremental re-index on create/modify/delete events.
- `ferret index --watch` accepted as an alias for `ferret watch`.
- Watcher respects `.ferretignore` and `.gitignore` — ignored files are not watched.
- Startup banner shows workspace path and number of directories under watch.
- Graceful Ctrl+C shutdown with exit code 0.
- Transient I/O errors on individual files are logged and skipped; the watcher continues.

**Incremental Indexing**
- Mtime-based fingerprinting: only changed files are re-parsed on subsequent `ferret index` runs.
- `ferret index --hash` opt-in flag for content-hash change detection (useful after VCS checkout
  with reset timestamps).
- `ferret index --rebuild` bypasses incremental logic and reindexes all files unconditionally.
- Incremental state persisted to `.ferret/index-state.json`; survives process restart.
- Corrupted state triggers automatic fallback to full reindex with a WARN log line.

**Performance**
- 10,000-file workspace indexed in under 60 seconds on Apple M-series / equivalent x64.
- 1,000-file workspace indexed in under 10 seconds.
- `ferret search` returns in under 200 ms for a 10,000-document index.
- `ferret serve` cold-start (process launch to MCP-ready) under 3 seconds.
- Peak index memory: under 512 MB at 10,000 files.
- `ferret serve` idle memory: under 100 MB after indexing 10,000 files.
- CI benchmark tests guard all six performance targets; benchmarks are tagged and skipped
  on machines without the `performance` test category tag.

**Diagnostics**
- `ferret doctor` command: checks workspace existence, index existence, index freshness,
  MCP server reachability, and .NET runtime version. Exits 0 on all-pass, 1 on any fail.
  Each FAIL line includes a one-line remediation hint.
- `ferret --log-level debug` global flag enables verbose logging for any command.
- `ferret index --verbose` prints per-file log lines (filename, parser, document ID).
- Structured log format: ISO 8601 timestamp + level + `[Component]` + message, written to stderr.
- `ferret index` completion summary: `Indexed N files, skipped M files, 0 errors in X.Xs.`

**Configuration**
- `.ferret/config.json` field-level validation: missing or malformed fields produce an error
  naming the specific field and its expected type.
- Unknown config keys produce a WARN (not a crash) naming the unrecognised key.
- `ferret config validate` command: exits 0 on valid config, 1 with diagnostics on invalid.
- `.ferretignore` supported at workspace root; patterns exclude files from indexing and watching.
- Environment variables override config file values; documented in `docs/CONFIGURATION.md`.
- `ferret --version` outputs `ferret X.Y.Z` matching the assembly version.

**Installer and Release Pipeline**
- Self-contained single-binary publish for `win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`
  using `--self-contained -p:PublishTrimmed=true`. Each binary under 100 MB.
- `scripts/install.sh` (macOS/Linux) and `scripts/install.ps1` (Windows): detect platform,
  download the correct binary, place it on PATH. Idempotent.
- GitHub Actions `release.yml` workflow: triggers on `v*` tags, builds all four platform
  binaries, attaches them as release assets.

**Documentation**
- `docs/QUICKSTART.md`: install → init → index → serve → Claude Desktop integration in under
  five minutes. Includes the exact JSON snippet for Claude Desktop `claude_desktop_config.json`.
- `docs/CLI-REFERENCE.md`: every shipped command documented with flags, arguments, exit codes,
  and one example invocation.
- `docs/CONFIGURATION.md`: every `.ferret/config.json` field (type, default, description) and
  every environment variable override.
- `docs/MCP-TOOLS.md`: all four MCP tools (`search`, `read_document`, `workspace_status`,
  `ferret_context`) with input/output schemas and examples.
- `docs/TROUBLESHOOTING.md`: five most common setup errors, `ferret doctor` FAIL messages,
  and remediation steps.
- `samples/` directory: small markdown and code workspace usable for verifying a fresh install.
- `README.md` updated: RC1 install instructions, full feature list through Sprint 13, link to
  `docs/QUICKSTART.md`.
- `docs/DOGFOOD.md`: 25-task log of real engineering work completed using Ferret as the primary
  context source before RC1 was declared.

**End-to-End Tests**
- E2E: index `samples/`, search for known term, assert file in top-3 results.
- E2E: index `samples/`, delete file, reindex, search unique term, assert zero results.
- E2E: start `ferret serve` subprocess, call MCP `search` tool, assert results returned.
- E2E: start `ferret serve` subprocess, call `ferret_context`, assert non-empty package.
- E2E: start `ferret watch`, create new file, wait ≤3 s, assert indexed.
- E2E: `ferret doctor` on correct workspace → exit 0, all PASS.
- E2E: `ferret doctor` with missing index → exit 1, index-check FAIL.

---

### Added — Sprint 13

**Context Assembly**
- `IContextAssembler` and `ContextAssemblyEngine`: builds a ranked context package from search
  results, trimming to a configurable token budget.
- `ContextPackage` / `DocumentExcerpt` value types carry provenance (file path, line range,
  relevance score) alongside the text excerpt.
- `ferret_context` MCP tool: accepts a natural-language query, returns a structured context
  package for use in LLM prompts.
- `IContextScorer`: relevance scoring strategy interface with BM25-based default implementation.
- `IExcerptExtractor`: window-based excerpt extraction; expands to nearest sentence boundary.
- Token budget enforcement: `ContextBudgetEnforcer` trims excerpt list to fit within a
  configurable token limit (default 8,192 tokens).

**MCP Server Enhancements**
- `workspace_status` MCP tool: returns index document count, last-indexed timestamp,
  workspace path, and Ferret version.
- MCP server wires `ferret_context` alongside existing `search` and `read_document` tools.
- Structured JSON error responses for malformed requests (no unhandled exceptions).

---

### Added — Sprints 8–12 (summary for release notes completeness)

- **Sprint 8 (Connector Platform):** `IConnector` abstraction; filesystem connector;
  `.gitignore` / `.ferretignore` filter chain.
- **Sprint 9 (Document Pipeline):** `IDocumentParser` abstraction; Markdown, C#, plain-text
  parsers; `IIndexPipeline` orchestration; `ferret index` command.
- **Sprint 10 (Search Platform):** BM25 keyword search; `IQueryParser`; `ferret search` command;
  JSON and table output formatters.
- **Sprint 11 (Integration Platform):** MCP server (`ferret serve`); `search` and `read_document`
  MCP tools; stdio transport; Claude Desktop integration.
- **Sprint 12 (AI Platform):** `IModelRouter`; `IModelRegistry`; Ollama provider;
  OpenAI provider; `Ferret.Configuration.AI`; AI options validation.

---

### Fixed — Sprint 14

- `ferret index` no longer crashes on binary files (`.exe`, `.png`, `.dll`) — they are
  detected and skipped with a WARN log line.
- `ferret serve` no longer exits with an unhandled exception when the index is absent —
  it starts and returns structured error responses from MCP tools.
- Rapid file modification sequences no longer cause the file watcher to emit redundant
  reindex events — debounce coalesces events within a 500 ms window.
- Corrupted `.ferret/index-state.json` no longer crashes the indexer — automatic fallback
  to full reindex with a WARN.

---

### Removed — Sprint 14

Nothing removed.

---

### Security — Sprint 14

No security-relevant changes in Sprint 14. Authentication and access control are deferred
to V2 (see Section 1.6 of the Sprint 14 spec for the full deferred list).

---

[0.14.0]: https://github.com/indoulia/Ferret/releases/tag/v0.14.0-sprint14
```

- [ ] **Step 2: Verify CHANGELOG.md is well-formed**

  ```powershell
  Test-Path CHANGELOG.md
  Select-String "\[0\.14\.0\]" CHANGELOG.md
  ```
  PASS = file exists, version header present.

---

## Task 13: Create DOGFOOD.md

- [ ] **Step 1: Write `docs/DOGFOOD.md`**

  Write the file with the following content:

```markdown
# Ferret Dogfood Log

**Purpose:** Record every real engineering task completed using Ferret as the primary
context source before RC1 is declared. Unit tests verify correctness; this log verifies
usability. Ferret must not be supplemented with manual file reads or GitHub search for
any task marked complete — if a workaround was needed, record it.

**Gate:** 25 tasks required. All high-impact failures (workaround used on 3+ tasks)
must be fixed and the 25th task must be completed after the last fix is merged.

---

## Task Log

| # | Task | Date | Ferret answered? | Workaround used? | Issue |
|---|------|------|-----------------|-----------------|-------|
| 1 | Locate the class that implements `IIndexPipeline` and read its constructor dependencies | 2026-06-29 | | | |
| 2 | Find all callers of `IIndexEngine.SearchAsync` to understand query call sites | 2026-06-29 | | | |
| 3 | Identify which parsers are registered in the DI container and in what order | 2026-06-29 | | | |
| 4 | Determine what happens when `ferret index` encounters a file larger than the configured size limit | 2026-06-29 | | | |
| 5 | Find the test that covers BM25 scoring and understand what inputs it uses | 2026-06-29 | | | |
| 6 | Locate every place where `.ferretignore` patterns are evaluated and understand the evaluation order | 2026-06-29 | | | |
| 7 | Understand the data flow from `ferret search "term"` CLI invocation to the SQLite query | 2026-06-29 | | | |
| 8 | Find where the MCP `search` tool JSON schema is defined and what validation is applied to inputs | 2026-06-29 | | | |
| 9 | Identify which component is responsible for the 500 ms debounce in `ferret watch` | 2026-06-29 | | | |
| 10 | Determine where incremental index state (`index-state.json`) is read and written | 2026-06-29 | | | |
| 11 | Locate the exception handler that catches I/O errors during file indexing and understand the fallback path | 2026-06-29 | | | |
| 12 | Find the implementation of `ferret doctor` and enumerate which checks it performs | 2026-06-29 | | | |
| 13 | Understand how `--log-level debug` is propagated from the CLI flag to the `ILogger` configuration | 2026-06-29 | | | |
| 14 | Find the publish configuration that enables single-file trimmed output and confirm `PublishTrimmed=true` | 2026-06-29 | | | |
| 15 | Locate the GitHub Actions workflow responsible for release asset publishing and identify the trigger condition | 2026-06-29 | | | |
| 16 | Understand how `ferret config validate` differentiates between missing fields and type-mismatched fields | 2026-06-29 | | | |
| 17 | Find the `ferret_context` MCP tool handler and trace how it invokes `IContextAssembler` | 2026-06-29 | | | |
| 18 | Identify where the token budget is enforced during context assembly and what the default limit is | 2026-06-29 | | | |
| 19 | Locate all E2E tests and understand how they start and stop `ferret serve` as a subprocess | 2026-06-29 | | | |
| 20 | Find the benchmark test for 10,000-file indexing and understand how the test workspace is generated | 2026-06-29 | | | |
| 21 | Determine which `IDocumentParser` handles C# files and what metadata fields it extracts | 2026-06-29 | | | |
| 22 | Understand how `workspace_status` MCP tool computes the last-indexed timestamp | 2026-06-29 | | | |
| 23 | Find the `read_document` MCP tool implementation and confirm it returns full file content without truncation | 2026-06-29 | | | |
| 24 | Locate where environment variable overrides are applied relative to config file loading — confirm precedence order | 2026-06-29 | | | |
| 25 | Understand the full startup sequence of `ferret serve` from `Program.cs` to "MCP server ready" log line | 2026-06-29 | | | |

---

## Failure Analysis

Record any task where `Workaround used?` is Yes, with a root cause and resolution status.

| Task # | Root cause | Sprint 14 fix? | GitHub issue |
|--------|-----------|----------------|-------------|
| *(none yet)* | | | |

---

## Sign-off

- [ ] All 25 tasks completed
- [ ] All high-impact failures (workaround on 3+ tasks) fixed
- [ ] Task 25 completed after last high-impact fix merged
- [ ] `docs/DOGFOOD.md` committed to `master` before tagging RC1
```

- [ ] **Step 2: Verify DOGFOOD.md is well-formed**

  ```powershell
  Test-Path docs/DOGFOOD.md
  (Select-String "^\| \d+\|" docs/DOGFOOD.md).Count
  ```
  PASS = file exists, 25 rows present.

---

## Task 14: Tag the Release

- [ ] **Step 1: Verify all P0 checklist gaps are resolved**

  ```powershell
  gh issue list --label "rc1-gap" --state open --json number,title,labels
  ```
  PASS = zero open issues with `rc1-gap` label that are also labeled `P0` or `blocking`. If any P0 gaps remain open: **do not tag** — halt and escalate.

- [ ] **Step 2: Commit CHANGELOG.md and DOGFOOD.md**

  ```powershell
  git add CHANGELOG.md docs/DOGFOOD.md
  git commit -m @'
  chore(sprint-14): RC1 CHANGELOG and DOGFOOD log

  Adds CHANGELOG.md RC1 entry covering Sprints 8-14 features.
  Adds docs/DOGFOOD.md 25-task engineering log template.

  Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
  '@
  ```

- [ ] **Step 3: Push master to remote**

  ```powershell
  git push origin master
  ```
  PASS = push succeeds, CI green on `master`.

- [ ] **Step 4: Create and push the release tag**

  ```powershell
  git tag v0.14.0-sprint14
  git push origin v0.14.0-sprint14
  ```
  PASS = tag pushed. CI `release.yml` workflow triggers.

- [ ] **Step 5: Verify release workflow completes and assets are published**

  ```powershell
  gh run watch --workflow release.yml
  gh release view v0.14.0-sprint14 --json assets,tagName,publishedAt
  ```
  PASS = four platform binaries attached as release assets (ferret-win-x64.exe, ferret-osx-arm64, ferret-osx-x64, ferret-linux-x64).

- [ ] **Step 6: Announce RC1**

  Post a brief note in the team channel (or as a GitHub Releases note) stating:
  - Tag: `v0.14.0-sprint14`
  - Install: `scripts/install.ps1` (Windows) or `scripts/install.sh` (macOS/Linux)
  - Quickstart: `docs/QUICKSTART.md`
  - Known gaps: link to open `rc1-gap` issues (non-P0)

---

## Self-Review Pass

After completing Tasks 1–14, run the following self-review before declaring the sprint done:

- [ ] Every one of the 77 checklist items has been evaluated and recorded as PASS or documented as a filed GitHub issue.
- [ ] No checklist item was skipped without a written justification.
- [ ] `CHANGELOG.md` is present at the repo root, contains the `[0.14.0]` entry, and the content accurately reflects what was shipped in Sprints 13 and 14.
- [ ] `docs/DOGFOOD.md` contains 25 task rows; the sign-off checklist at the bottom is accurate.
- [ ] The git tag `v0.14.0-sprint14` exists on `master` and is pushed to `origin`.
- [ ] The GitHub release `v0.14.0-sprint14` has four platform binary assets attached.
- [ ] No open `rc1-gap` P0 issues exist (confirm via `gh issue list --label "rc1-gap,P0" --state open`).
- [ ] The `chore(sprint-14):` commit containing CHANGELOG.md and DOGFOOD.md is the commit at the tag.
