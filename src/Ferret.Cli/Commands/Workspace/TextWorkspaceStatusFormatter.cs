using Ferret.Cli.Cli;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Plain-text implementation of IWorkspaceStatusFormatter.</summary>
internal sealed class TextWorkspaceStatusFormatter : IWorkspaceStatusFormatter
{
    /// <inheritdoc/>
    public void Format(WorkspaceStatusView view, IOutputFormatter output)
    {
        if (view.ErrorMessage is not null)
        {
            output.WriteLine($"error: {view.ErrorMessage}");
            output.WriteLine("The workspace data may be corrupt. Try re-initialising with 'ferret workspace init'.");
            return;
        }

        if (!view.IsInWorkspace)
        {
            output.WriteLine("Not in a Ferret workspace.");
            output.WriteLine();
            output.WriteLine("Run 'ferret workspace init' to initialise one.");
            return;
        }

        output.WriteLine($"Workspace: {view.Name}");
        output.WriteLine($"  ID:      {view.Id}");
        output.WriteLine($"  Root:    {view.RootPath}");
        output.WriteLine($"  Created: {view.CreatedAt}");
    }
}
