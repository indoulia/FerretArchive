namespace Ferret.Core.Ai.Models;

/// <summary>Immutable description of a model's identity and capabilities.</summary>
public sealed record ModelDescriptor
{
    /// <summary>Gets the fully-qualified model identifier.</summary>
    public required ModelId Id { get; init; }

    /// <summary>Gets the identifier of the provider that owns this model.</summary>
    public required ProviderId ProviderId { get; init; }

    /// <summary>Gets the human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the capability flags for this model.</summary>
    public required ModelCapabilities Capabilities { get; init; }

    /// <summary>Gets the maximum context window in tokens, or <see langword="null"/> if not published by the provider.</summary>
    public long? ContextWindow { get; init; }

    /// <summary>Gets an optional human-readable description of the model.</summary>
    public string? Description { get; init; }
}
