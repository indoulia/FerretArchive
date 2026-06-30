# ADR-004 — Runtime Engine Container vs Composition Root

| Field | Value |
|---|---|
| **ADR ID** | ADR-004 |
| **Sprint** | Sprint 4 |
| **Status** | Accepted |
| **Decision** | Composition Root |
| **Date** | 2026-06-28 |
| **Author** | Ferret Core Team |

---

## Context

Sprint 4 defines the `IRuntimeHost` and `IRuntimeBuilder` contracts in `Ferret.Core.Runtime`. Before implementing these contracts in Sprint 5, the team must decide whether the runtime should act as a **DI container** (responsible for resolving engine dependencies) or as a **composition root** (responsible only for module lifecycle, with dependency resolution delegated to an external DI container).

Two concrete architectures were evaluated:

### Option A: Runtime as Engine Container

The `IRuntimeHost` implementation provides a service locator or DI container directly. Engines request dependencies from the host using `runtimeHost.GetService<T>()`. The runtime is responsible for knowing which concrete type to provide for each interface.

**Advantages:**
- Single object graph managed in one place
- Modules cannot depend on the external DI container framework
- Simpler startup: one registration API

**Disadvantages:**
- `IRuntimeHost` violates the Interface Segregation Principle — it becomes responsible for both lifecycle and dependency resolution
- The runtime must understand every module's dependencies, creating implicit coupling
- Difficult to test: replacing one engine type requires understanding the full dependency graph
- `Ferret.Core` remains zero-dependency, so the container cannot live there; it must live in `Ferret.Runtime`, creating a hard dependency on a DI framework in the platform's primary assembly

### Option B: Runtime as Composition Root (Chosen)

The `IRuntimeHost` implementation is responsible **only** for module lifecycle (starting and stopping modules in order). Dependency injection is performed by an external composition root (in `Ferret.Runtime`) that wires the DI container before passing built modules to the host.

`IRuntimeBuilder` is the composition root's entry point: it accepts module descriptors and returns a configured `IRuntimeHost`. The `IRuntimeBuilder` implementation (in Sprint 5) is responsible for registering module services in the DI container and constructing each module's concrete type.

**Advantages:**
- `IRuntimeHost` has a narrow, lifecycle-only contract — fully defined by its interface
- `Ferret.Core` stays zero-dependency: no DI framework reference needed
- Each module's dependencies are registered in its own descriptor, keeping coupling local
- Testable: modules can be tested with fakes injected at the composition root level, without the full host
- Aligns with the dependency inversion principle — upper layers (composition root) depend on lower interfaces (`IModule`), not the reverse

**Disadvantages:**
- Requires a DI container in `Ferret.Runtime` (acceptable — `Ferret.Runtime` is a platform assembly, not a kernel)
- Two concepts (`IRuntimeBuilder` and the DI container registration) must be kept consistent

---

## Decision

**Option B: Composition Root.**

The runtime host manages lifecycle only. The composition root (`IRuntimeBuilder` implementation) is responsible for wiring dependencies via a DI container in `Ferret.Runtime`. This decision aligns with the zero-dependency constraint on `Ferret.Core` and keeps the `IRuntimeHost` contract narrow and testable.

---

## Consequences

1. `IRuntimeBuilder.AddModule(IModuleDescriptor)` accepts a descriptor that knows how to register its own services (the descriptor carries a `RegisterServices(IServiceCollection)` method or equivalent, defined in Sprint 5).
2. The DI container implementation is selected in Sprint 5. Microsoft.Extensions.DependencyInjection is the default candidate (already used by the .NET ecosystem; lightweight; does not require NuGet packages in `Ferret.Core`).
3. `IRuntimeHost` and `IRuntimeBuilder` contracts (defined in Sprint 4) do not change as a result of this decision. The implementation details are deferred to Sprint 5.

---

## Traceability

| Input | Role |
|---|---|
| ARCH-001 §5 | Layered architecture — upper layers depend on lower interfaces |
| ARCH-001 §8 | Dependency rules — `Ferret.Core` has zero project references |
| Sprint 4 contracts | `IRuntimeHost`, `IRuntimeBuilder` interfaces that this decision contextualises |
