using System.Globalization;

using Ferret.Cli.Cli;
using Ferret.Core.Documents;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Reports the content parsers registered in the host and the number of supported file extensions.</summary>
internal sealed class InstalledParsersCheck : IDiagnosticCheck
{
    private readonly int _parserCount;
    private readonly int _supportedExtensionCount;

    internal InstalledParsersCheck(
        IReadOnlyList<IContentParser> parsers,
        int parserCount,
        int supportedExtensionCount)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        _parserCount = parserCount;
        _supportedExtensionCount = supportedExtensionCount;
    }

    /// <inheritdoc/>
    public string Name => string.Create(
        CultureInfo.InvariantCulture,
        $"Parser platform: {_parserCount} parsers, {_supportedExtensionCount} extensions");

    /// <inheritdoc/>
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        var result = _parserCount > 0
            ? DiagnosticCheckResult.Pass()
            : DiagnosticCheckResult.Warn("No content parsers are registered; indexing will skip all files.");
        return Task.FromResult(result);
    }
}
