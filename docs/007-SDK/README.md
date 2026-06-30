# 007 — SDK

Developer SDK and REST/gRPC API reference for Ferret.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added in Sprint 1)_ | | |

---

## SDK Components (Planned)

| Package | Description |
|---|---|
| `Ferret.Core` | Core domain abstractions — no infrastructure dependencies |
| `Ferret.Client` | HTTP client for the Ferret API |
| `Ferret.Plugin.Sdk` | SDK for building plugins |
| `Ferret.Mcp.Sdk` | SDK for building MCP servers/clients |

---

## REST API

- Base URL: `https://api.Ferret.dev/v1`
- Auth: Bearer JWT or API key
- All responses: `application/json`
- Errors: RFC 9457 Problem Details

---

## Template

Use [docs/templates/api.md](../templates/api.md) for REST/gRPC API endpoint documentation.
