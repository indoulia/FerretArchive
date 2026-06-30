namespace Ferret.Core.Workspace;

/// <summary>Canonical directory and file names within a <c>.ferret</c> workspace root.
/// Shared by all subsystems that construct paths inside the workspace directory tree.
/// Consumers should prefer these constants over inline string literals.</summary>
public static class WorkspaceLayout
{
    /// <summary>The name of the workspace root directory.</summary>
    public const string RootDirectoryName = ".ferret";

    /// <summary>The name of the workspace manifest file.</summary>
    public const string ManifestFileName = "workspace.json";
}
