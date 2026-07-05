using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Renders <c>ferret workspaces show</c> output.</summary>
internal interface IWorkspacesShowFormatter
{
    /// <summary>Writes full detail for one workspace.</summary>
    /// <param name="entry">The workspace to render.</param>
    /// <param name="output">The output sink.</param>
    void Format(WorkspaceRegistryEntry entry, IOutputFormatter output);
}
