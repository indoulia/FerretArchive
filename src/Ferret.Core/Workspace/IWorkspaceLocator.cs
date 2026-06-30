namespace Ferret.Core.Workspace;

/// <summary>Locates a workspace root by searching from a given starting path.</summary>
public interface IWorkspaceLocator
{
    /// <summary>Searches for a workspace root starting at <paramref name="searchPath"/> and walking up the directory tree.</summary>
    /// <param name="searchPath">The path from which to begin the search.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the workspace root path if found; otherwise a null value.</returns>
    Task<WorkspacePath?> LocateAsync(WorkspacePath searchPath, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if <paramref name="rootPath"/> is an initialised workspace root.</summary>
    /// <param name="rootPath">The path to test.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a boolean value indicating whether the path is an initialised workspace root.</returns>
    Task<bool> ExistsAsync(WorkspacePath rootPath, CancellationToken ct = default);
}
