# Security Policy

## Supported Versions

| Version | Supported |
|---|---|
| 0.x (pre-release) | Yes — latest only |

Once v1.0 ships, this table will list supported stable branches.

---

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Report vulnerabilities privately via one of these channels:

1. **GitHub Private Vulnerability Reporting** — click *Security → Report a vulnerability* in this repository.
2. **Email** — send details to `security@ferret.dev` (PGP key available on request).

Include as much of the following as possible:

- Type of issue (e.g. buffer overflow, SQL injection, cross-site scripting, credential leakage)
- Full path of source file(s) related to the issue
- Location of the affected code (tag / branch / commit / direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce
- Proof-of-concept or exploit code (if possible)
- Impact assessment — how an attacker might exploit it

---

## Response Timeline

| Milestone | Target |
|---|---|
| Initial acknowledgement | 48 hours |
| Severity triage | 5 business days |
| Fix ready for review | 30 days (critical: 7 days) |
| Public disclosure | After fix ships or 90 days, whichever is sooner |

---

## Disclosure Policy

We follow [coordinated vulnerability disclosure](https://en.wikipedia.org/wiki/Coordinated_vulnerability_disclosure). We will credit reporters unless they request anonymity.

---

## Security Hardening Guide

Refer to [docs/guides/security-hardening.md](docs/guides/security-hardening.md) for deployment best practices.
