using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.Core.Search;

/// <summary>Retrieves documents by identifier from the platform document store.</summary>
public interface IDocumentService
{
    /// <summary>Returns the document with the given identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The document identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="Document"/>, or <see langword="null"/> if not found.</returns>
    Task<Document?> GetAsync(DocumentId id, CancellationToken ct);
}
