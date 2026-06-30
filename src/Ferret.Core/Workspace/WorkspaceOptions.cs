namespace Ferret.Core.Workspace;

/// <summary>Options that influence workspace engine operations.</summary>
public sealed class WorkspaceOptions
{
    /// <summary>Gets or sets a value indicating whether the workspace is opened in read-only mode.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>Gets or sets the list of plugin identifiers to activate for this workspace. An empty list activates all configured plugins.</summary>
    public IReadOnlyList<string> PluginIds { get; set; } = Array.Empty<string>();
}
