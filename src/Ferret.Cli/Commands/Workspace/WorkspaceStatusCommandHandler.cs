using Ferret.Cli.Cli;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Workspace;

/// <summary>Handles 'ferret workspace status'.</summary>
internal sealed class WorkspaceStatusCommandHandler : ICommandHandler
{
    private readonly IWorkspaceLocator _locator;
    private readonly IWorkspaceEngine _engine;
    private readonly IWorkspaceStatusFormatter _formatter;
    private readonly IWorkspaceRegistryAutoMigrator _autoMigrator;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceStatusCommandHandler"/> class.</summary>
    public WorkspaceStatusCommandHandler(
        IWorkspaceLocator locator,
        IWorkspaceEngine engine,
        IWorkspaceStatusFormatter formatter,
        IWorkspaceRegistryAutoMigrator autoMigrator)
    {
        _locator = locator;
        _engine = engine;
        _formatter = formatter;
        _autoMigrator = autoMigrator;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var cwd = WorkspacePath.Create(context.WorkingDirectory);
        var root = await _locator.LocateAsync(cwd, context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            _formatter.Format(new WorkspaceStatusView(IsInWorkspace: false), context.Services.Output);
            return CommandResult.Success;
        }

        await _autoMigrator.EnsureMigratedAsync(root.FullPath, context.CancellationToken).ConfigureAwait(false);

        WorkspaceStatusView view;
        try
        {
            var workspace = await _engine.LoadAsync(root, ct: context.CancellationToken).ConfigureAwait(false);
            view = new WorkspaceStatusView(
                IsInWorkspace: true,
                Name: workspace.Metadata.Name,
                Id: workspace.Id.ToString(),
                RootPath: workspace.RootPath.FullPath,
                CreatedAt: workspace.Metadata.CreatedAt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            view = new WorkspaceStatusView(IsInWorkspace: false, ErrorMessage: "Workspace data is corrupt or unreadable.");
            _formatter.Format(view, context.Services.Output);
            return CommandResult.Failure;
        }

        _formatter.Format(view, context.Services.Output);
        return CommandResult.Success;
    }
}
