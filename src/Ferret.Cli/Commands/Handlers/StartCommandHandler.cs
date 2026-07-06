using Ferret.Cli.Cli;
using Ferret.Cli.Infrastructure;
using Ferret.Cli.Modules;
using Ferret.Runtime.Bootstrap;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Builds the runtime, starts it, blocks until cancellation, then shuts down cleanly.
///      TestCancellationToken allows tests to cancel without blocking indefinitely.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class StartCommandHandler : ICommandHandler
{
    /// <summary>Gets or sets a test hook: set to a pre-cancelled or short-lived token in tests to avoid blocking.</summary>
    internal static CancellationToken TestCancellationToken { get; set; } = CancellationToken.None;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var cancellationToken = TestCancellationToken.CanBeCanceled
            ? TestCancellationToken
            : context.CancellationToken;

        var output = context.Services.Output;
        output.WriteLine($"Ferret {FerretPlatform.Version}");
        output.WriteLine(VersionCommandHandler.PoweredBy);
        output.WriteLine();
        output.WriteLine("Starting runtime...");
        output.WriteLine("Loading modules...");

        // Sprint 7: plumb configPath into RuntimeBuilder when daemon config is wired.
        var runtimeHost = new RuntimeBuilder()
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning))
            .AddModule(new DiagnosticsModule(NullLogger<DiagnosticsModule>.Instance))
            .Build();

        var statusFilePath = RuntimeStatusFile.ResolvePath(context.WorkingDirectory);

        try
        {
            await runtimeHost.StartAsync(cancellationToken).ConfigureAwait(false);
            output.WriteLine("DiagnosticsModule activated.");
            output.WriteLine("Runtime ready.");

            // Durable marker: 'ferret status' runs as a separate process and cannot see this
            // process's in-memory RuntimeState, so it needs something on disk to check instead.
            RuntimeStatusFile.Write(statusFilePath, Environment.ProcessId, DateTimeOffset.UtcNow);

            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on Ctrl+C / shutdown; fall through to stop the runtime.
        }
        finally
        {
            await runtimeHost.StopAsync(CancellationToken.None).ConfigureAwait(false);
            if (runtimeHost is IAsyncDisposable d)
            {
                await d.DisposeAsync().ConfigureAwait(false);
            }

            RuntimeStatusFile.Delete(statusFilePath);
        }

        return CommandResult.Success;
    }
}
