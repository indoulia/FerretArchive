using Ferret.Search.Parsing;

using Xunit;

namespace Ferret.Search.Tests.Parsing;

public sealed class LexerTests
{
    [Fact]
    public void SingleKeyword_Produces_One_Word_Token()
    {
        var tokens = Tokenize("authentication");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Word, tokens[0].Kind);
        Assert.Equal("authentication", tokens[0].Value);
    }

    [Fact]
    public void TwoKeywords_Produce_Two_Word_Tokens_In_Order()
    {
        var tokens = Tokenize("authentication token");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Word, tokens[0].Kind);
        Assert.Equal("authentication", tokens[0].Value);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("token", tokens[1].Value);
    }

    [Fact]
    public void QuotedPhrase_Produces_Phrase_Token_Without_Quotes()
    {
        var tokens = Tokenize("\"runtime builder\"");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal("runtime builder", tokens[0].Value);
    }

    [Fact]
    public void TrailingAsterisk_Produces_Prefix_Token_Without_Asterisk()
    {
        var tokens = Tokenize("auth*");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Prefix, tokens[0].Kind);
        Assert.Equal("auth", tokens[0].Value);
    }

    [Fact]
    public void EmptyInput_Produces_No_Tokens()
    {
        Assert.Empty(Tokenize(string.Empty));
    }

    [Fact]
    public void WhitespaceOnly_Produces_No_Tokens()
    {
        Assert.Empty(Tokenize("   "));
    }

    [Fact]
    public void PhraseAndKeyword_Produce_Two_Tokens_In_Order()
    {
        var tokens = Tokenize("\"context window\" token");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal("context window", tokens[0].Value);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("token", tokens[1].Value);
    }

    [Fact]
    public void PrefixAndKeyword_Produce_Two_Tokens_In_Order()
    {
        var tokens = Tokenize("auth* token");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Prefix, tokens[0].Kind);
        Assert.Equal("auth", tokens[0].Value);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("token", tokens[1].Value);
    }

    [Fact]
    public void UnclosedQuote_Produces_Phrase_Token_From_Remaining_Input()
    {
        var tokens = Tokenize("\"unclosed phrase");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal("unclosed phrase", tokens[0].Value);
    }

    [Fact]
    public void AsteriskAlone_Produces_Prefix_Token_With_Empty_Value()
    {
        var tokens = Tokenize("*");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Prefix, tokens[0].Kind);
        Assert.Equal(string.Empty, tokens[0].Value);
    }

    [Fact]
    public void LeadingAndTrailingWhitespace_Is_Stripped()
    {
        var tokens = Tokenize("  auth  ");
        Assert.Single(tokens);
        Assert.Equal("auth", tokens[0].Value);
    }

    [Fact]
    public void Token_Position_Reflects_Start_Offset_In_Input()
    {
        var tokens = Tokenize("auth token");
        Assert.Equal(0, tokens[0].Position);
        Assert.Equal(5, tokens[1].Position);
    }

    [Fact]
    public void ThreeTerms_Produce_Three_Tokens()
    {
        var tokens = Tokenize("\"runtime\" auth* token");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal(TokenKind.Prefix, tokens[1].Kind);
        Assert.Equal(TokenKind.Word, tokens[2].Kind);
    }

    private static IReadOnlyList<Token> Tokenize(string input) =>
        new Lexer(input).Tokenize();
}
