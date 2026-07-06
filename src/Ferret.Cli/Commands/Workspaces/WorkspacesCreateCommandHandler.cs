using Ferret.Cli.Cli;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces create'.</summary>
internal sealed class WorkspacesCreateCommandHandler : ICommandHandler
{
    private static readonly string[] ValidKinds = ["personal", "team"];

    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesCreateCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    public WorkspacesCreateCommandHandler(IWorkspaceRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name");
        if (string.IsNullOrWhiteSpace(name))
        {
            context.Services.Output.WriteError("A workspace name is required. Usage: ferret workspaces create --name <name> [--kind personal|team].");
            return CommandResult.Failure;
        }

        var kindOption = context.GetOption<string>("kind") ?? "personal";
        var kind = ValidKinds.FirstOrDefault(k => string.Equals(k, kindOption, StringComparison.OrdinalIgnoreCase));
        if (kind is null)
        {
            context.Services.Output.WriteError($"Invalid --kind '{kindOption}'. Valid values: {string.Join(", ", ValidKinds)}.");
            return CommandResult.Failure;
        }

        var existing = await _registry.ListAsync(context.CancellationToken).ConfigureAwait(false);
        if (existing.Any(e => string.Equals(e.Name, name, StringComparison.Ordinal)))
        {
            context.Services.Output.WriteError($"A workspace named '{name}' already exists. Choose a different name, or use 'ferret workspaces show {name}' to inspect it.");
            return CommandResult.Failure;
        }

        var entry = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = name,
            Kind = kind,
        };
        await _registry.SaveAsync(entry, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteSuccess($"Created workspace '{name}' (id: {entry.WorkspaceId}, kind: {entry.Kind}). Next: ferret workspaces add-repo {name} <path>.");
        return CommandResult.Success;
    }
}
