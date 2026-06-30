using Ferret.Cli.Cli;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Formats a WorkspaceStatusView for CLI output.</summary>
internal interface IWorkspaceStatusFormatter
{
    /// <summary>Writes the workspace status to the output formatter.</summary>
    /// <param name="view">The view model to render.</param>
    /// <param name="output">The output formatter to write to.</param>
    void Format(WorkspaceStatusView view, IOutputFormatter output);
}
