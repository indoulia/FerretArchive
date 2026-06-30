using Ferret.Cli.Search;

using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class AnsiTextStylerTests
{
    private readonly AnsiTextStyler _styler = new AnsiTextStyler();

    [Fact]
    public void Match_Wraps_Text_In_Bold_Escape_Sequence()
    {
        var result = _styler.Match("authentication");
        Assert.StartsWith("\x1B[1m", result, System.StringComparison.Ordinal);
        Assert.EndsWith("\x1B[0m", result, System.StringComparison.Ordinal);
        Assert.Contains("authentication", result, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Muted_Wraps_Text_In_Dim_Escape_Sequence()
    {
        var result = _styler.Muted("metadata");
        Assert.StartsWith("\x1B[2m", result, System.StringComparison.Ordinal);
        Assert.EndsWith("\x1B[0m", result, System.StringComparison.Ordinal);
        Assert.Contains("metadata", result, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Normal_Returns_Text_Unchanged()
    {
        var result = _styler.Normal("plain text");
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void Match_Preserves_Inner_Text_Verbatim()
    {
        var result = _styler.Match("auth token");
        Assert.Contains("auth token", result, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Muted_Preserves_Inner_Text_Verbatim()
    {
        var result = _styler.Muted("12ms · bm25");
        Assert.Contains("12ms · bm25", result, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Match_Returns_Non_Empty_String()
    {
        Assert.False(string.IsNullOrEmpty(_styler.Match("x")));
    }

    [Fact]
    public void Match_And_Normal_Produce_Different_Output_For_Same_Input()
    {
        var text = "authentication";
        Assert.NotEqual(_styler.Match(text), _styler.Normal(text));
    }

    [Fact]
    public void Muted_And_Normal_Produce_Different_Output_For_Same_Input()
    {
        var text = "metadata";
        Assert.NotEqual(_styler.Muted(text), _styler.Normal(text));
    }
}
