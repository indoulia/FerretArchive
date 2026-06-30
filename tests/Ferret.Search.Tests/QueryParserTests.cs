using Ferret.Core.Search;

using Xunit;

namespace Ferret.Search.Tests;

public sealed class QueryParserTests
{
    private readonly QueryParser _parser = new QueryParser();

    // ── Failure cases ────────────────────────────────────────────────────────

    [Fact]
    public void EmptyString_Returns_Failure()
    {
        var result = _parser.Parse(string.Empty);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(SearchDiagnosticSeverity.Error, result.Diagnostics[0].Severity);
    }

    [Fact]
    public void WhitespaceOnly_Returns_Failure()
    {
        var result = _parser.Parse("   ");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Failure_Query_Is_Null()
    {
        var result = _parser.Parse(string.Empty);
        Assert.Null(result.Query);
    }

    // ── Single-term success cases ─────────────────────────────────────────────

    [Fact]
    public void SingleKeyword_Returns_KeywordExpression()
    {
        var result = _parser.Parse("authentication");
        Assert.True(result.IsSuccess);
        var keyword = Assert.IsType<KeywordExpression>(result.Query!.Root);
        Assert.Equal("authentication", keyword.Value);
    }

    [Fact]
    public void QuotedPhrase_Returns_PhraseExpression()
    {
        var result = _parser.Parse("\"runtime builder\"");
        Assert.True(result.IsSuccess);
        var phrase = Assert.IsType<PhraseExpression>(result.Query!.Root);
        Assert.Equal("runtime builder", phrase.Value);
    }

    [Fact]
    public void PrefixQuery_Returns_PrefixExpression()
    {
        var result = _parser.Parse("auth*");
        Assert.True(result.IsSuccess);
        var prefix = Assert.IsType<PrefixExpression>(result.Query!.Root);
        Assert.Equal("auth", prefix.Prefix);
    }

    // ── Multi-term AND cases ──────────────────────────────────────────────────

    [Fact]
    public void TwoKeywords_Returns_AndExpression_With_Two_Keyword_Operands()
    {
        var result = _parser.Parse("authentication token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(2, and.Operands.Count);
        Assert.IsType<KeywordExpression>(and.Operands[0]);
        Assert.IsType<KeywordExpression>(and.Operands[1]);
        Assert.Equal("authentication", ((KeywordExpression)and.Operands[0]).Value);
        Assert.Equal("token", ((KeywordExpression)and.Operands[1]).Value);
    }

    [Fact]
    public void PhraseAndKeyword_Returns_AndExpression()
    {
        var result = _parser.Parse("\"context window\" token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(2, and.Operands.Count);
        Assert.IsType<PhraseExpression>(and.Operands[0]);
        Assert.IsType<KeywordExpression>(and.Operands[1]);
    }

    [Fact]
    public void PrefixAndKeyword_Returns_AndExpression()
    {
        var result = _parser.Parse("auth* token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(2, and.Operands.Count);
        Assert.IsType<PrefixExpression>(and.Operands[0]);
        Assert.IsType<KeywordExpression>(and.Operands[1]);
        Assert.Equal("auth", ((PrefixExpression)and.Operands[0]).Prefix);
        Assert.Equal("token", ((KeywordExpression)and.Operands[1]).Value);
    }

    [Fact]
    public void ThreeTerms_Returns_AndExpression_With_Three_Operands()
    {
        var result = _parser.Parse("authentication token session");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(3, and.Operands.Count);
    }

    [Fact]
    public void PhraseAndPrefixAndKeyword_Returns_AndExpression_With_Correct_Types()
    {
        var result = _parser.Parse("\"runtime builder\" auth* token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(3, and.Operands.Count);
        Assert.IsType<PhraseExpression>(and.Operands[0]);
        Assert.IsType<PrefixExpression>(and.Operands[1]);
        Assert.IsType<KeywordExpression>(and.Operands[2]);
    }

    // ── OriginalText preservation ─────────────────────────────────────────────

    [Fact]
    public void OriginalText_Is_Preserved_Verbatim_In_Query()
    {
        const string raw = "auth* \"context window\"";
        var result = _parser.Parse(raw);
        Assert.True(result.IsSuccess);
        Assert.Equal(raw, result.Query!.OriginalText);
    }

    [Fact]
    public void OriginalText_Preserves_Casing()
    {
        const string raw = "RuntimeBuilder";
        var result = _parser.Parse(raw);
        Assert.True(result.IsSuccess);
        Assert.Equal("RuntimeBuilder", result.Query!.OriginalText);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Success_Result_Has_Empty_Diagnostics()
    {
        var result = _parser.Parse("authentication");
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void UnclosedQuote_Is_Parsed_Leniently_As_PhraseExpression()
    {
        var result = _parser.Parse("\"unclosed phrase");
        Assert.True(result.IsSuccess);
        Assert.IsType<PhraseExpression>(result.Query!.Root);
        Assert.Equal("unclosed phrase", ((PhraseExpression)result.Query.Root).Value);
    }

    [Fact]
    public void Keyword_Value_Preserves_Case()
    {
        var result = _parser.Parse("RuntimeBuilder");
        Assert.True(result.IsSuccess);
        var keyword = Assert.IsType<KeywordExpression>(result.Query!.Root);
        Assert.Equal("RuntimeBuilder", keyword.Value);
    }
}
