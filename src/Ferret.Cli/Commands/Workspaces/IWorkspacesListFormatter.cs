using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Renders <c>ferret workspaces list</c> output.</summary>
internal interface IWorkspacesListFormatter
{
    /// <summary>Writes the list of workspaces.</summary>
    /// <param name="entries">The workspaces to render.</param>
    /// <param name="output">The output sink.</param>
    void Format(IReadOnlyList<WorkspaceRegistryEntry> entries, IOutputFormatter output);
}
