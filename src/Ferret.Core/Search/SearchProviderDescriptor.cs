namespace Ferret.Core.Search;

/// <summary>
/// Static descriptor for a registered search provider type. Immutable.
/// Mirrors <c>ConnectorDescriptor</c> and <c>ParserDescriptor</c> in their respective platforms.
/// </summary>
public sealed record SearchProviderDescriptor
{
    /// <summary>Gets the unique provider identifier (e.g. "bm25", "semantic", "hybrid").</summary>
    public required string Id { get; init; }

    /// <summary>Gets the human-readable provider name for display.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the provider version string.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the capabilities this provider supports.</summary>
    public required SearchCapabilities Capabilities { get; init; }
}
