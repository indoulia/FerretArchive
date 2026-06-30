namespace Ferret.Core.Documents;

/// <summary>
/// Resolves MIME type information from a file name.
/// Lives in Ferret.Core so that Ferret.Connectors.* can populate AssetDescriptor.MediaType
/// without referencing Ferret.ParserPlatform. The implementation lives in Ferret.ParserPlatform.
/// Resolution happens once at the connector edge — never re-resolved downstream.
/// </summary>
public interface IMimeTypeResolver
{
    /// <summary>Resolves the MIME type and related metadata for the given file name.
    /// Never throws. Returns <see cref="MediaTypeInfo.Unknown"/> for unrecognized file types.</summary>
    /// <param name="fileName">The file name including extension (e.g. "README.md").</param>
    /// <returns>A <see cref="MediaTypeInfo"/> describing the resolved type.</returns>
    MediaTypeInfo Resolve(string fileName);
}
