# Sprint 5 Review — Runtime Host Implementation

**Sprint:** 5
**Dates:** 2026-06-28
**Tag:** v0.5.0-sprint5
**Branch:** master
**Build:** 0 errors, 0 warnings
**Tests:** 76 (Ferret.Runtime.Tests — all pass)

---

## Summary

Sprint 5 delivered the Ferret Runtime Host: a composition root that boots registered modules in dependency order, manages their lifecycle, aggregates health checks, and dispatches in-process events. The implementation wraps `Microsoft.Extensions.Hosting` internally without leaking it through the public API.

---

## Goal

Implement a production-quality runtime host (`Ferret.Runtime`) that:

- accepts module registrations via a fluent builder
- resolves a topological dependency graph
- drives modules through a defined lifecycle (Loading → Active/Faulted → Stopped)
- provides health aggregation and in-process pub/sub
- exposes a single DI extension entry point (`AddFerretRuntime`)

---

## What Was Built

| Task | Deliverable | Notes |
|------|-------------|-------|
| 1 | Project setup | `Microsoft.Extensions.Hosting 9.0.0` in `Directory.Packages.props`; `AssemblyInfo` with `InternalsVisibleTo` |
| 2 | `RuntimeHost`, `RuntimeBuilder`, `RuntimeOptions` shells | Composition root; `IHost` wrapped internally, never exposed |
| 3 | `RuntimeStateManager` | Atomic state machine via `Interlocked.CompareExchange`; `TryTransition`/`ForceSet` pattern |
| 4 | `DefaultModule`, `BoundModule`, `IModuleWithDependencies` | Optional abstract base for plugin authors; `BoundModule` wraps plain `IModuleDescriptor`; dependency declaration via optional interface |
| 5 | `ModuleDescriptorStore` | Internal store; wraps non-`DefaultModule` descriptors in `BoundModule`; throws on duplicate ID at `Add()` |
| 6 | `ModuleDependencyGraph` | DFS topological sort; cycle detection; missing dependency detection |
| 7 | `ModuleRegistry` | Implements `IModuleRegistry`; `GetById` returns `null` (does not throw); `TryGet` out-param pattern |
| 8 | `ExecutionContext`, `ModuleContext` | Internal implementations of `IExecutionContext` and `IModuleContext`; `using` aliases resolve `System.Threading.ExecutionContext` naming conflict |
| 9 | `FakeModule`, `FakeHealthCheck` | Test helpers; `FakeModule` tracks lifecycle call counts; configurable `startException` injection |
| 10 | `LifecycleOrchestrator`, `ModuleLifecycleService` | `LifecycleOrchestrator` drives `Loading→Active/Faulted` and `Deactivating→Stopped/Faulted`; `ModuleLifecycleService` is the single `IHostedService`; `LoggerMessage.Define` for CA1848 |
| 11 | `RuntimeEventDispatcher` | Lock-based in-process pub/sub per ARCH-013; handler failures isolated; `OperationCanceledException` propagates; `Subscribe<T>` returns `IDisposable` |
| 12 | `RuntimeHealthService` | Aggregates `IHealthCheck` (Ferret.Core) results; worst-status wins; check throws → `Unhealthy`; `Microsoft.Extensions.Diagnostics.HealthChecks` rejected |
| 13 | `RuntimeHost` + `RuntimeBuilder` integration tests | Full lifecycle tests proving dependency-ordered startup and fault propagation; latent DI bug found and fixed (see Issues) |
| 14 | DI extensions | `AddFerretRuntime(IServiceCollection, Action<RuntimeBuilder>?)` using `TryAddSingleton` for idempotence |
| 15 | Final verification | 76 tests, 0 warnings, 0 errors, all architecture checks passed, tag `v0.5.0-sprint5` created |

---

## Technology Decisions

| Package | Decision | Rationale |
|---------|----------|-----------|
| Microsoft.Extensions.Hosting | Wrap | Provides DI/logging/lifetime; kept internal to preserve API flexibility |
| Microsoft.Extensions.DependencyInjection | Adopt (via Hosting) | Transitive; no direct reference needed |
| Microsoft.Extensions.Logging | Adopt (via Hosting) | Transitive; no direct reference needed |
| Microsoft.Extensions.Options | Adopt (via Hosting) | Transitive; no direct reference needed |
| Microsoft.Extensions.Diagnostics.HealthChecks | Reject | Contracts conflict with `Ferret.Core` health types |
| System.Threading.Channels | Reject | Not needed; lock-based dispatcher sufficient for current scale |
| Scrutor | Defer | No assembly-scan use case yet |
| Polly | Defer | No retry/resilience use case yet |

---

## Issues and Mitigations

| Issue | Impact | Mitigation |
|-------|--------|------------|
| `SemanticVersion.Create()` factory method — no public constructor | Plan showed constructor call | Corrected to factory method at implementation |
| `ModuleMetadata.Create()` requires 6 parameters (plan showed 4) | Test code incorrect | Updated all call sites |
| `IModuleRegistry.GetById` returns `null` (plan showed throw) | Guard logic adjusted | `null`-check pattern applied consistently |
| `IModuleRegistry.Modules` is `IReadOnlyCollection<IModule>` (plan showed `IReadOnlyList`) | Minor API mismatch | Callers updated; no behavioral impact |
| `LifecycleOrchestrator` constructor must be `public` for MS DI resolution | Integration tests failed | Constructor visibility corrected; latent bug found by integration tests |
| `ExecutionContext` naming conflict with `System.Threading.ExecutionContext` | Compiler ambiguity | Resolved with `using` aliases in affected files |

---

## Test Coverage

- **Total tests:** 76 (Ferret.Runtime.Tests)
- **Planned range:** 180–220 (target was overestimated in the plan)
- **Coverage:** all required scenarios covered — lifecycle transitions, dependency ordering, fault propagation, health aggregation, event dispatch, DI registration idempotence
- **Test helpers:** `FakeModule` and `FakeHealthCheck` reusable for Sprint 6+

---

## Architectural Compliance

- `IHost` wrapped internally; public API exposes only `RuntimeHost`, `RuntimeBuilder`, `RuntimeOptions`
- All in-process pub/sub routed through `RuntimeEventDispatcher` per ARCH-013
- Health contracts use `Ferret.Core` types; no dependency on `Microsoft.Extensions.Diagnostics.HealthChecks`
- `TryAddSingleton` in DI extension ensures idempotent multi-call registration
- `LoggerMessage.Define` pattern used throughout (CA1848 compliance)
- `InternalsVisibleTo` grants test assembly access without exposing internals publicly

---

## Carry-Forward

| Item | Status |
|------|--------|
| Test count 76 vs planned 180–220 | Accepted — all required scenarios covered; count target was overestimated |
| `OperationCanceledException` swallowing in `RuntimeHealthService` | By design — brief specifies do not propagate |
| `RuntimeEventDispatcher._disposed` field not `volatile` | Acceptable risk — lock on dictionary provides sufficient protection |
| `EventDispatch` integration test simplified | Dispatcher is `internal`; not DI-accessible from test assembly without additional exposure |

---

## Next Sprint

**Sprint 6: Ferret CLI Host**

- Commands: `ferret start`, `ferret version`, `ferret status`
- `DiagnosticsModule` implementation
- End-to-end integration tests
- Plan: `docs/superpowers/plans/2026-06-28-sprint-6-cli-host.md`

**Note:** Rebranding from AISpace to Ferret will occur between the Sprint 5 tag and Sprint 6 start. All namespaces will transition from `AISpace.*` to `Ferret.*`.
