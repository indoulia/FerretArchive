using Ferret.Core.Documents;

namespace Ferret.Core.Tests.Documents;

public sealed class ExtractionLimiterTests
{
    [Fact]
    public void Unlimited_By_Default_Returns_Text_Unchanged()
    {
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hello world", new ParserOptions());
        Assert.Equal("hello world", text);
        Assert.False(truncated);
    }

    [Fact]
    public void Truncates_When_Over_Limit()
    {
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hello world", new ParserOptions { MaxExtractedCharacters = 5 });
        Assert.Equal("hello", text);
        Assert.True(truncated);
    }

    [Fact]
    public void No_Truncation_When_Under_Limit()
    {
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hi", new ParserOptions { MaxExtractedCharacters = 5 });
        Assert.Equal("hi", text);
        Assert.False(truncated);
    }

    [Fact]
    public void Limit_Larger_Than_Int_MaxValue_Does_Not_Truncate_Or_Overflow()
    {
        var options = new ParserOptions { MaxExtractedCharacters = (long)int.MaxValue + 1000 };
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hello world", options);
        Assert.Equal("hello world", text);
        Assert.False(truncated);
    }
}
