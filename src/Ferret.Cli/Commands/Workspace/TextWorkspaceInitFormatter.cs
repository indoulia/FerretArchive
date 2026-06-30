using Ferret.Cli.Cli;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Plain-text implementation of IWorkspaceInitFormatter.</summary>
internal sealed class TextWorkspaceInitFormatter : IWorkspaceInitFormatter
{
    /// <inheritdoc/>
    public void Format(WorkspaceInitView view, IOutputFormatter output)
    {
        if (!view.Succeeded)
        {
            output.WriteLine($"error: {view.ErrorMessage}");
            return;
        }

        output.WriteLine($"Initialised Ferret workspace at {view.RootPath}");
        output.WriteLine($"  {view.FerretPath}/");
        output.WriteLine();
        output.WriteLine("Next: run 'ferret workspace status' to verify.");
    }
}
