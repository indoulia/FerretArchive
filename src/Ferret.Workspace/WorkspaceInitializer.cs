using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace;

/// <summary>Creates the ContextOS .ferret directory tree and seed files for a new workspace.</summary>
public sealed class WorkspaceInitializer
{
    /// <summary>Creates the .ferret directory tree, config files, workspace.json, and state.json.</summary>
    /// <param name="rootPath">The directory to initialise as the workspace root.</param>
    /// <param name="options">Optional workspace initialisation options.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the newly created <see cref="WorkspaceContext"/>.</returns>
    public static async Task<WorkspaceContext> InitialiseAsync(
        WorkspacePath rootPath,
        WorkspaceOptions? options,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var ferretRoot = Path.Join(rootPath.FullPath, WorkspaceLayout.RootDirectoryName);
        Directory.CreateDirectory(ferretRoot);

        foreach (var sub in WorkspaceLayout.AllDirectories)
        {
            Directory.CreateDirectory(Path.Join(ferretRoot, sub.Replace('/', Path.DirectorySeparatorChar)));
        }

        var configDir = Path.Join(ferretRoot, WorkspaceLayout.ConfigDirectoryName);
        foreach (var fileName in WorkspaceLayout.ConfigFileNames)
        {
            await File.WriteAllTextAsync(Path.Join(configDir, fileName), "{}", ct).ConfigureAwait(false);
        }

        var idString = Guid.NewGuid().ToString();
        var rawName = Path.GetFileName(rootPath.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var name = string.IsNullOrEmpty(rawName) ? "workspace" : rawName;
        var now = DateTimeOffset.UtcNow;

        var manifest = new WorkspaceManifest
        {
            Id = idString,
            Name = name,
            Description = string.Empty,
            SchemaVersion = "1.0",
            FerretVersion = "0.7.0",
            ContextOsVersion = "1.0",
            CreatedAt = now,
            WorkspaceType = "repository",
        };
        await JsonWorkspaceStore.WriteManifestAsync(rootPath, manifest, ct).ConfigureAwait(false);

        var stateDto = new WorkspaceStateDto { Statistics = new StatisticsDto { SchemaVersion = "1.0" } };
        await JsonWorkspaceStore.WriteStateAsync(rootPath, stateDto, ct).ConfigureAwait(false);

        var id = WorkspaceId.Create(idString);
        var metadata = WorkspaceMetadata.Create(name, string.Empty, "1.0", now);
        var capabilities = WorkspaceCapabilities.Create(options?.ReadOnly ?? false, 0, 0);
        return WorkspaceContext.Create(rootPath, id, metadata, capabilities);
    }
}
