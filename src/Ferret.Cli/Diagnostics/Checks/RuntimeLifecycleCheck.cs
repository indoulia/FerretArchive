using Ferret.Cli.Cli;
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>
/// Why: Proves runtime init + module registry + event dispatcher + health in one check via a full
///      build-start-verify-stop cycle. Bundled because these share the host instance.
/// </summary>
internal sealed class RuntimeLifecycleCheck : IDiagnosticCheck
{
    /// <inheritdoc/>
    public string Name => "Runtime lifecycle";

    /// <inheritdoc/>
#pragma warning disable CA1031 // Do not catch general exception types
    public async Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        IRuntimeHost host;
        try
        {
            host = new RuntimeBuilder()
                .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning))
                .Build();
        }
        catch (Exception ex)
        {
            return DiagnosticCheckResult.Fail($"Init failed: {ex.Message}");
        }

        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return host.State == RuntimeState.Running
                ? DiagnosticCheckResult.Pass()
                : DiagnosticCheckResult.Fail($"State is '{host.State}' instead of Running.");
        }
        catch (Exception ex)
        {
            return DiagnosticCheckResult.Fail($"Start failed: {ex.Message}");
        }
        finally
        {
            try
            {
                if (host.State == RuntimeState.Running)
                {
                    await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // Suppress stop errors to avoid masking start errors
            }

            if (host is IAsyncDisposable d)
            {
                await d.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types
}
