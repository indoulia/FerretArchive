namespace Ferret.Core.Enumerations;

/// <summary>Represents the execution state of a review workflow.</summary>
public enum ReviewStatus
{
    /// <summary>The review has been created but not yet started.</summary>
    Pending = 0,

    /// <summary>The review is actively in progress.</summary>
    InProgress = 1,

    /// <summary>The review has been completed.</summary>
    Complete = 2,

    /// <summary>The review was abandoned before completion.</summary>
    Abandoned = 3,
}
