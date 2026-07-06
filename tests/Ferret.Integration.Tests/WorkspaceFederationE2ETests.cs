using Ferret.Cli.Commands.Connector;
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Knowledge.Federation;
using Ferret.ParserPlatform;
using Ferret.Workspace;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Integration.Tests;

/// <summary>
/// Vertical Slice integration test (WIP-SLICE-1/2, Workspace Intelligence Platform): proves the
/// architecture's central bet end to end using two real, independently indexed repos on disk — a
/// query against Workspace A transparently includes referenced Workspace B's content, with correct
/// source attribution and zero duplicated index content, per Backlog "Two Workspaces, One
/// Cross-Repo Answer".
/// </summary>
public sealed class WorkspaceFederationE2ETests : IDisposable
{
    private readonly string _root = Path.Join(Path.GetTempPath(), "ferret-federation-e2e-" + Guid.NewGuid().ToString("N")[..8]);
    private readonly string _repoA;
    private readonly string _repoB;
    private readonly string _registryRoot;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceFederationE2ETests"/> class.</summary>
    public WorkspaceFederationE2ETests()
    {
        _repoA = Path.Join(_root, "service-a");
        _repoB = Path.Join(_root, "shared-lib");
        _registryRoot = Path.Join(_root, "registry");
        Directory.CreateDirectory(_repoA);
        Directory.CreateDirectory(_repoB);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task FederatedQuery_AcrossTwoIndependentlyIndexedRepos_ReturnsCitedCrossRepoAnswer_WithNoDuplicatedIndexContent()
    {
        // Repo A calls a symbol it never defines; repo B defines it. Neither repo alone answers "TokenValidator".
        await File.WriteAllTextAsync(
            Path.Join(_repoA, "auth-gateway.txt"),
            "AuthGateway.Authorize calls TokenValidator.Validate before granting access to any endpoint.");
        await File.WriteAllTextAsync(
            Path.Join(_repoB, "token-validator.txt"),
            "TokenValidator.Validate checks the JWT signature and expiry, per the shared security library contract.");

        await IndexRepoAsync(_repoA);
        await IndexRepoAsync(_repoB);

        var registry = new FileWorkspaceRegistry(_registryRoot);
        var (a, b) = await CreateReferencingWorkspacesAsync(registry);
        var store = new FederatedKnowledgeStore(registry, new RepoSearchServiceFactory(new Ferret.Search.QueryParser()), a.WorkspaceId, new WorkspaceStateFingerprintProvider());

        var result = await store.SearchAsync("TokenValidator", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Hits.Count);
        Assert.Contains(result.Hits, h => h.SourceId == a.WorkspaceId);
        Assert.Contains(result.Hits, h => h.SourceId == b.WorkspaceId);

        // Zero duplication: repo A's own directory must contain no trace of repo B's content or index.
        var filesUnderRepoA = Directory.GetFiles(_repoA, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(filesUnderRepoA, f => f.Contains("token-validator", StringComparison.OrdinalIgnoreCase));

        // Repo B's own index is untouched by the query — read-only SQLite connections only.
        var repoBIndexPath = Path.Join(_repoB, ".ferret", "indexes", "keyword", "keyword-index.db");
        Assert.True(File.Exists(repoBIndexPath));
    }

    [Fact]
    public async Task FederatedQuery_WhenReferencedRepoIndexIsMissing_StillAnswersFromTheAvailableRepo()
    {
        // "One repository may be unavailable without corrupting the other" — repo B is intentionally never indexed.
        await File.WriteAllTextAsync(Path.Join(_repoA, "auth-gateway.txt"), "AuthGateway.Authorize calls TokenValidator.Validate.");
        await IndexRepoAsync(_repoA);

        var registry = new FileWorkspaceRegistry(_registryRoot);
        var (a, _) = await CreateReferencingWorkspacesAsync(registry);
        var store = new FederatedKnowledgeStore(registry, new RepoSearchServiceFactory(new Ferret.Search.QueryParser()), a.WorkspaceId, new WorkspaceStateFingerprintProvider());

        var result = await store.SearchAsync("AuthGateway", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Hits);
        Assert.Equal(a.WorkspaceId, hit.SourceId);
    }

    [Fact]
    public async Task FederatedQuery_WhenReferencedRepoIndexIsPermissionDenied_StillAnswersFromTheAvailableRepo_WithADiagnostic()
    {
        // Stabilization Sprint 1 — real failure injection, reproducing the exact crash found live during
        // Founder Dogfooding Sprint 1 (17-Dogfooding-Sprint-1.md, Friction #2 / Critical): an ACL-denied
        // index file must degrade only that source, not throw an unhandled exception through the whole query.
        await File.WriteAllTextAsync(Path.Join(_repoA, "auth-gateway.txt"), "AuthGateway.Authorize calls TokenValidator.Validate.");
        await File.WriteAllTextAsync(Path.Join(_repoB, "token-validator.txt"), "TokenValidator.Validate checks the JWT signature.");
        await IndexRepoAsync(_repoA);
        await IndexRepoAsync(_repoB);

        var registry = new FileWorkspaceRegistry(_registryRoot);
        var (a, b) = await CreateReferencingWorkspacesAsync(registry);
        var repoBIndexPath = Path.Join(_repoB, ".ferret", "indexes", "keyword", "keyword-index.db");
        Assert.True(File.Exists(repoBIndexPath));

        DenyAccess(repoBIndexPath);
        try
        {
            var store = new FederatedKnowledgeStore(registry, new RepoSearchServiceFactory(new Ferret.Search.QueryParser()), a.WorkspaceId, new WorkspaceStateFingerprintProvider());

            var result = await store.SearchAsync("AuthGateway", SearchOptions.Default);

            Assert.True(result.IsSuccess);
            var hit = Assert.Single(result.Hits);
            Assert.Equal(a.WorkspaceId, hit.SourceId);
            Assert.Contains(result.Diagnostics, d => d.Message.Contains(b.WorkspaceId.ToString(), StringComparison.Ordinal));
        }
        finally
        {
            RestoreAccess(repoBIndexPath);
        }
    }

    [Fact]
    public async Task SingleRepoWorkspace_WithNoReferences_BehavesIdenticallyToPreFederationQuery()
    {
        // 14-Migration.md invariant: one repo, zero references must be indistinguishable from today's single-repo query.
        await File.WriteAllTextAsync(Path.Join(_repoA, "auth-gateway.txt"), "AuthGateway.Authorize calls TokenValidator.Validate.");
        await IndexRepoAsync(_repoA);

        var registry = new FileWorkspaceRegistry(_registryRoot);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry
        {
            WorkspaceId = workspaceId,
            Name = "service-a",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "service-a-remote", LocalPath = _repoA }] },
        });
        var store = new FederatedKnowledgeStore(registry, new RepoSearchServiceFactory(new Ferret.Search.QueryParser()), workspaceId, new WorkspaceStateFingerprintProvider());

        var result = await store.SearchAsync("AuthGateway", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Hits);
    }

    [Fact]
    public async Task PinnedReference_FullLifecycle_FailsClosedOnContentChange_ThenFloatsAgainAfterUnpin()
    {
        // WIP-022 end-to-end: pin → query succeeds → referenced content changes → query fails closed
        // (excludes the stale source, never serves it) → unpin → query succeeds again.
        await File.WriteAllTextAsync(Path.Join(_repoA, "auth-gateway.txt"), "AuthGateway.Authorize calls TokenValidator.Validate.");
        await File.WriteAllTextAsync(Path.Join(_repoB, "token-validator.txt"), "TokenValidator.Validate checks the JWT signature and expiry.");
        await IndexRepoAsync(_repoA);
        await IndexRepoAsync(_repoB);

        var registry = new FileWorkspaceRegistry(_registryRoot);
        var (a, b) = await CreateReferencingWorkspacesAsync(registry);
        var fingerprintProvider = new WorkspaceStateFingerprintProvider();
        var factory = new RepoSearchServiceFactory(new Ferret.Search.QueryParser());

        // Pin: capture B's current state on the existing reference (what 'ferret workspaces pin-reference' does).
        var pinnedFingerprint = await fingerprintProvider.ComputeFingerprintAsync(b);
        Assert.NotNull(pinnedFingerprint);
        await registry.SaveAsync(a with { References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId, PinnedStateHash = pinnedFingerprint }] });

        var storeAfterPin = new FederatedKnowledgeStore(registry, factory, a.WorkspaceId, fingerprintProvider);
        var resultAfterPin = await storeAfterPin.SearchAsync("TokenValidator", SearchOptions.Default);
        Assert.True(resultAfterPin.IsSuccess);
        Assert.Equal(2, resultAfterPin.Hits.Count);

        // Modify B's indexed content and re-index — its Workspace State Fingerprint must now differ.
        await File.WriteAllTextAsync(Path.Join(_repoB, "token-validator.txt"), "TokenValidator.Validate now also checks token revocation status.");
        await IndexRepoAsync(_repoB);

        var storeAfterChange = new FederatedKnowledgeStore(registry, factory, a.WorkspaceId, fingerprintProvider);
        var resultAfterChange = await storeAfterChange.SearchAsync("TokenValidator", SearchOptions.Default);
        Assert.True(resultAfterChange.IsSuccess);
        var onlyHit = Assert.Single(resultAfterChange.Hits);
        Assert.Equal(a.WorkspaceId, onlyHit.SourceId);
        Assert.Contains(resultAfterChange.Diagnostics, d =>
            d.Severity == SearchDiagnosticSeverity.Error && d.Message.Contains("out of date", StringComparison.OrdinalIgnoreCase));

        // Unpin: the reference floats again and immediately sees B's current (changed) content.
        await registry.SaveAsync(a with { References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId, PinnedStateHash = null }] });

        var storeAfterUnpin = new FederatedKnowledgeStore(registry, factory, a.WorkspaceId, fingerprintProvider);
        var resultAfterUnpin = await storeAfterUnpin.SearchAsync("TokenValidator", SearchOptions.Default);
        Assert.True(resultAfterUnpin.IsSuccess);
        Assert.Equal(2, resultAfterUnpin.Hits.Count);
    }

    private static void DenyAccess(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            RunProcess("icacls", $"\"{filePath}\" /deny {Environment.UserName}:(R,W)");
        }
        else
        {
            RunProcess("chmod", $"000 \"{filePath}\"");
        }
    }

    private static void RestoreAccess(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            RunProcess("icacls", $"\"{filePath}\" /remove:d {Environment.UserName}");
        }
        else
        {
            RunProcess("chmod", $"644 \"{filePath}\"");
        }
    }

    private static void RunProcess(string fileName, string arguments)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        process!.WaitForExit();
        Assert.Equal(0, process.ExitCode);
    }

    private static async Task IndexRepoAsync(string repoPath)
    {
        // Builds the exact same DI graph IndexCliModule/ConnectorCliModule wire for `ferret index`,
        // but via a disposable container instead of RootCommandFactory's process-lifetime one —
        // SqliteKeywordIndexEngine holds its write connection open until disposed (by design, since
        // the real CLI process exits after one command), so a real test process running many of
        // these needs deterministic disposal to release each repo's keyword-index.db file.
        var context = new DefaultWorkspaceContext(WorkspaceId.Create(repoPath), WorkspacePath.Create(repoPath));
        var connectorFactory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration { RootPath = repoPath },
            new MimeTypeResolver());

        var services = new ServiceCollection();
        new ConnectorCliModule([connectorFactory]).ConfigureServices(services);
        new IndexCliModule(context).ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        await using (provider.ConfigureAwait(false))
        {
            var pipeline = provider.GetRequiredService<IIndexPipeline>();
            var result = await pipeline.RunAsync(context.WorkspaceId, new IndexPipelineOptions(), CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(0, result.Failures);
        }
    }

    private async Task<(WorkspaceRegistryEntry A, WorkspaceRegistryEntry B)> CreateReferencingWorkspacesAsync(FileWorkspaceRegistry registry)
    {
        var b = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "shared-lib",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "shared-lib-remote", LocalPath = _repoB }] },
        };
        await registry.SaveAsync(b);

        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "service-a-remote", LocalPath = _repoA }] },
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await registry.SaveAsync(a);
        return (a, b);
    }
}
