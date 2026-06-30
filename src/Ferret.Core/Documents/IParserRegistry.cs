namespace Ferret.Core.Documents;

/// <summary>Read-only registry of all registered content parser descriptors.
/// Mirrors IConnectorRegistry in the Connector Platform.</summary>
public interface IParserRegistry
{
    /// <summary>Returns all registered parser descriptors, ordered by priority descending.</summary>
    /// <returns>All registered <see cref="ParserDescriptor"/> instances ordered by priority descending.</returns>
    IReadOnlyList<ParserDescriptor> GetAll();

    /// <summary>Returns the descriptor for the given parser ID, or null if not registered.</summary>
    /// <param name="id">The parser identifier to look up.</param>
    /// <returns>The matching <see cref="ParserDescriptor"/>, or <see langword="null"/> if not registered.</returns>
    ParserDescriptor? GetById(ParserId id);

    /// <summary>Returns the highest-priority parser that can handle the given media type,
    /// or null if no registered parser supports it.
    /// Callers check for null — there is no separate CanParse method on the registry.</summary>
    /// <param name="mediaType">The MIME type to find a parser for.</param>
    /// <returns>The highest-priority <see cref="IContentParser"/> for the given media type, or <see langword="null"/>.</returns>
    IContentParser? GetParserFor(string mediaType);
}
