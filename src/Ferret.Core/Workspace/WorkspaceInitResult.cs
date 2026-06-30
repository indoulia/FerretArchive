namespace Ferret.Core.Workspace;

/// <summary>The result of a workspace initialisation operation.</summary>
public sealed class WorkspaceInitResult
{
    private WorkspaceInitResult(bool succeeded, WorkspaceContext? context, string? errorMessage)
    {
        Succeeded = succeeded;
        Context = context;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the initialisation succeeded.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets the workspace context created by the initialisation, or <see langword="null"/> if it failed.</summary>
    public WorkspaceContext? Context { get; }

    /// <summary>Gets the error message if the initialisation failed, or <see langword="null"/> if it succeeded.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful initialisation result.</summary>
    /// <param name="context">The workspace context that was created.</param>
    /// <returns>A successful <see cref="WorkspaceInitResult"/>.</returns>
    public static WorkspaceInitResult Success(WorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WorkspaceInitResult(true, context, null);
    }

    /// <summary>Creates a failed initialisation result.</summary>
    /// <param name="errorMessage">A message describing the failure.</param>
    /// <returns>A failed <see cref="WorkspaceInitResult"/>.</returns>
    public static WorkspaceInitResult Failure(string errorMessage)
    {
        return new WorkspaceInitResult(false, null, errorMessage ?? "Initialisation failed.");
    }
}
