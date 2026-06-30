using Ferret.Core.Connectors;

namespace Ferret.Core.Documents;

/// <summary>Contextual information provided to a parser alongside the content stream.
/// Gives the parser access to asset provenance without requiring extra parameters.</summary>
public sealed class ParseContext
{
    /// <summary>Gets the asset descriptor for the content being parsed.</summary>
    public required AssetDescriptor Asset { get; init; }

    /// <summary>Creates a <see cref="ParseContext"/> for the given asset.</summary>
    /// <param name="asset">The asset whose content is being parsed.</param>
    /// <returns>A new <see cref="ParseContext"/>.</returns>
    public static ParseContext For(AssetDescriptor asset) => new() { Asset = asset };
}
