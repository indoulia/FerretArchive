using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Renders <c>ferret workspaces list</c> output as text.</summary>
internal sealed class TextWorkspacesListFormatter : IWorkspacesListFormatter
{
    /// <inheritdoc/>
    public void Format(IReadOnlyList<WorkspaceRegistryEntry> entries, IOutputFormatter output)
    {
        if (entries.Count == 0)
        {
            output.WriteLine("No workspaces yet. Create one with: ferret workspaces create --name <name>.");
            return;
        }

        output.WriteLine($"{"NAME",-24} {"KIND",-10} {"REPOS",-6} ID");
        foreach (var entry in entries.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            output.WriteLine($"{entry.Name,-24} {entry.Kind,-10} {entry.Members.Repos.Count,-6} {entry.WorkspaceId}");
        }
    }
}
