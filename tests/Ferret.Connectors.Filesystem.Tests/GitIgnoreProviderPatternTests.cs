using Ferret.Connectors.Filesystem.Ignore;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class GitIgnoreProviderPatternTests
{
    // ** — matches across path segments

    [Theory]
    [InlineData("**/bin", "src/MyLib/bin")]
    [InlineData("**/bin", "bin")]
    [InlineData("**/obj/**", "src/MyLib/obj/Debug/net9.0/out.dll")]
    [InlineData("**/*.log", "logs/debug/server.log")]
    public void MatchesPattern_DoubleGlob_Matches(string pattern, string input)
        => Assert.True(GitIgnoreProvider.MatchesPattern(pattern, input));

    [Theory]
    [InlineData("**/bin", "src/bin_backup")]
    [InlineData("**/bin", "src/bingo")]
    public void MatchesPattern_DoubleGlob_NoMatch(string pattern, string input)
        => Assert.False(GitIgnoreProvider.MatchesPattern(pattern, input));

    // Leading / — anchors to root (path must start with the remainder)

    [Theory]
    [InlineData("/dist", "dist")]
    [InlineData("/dist", "dist/bundle.js")]
    public void MatchesPattern_LeadingSlash_Matches(string pattern, string input)
        => Assert.True(GitIgnoreProvider.MatchesPattern(pattern, input));

    [Theory]
    [InlineData("/dist", "src/dist")]
    [InlineData("/dist", "build/dist/app")]
    public void MatchesPattern_LeadingSlash_NoMatch(string pattern, string input)
        => Assert.False(GitIgnoreProvider.MatchesPattern(pattern, input));
}
