namespace Ferret.Cli.Commands.Models;

/// <summary>Row data for the <c>ferret models list</c> tabular output.</summary>
internal sealed record ModelsListViewModel
{
    /// <summary>Gets the provider prefix (e.g. "ollama").</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the fully-qualified model identifier (e.g. "ollama/llama3.2").</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets the comma-separated capability names (e.g. "Chat, Embedding").</summary>
    public required string Capabilities { get; init; }

    /// <summary>Gets the context window size formatted for display (e.g. "128k" or "—").</summary>
    public required string ContextWindow { get; init; }
}
