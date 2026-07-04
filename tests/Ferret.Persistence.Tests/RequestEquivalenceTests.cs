using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class RequestEquivalenceTests
{
    [Fact]
    public void AreEquivalent_SameEngineResponsibilityAndRequestPath_ReturnsTrue()
    {
        var result = RequestEquivalence.AreEquivalent("ParseFile", "/repo/a.md", "ParseFile", "/repo/a.md");

        Assert.True(result);
    }

    [Fact]
    public void AreEquivalent_DifferentRequestPath_ReturnsFalse()
    {
        var result = RequestEquivalence.AreEquivalent("ParseFile", "/repo/a.md", "ParseFile", "/repo/b.md");

        Assert.False(result);
    }
}
