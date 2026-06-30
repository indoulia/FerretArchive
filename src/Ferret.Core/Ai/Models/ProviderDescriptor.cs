namespace Ferret.Core.Ai.Models;

/// <summary>Immutable description of a provider's identity and aggregate capabilities.</summary>
public sealed record ProviderDescriptor
{
    /// <summary>Gets the provider identifier.</summary>
    public required ProviderId Id { get; init; }

    /// <summary>Gets the human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the union of capabilities across all models this provider can serve.</summary>
    public required ModelCapabilities Capabilities { get; init; }

    /// <summary>Gets the provider's self-reported version string.</summary>
    public required string Version { get; init; }
}
