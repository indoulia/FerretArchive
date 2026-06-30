using Ferret.Core.Enumerations;

namespace Ferret.Core.Results;

/// <summary>Represents the outcome of a review workflow.</summary>
public sealed class ReviewResult
{
    /// <summary>Initializes a new instance of the <see cref="ReviewResult"/> class.</summary>
    /// <param name="status">The final status of the review.</param>
    /// <param name="findings">The findings produced by the review.</param>
    /// <param name="summary">A human-readable summary of the review outcome.</param>
    public ReviewResult(ReviewStatus status, IReadOnlyList<string> findings, string summary)
    {
        Status = status;
        Findings = findings;
        Summary = summary;
    }

    /// <summary>Gets the final status of the review.</summary>
    public ReviewStatus Status { get; }

    /// <summary>Gets the findings produced by the review.</summary>
    public IReadOnlyList<string> Findings { get; }

    /// <summary>Gets a human-readable summary of the review outcome.</summary>
    public string Summary { get; }
}
