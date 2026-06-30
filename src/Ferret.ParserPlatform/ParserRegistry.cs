using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Immutable registry of content parsers, ordered by priority descending.
/// Constructed exclusively via <see cref="ParserRegistryBuilder.Build"/>.
/// </summary>
internal sealed class ParserRegistry : IParserRegistry
{
    private readonly IReadOnlyList<IContentParser> _parsers;
    private readonly Dictionary<string, ParserDescriptor> _byId;

    internal ParserRegistry(IReadOnlyList<IContentParser> parsers)
    {
        _parsers = parsers;
        _byId = parsers.ToDictionary(
            p => p.Descriptor.Id.Value,
            p => p.Descriptor,
            StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParserDescriptor> GetAll() =>
        _parsers.Select(p => p.Descriptor).ToList();

    /// <inheritdoc/>
    public ParserDescriptor? GetById(ParserId id) =>
        _byId.GetValueOrDefault(id.Value);

    /// <inheritdoc/>
    public IContentParser? GetParserFor(string mediaType) =>
        _parsers.FirstOrDefault(p => p.CanParse(mediaType));
}
