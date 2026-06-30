# tests

Integration and end-to-end tests for Ferret.

Unit tests live alongside their source projects in `src/` (e.g. `src/Ferret.Core.Tests/`).  
This folder contains tests that require external infrastructure (databases, message queues, real HTTP endpoints).

---

## Layout (planned)

```
tests/
├── Ferret.IntegrationTests/   Integration tests — require Docker Compose
└── Ferret.E2ETests/           End-to-end CLI and API tests
```

---

## Running Integration Tests

```powershell
# Start required infrastructure
docker compose -f tests/docker-compose.yml up -d

# Run integration tests
dotnet test tests/ --filter "Category=Integration"

# Tear down
docker compose -f tests/docker-compose.yml down
```

---

## Test Categories

| Category | Filter | Description |
|---|---|---|
| `Unit` | (default) | Fast, no I/O, co-located with source |
| `Integration` | `Category=Integration` | Requires Docker infrastructure |
| `E2E` | `Category=E2E` | Full system, requires a running Ferret instance |
| `Slow` | `Category=Slow` | >5 s — excluded from PR CI by default |
