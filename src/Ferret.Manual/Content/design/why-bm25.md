# Why BM25 Before Vectors?

Ferret RC1 ships with BM25 keyword search only. No embeddings, no semantic similarity, no vector database. This was intentional.

## BM25 is underrated

BM25 (Best Match 25) is a probabilistic keyword ranking function that has been the backbone of production search engines for 30 years. When you search for `IIndexPipeline`, BM25 finds it. When you search for `ferret index --rebuild`, BM25 finds it.

For code search, BM25 is excellent. Code has high identifier density. Identifiers are exact or near-exact. BM25's term frequency weighting rewards documents that use your search terms heavily — which is exactly what you want when searching for a class name or a CLI flag.

## Why vectors are not in RC1

**Vectors require an embedding model.** Every document must be embedded before indexing. This means either bundling a model (large binary, licensing concerns) or calling an external API (requires credentials, network, cost).

**Vectors are opaque.** BM25 results are explainable: "this document ranks high because it contains 'IIndexPipeline' 4 times." Vector results are not: "this document ranks high because its embedding is similar in 768 dimensions." Debugging is hard.

**Vectors are not always better.** For exact identifier lookup, BM25 outperforms semantic search. Semantic search excels for natural-language questions ("how does Ferret handle file deletions?") — and that use case belongs in the Context Assembly layer, not the raw search layer.

## The plan

Sprint 16 will add hybrid search: BM25 + vector similarity, combined with Reciprocal Rank Fusion. The SQLite index store will be joined by a second vector store. Keyword search results will improve with semantic re-ranking.

RC1 ships BM25 because it works, it's fast, and it requires zero external dependencies.

## Related

- [Why SQLite?](why-sqlite) — the index store choice
- [Search Architecture](../architecture/search-flow) — the search flow
