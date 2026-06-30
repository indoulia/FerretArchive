using Ferret.Core.Primitives;
using Ferret.Core.Results;
using Ferret.Core.Workspace;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace;

/// <summary>
/// Implements IWorkspaceEngine for Sprint 7: init and load.
/// GetHealthAsync, GetChangesetAsync, UpgradeAsync, ValidateAsync are deferred to Sprint 8.
/// </summary>
public sealed class WorkspaceEngine : IWorkspaceEngine
{
    private readonly WorkspaceLocator _locator = new();

    /// <inheritdoc/>
    public async Task<WorkspaceInitResult> InitialiseAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        if (await _locator.ExistsAsync(rootPath, ct).ConfigureAwait(false))
        {
            return WorkspaceInitResult.Failure($"Workspace already exists at: {rootPath.FullPath}");
        }

        var context = await WorkspaceInitializer.InitialiseAsync(rootPath, options, ct).ConfigureAwait(false);
        return WorkspaceInitResult.Success(context);
    }

    /// <inheritdoc/>
    public async Task<WorkspaceContext> LoadAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var manifest = await JsonWorkspaceStore.ReadManifestAsync(rootPath, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No workspace manifest found at: {rootPath.FullPath}");

        var id = WorkspaceId.Create(manifest.Id);
        var metadata = WorkspaceMetadata.Create(manifest.Name, manifest.Description, manifest.SchemaVersion, manifest.CreatedAt);
        var capabilities = WorkspaceCapabilities.Create(options?.ReadOnly ?? false, 0, 0);
        return WorkspaceContext.Create(rootPath, id, metadata, capabilities);
    }

    /// <inheritdoc/>
    public Task<WorkspaceHealthReport> GetHealthAsync(WorkspaceContext context, HealthCheckDepth depth = HealthCheckDepth.Quick, CancellationToken ct = default) =>
        throw new NotImplementedException("Workspace health — Sprint 8.");

    /// <inheritdoc/>
    public Task<Changeset> GetChangesetAsync(WorkspaceContext context, CancellationToken ct = default) =>
        throw new NotImplementedException("Change detection — Sprint 8.");

    /// <inheritdoc/>
    public Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext context, CancellationToken ct = default) =>
        throw new NotImplementedException("Workspace upgrade — Sprint 8.");

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateAsync(WorkspaceContext context, CancellationToken ct = default) =>
        throw new NotImplementedException("Workspace validation — Sprint 8.");
}
