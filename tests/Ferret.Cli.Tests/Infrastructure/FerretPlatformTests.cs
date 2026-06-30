using Ferret.Cli.Infrastructure;

namespace Ferret.Cli.Tests.Infrastructure;

public sealed class FerretPlatformTests
{
    [Fact]
    public void Version_IsNotEmpty() => Assert.False(string.IsNullOrWhiteSpace(FerretPlatform.Version));

    [Fact]
    public void Version_MatchesSemVer() => Assert.Matches(@"^\d+\.\d+\.\d+", FerretPlatform.Version);

    [Fact]
    public void RuntimeInfo_ContainsDotNet() => Assert.Contains(".NET", FerretPlatform.RuntimeInfo, StringComparison.OrdinalIgnoreCase);
}
