using Ferret.Cli.Cli;
using Ferret.Core.Git;
using Ferret.Core.Indexing;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Checks that the keyword index database exists, was written within the last 24 hours,
/// and (when a workspace root and state store are supplied) was built against the current git HEAD.</summary>
internal sealed class IndexFreshnessCheck : IDiagnosticCheck
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);
    private readonly string _dbPath;
    private readonly string? _workspaceRoot;
    private readonly IIndexStateStore? _stateStore;

    internal IndexFreshnessCheck(string dbPath, string? workspaceRoot = null, IIndexStateStore? stateStore = null)
    {
        ArgumentNullException.ThrowIfNull(dbPath);
        _dbPath = dbPath;
        _workspaceRoot = workspaceRoot;
        _stateStore = stateStore;
    }

    /// <inheritdoc/>
    public string Name => "Index freshness";

    /// <inheritdoc/>
    public async Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        if (!File.Exists(_dbPath))
        {
            return DiagnosticCheckResult.Warn($"Index not found: {_dbPath}. Run 'ferret index' to build.");
        }

        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(_dbPath);
        if (age > MaxAge)
        {
            return DiagnosticCheckResult.Warn(
                $"Index is {age.TotalHours:F1}h old (limit: 24h). Run 'ferret index' to refresh.");
        }

        if (_workspaceRoot is not null && _stateStore is not null)
        {
            var indexedHead = await _stateStore.GetIndexedGitHeadAsync(cancellationToken).ConfigureAwait(false);
            var currentHead = GitHeadResolver.TryResolveHeadSha(_workspaceRoot);
            if (indexedHead is not null && currentHead is not null &&
                !string.Equals(indexedHead, currentHead, StringComparison.Ordinal))
            {
                return DiagnosticCheckResult.Warn(
                    $"Index was built on git commit {Short(indexedHead)}, but HEAD is now {Short(currentHead)}. " +
                    "Run 'ferret index' to refresh.");
            }
        }

        return DiagnosticCheckResult.Pass();
    }

    private static string Short(string sha) => sha[..Math.Min(7, sha.Length)];
}
