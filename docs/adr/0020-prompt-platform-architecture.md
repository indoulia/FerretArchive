# ADR-0020: Prompt Platform Architecture

**Status:** Accepted  
**Sprint:** 12  
**Date:** 2026-06-29

## Context

Sprint 12 introduces a prompt template system that feature packages use to register and render structured prompts. Templates need versioning, variable substitution, and validation before Sprint 13 begins assembling context prompts. The renderer must be stateless so it can be shared across concurrent requests.

## Decisions

### 1. Templates use `{{variable}}` substitution; missing required variables are errors

`PromptTemplate` declares `RequiredVariables: IReadOnlyList<string>`. `IPromptRenderer.Render(promptTemplate, variables)` substitutes all `{{variable}}` placeholders. If any required variable is absent from `PromptVariables`, `Render` throws `PromptRenderException`. `Validate(promptTemplate, variables)` returns the list of missing required variables without throwing — callers use it for pre-flight checks.

### 2. `PromptRegistry` is immutable after startup

`PromptRegistry` is built from `IEnumerable<PromptTemplate>` at DI construction time and is immutable thereafter. Feature packages register templates via DI — the registry collects them at startup. This matches the immutability pattern established for `IMcpToolRegistry` (ADR-0017) and `ModelRegistry` (ADR-0019).

### 3. Templates are registered via DI (`IEnumerable<PromptTemplate>`)

Feature packages call `services.AddSingleton<PromptTemplate>(new PromptTemplate { ... })` or equivalent. `PromptsModule` collects all registered `PromptTemplate` instances from the container and passes them to `PromptRegistry`. This is the same pattern used for `IMcpTool` registrations in ADR-0017 — no central registry of template names, no magic string lookups at registration time.

### 4. Renderer is stateless

`PromptRenderer : IPromptRenderer` has no instance state. It receives all inputs via method parameters. This allows a single singleton instance to serve concurrent calls safely without locking. `PromptVariables` is an immutable builder — `.Set(key, value)` returns a new instance.

## Consequences

- Feature packages own their templates — no coupling to a central template list.
- Sprint 13 (context assembly) adds templates for workspace-context and file-summary prompts via `services.AddSingleton<PromptTemplate>(...)`.
- `ferret prompt list` shows all registered templates; Sprint 12 shows the empty-state message because no templates are registered until Sprint 13.
- Missing required variables are caught at render time, not at registration time, which keeps template registration cheap.
