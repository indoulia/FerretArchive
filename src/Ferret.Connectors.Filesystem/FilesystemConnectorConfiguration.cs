namespace Ferret.Connectors.Filesystem;

/// <summary>Configuration for a FilesystemConnector instance.</summary>
public sealed class FilesystemConnectorConfiguration
{
    /// <summary>Gets the root directory path to discover from. Defaults to current directory.</summary>
    public string RootPath { get; init; } = ".";

    /// <summary>Gets the file extensions to include (empty means all extensions).</summary>
    public IReadOnlyList<string> IncludeExtensions { get; init; } = [];

    /// <summary>Gets the file extensions to exclude.</summary>
    public IReadOnlyList<string> ExcludeExtensions { get; init; } = [];
}
