using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

namespace Ferret.Manual;

/// <summary>Starts <see cref="ManualServer"/>, opens a browser, and blocks until the process is cancelled.</summary>
public sealed class ManualCommandHandler
{
    private static readonly Action<ILogger, string, Exception?> LogRunning =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "ManualRunning"),
            "The Ferret Manual is running at {Url}");

    private static readonly Action<ILogger, string, Exception?> LogBrowserFailed =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(2, "BrowserFailed"),
            "Could not open browser: {Message}");

    private readonly ILogger<ManualCommandHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ManualCommandHandler"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public ManualCommandHandler(ILogger<ManualCommandHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>Runs the manual server on the specified port and blocks until <paramref name="ct"/> is cancelled.</summary>
    /// <param name="port">TCP port for the manual server (default 7070).</param>
    /// <param name="ct">Cancellation token — fires on Ctrl+C.</param>
    /// <returns>Exit code (0 on graceful shutdown).</returns>
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "User-facing CLI startup message; localization not required.")]
    public async Task<int> HandleAsync(int port = 7070, CancellationToken ct = default)
    {
        using var server = new ManualServer(port);
        var url = server.BaseUrl;

        Console.WriteLine(FormattableString.Invariant($"The Ferret Manual → {url}"));
        Console.WriteLine("Press Ctrl+C to stop.");
        LogRunning(_logger, url.ToString(), null);

        _ = server.StartAsync(ct);

        OpenBrowser(url.ToString());

        try
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C — graceful shutdown
        }

        return 0;
    }

    private void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (InvalidOperationException ex)
        {
            LogBrowserFailed(_logger, ex.Message, null);
        }
        catch (PlatformNotSupportedException ex)
        {
            LogBrowserFailed(_logger, ex.Message, null);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            LogBrowserFailed(_logger, ex.Message, null);
        }
    }
}
