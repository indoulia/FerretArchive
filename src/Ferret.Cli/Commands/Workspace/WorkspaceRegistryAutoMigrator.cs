using Ferret.Workspace.Graph;

using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Commands.Workspace;

/// <inheritdoc cref="IWorkspaceRegistryAutoMigrator"/>
internal sealed partial class WorkspaceRegistryAutoMigrator : IWorkspaceRegistryAutoMigrator
{
    private readonly IWorkspaceRegistry _registry;
    private readonly ILogger<WorkspaceRegistryAutoMigrator> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceRegistryAutoMigrator"/> class.</summary>
    public WorkspaceRegistryAutoMigrator(IWorkspaceRegistry registry, ILogger<WorkspaceRegistryAutoMigrator> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logger);
        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task EnsureMigratedAsync(string repoPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoPath);

        try
        {
            var identity = await RepoIdentityResolver.ResolveAsync(repoPath, ct).ConfigureAwait(false);
            var entries = await _registry.ListAsync(ct).ConfigureAwait(false);
            if (entries.Any(e => e.Members.Repos.Any(r => r.Remote == identity)))
            {
                return;
            }

            var rawName = Path.GetFileName(repoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var entry = new WorkspaceRegistryEntry
            {
                WorkspaceId = Guid.NewGuid(),
                Name = string.IsNullOrEmpty(rawName) ? "workspace" : rawName,
                Kind = "personal",
                Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = identity, LocalPath = repoPath }] },
            };
            await _registry.SaveAsync(entry, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogMigrationSkipped(_logger, repoPath, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Workspace registry auto-migration skipped for {RepoPath}: falling back to no-registry behavior.")]
    private static partial void LogMigrationSkipped(ILogger logger, string repoPath, Exception exception);
}
