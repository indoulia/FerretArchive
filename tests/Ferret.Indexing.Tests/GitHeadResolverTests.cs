using Ferret.Indexing;

namespace Ferret.Indexing.Tests;

public sealed class GitHeadResolverTests : IDisposable
{
    private readonly string _root;

    public GitHeadResolverTests()
    {
        _root = Path.Join(Path.GetTempPath(), $"ferret-githead-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void TryResolveHeadSha_NoGitDirectory_ReturnsNull()
    {
        Assert.Null(GitHeadResolver.TryResolveHeadSha(_root));
    }

    [Fact]
    public void TryResolveHeadSha_LooseRef_ReturnsCommitSha()
    {
        var sha = "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0";
        WriteGitFile(".git/HEAD", "ref: refs/heads/main\n");
        WriteGitFile(".git/refs/heads/main", sha + "\n");

        Assert.Equal(sha, GitHeadResolver.TryResolveHeadSha(_root));
    }

    [Fact]
    public void TryResolveHeadSha_DetachedHead_ReturnsRawSha()
    {
        var sha = "1111111222223333344444555556666677777888";
        WriteGitFile(".git/HEAD", sha + "\n");

        Assert.Equal(sha, GitHeadResolver.TryResolveHeadSha(_root));
    }

    [Fact]
    public void TryResolveHeadSha_PackedRefsFallback_ReturnsCommitSha()
    {
        var sha = "abcdef0123456789abcdef0123456789abcdef01";
        WriteGitFile(".git/HEAD", "ref: refs/heads/main\n");
        WriteGitFile(".git/packed-refs", $"# pack-refs with: peeled fully-peeled sorted\n{sha} refs/heads/main\n");

        Assert.Equal(sha, GitHeadResolver.TryResolveHeadSha(_root));
    }

    [Fact]
    public void TryResolveHeadSha_UnresolvableRef_ReturnsNull()
    {
        WriteGitFile(".git/HEAD", "ref: refs/heads/does-not-exist\n");

        Assert.Null(GitHeadResolver.TryResolveHeadSha(_root));
    }

    [Fact]
    public void TryResolveHeadSha_NullOrWhitespaceRoot_Throws()
    {
        Assert.Throws<ArgumentException>(() => GitHeadResolver.TryResolveHeadSha(" "));
    }

    private void WriteGitFile(string relativePath, string content)
    {
        var fullPath = Path.Join(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
    }
}
