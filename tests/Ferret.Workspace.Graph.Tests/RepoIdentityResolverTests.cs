namespace Ferret.Workspace.Graph.Tests;

public sealed class RepoIdentityResolverTests : IDisposable
{
    private readonly string _repoPath;

    public RepoIdentityResolverTests()
    {
        _repoPath = Path.Join(Path.GetTempPath(), $"ferret-repo-identity-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_repoPath);
    }

    [Fact]
    public async Task ResolveAsync_WhenPathDoesNotExist_ThrowsRepoIdentityResolutionException()
    {
        var missingPath = Path.Join(_repoPath, "does-not-exist");

        var exception = await Assert.ThrowsAsync<RepoIdentityResolutionException>(() => RepoIdentityResolver.ResolveAsync(missingPath));

        Assert.Equal(missingPath, exception.RepoPath);
    }

    [Fact]
    public async Task ResolveAsync_WhenPathIsNotAGitRepository_ThrowsRepoIdentityResolutionException()
    {
        await Assert.ThrowsAsync<RepoIdentityResolutionException>(() => RepoIdentityResolver.ResolveAsync(_repoPath));
    }

    [Fact]
    public async Task ResolveAsync_WithOriginRemote_ReturnsCanonicalizedOrigin()
    {
        WriteGitConfig("""
            [remote "origin"]
                url = git@github.com:acme/service-a.git
            [remote "upstream"]
                url = git@github.com:upstream/service-a.git
            """);

        var result = await RepoIdentityResolver.ResolveAsync(_repoPath);

        Assert.Equal("github.com/acme/service-a", result);
    }

    [Fact]
    public async Task ResolveAsync_WithNoOriginButOneOtherRemote_ReturnsThatRemote()
    {
        WriteGitConfig("""
            [remote "upstream"]
                url = git@github.com:acme/service-a.git
            """);

        var result = await RepoIdentityResolver.ResolveAsync(_repoPath);

        Assert.Equal("github.com/acme/service-a", result);
    }

    [Fact]
    public async Task ResolveAsync_WithNoOriginAndMultipleOtherRemotes_ReturnsAlphabeticallyFirst()
    {
        WriteGitConfig("""
            [remote "zzz-mirror"]
                url = git@github.com:mirror/service-a.git
            [remote "alpha-fork"]
                url = git@github.com:alpha/service-a.git
            """);

        var result = await RepoIdentityResolver.ResolveAsync(_repoPath);

        Assert.Equal("github.com/alpha/service-a", result);
    }

    [Fact]
    public async Task ResolveAsync_WithNoRemotesAtAll_ReturnsLocalFallbackIdentity()
    {
        WriteGitConfig("[core]\n\trepositoryformatversion = 0");

        var result = await RepoIdentityResolver.ResolveAsync(_repoPath);

        Assert.StartsWith("local:", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveAsync_WithNoRemotesAtAll_PersistsIdentityFile()
    {
        WriteGitConfig("[core]\n\trepositoryformatversion = 0");

        await RepoIdentityResolver.ResolveAsync(_repoPath);

        Assert.True(File.Exists(Path.Join(_repoPath, ".ferret", "workspace-identity.json")));
    }

    [Fact]
    public async Task ResolveAsync_WithNoRemotesAtAll_CalledTwice_ReturnsTheSameIdentityBothTimes()
    {
        WriteGitConfig("[core]\n\trepositoryformatversion = 0");

        var first = await RepoIdentityResolver.ResolveAsync(_repoPath);
        var second = await RepoIdentityResolver.ResolveAsync(_repoPath);

        Assert.Equal(first, second);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoPath))
        {
            Directory.Delete(_repoPath, recursive: true);
        }
    }

    private void WriteGitConfig(string content)
    {
        var gitDir = Path.Join(_repoPath, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Join(gitDir, "config"), content);
    }
}
