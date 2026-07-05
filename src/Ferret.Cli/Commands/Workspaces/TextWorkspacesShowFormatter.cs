using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Renders <c>ferret workspaces show</c> output as text.</summary>
internal sealed class TextWorkspacesShowFormatter : IWorkspacesShowFormatter
{
    /// <inheritdoc/>
    public void Format(WorkspaceRegistryEntry entry, IOutputFormatter output)
    {
        output.WriteLine($"Name:          {entry.Name}");
        output.WriteLine($"ID:            {entry.WorkspaceId}");
        output.WriteLine($"Kind:          {entry.Kind}");
        output.WriteLine($"Schema:        {entry.SchemaVersion}");
        output.WriteLine();

        output.WriteLine($"Repos ({entry.Members.Repos.Count}):");
        if (entry.Members.Repos.Count == 0)
        {
            output.WriteLine("  (none — add one with: ferret workspaces add-repo " + entry.Name + " <path>)");
        }

        foreach (var repo in entry.Members.Repos)
        {
            output.WriteLine($"  - {repo.Remote}" + (repo.LocalPath is null ? string.Empty : $" ({repo.LocalPath})"));
        }

        output.WriteLine();
        output.WriteLine($"Documents ({entry.Members.Documents.Count}):");
        foreach (var document in entry.Members.Documents)
        {
            output.WriteLine($"  - {document.Path} [{document.Type}]");
        }
    }
}
