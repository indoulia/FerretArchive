using Ferret.Core.Workspace;

namespace Ferret.Workspace;

/// <summary>Locates workspace roots by walking up the file system from a starting path.</summary>
public sealed class WorkspaceLocator : IWorkspaceLocator
{
    /// <inheritdoc/>
    public Task<WorkspacePath?> LocateAsync(WorkspacePath searchPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(searchPath);
        var current = searchPath.FullPath;
        while (current is not null)
        {
            var ferretDir = Path.Combine(current, WorkspaceLayout.RootDirectoryName);
            var manifest = Path.Combine(ferretDir, WorkspaceLayout.ManifestFileName);
            if (Directory.Exists(ferretDir) && File.Exists(manifest))
            {
                return Task.FromResult<WorkspacePath?>(WorkspacePath.Create(current));
            }

            current = Path.GetDirectoryName(current);
        }

        return Task.FromResult<WorkspacePath?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(WorkspacePath rootPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var ferretDir = Path.Combine(rootPath.FullPath, WorkspaceLayout.RootDirectoryName);
        var manifest = Path.Combine(ferretDir, WorkspaceLayout.ManifestFileName);
        return Task.FromResult(Directory.Exists(ferretDir) && File.Exists(manifest));
    }
}
