# Ferret RC1 — Validation Report

**Version:** 0.14.0-rc1
**Platform:** Windows x64 (self-contained single-file)
**Date:** 2026-06-30
**Decision:** ✅ **GO** (with minor non-blocking issues noted)

---

## 1. Build Information

| Field | Value |
|---|---|
| Product version | 0.14.0 (RC1) |
| Configuration | Release |
| Runtime ID | win-x64 |
| Deployment | Self-contained, single-file (`PublishSingleFile=true`, `SelfContained=true`) |
| Trimming | Disabled (`PublishTrimmed=false`) — favors reliability over size |
| Native libs | Embedded for self-extract (`IncludeNativeLibrariesForSelfExtract=true`) |
| .NET SDK | 9.0.313 |
| Target framework | net9.0 |
| Embedded version string | `0.14.0+<commit>` (via `AssemblyInformationalVersion`) |
| `ferret.exe` size | 38,978,901 bytes (≈37.2 MB) |

### Source changes made for RC1 (release-blocking fixes only)

1. **`src/Ferret.Cli/Ferret.Cli.csproj`** — bumped stale `<Version>0.11.0</Version>` → `0.14.0`. The explicit `<Version>` overrides `VersionPrefix`, so the published binary previously reported `0.11.0` for a `0.14.0` release. *(Defect RC1-001)*
2. **`src/Ferret.Cli/Ferret.Cli.csproj`** — added `<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>`. Without it, the native `e_sqlite3.dll` was emitted as a sidecar file, so a true single-file `ferret.exe` crashed on every SQLite operation (index/search/context/serve). *(Defect RC1-002)*
3. **`src/Ferret.Mcp/Transport/Stdio/StdioTransport.cs`** — MCP `serverInfo.version` was hardcoded to `0.11.0`; now derived from the running assembly's informational version (commit suffix stripped). *(Defect RC1-003)*

No feature or architectural changes were introduced.

---

## 2. Package Contents

**Archive:** `Ferret-0.14.0-rc1-win-x64.zip` (33,498,412 bytes, ≈31.9 MB)
**SHA256(zip):** `124120923bd5e2ae9fa1dff5e0f7aab7cfee8ee85dd30f61387f18d03aa9957e`

Archive root folder: `Ferret-0.14.0-rc1-win-x64/`

| File | Size (bytes) | SHA256 |
|---|---|---|
| `ferret.exe` | 38,978,901 | `7cafedf34ee0aa22f9c1829b64f8357c2f5b1ce7b075d089dc5e5bf8c569bc43` |
| `LICENSE` | 1,097 | `b7f67431745306afdd8259eea11ac7f879a97b874b5a9c27eb0aefdfe4580e3d` |
| `README.md` | 2,446 | `9e59d9c22c5b7052968b823c3f241a4f2fea78fe084692e7f35930c4161be81a` |
| `CHANGELOG.md` | 9,604 | `31b0ec869358d7927e3b06b24c9a485e5a86763d6a3f6fcf890200f9f061f8ad` |
| `install.ps1` | 2,268 | `2701bf9528a5f0021747fb8d663f92c820bbd2b1290cacaba7d4fc7f7d79a261` |
| `uninstall.ps1` | 1,559 | `381cdd042e632ed67b23e6fbcfeeee786c4cf79721ec316724cdc54d36890cee` |
| `SHA256SUMS.txt` | 470 | (manifest of the above) |

`SHA256SUMS.txt` was re-verified against the extracted files on a clean machine: **6/6 OK, 0 failures.**

`install.ps1` / `uninstall.ps1` are new for RC1 (per-user, no-admin): install copies `ferret.exe` to `%LOCALAPPDATA%\Programs\Ferret` and adds it to the user `PATH`; uninstall reverses both and preserves workspace data.

---

## 3. Smoke Test Results

Validated on a clean directory: ZIP extracted fresh, installed via `install.ps1`, exercised against a throwaway sample workspace (3 markdown/text files). The installed `ferret.exe` was used for every command — nothing from the dev tree.

| # | Step | Command | Result |
|---|---|---|---|
| 1 | Install | `install.ps1 -InstallDir <dir>` | ✅ exit 0; copied exe, updated PATH, printed version |
| 2 | Initialize workspace | `ferret workspace init` | ✅ exit 0; `.ferret/` created |
| 3 | Workspace status | `ferret workspace status` | ✅ exit 0; ID + root + created date |
| 4 | Index | `ferret index` | ✅ exit 0; Discovered 3 / Indexed 3 / Failed 0 |
| 5 | Search (text) | `ferret search authentication --no-highlight` | ✅ exit 0; 2 ranked hits (bm25-fts5) |
| 6 | Search (JSON) | `ferret search token --format json` | ✅ exit 0; well-formed JSON |
| 7 | Context | `ferret context "authentication tokens" --max-documents 3` | ✅ exit 0; token-budgeted context assembled |
| 8 | MCP server | `ferret serve` (JSON-RPC `initialize`) | ✅ valid response; capabilities tools/resources/logging; `serverInfo.version = 0.14.0` |
| 9 | Manual | `ferret manual --port 7071` | ✅ HTTP 200, 48 KB page, correct title |
| 10 | Watch | `ferret watch` | ✅ starts, prints watch banner, runs until Ctrl+C |
| 11 | Doctor | `ferret doctor` | ✅ exit 0; all checks pass (1 informational warning) |
| 12 | Uninstall | `uninstall.ps1` | ✅ exit 0; dir + PATH entry removed; workspace data preserved |
| 13 | Version | `ferret --version` / `ferret version` | ✅ `0.14.0+<commit>` |
| 14 | Self-contained | run on host without project SDK on PATH | ✅ runs (single file, embedded runtime + native libs) |

**Unit regression (changed project):** `Ferret.Mcp.Tests` — ✅ **51 passed, 0 failed, 0 skipped** (80 ms).

---

## 4. Known Issues

All issues below are **non-blocking** for RC1.

| ID | Severity | Area | Description |
|---|---|---|---|
| RC1-004 | Low (cosmetic) | search JSON | `canonicalUri` renders as `file:///filesystem:///notes.txt` (doubled scheme). Display/`documentId` are correct; ranking unaffected. |
| RC1-005 | Low (cosmetic) | search text | Document title is printed twice in the text renderer (e.g. `Sample Project` / `Sample Project`). |
| RC1-006 | Low | doctor | `AI provider configured` reports a warning when no Ollama/OpenAI provider is set. Expected for a default install; informational only. |
| RC1-007 | Info | publish output | Release publish also emits `*.pdb` / `*.xml` sidecars into `artifacts/win-x64/`. These are **not** included in the package (only `ferret.exe` is shipped), so there is no user impact. |

### Resolved during validation (were release-blocking)

| ID | Area | Resolution |
|---|---|---|
| RC1-001 | Versioning | Binary reported `0.11.0`; fixed csproj `<Version>` → `0.14.0`. |
| RC1-002 | SQLite native lib | Single-file crashed (`DllNotFoundException: e_sqlite3`); fixed via `IncludeNativeLibrariesForSelfExtract`. |
| RC1-003 | MCP version drift | `serverInfo.version` hardcoded `0.11.0`; now derived from assembly version. |

---

## 5. Recommended Go / No-Go Decision

### ✅ GO for RC1

**Rationale:** The complete end-user journey — install → initialize → index → search → context → MCP serve → manual → watch → uninstall — succeeds from the packaged single-file executable on a clean environment. All three release-blocking defects discovered during validation were fixed and re-verified. Remaining known issues are cosmetic or informational and do not affect correctness, installability, or the documented workflow.

**Conditions / follow-ups for GA (not blocking RC1):**
- Address cosmetic search-rendering issues RC1-004 and RC1-005.
- Suppress dependency `*.pdb`/`*.xml` sidecars in the publish output (RC1-007) for a cleaner `artifacts/` directory.
- Produce and validate the remaining platform packages (osx-arm64, osx-x64, linux-x64) before GA; `publish.ps1` already supports them.
