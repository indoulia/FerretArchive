namespace Ferret.Cli.Commands.Models;

/// <summary>Detail data for the <c>ferret models info</c> output.</summary>
internal sealed record ModelsInfoViewModel
{
    /// <summary>Gets the fully-qualified model identifier.</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets the provider prefix.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the comma-separated capability names.</summary>
    public required string Capabilities { get; init; }

    /// <summary>Gets the context window formatted as "N,NNN tokens" or "—" if unknown.</summary>
    public required string ContextWindow { get; init; }

    /// <summary>Gets the model status (always "Registered" in Sprint 12).</summary>
    public required string Status { get; init; }
}
