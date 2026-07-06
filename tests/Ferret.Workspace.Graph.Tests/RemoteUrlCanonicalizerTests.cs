namespace Ferret.Workspace.Graph.Tests;

public sealed class RemoteUrlCanonicalizerTests
{
    [Fact]
    public void Canonicalize_SshShorthand_ProducesHostSlashPath()
    {
        var result = RemoteUrlCanonicalizer.Canonicalize("git@github.com:acme/service-a.git");

        Assert.Equal("github.com/acme/service-a", result);
    }

    [Fact]
    public void Canonicalize_Https_ProducesHostSlashPath()
    {
        var result = RemoteUrlCanonicalizer.Canonicalize("https://github.com/acme/service-a.git");

        Assert.Equal("github.com/acme/service-a", result);
    }

    [Fact]
    public void Canonicalize_SshShorthandAndHttps_ForTheSameRepo_ProduceTheSameIdentity()
    {
        var sshResult = RemoteUrlCanonicalizer.Canonicalize("git@github.com:acme/service-a.git");
        var httpsResult = RemoteUrlCanonicalizer.Canonicalize("https://github.com/acme/service-a");

        Assert.Equal(sshResult, httpsResult);
    }

    [Fact]
    public void Canonicalize_SshUrlScheme_ProducesHostSlashPath()
    {
        var result = RemoteUrlCanonicalizer.Canonicalize("ssh://git@github.com/acme/service-a.git");

        Assert.Equal("github.com/acme/service-a", result);
    }

    [Fact]
    public void Canonicalize_IsCaseInsensitiveOnHost()
    {
        var result = RemoteUrlCanonicalizer.Canonicalize("git@GitHub.com:acme/service-a.git");

        Assert.Equal("github.com/acme/service-a", result);
    }

    [Fact]
    public void Canonicalize_HttpsWithoutGitSuffix_MatchesHttpsWithGitSuffix()
    {
        var withSuffix = RemoteUrlCanonicalizer.Canonicalize("https://github.com/acme/service-a.git");
        var withoutSuffix = RemoteUrlCanonicalizer.Canonicalize("https://github.com/acme/service-a");

        Assert.Equal(withSuffix, withoutSuffix);
    }
}
