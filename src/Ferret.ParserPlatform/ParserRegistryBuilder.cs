using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Static factory that constructs an immutable <see cref="IParserRegistry"/> from a collection
/// of registered parsers. Validates uniqueness invariants at build time.
/// </summary>
public static class ParserRegistryBuilder
{
    /// <summary>
    /// Builds an <see cref="IParserRegistry"/> from the provided parsers, ordered by priority descending.
    /// </summary>
    /// <param name="parsers">The parsers to register.</param>
    /// <returns>An immutable <see cref="IParserRegistry"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two parsers share the same <see cref="ParserId"/>, or the same (SupportedMediaType, Priority).
    /// </exception>
    public static IParserRegistry Build(IEnumerable<IContentParser> parsers)
    {
        var ordered = parsers
            .OrderByDescending(p => p.Descriptor.Priority)
            .ToList();

        ValidateDuplicateParserId(ordered);
        ValidateDuplicateMediaTypePriority(ordered);

        return new ParserRegistry(ordered);
    }

    private static void ValidateDuplicateParserId(List<IContentParser> parsers)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in parsers.Select(parser => parser.Descriptor.Id.Value))
        {
            if (!seen.Add(id))
            {
                throw new InvalidOperationException(
                    $"Duplicate ParserId '{id}' — each parser must have a unique identifier.");
            }
        }
    }

    private static void ValidateDuplicateMediaTypePriority(List<IContentParser> parsers)
    {
        var seen = new HashSet<(string MediaType, int Priority)>();
        foreach (var parser in parsers)
        {
            foreach (var mediaType in parser.Descriptor.SupportedMediaTypes)
            {
                var key = (mediaType, parser.Descriptor.Priority);
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate (MediaType='{mediaType}', Priority={parser.Descriptor.Priority}) " +
                        $"combination — assign different priorities to parsers that handle the same media type.");
                }
            }
        }
    }
}
