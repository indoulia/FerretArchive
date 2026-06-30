namespace Ferret.Core.Workspace;

/// <summary>Describes the runtime capabilities of an open workspace.</summary>
public sealed class WorkspaceCapabilities
{
    private WorkspaceCapabilities(bool readOnly, int pluginCount, int indexedFileCount)
    {
        ReadOnly = readOnly;
        PluginCount = pluginCount;
        IndexedFileCount = indexedFileCount;
    }

    /// <summary>Gets a value indicating whether this workspace was opened in read-only mode.</summary>
    public bool ReadOnly { get; }

    /// <summary>Gets the number of active plugins in this workspace.</summary>
    public int PluginCount { get; }

    /// <summary>Gets the number of files in the current index.</summary>
    public int IndexedFileCount { get; }

    /// <summary>Creates a new <see cref="WorkspaceCapabilities"/> instance.</summary>
    /// <param name="readOnly">Whether the workspace is read-only.</param>
    /// <param name="pluginCount">Number of active plugins.</param>
    /// <param name="indexedFileCount">Number of indexed files.</param>
    /// <returns>A new <see cref="WorkspaceCapabilities"/> instance.</returns>
    public static WorkspaceCapabilities Create(bool readOnly, int pluginCount, int indexedFileCount)
    {
        return new WorkspaceCapabilities(readOnly, pluginCount, indexedFileCount);
    }
}
