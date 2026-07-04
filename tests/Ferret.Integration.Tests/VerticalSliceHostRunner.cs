using System.Diagnostics;

namespace Ferret.Integration.Tests;

/// <summary>
/// Shared launcher for <c>Ferret.VerticalSliceHost</c>, used by every test that needs a genuine
/// process boundary (per the vertical slice plan's Global Constraints). Extracted so T7's and
/// T9's tests compose the same subprocess-launch mechanics instead of duplicating them.
/// </summary>
internal static class VerticalSliceHostRunner
{
    /// <summary>Launches the host in the given mode, waits for it to fully exit, and returns its trimmed stdout.</summary>
    internal static async Task<string> RunAsync(string mode, string rootPath, string fileName, string storePath)
    {
        var hostDllPath = FindHostDll();
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = rootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(hostDllPath);
        startInfo.ArgumentList.Add(mode);
        startInfo.ArgumentList.Add(rootPath);
        startInfo.ArgumentList.Add(fileName);
        startInfo.ArgumentList.Add(storePath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start host process.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Host process exited with code {process.ExitCode}: {stderr}");
        }

        return stdout.Trim();
    }

    private static string FindHostDll()
    {
        var binDir = new DirectoryInfo(AppContext.BaseDirectory);
        var config = binDir.Parent ?? throw new InvalidOperationException("Cannot locate build configuration directory.");
        var testsDir = config.Parent?.Parent?.Parent ?? throw new InvalidOperationException("Cannot locate tests directory.");
        var hostDllPath = Path.Combine(testsDir.FullName, "Ferret.VerticalSliceHost", "bin", config.Name, binDir.Name, "Ferret.VerticalSliceHost.dll");

        if (!File.Exists(hostDllPath))
        {
            throw new FileNotFoundException($"Host executable not built: {hostDllPath}. Build Ferret.VerticalSliceHost before running this test.");
        }

        return hostDllPath;
    }
}
