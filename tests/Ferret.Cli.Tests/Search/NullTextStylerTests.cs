using Ferret.Cli.Search;

using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class NullTextStylerTests
{
    private readonly NullTextStyler _styler = new NullTextStyler();

    [Fact]
    public void Match_Returns_Text_Unchanged()
    {
        Assert.Equal("authentication", _styler.Match("authentication"));
    }

    [Fact]
    public void Muted_Returns_Text_Unchanged()
    {
        Assert.Equal("metadata", _styler.Muted("metadata"));
    }

    [Fact]
    public void Normal_Returns_Text_Unchanged()
    {
        Assert.Equal("plain", _styler.Normal("plain"));
    }

    [Fact]
    public void All_Methods_Are_Pure_Passthrough()
    {
        const string input = "any text";
        Assert.Equal(input, _styler.Match(input));
        Assert.Equal(input, _styler.Muted(input));
        Assert.Equal(input, _styler.Normal(input));
    }
}
