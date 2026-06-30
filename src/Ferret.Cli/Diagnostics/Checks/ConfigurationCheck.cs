using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Diagnostic check that validates Ferret configuration can be loaded.</summary>
internal sealed class ConfigurationCheck : IDiagnosticCheck
{
    private readonly string? _configPath;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationCheck"/> class.</summary>
    /// <param name="configPath">Optional path to a configuration file.</param>
    internal ConfigurationCheck(string? configPath = null)
    {
        _configPath = configPath;
    }

    /// <inheritdoc/>
    public string Name => "Configuration loaded";

    /// <inheritdoc/>
#pragma warning disable CA1031 // Do not catch general exception types
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        try
        {
            FerretConfigLoader.Load(_configPath);
            return Task.FromResult(DiagnosticCheckResult.Pass());
        }
        catch (Exception ex)
        {
            return Task.FromResult(DiagnosticCheckResult.Fail(ex.Message));
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types

}
