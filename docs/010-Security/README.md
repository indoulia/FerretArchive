# 010 — Security

Security policies, threat models, and hardening guides for Ferret.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added)_ | | |

---

## Security Principles

1. **Zero trust** — no component trusts another by default
2. **Least privilege** — every component and plugin has the minimum permissions needed
3. **Defence in depth** — multiple independent security controls
4. **Secrets never in code** — environment variables, secret stores, or vault
5. **Dependency hygiene** — weekly NuGet vulnerability scans via CI

---

## Reporting a Vulnerability

See [SECURITY.md](../../SECURITY.md) at the repository root.

---

## Automated Scanning

| Tool | Trigger | Scope |
|---|---|---|
| CodeQL | PR + weekly | Static analysis |
| `dotnet list package --vulnerable` | Every CI build | NuGet CVEs |
| OWASP Dependency Check | Weekly | Transitive deps |
| GitHub Dependabot | Continuous | Dependency PRs |
