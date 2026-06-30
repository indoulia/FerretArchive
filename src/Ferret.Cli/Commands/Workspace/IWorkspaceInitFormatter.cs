using Ferret.Cli.Cli;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Formats a WorkspaceInitView for CLI output.</summary>
internal interface IWorkspaceInitFormatter
{
    /// <summary>Writes the initialisation result to the output formatter.</summary>
    /// <param name="view">The view model to render.</param>
    /// <param name="output">The output formatter to write to.</param>
    void Format(WorkspaceInitView view, IOutputFormatter output);
}
