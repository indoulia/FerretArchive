using Ferret.Core.Search;
using Ferret.Search.Providers.Bm25;

using Xunit;

namespace Ferret.Search.Tests.Providers.Bm25;

public sealed class QueryTranslatorTests
{
    [Fact]
    public void Keyword_Translates_To_Bare_Word()
    {
        var result = QueryTranslator.Translate(new KeywordExpression("authentication"));
        Assert.Equal("authentication", result);
    }

    [Fact]
    public void Phrase_Translates_To_Double_Quoted_String()
    {
        var result = QueryTranslator.Translate(new PhraseExpression("runtime builder"));
        Assert.Equal("\"runtime builder\"", result);
    }

    [Fact]
    public void Prefix_Translates_To_Word_With_Asterisk()
    {
        var result = QueryTranslator.Translate(new PrefixExpression("auth"));
        Assert.Equal("auth*", result);
    }

    [Fact]
    public void EmptyPrefix_Translates_To_Bare_Asterisk()
    {
        var result = QueryTranslator.Translate(new PrefixExpression(string.Empty));
        Assert.Equal("*", result);
    }

    [Fact]
    public void AndExpression_Joins_Terms_With_Space()
    {
        var expr = new AndExpression([
            new KeywordExpression("authentication"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("authentication token", result);
    }

    [Fact]
    public void AndExpression_With_Phrase_And_Keyword()
    {
        var expr = new AndExpression([
            new PhraseExpression("context window"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("\"context window\" token", result);
    }

    [Fact]
    public void AndExpression_With_Prefix_And_Keyword()
    {
        var expr = new AndExpression([
            new PrefixExpression("auth"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("auth* token", result);
    }

    [Fact]
    public void Phrase_With_Inner_Quotes_Doubles_Them()
    {
        var result = QueryTranslator.Translate(new PhraseExpression("say \"hello\""));
        Assert.Equal("\"say \"\"hello\"\"\"", result);
    }

    [Fact]
    public void Keyword_With_Hyphen_Is_Quoted()
    {
        // A bare hyphen in FTS5 MATCH syntax is the NOT operator, not a literal character —
        // an unquoted "nem-3795" parses as "nem AND NOT 3795", not a search for the literal term.
        var result = QueryTranslator.Translate(new KeywordExpression("nem-3795"));
        Assert.Equal("\"nem-3795\"", result);
    }

    [Fact]
    public void Keyword_That_Matches_Fts5_Reserved_Word_Is_Quoted()
    {
        // "AND" is a reserved FTS5 operator — must be quoted to search for the literal word
        var result = QueryTranslator.Translate(new KeywordExpression("AND"));
        Assert.Equal("\"AND\"", result);
    }

    [Fact]
    public void Keyword_NOT_Is_Quoted()
    {
        var result = QueryTranslator.Translate(new KeywordExpression("NOT"));
        Assert.Equal("\"NOT\"", result);
    }

    [Fact]
    public void Keyword_OR_Is_Quoted()
    {
        var result = QueryTranslator.Translate(new KeywordExpression("OR"));
        Assert.Equal("\"OR\"", result);
    }

    [Fact]
    public void ThreeTerm_And_Produces_Space_Separated_String()
    {
        var expr = new AndExpression([
            new PhraseExpression("runtime builder"),
            new PrefixExpression("auth"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("\"runtime builder\" auth* token", result);
    }
}
