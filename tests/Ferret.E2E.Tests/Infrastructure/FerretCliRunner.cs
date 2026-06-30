using System.Diagnostics;
using System.Text;

namespace Ferret.E2E.Tests.Infrastructure;

/// <summary>Runs a process and captures its stdout and stderr.</summary>
internal static class FerretCliRunner
{
    /// <summary>Runs the ferret binary with the given arguments in the given working directory.</summary>
    /// <param name="binaryPath">Absolute path to the ferret binary.</param>
    /// <param name="args">Command-line arguments to pass.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="timeout">Maximum time to wait for the process to exit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exit code, captured stdout, and captured stderr.</returns>
    internal static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string binaryPath,
        string args,
        string workingDir,
        TimeSpan timeout,
        CancellationToken ct = default) =>
        RunProcessAsync(binaryPath, args, workingDir, timeout, ct);

    /// <summary>Runs any process and captures stdout/stderr. Used internally and by FerretBinaryLocator.</summary>
    internal static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDir,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        // Capture both streams asynchronously to avoid deadlocks on large output.
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdoutBuilder.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout — kill the process, return what we have.
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Process already exited — nothing to kill.
            }

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
    }
}
