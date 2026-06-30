namespace Ferret.Cli.Tests;

internal sealed class TempDirectory : IDisposable
{
    internal TempDirectory() => Directory.CreateDirectory(Path);

    internal string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "ferret-cli-tests-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
