using Ferret.AI.Context;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class TokenEstimatorTests
{
    [Fact]
    public void Estimate_EmptyString_ReturnsOne()
    {
        Assert.Equal(1, TokenEstimator.Estimate(string.Empty));
    }

    [Fact]
    public void Estimate_FourCharString_ReturnsOne()
    {
        Assert.Equal(1, TokenEstimator.Estimate("abcd"));
    }

    [Fact]
    public void Estimate_EightCharString_ReturnsTwo()
    {
        Assert.Equal(2, TokenEstimator.Estimate("abcdefgh"));
    }

    [Fact]
    public void Estimate_OneCharString_ReturnsOne()
    {
        Assert.Equal(1, TokenEstimator.Estimate("a"));
    }

    [Fact]
    public void Estimate_HundredCharString_ReturnsTwentyFive()
    {
        Assert.Equal(25, TokenEstimator.Estimate(new string('x', 100)));
    }

    [Fact]
    public void Estimate_NullString_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => TokenEstimator.Estimate(null!));
    }
}
