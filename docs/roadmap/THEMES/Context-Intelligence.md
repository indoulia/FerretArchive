# Theme: Context Intelligence

Better organization, structuring, and retrieval of context — stopping short of reasoning over it. Every idea under this theme must remain inside FEP-001's product boundary: it must make context more complete, current, or trustworthy, not produce a conclusion a consumer could disagree with.

## Capabilities This Theme Touches

[Context Organization](../../FEP/capabilities/FEP-002-CAP-03-Context-Organization.md), [Context Assembly](../../FEP/capabilities/FEP-002-CAP-05-Context-Assembly.md)

## Roadmap Items

- **Semantic retrieval augmentation** — similarity-based retrieval as a structuring/ranking technique, not a reasoning step. See [Research Candidates](../FERRET-PRODUCT-ROADMAP.md#6-research-candidates) — feasibility and token-cost trade-off are unresolved.
- **Context Health & Trust Surfacing** — surfacing freshness/provenance/coverage directly to consumers. See [FUTURE/V3.md](../Future/V3.md).

## Guardrail

`FUTURE-002-Enterprise-Intelligence-Vision.md`'s "Model Platform" framing (embedding chat/completion models) is out of scope for this theme — that crosses into reasoning, which is outside Ferret per [FEP-001 §1.3](../../FEP/FEP-001-Product-Architecture.md). Retrieval techniques that only rank or structure existing content stay in scope; techniques that generate or synthesize new content do not.
