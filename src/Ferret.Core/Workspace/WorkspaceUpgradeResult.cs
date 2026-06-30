namespace Ferret.Core.Workspace;

/// <summary>The result of a workspace schema upgrade operation.</summary>
public sealed class WorkspaceUpgradeResult
{
    private WorkspaceUpgradeResult(bool succeeded, string? fromVersion, string? toVersion, IReadOnlyList<string> stepsApplied, string? errorMessage)
    {
        Succeeded = succeeded;
        FromVersion = fromVersion;
        ToVersion = toVersion;
        StepsApplied = stepsApplied;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the upgrade succeeded.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets the schema version before the upgrade, or <see langword="null"/> if the upgrade was not attempted.</summary>
    public string? FromVersion { get; }

    /// <summary>Gets the schema version after the upgrade, or <see langword="null"/> if the upgrade failed.</summary>
    public string? ToVersion { get; }

    /// <summary>Gets the ordered list of migration step identifiers that were applied.</summary>
    public IReadOnlyList<string> StepsApplied { get; }

    /// <summary>Gets the error message if the upgrade failed, or <see langword="null"/> if it succeeded.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful upgrade result.</summary>
    /// <param name="fromVersion">The version before the upgrade.</param>
    /// <param name="toVersion">The version after the upgrade.</param>
    /// <param name="stepsApplied">The migration steps that were applied.</param>
    /// <returns>A successful <see cref="WorkspaceUpgradeResult"/>.</returns>
    public static WorkspaceUpgradeResult Success(string fromVersion, string toVersion, IEnumerable<string> stepsApplied)
    {
        return new WorkspaceUpgradeResult(true, fromVersion, toVersion, (stepsApplied ?? Enumerable.Empty<string>()).ToList().AsReadOnly(), null);
    }

    /// <summary>Creates a failed upgrade result.</summary>
    /// <param name="errorMessage">A message describing the failure.</param>
    /// <param name="fromVersion">The version that was being upgraded from, if known.</param>
    /// <returns>A failed <see cref="WorkspaceUpgradeResult"/>.</returns>
    public static WorkspaceUpgradeResult Failure(string errorMessage, string? fromVersion = null)
    {
        return new WorkspaceUpgradeResult(false, fromVersion, null, Array.Empty<string>(), errorMessage ?? "Upgrade failed.");
    }
}
