using Ferret.Cli.Cli;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Handles 'ferret workspace init'.</summary>
internal sealed class WorkspaceInitCommandHandler : ICommandHandler
{
    private readonly IWorkspaceEngine _engine;
    private readonly IWorkspaceInitFormatter _formatter;
    private readonly IWorkspaceRegistryAutoMigrator _autoMigrator;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceInitCommandHandler"/> class.</summary>
    public WorkspaceInitCommandHandler(IWorkspaceEngine engine, IWorkspaceInitFormatter formatter, IWorkspaceRegistryAutoMigrator autoMigrator)
    {
        _engine = engine;
        _formatter = formatter;
        _autoMigrator = autoMigrator;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var result = await _engine.InitialiseAsync(rootPath, ct: context.CancellationToken).ConfigureAwait(false);

        var view = result.Succeeded
            ? new WorkspaceInitView(true, rootPath.FullPath, System.IO.Path.Join(rootPath.FullPath, ".ferret"), null)
            : new WorkspaceInitView(false, null, null, result.ErrorMessage);

        _formatter.Format(view, context.Services.Output);

        if (result.Succeeded)
        {
            await _autoMigrator.EnsureMigratedAsync(rootPath.FullPath, context.CancellationToken).ConfigureAwait(false);
        }

        return result.Succeeded ? CommandResult.Success : CommandResult.Failure;
    }
}
