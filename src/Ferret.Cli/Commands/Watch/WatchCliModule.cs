using Ferret.Cli.Cli;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Watch;

/// <summary>CLI module for the <c>ferret watch</c> command.
/// Registers <see cref="WatchCommandHandler"/> into the DI container.</summary>
internal sealed class WatchCliModule : CliModuleBase
{
    private readonly IWorkspaceContext _workspaceContext;

    /// <summary>Initializes a new instance of the <see cref="WatchCliModule"/> class.</summary>
    /// <param name="workspaceContext">Provides workspace root for watching.</param>
    public WatchCliModule(IWorkspaceContext workspaceContext)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        _workspaceContext = workspaceContext;
    }

    /// <inheritdoc/>
    public override string Name => "ferret.watch";

    /// <inheritdoc/>
    public override string Description => "File-system watch with automatic incremental re-indexing.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("watch", "Watch the workspace for file changes and automatically re-index."),
            typeof(WatchCommandHandler));
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<WatchCommandHandler>();
    }
}
