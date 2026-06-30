namespace Ferret.AI.Context;

/// <summary>
/// Approximates token count using the 4-characters-per-token heuristic.
/// Returns at least 1 for any non-null input, including empty strings.
/// Suitable for token budget enforcement; not a replacement for model-specific tokenizers.
/// </summary>
public static class TokenEstimator
{
    /// <summary>
    /// Estimates the token count for <paramref name="text"/>.
    /// Formula: <c>Math.Max(1, text.Length / 4)</c>.
    /// </summary>
    /// <param name="text">The text to estimate. Must not be null.</param>
    /// <returns>Estimated token count, minimum 1.</returns>
    public static int Estimate(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Math.Max(1, text.Length / 4);
    }
}
