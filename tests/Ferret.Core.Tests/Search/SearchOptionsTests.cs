using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchOptionsTests
{
    [Fact]
    public void SearchOptions_Default_MaxResults_Is_10()
    {
        Assert.Equal(10, SearchOptions.Default.MaxResults);
    }

    [Fact]
    public void SearchOptions_Default_HighlightEnabled_Is_True()
    {
        Assert.True(SearchOptions.Default.HighlightEnabled);
    }

    [Fact]
    public void SearchOptions_Default_SnippetLength_Is_160()
    {
        Assert.Equal(160, SearchOptions.Default.SnippetLength);
    }

    [Fact]
    public void SearchOptions_Default_Mode_Is_Keyword()
    {
        Assert.Equal(SearchExecutionMode.Keyword, SearchOptions.Default.Mode);
    }

    [Fact]
    public void SearchOptions_Default_IncludePassages_Is_False()
    {
        Assert.False(SearchOptions.Default.IncludePassages);
    }

    [Fact]
    public void SearchOptions_Can_Be_Customised()
    {
        var opts = new SearchOptions { MaxResults = 5, IncludePassages = true, HighlightEnabled = false };
        Assert.Equal(5, opts.MaxResults);
        Assert.True(opts.IncludePassages);
        Assert.False(opts.HighlightEnabled);
    }

    [Fact]
    public void ExecutionMode_Has_Four_Values()
    {
        Assert.Equal(4, Enum.GetValues<SearchExecutionMode>().Length);
    }

    [Fact]
    public void ExecutionMode_Auto_Is_Zero()
    {
        Assert.Equal(0, (int)SearchExecutionMode.Auto);
    }

    [Fact]
    public void ExecutionMode_Keyword_Is_One()
    {
        Assert.Equal(1, (int)SearchExecutionMode.Keyword);
    }
}
