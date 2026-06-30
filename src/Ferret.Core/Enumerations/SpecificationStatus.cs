namespace Ferret.Core.Enumerations;

/// <summary>Represents the review lifecycle state of a specification document.</summary>
public enum SpecificationStatus
{
    /// <summary>The specification is in draft state and not yet under review.</summary>
    Draft = 0,

    /// <summary>The specification has been submitted and is under review.</summary>
    UnderReview = 1,

    /// <summary>The specification has been approved.</summary>
    Approved = 2,

    /// <summary>The specification has been rejected.</summary>
    Rejected = 3,

    /// <summary>The specification has been superseded by a newer version.</summary>
    Superseded = 4,
}
