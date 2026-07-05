using Ferret.Cli.Commands.Workspaces;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Tests.Commands.Workspaces;

public sealed class WorkspaceStateFingerprintProviderTests : IDisposable
{
    private readonly string _root;
    private readonly WorkspaceStateFingerprintProvider _provider = new();

    public WorkspaceStateFingerprintProviderTests()
    {
        _root = Path.Join(Path.GetTempPath(), $"ferret-fingerprint-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task ComputeFingerprintAsync_CalledTwiceOnUnchangedContent_ReturnsTheSameValue()
    {
        var repoPath = CreateRepo("repo-a", ("file.txt", "hello"));
        var entry = WorkspaceWithRepo(repoPath);

        var first = await _provider.ComputeFingerprintAsync(entry);
        var second = await _provider.ComputeFingerprintAsync(entry);

        Assert.NotNull(first);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task ComputeFingerprintAsync_SameIdentityAndContentAtADifferentCheckoutPathAndMtime_ReturnsTheSameValue()
    {
        // Portability: a fresh clone/checkout of the identical repo resets mtimes and may live at a
        // different local path, but must fingerprint identically (ADR-0027 Amendment invariant #3).
        const string sharedRemoteIdentity = "git@example.com:org/shared-lib.git";
        var checkout1 = CreateRepo("checkout-1", ("file.txt", "hello"));
        var checkout2 = CreateRepo("checkout-2", ("file.txt", "hello"));
        File.SetLastWriteTimeUtc(Path.Join(checkout2, "file.txt"), DateTime.UtcNow.AddDays(-30));

        var fingerprint1 = await _provider.ComputeFingerprintAsync(WorkspaceWithRepo(sharedRemoteIdentity, checkout1));
        var fingerprint2 = await _provider.ComputeFingerprintAsync(WorkspaceWithRepo(sharedRemoteIdentity, checkout2));

        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task ComputeFingerprintAsync_WhenFileContentChanges_ReturnsADifferentValue()
    {
        var repoPath = CreateRepo("repo-a", ("file.txt", "hello"));
        var entry = WorkspaceWithRepo(repoPath);
        var before = await _provider.ComputeFingerprintAsync(entry);

        await File.WriteAllTextAsync(Path.Join(repoPath, "file.txt"), "goodbye");
        var after = await _provider.ComputeFingerprintAsync(entry);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public async Task ComputeFingerprintAsync_WhenRepoLocalPathIsUnreachable_ReturnsNull()
    {
        var entry = WorkspaceWithRepo(Path.Join(_root, "does-not-exist"));

        var result = await _provider.ComputeFingerprintAsync(entry);

        Assert.Null(result);
    }

    private static WorkspaceRegistryEntry WorkspaceWithRepo(string repoPath) =>
        WorkspaceWithRepo(remote: repoPath, localPath: repoPath);

    private static WorkspaceRegistryEntry WorkspaceWithRepo(string remote, string localPath) => new()
    {
        WorkspaceId = Guid.NewGuid(),
        Name = "test",
        Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = remote, LocalPath = localPath }] },
    };

    private string CreateRepo(string name, params (string RelativePath, string Content)[] files)
    {
        var repoPath = Path.Join(_root, name);
        Directory.CreateDirectory(repoPath);
        foreach (var (relativePath, content) in files)
        {
            File.WriteAllText(Path.Join(repoPath, relativePath), content);
        }

        return repoPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
