# 011 — Performance

Performance targets, benchmarks, and profiling documentation for Ferret.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added)_ | | |

---

## SLOs (Target — to be refined in Sprint 1+)

| Endpoint / Operation | P50 | P99 | Notes |
|---|---|---|---|
| Agent invocation (API) | < 100 ms | < 500 ms | Excluding LLM latency |
| Tool call (MCP) | < 50 ms | < 250 ms | Network excluded |
| CLI startup | < 500 ms | < 1 s | Cold start |
| Plugin load | < 200 ms | < 1 s | Per plugin |

---

## Benchmarking

BenchmarkDotNet is the standard tool for micro-benchmarks.  
Load testing uses [k6](https://k6.io/).

---

## Profiling

- CPU: dotnet-trace + SpeedScope
- Memory: dotnet-gcdump + PerfView / VS Diagnostic Tools
- Heap: dotnet-heapview
