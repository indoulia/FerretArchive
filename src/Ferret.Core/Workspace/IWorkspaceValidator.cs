using Ferret.Core.Results;

namespace Ferret.Core.Workspace;

/// <summary>Validates the configuration and structural integrity of an open workspace.</summary>
public interface IWorkspaceValidator
{
    /// <summary>Validates the workspace and returns a result containing any validation failures.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="ValidationResult"/>.</returns>
    Task<ValidationResult> ValidateAsync(WorkspaceContext context, CancellationToken ct = default);
}
