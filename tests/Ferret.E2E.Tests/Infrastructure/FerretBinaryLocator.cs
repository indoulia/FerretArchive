namespace Ferret.E2E.Tests.Infrastructure;

/// <summary>Resolves and publishes the ferret CLI binary once per test session.</summary>
internal static class FerretBinaryLocator
{
    // Readonly fields first (SA1214), then non-readonly.
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static string? _binaryPath;

    /// <summary>Returns the absolute path to the ferret binary, publishing it if needed.</summary>
    internal static async Task<string> GetOrPublishAsync(CancellationToken ct = default)
    {
        if (Volatile.Read(ref _binaryPath) is not null)
        {
            return _binaryPath!;
        }

        await Lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var cached = Volatile.Read(ref _binaryPath);
            if (cached is not null)
            {
                return cached;
            }

            // Resolve repo root: this assembly lives in tests/Ferret.E2E.Tests/bin/...
            // Walk up until we find the src/ directory containing Ferret.Cli.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Ferret.Cli")))
            {
                dir = dir.Parent;
            }

            if (dir is null)
            {
                throw new InvalidOperationException(
                    "Cannot locate repo root from " + AppContext.BaseDirectory);
            }

            var cliProjectPath = Path.Combine(dir.FullName, "src", "Ferret.Cli", "Ferret.Cli.csproj");
            var publishDir = Path.Combine(
                Path.GetTempPath(),
                "ferret-e2e-publish-" + Environment.ProcessId);

            var (exitCode, _, stderr) = await FerretCliRunner.RunProcessAsync(
                "dotnet",
                $"publish \"{cliProjectPath}\" -c Release -f net9.0 -o \"{publishDir}\" --nologo -v quiet",
                workingDir: dir.FullName,
                timeout: TimeSpan.FromSeconds(120),
                ct: ct).ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"dotnet publish failed (exit {exitCode}):\n{stderr}");
            }

            // AssemblyName in Ferret.Cli.csproj is "ferret" (not "Ferret.Cli").
            var exeName = OperatingSystem.IsWindows() ? "ferret.exe" : "ferret";
            var binaryPath = Path.Combine(publishDir, exeName);

            if (!File.Exists(binaryPath))
            {
                throw new FileNotFoundException(
                    $"Published binary not found at: {binaryPath}");
            }

            Volatile.Write(ref _binaryPath, binaryPath);
            return _binaryPath!;
        }
        finally
        {
            Lock.Release();
        }
    }
}
