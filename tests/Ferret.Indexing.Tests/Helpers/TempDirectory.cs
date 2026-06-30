namespace Ferret.Indexing.Tests.Helpers;

/// <summary>Creates a temporary directory and deletes it on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    internal TempDirectory()
    {
        Path = System.IO.Path.Join(
            System.IO.Path.GetTempPath(),
            $"ferret-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the absolute path to the temporary directory.</summary>
    internal string Path { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
