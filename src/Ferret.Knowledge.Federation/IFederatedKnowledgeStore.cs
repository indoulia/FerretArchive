using Ferret.Core.Search;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// The storage-layer abstraction for a federated query (01-Architecture.md §2, ARCH-001 §27.2).
/// Implements the same shape as the local per-repo storage abstraction (<see cref="ISearchService"/>
/// is this codebase's <c>IKnowledgeStore</c>) so callers above the storage layer cannot tell a
/// federated query from a local one — no new query API is introduced.
/// </summary>
public interface IFederatedKnowledgeStore : ISearchService;
