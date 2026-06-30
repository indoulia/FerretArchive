namespace Ferret.Core.Ai.Models;

/// <summary>Token consumption for a single model call.</summary>
public sealed record TokenUsage
{
    /// <summary>Gets the number of tokens in the input (prompt).</summary>
    public required int InputTokens { get; init; }

    /// <summary>Gets the number of tokens in the generated output.</summary>
    public required int OutputTokens { get; init; }

    /// <summary>Gets the total token count (input + output).</summary>
    public required int TotalTokens { get; init; }
}
