using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchQueryAstTests
{
    [Fact]
    public void KeywordExpression_Equality_By_Value()
    {
        Assert.Equal(new KeywordExpression("auth"), new KeywordExpression("auth"));
    }

    [Fact]
    public void KeywordExpression_Inequality_Different_Value()
    {
        Assert.NotEqual(new KeywordExpression("auth"), new KeywordExpression("token"));
    }

    [Fact]
    public void PhraseExpression_Equality_By_Value()
    {
        Assert.Equal(new PhraseExpression("runtime builder"), new PhraseExpression("runtime builder"));
    }

    [Fact]
    public void PrefixExpression_Equality_By_Prefix()
    {
        Assert.Equal(new PrefixExpression("auth"), new PrefixExpression("auth"));
    }

    [Fact]
    public void AndExpression_Equality_By_Operands()
    {
        var a = new AndExpression([new KeywordExpression("auth"), new KeywordExpression("token")]);
        var b = new AndExpression([new KeywordExpression("auth"), new KeywordExpression("token")]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void SearchQuery_Preserves_OriginalText()
    {
        var q = new SearchQuery
        {
            OriginalText = "auth token",
            Root = new AndExpression([new KeywordExpression("auth"), new KeywordExpression("token")]),
        };
        Assert.Equal("auth token", q.OriginalText);
    }

    [Fact]
    public void SearchParseResult_Success_IsSuccess_True_And_Query_Set()
    {
        var q = new SearchQuery { OriginalText = "auth", Root = new KeywordExpression("auth") };
        var result = SearchParseResult.Success(q);
        Assert.True(result.IsSuccess);
        Assert.Same(q, result.Query);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SearchParseResult_Failure_IsSuccess_False_And_Has_Error_Diagnostic()
    {
        var result = SearchParseResult.Failure("unexpected token at position 3");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Query);
        Assert.Single(result.Diagnostics);
        Assert.Equal(SearchDiagnosticSeverity.Error, result.Diagnostics[0].Severity);
        Assert.Equal("unexpected token at position 3", result.Diagnostics[0].Message);
    }

    [Fact]
    public void SearchParseResult_Failure_With_Diagnostics_List()
    {
        var diagnostics = new[]
        {
            new SearchDiagnostic(SearchDiagnosticSeverity.Error, "msg1"),
            new SearchDiagnostic(SearchDiagnosticSeverity.Warning, "msg2"),
        };
        var result = SearchParseResult.Failure(diagnostics);
        Assert.Equal(2, result.Diagnostics.Count);
    }

    [Fact]
    public void SearchDiagnostic_Position_Defaults_To_Null()
    {
        var d = new SearchDiagnostic(SearchDiagnosticSeverity.Error, "msg");
        Assert.Null(d.Position);
    }

    [Fact]
    public void SearchDiagnostic_With_Position_Preserves_It()
    {
        var d = new SearchDiagnostic(SearchDiagnosticSeverity.Warning, "warn", 5);
        Assert.Equal(5, d.Position);
    }
}
