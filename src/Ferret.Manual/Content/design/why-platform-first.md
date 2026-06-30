# Why Platform-First?

Ferret spent Sprints 1–12 building a platform before shipping significant user-facing features. The first `ferret search` command did not appear until Sprint 10. This was intentional.

## The platform compounds

Every feature built on a stable platform costs less than one built without one. The connector platform (Sprint 8) means adding a new file type is one `IParser` implementation. The provider platform (Sprint 12) means switching AI models is one config change.

A platform is not overhead. It is deferred feature cost paid upfront.

## The alternative was fragility

We evaluated an "implement features first, extract platform later" approach. The risk: every feature built without abstractions becomes a dependency that must be unentangled when the platform arrives. We have seen this pattern cause 2-3x rework cost in previous projects.

Ferret's architecture is frozen at v1.0 (ADR-0012). This means contributors know exactly which interfaces to implement, which boundaries to respect, and which changes require an ADR.

## What we gave up

Platform-first means later user-facing features. Ferret could not be used by real users until Sprint 10. For a developer tool this is acceptable; for a consumer product it would not be.

The trade-off is: slow start, fast execution. Every post-RC1 sprint delivers user value on a foundation that does not need to be rebuilt.

## Related

- [Architecture Explorer](../architecture/index) — the resulting platform
- [Extension Points](../architecture/extension-points) — what the platform exposes
