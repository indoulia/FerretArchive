# Why Manual, Not Docs?

This application is called *The Ferret Manual*, not *The Ferret Docs* or *Ferret Documentation*. The distinction is intentional.

## The word "docs" is overloaded

"Docs" has come to mean API reference: generated class listings, method signatures, parameter tables. It is what most projects ship because it is easy to generate from code comments.

Ferret does not need API documentation for end users. End users do not call `DocRegistry.GetPage()`. They run `ferret search`. What they need is a manual.

## A manual teaches

A manual explains how to accomplish goals. It has a Getting Started section that takes you from zero to working in five minutes. It has a User Guide that covers daily workflows. It has a Design Decisions section (this one) that teaches the *why*, not just the *what*.

The Rust Book is a manual. The Kubernetes documentation is (mostly) a manual. Microsoft Learn is a manual. These are the references we modelled this on.

## The manual is also a dogfooding opportunity

After RC1, Ferret will index its own manual. Searching for "how does context assembly work?" will return results from `architecture/context-assembly.md`. The documentation becomes part of the knowledge base.

This is only possible because the manual is authored in Markdown and embedded in the binary — not generated from code, not hosted externally.

## Related

- [Getting Started](../getting-started/index) — the manual's starting point
- [Architecture Explorer](../architecture/index) — the platform behind the manual
