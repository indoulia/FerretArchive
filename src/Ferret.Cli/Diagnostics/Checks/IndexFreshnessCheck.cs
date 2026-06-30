using Ferret.Cli.Cli;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Checks that the keyword index database exists and was written within the last 24 hours.</summary>
internal sealed class IndexFreshnessCheck : IDiagnosticCheck
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);
    private readonly string _dbPath;

    internal IndexFreshnessCheck(string dbPath)
    {
        ArgumentNullException.ThrowIfNull(dbPath);
        _dbPath = dbPath;
    }

    /// <inheritdoc/>
    public string Name => "Index freshness";

    /// <inheritdoc/>
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        if (!File.Exists(_dbPath))
        {
            return Task.FromResult(DiagnosticCheckResult.Warn($"Index not found: {_dbPath}. Run 'ferret index' to build."));
        }

        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(_dbPath);
        if (age > MaxAge)
        {
            return Task.FromResult(DiagnosticCheckResult.Warn(
                $"Index is {age.TotalHours:F1}h old (limit: 24h). Run 'ferret index' to refresh."));
        }

        return Task.FromResult(DiagnosticCheckResult.Pass());
    }
}
