using Ferret.Core.Search;
using Ferret.Search.Providers.Bm25;
using Xunit;

namespace Ferret.Search.Tests.Providers.Bm25;

public sealed class HighlightParserTests
{
    // Sentinel constants (same as HighlightParser internals)
    private const char Open = '\x02';
    private const char Close = '\x03';

    [Fact]
    public void Plain_Text_Produces_Single_Normal_Span()
    {
        var ht = HighlightParser.Parse("hello world");
        Assert.Single(ht.Spans);
        Assert.Equal("hello world", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
    }

    [Fact]
    public void Match_Sentinel_Produces_Match_Span()
    {
        var ht = HighlightParser.Parse($"{Open}auth{Close}");
        Assert.Single(ht.Spans);
        Assert.Equal("auth", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[0].Kind);
    }

    [Fact]
    public void Normal_Then_Match_Produces_Two_Spans()
    {
        var ht = HighlightParser.Parse($"before {Open}auth{Close}");
        Assert.Equal(2, ht.Spans.Count);
        Assert.Equal("before ", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
        Assert.Equal("auth", ht.Spans[1].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[1].Kind);
    }

    [Fact]
    public void Match_Then_Normal_Produces_Two_Spans()
    {
        var ht = HighlightParser.Parse($"{Open}auth{Close} token");
        Assert.Equal(2, ht.Spans.Count);
        Assert.Equal("auth", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[0].Kind);
        Assert.Equal(" token", ht.Spans[1].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[1].Kind);
    }

    [Fact]
    public void Normal_Match_Normal_Produces_Three_Spans()
    {
        var ht = HighlightParser.Parse($"before {Open}auth{Close} after");
        Assert.Equal(3, ht.Spans.Count);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
        Assert.Equal(TextSpanKind.Match, ht.Spans[1].Kind);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[2].Kind);
    }

    [Fact]
    public void Multiple_Matches_Produce_Correct_Span_Sequence()
    {
        var ht = HighlightParser.Parse($"a {Open}b{Close} c {Open}d{Close} e");
        Assert.Equal(5, ht.Spans.Count);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
        Assert.Equal(TextSpanKind.Match, ht.Spans[1].Kind);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[2].Kind);
        Assert.Equal(TextSpanKind.Match, ht.Spans[3].Kind);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[4].Kind);
    }

    [Fact]
    public void Empty_Input_Produces_Empty_Spans()
    {
        var ht = HighlightParser.Parse(string.Empty);
        Assert.Empty(ht.Spans);
    }

    [Fact]
    public void Ellipsis_In_Snippet_Is_Treated_As_Normal_Text()
    {
        var ht = HighlightParser.Parse($"...before {Open}auth{Close} after...");
        Assert.Equal(3, ht.Spans.Count);
        Assert.Equal("...before ", ht.Spans[0].Text);
        Assert.Equal("auth", ht.Spans[1].Text);
        Assert.Equal(" after...", ht.Spans[2].Text);
    }

    [Fact]
    public void Match_Span_Text_Does_Not_Include_Sentinels()
    {
        var ht = HighlightParser.Parse($"{Open}authentication{Close}");
        Assert.Equal("authentication", ht.Spans[0].Text);
        Assert.DoesNotContain("\x02", ht.Spans[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("\x03", ht.Spans[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_Value_Is_Preserved_Including_Spaces()
    {
        var ht = HighlightParser.Parse($"{Open}runtime builder{Close}");
        Assert.Single(ht.Spans);
        Assert.Equal("runtime builder", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[0].Kind);
    }
}
