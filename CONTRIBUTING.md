# Contributing to Ferret

Thank you for your interest in contributing! This document explains how to participate in the project.

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Documentation](#documentation)

---

## Code of Conduct

All participants are expected to follow our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Getting Started

### Prerequisites

| Tool | Minimum Version |
|---|---|
| .NET SDK | 9.0 |
| PowerShell | 7.4 |
| Git | 2.40 |
| Docker (optional) | 24.0 |

### Bootstrap

```powershell
git clone https://github.com/indoulia/Ferret.git
cd ferret
./scripts/bootstrap.ps1
```

The bootstrap script installs local tools, restores NuGet packages, and validates the environment.

---

## Development Workflow

1. **Fork** the repository and create a feature branch from `main`.
2. **Branch naming** — use `feat/`, `fix/`, `docs/`, `refactor/`, `test/` prefixes.
   ```
   feat/add-mcp-transport-layer
   fix/agent-timeout-handling
   docs/adr-012-storage-backend
   ```
3. **Commit messages** follow [Conventional Commits](https://www.conventionalcommits.org/).
   ```
   feat(mcp): add SSE transport adapter
   fix(cli): correct default config path on Windows
   docs(adr): record decision on plugin isolation model
   ```
4. **Keep commits small and focused.** One logical change per commit.

---

## Coding Standards

- All C# code must pass `dotnet format` with no warnings.
- Enable `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — the build enforces this.
- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- No raw `string` for identifiers; prefer `nameof()`, constants, or typed enums.
- Async all the way — no `.Result` or `.Wait()` outside of entry points.
- Structured logging only — `ILogger<T>`, never `Console.WriteLine` in library code.

---

## Testing Requirements

- Every new feature must ship with unit tests.
- Bug fixes must include a regression test.
- Aim for ≥ 80 % line coverage on new code.
- Integration tests live in `tests/` and may require Docker.

```powershell
# Run all unit tests
dotnet test src/Ferret.sln --filter "Category!=Integration"

# Run integration tests (requires Docker)
dotnet test src/Ferret.sln --filter "Category=Integration"
```

---

## Pull Request Process

1. Ensure `dotnet build src/Ferret.sln` succeeds with zero warnings.
2. Ensure all tests pass locally.
3. Fill in the PR template completely — link the related issue.
4. Request a review from at least one maintainer.
5. Address all review comments before merging.
6. PRs are squash-merged into `main`.

---

## Issue Reporting

Use the GitHub Issue templates:

- **Bug report** — for unexpected behaviour.
- **Feature request** — for new capabilities.

Please search existing issues before opening a new one.

---

## Documentation

- Update `CHANGELOG.md` under `[Unreleased]` for every user-visible change.
- New features must include an entry in the relevant `docs/` section.
- Architecture decisions must be recorded as an ADR in `docs/adr/` using the [template](templates/adr.md).

---

Thank you for making Ferret better!
