using Ferret.Core.Search;

using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchHighlightTests
{
    [Fact]
    public void TextSpan_Equality_By_Value()
    {
        Assert.Equal(new TextSpan("hello", TextSpanKind.Normal), new TextSpan("hello", TextSpanKind.Normal));
    }

    [Fact]
    public void TextSpan_Inequality_Different_Kind()
    {
        Assert.NotEqual(new TextSpan("hello", TextSpanKind.Normal), new TextSpan("hello", TextSpanKind.Match));
    }

    [Fact]
    public void HighlightedText_Plain_Creates_Single_Normal_Span()
    {
        var ht = HighlightedText.Plain("hello world");
        Assert.Single(ht.Spans);
        Assert.Equal("hello world", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
    }

    [Fact]
    public void HighlightedText_Empty_Has_No_Spans()
    {
        Assert.Empty(HighlightedText.Empty.Spans);
    }

    [Fact]
    public void HighlightedText_Spans_Is_ReadOnly()
    {
        var ht = HighlightedText.Plain("x");
        Assert.IsAssignableFrom<IReadOnlyList<TextSpan>>(ht.Spans);
    }

    [Fact]
    public void TextSpanKind_Has_Six_Values()
    {
        Assert.Equal(6, Enum.GetValues<TextSpanKind>().Length);
    }

    [Fact]
    public void SearchHitKind_Has_Three_Values()
    {
        Assert.Equal(3, Enum.GetValues<SearchHitKind>().Length);
    }

    [Fact]
    public void SearchHitKind_File_Is_Zero()
    {
        Assert.Equal(0, (int)SearchHitKind.File);
    }

    [Fact]
    public void SearchHitKind_Segment_Is_Two()
    {
        Assert.Equal(2, (int)SearchHitKind.Segment);
    }
}
