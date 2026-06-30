using System.Text.Json;

using Ferret.Core.Workspace;

namespace Ferret.Workspace.Persistence;

/// <summary>Reads and writes workspace.json and state.json from the .ferret directory.</summary>
internal sealed class JsonWorkspaceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Reads the workspace manifest, or returns null if the file does not exist.</summary>
    internal static async Task<WorkspaceManifest?> ReadManifestAsync(WorkspacePath rootPath, CancellationToken ct)
    {
        var path = ManifestPath(rootPath);
        if (!File.Exists(path))
        {
            return null;
        }

        var stream = File.OpenRead(path);
        await using (stream.ConfigureAwait(false))
        {
            return await JsonSerializer.DeserializeAsync<WorkspaceManifest>(stream, JsonOptions, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Writes the workspace manifest to workspace.json.</summary>
    internal static async Task WriteManifestAsync(WorkspacePath rootPath, WorkspaceManifest manifest, CancellationToken ct)
    {
        var path = ManifestPath(rootPath);
        var stream = File.Create(path);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Reads the workspace state, or returns null if the file does not exist.</summary>
    internal static async Task<WorkspaceStateDto?> ReadStateAsync(WorkspacePath rootPath, CancellationToken ct)
    {
        var path = StatePath(rootPath);
        if (!File.Exists(path))
        {
            return null;
        }

        var stream = File.OpenRead(path);
        await using (stream.ConfigureAwait(false))
        {
            return await JsonSerializer.DeserializeAsync<WorkspaceStateDto>(stream, JsonOptions, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Writes the workspace state to state.json.</summary>
    internal static async Task WriteStateAsync(WorkspacePath rootPath, WorkspaceStateDto dto, CancellationToken ct)
    {
        var path = StatePath(rootPath);
        var stream = File.Create(path);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, dto, JsonOptions, ct).ConfigureAwait(false);
        }
    }

    private static string ManifestPath(WorkspacePath rootPath) =>
        Path.Join(rootPath.FullPath, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.ManifestFileName);

    private static string StatePath(WorkspacePath rootPath) =>
        Path.Join(rootPath.FullPath, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.StateFileName);
}
