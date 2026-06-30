namespace Ferret.Core.Connectors;

/// <summary>
/// Universal connector-agnostic asset abstraction — the lingua franca of ContextOS.
/// Every connector produces AssetDescriptors. Every pipeline stage consumes them.
/// </summary>
public sealed record AssetDescriptor
{
    /// <summary>Gets the stable asset identifier derived from CanonicalUri.</summary>
    public required AssetId Id { get; init; }

    /// <summary>Gets the connector type that produced this asset.</summary>
    public required ConnectorId ConnectorId { get; init; }

    /// <summary>Gets the workspace-scoped instance that produced this asset.</summary>
    public required ConnectorInstanceId InstanceId { get; init; }

    /// <summary>Gets the kind of asset.</summary>
    public required AssetKind Kind { get; init; }

    /// <summary>Gets the stable, normalized, workspace-relative canonical URI.</summary>
    public required Uri CanonicalUri { get; init; }

    /// <summary>Gets the human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the last modification timestamp.</summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>Gets the optional lightweight fingerprint for change detection.</summary>
    public AssetFingerprint? Fingerprint { get; init; }

    /// <summary>Gets the optional file size in bytes.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Gets the optional MIME type.</summary>
    public string? MediaType { get; init; }

    /// <summary>Gets connector-specific metadata key-value pairs.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
