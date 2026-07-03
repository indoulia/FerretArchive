namespace Ferret.ParserPlatform;

/// <summary>An extension mapped to the media type it resolves to (a view over the resolver map).</summary>
/// <param name="Extension">The file extension, including the leading dot (e.g. <c>.pdf</c>).</param>
/// <param name="MediaType">The media type the extension resolves to.</param>
public sealed record ExtensionMediaType(string Extension, string MediaType);
