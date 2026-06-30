# FAQ

Frequently asked questions about Ferret RC1.

---

## What file types does Ferret index?

Ferret RC1 indexes: `.cs`, `.md`, `.json`, `.txt`, `.xml`, `.yaml`/`.yml`, `.html`, `.ini`, `.config`, and most plain-text source files. Binary files (`.dll`, `.exe`, `.png`, `.db`) are skipped. See [Parsers](../user-guide/parsers) for the full list.

---

## How is Ferret different from GitHub Copilot?

GitHub Copilot is an AI code completion tool integrated into your editor. It uses a cloud model trained on public code.

Ferret is a workspace indexer and MCP server. It does not generate code — it makes your specific codebase searchable by AI assistants like Claude. Ferret is local-first: your code never leaves your machine (unless you choose a cloud AI provider). Ferret and Copilot are complementary, not competing.

---

## Does Ferret send my code to the cloud?

Not by default. Ferret indexes your code locally using SQLite and serves search results over the local MCP stdio transport. Your code is never transmitted to any external service by Ferret itself.

If you configure an AI provider (OpenAI, Anthropic), your prompts and context packages are sent to that provider's API. Use Ollama for a fully local setup with no external API calls.

---

## What is MCP?

MCP (Model Context Protocol) is an open protocol that allows AI assistants to call external tools. Claude Desktop, Cursor, and VS Code with GitHub Copilot all support MCP. Ferret implements MCP as a stdio server. When an AI assistant asks "search my codebase for X", it calls Ferret's `ferret_search` MCP tool.

---

## Does Ferret require a .NET SDK to run?

No. Ferret ships as a self-contained binary. The .NET 9 runtime is bundled inside the executable. You do not need to install .NET or any SDK.

---

## How large can my workspace be?

Ferret has been tested on workspaces with up to 50,000 documents. The SQLite FTS5 index handles this comfortably. Index size is roughly 1-3x the size of the raw text content. A typical .NET project with 5,000 source files produces an index around 20-50 MB.

---

## Can I index multiple projects in one workspace?

Yes. Configure multiple connector instances in `workspace.json`, each with a different `root` path. They all contribute to the same index and are searchable together.

---

## How do I exclude files from indexing?

Two ways:
1. Add `.ferretignore` at the workspace root (gitignore syntax)
2. Add `exclude` patterns to the connector in `workspace.json`

Both methods produce the same result. `.ferretignore` is easier for users; `workspace.json` excludes are better for connector-specific rules.

---

## Does Ferret support Windows, macOS, and Linux?

Yes. Ferret ships self-contained binaries for:
- `win-x64` (Windows 10/11, Windows Server)
- `osx-arm64` (Apple Silicon)
- `osx-x64` (Intel Mac)
- `linux-x64` (Ubuntu 20.04+, Debian, RHEL)

---

## Can I use Ferret with Cursor?

Yes. Add Ferret as an MCP server in Cursor → Settings → MCP. See [Connect Claude](../getting-started/connect-claude) — the Cursor configuration is identical.

---

## What is the performance impact of `ferret watch`?

`ferret watch` uses the OS filesystem event API (FSEvents on macOS, inotify on Linux, ReadDirectoryChangesW on Windows). The CPU overhead is negligible when files are not changing. When changes occur, the debounced re-index runs in the background.

---

## How do I update Ferret?

Download the new binary, replace the existing one, and run `ferret doctor`. If the workspace schema has changed, doctor will prompt you to upgrade.

---

## What does "RC1" mean?

RC1 (Release Candidate 1) is the first production-ready release of Ferret, corresponding to v0.14.0. It represents the completion of Sprint 14 and the end of the initial 14-sprint development cycle. Post-RC1 development will focus on dogfooding and evidence-driven feature additions.

---

## Why is the CLI tool called `ferret`?

A ferret finds things. The name reflects the product's core value: surfacing relevant context from your codebase. The name was chosen in Sprint 5 (ADR-0005) to replace the working title "AISpace", which was too generic.

---

## Where are the logs?

Ferret does not write persistent log files by default. Use `ferret doctor --verbose` for diagnostic output. To enable debug logging:

```bash
FERRET_LOGGING__LOGLEVEL__DEFAULT=Debug ferret index
```

## Related

- [Troubleshooting](../troubleshooting) — specific error fixes
- [Getting Started](../getting-started/index) — installation and first use
- [CLI Reference](../reference/cli) — complete command reference
