using Ferret.Core.Documents;

using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class ParseResultTests
{
    [Fact]
    public void Success_IsSuccess_True_And_Value_Set()
    {
        var result = ParseResult<string>.Success("hello");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
        Assert.Equal(ParseResultKind.Success, result.Kind);
    }

    [Fact]
    public void Unsupported_IsSuccess_False_And_Contains_MediaType()
    {
        var result = ParseResult<string>.Unsupported("application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(ParseResultKind.Unsupported, result.Kind);
        Assert.Contains("application/pdf", result.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_IsSuccess_False()
    {
        var result = ParseResult<string>.Empty();
        Assert.False(result.IsSuccess);
        Assert.Equal(ParseResultKind.Empty, result.Kind);
    }

    [Fact]
    public void Failed_IsSuccess_False_And_Has_Error_Diagnostic()
    {
        var result = ParseResult<string>.Failed("bad JSON at line 3");
        Assert.False(result.IsSuccess);
        Assert.Equal(ParseResultKind.Failed, result.Kind);
        Assert.Single(result.Diagnostics);
        Assert.Equal(ParseDiagnosticSeverity.Error, result.Diagnostics[0].Severity);
        Assert.Equal("bad JSON at line 3", result.Diagnostics[0].Message);
    }

    [Fact]
    public void Success_Value_Is_Null_For_Non_Success_Results()
    {
        Assert.Null(ParseResult<string>.Unsupported("x/y").Value);
        Assert.Null(ParseResult<string>.Empty().Value);
        Assert.Null(ParseResult<string>.Failed("err").Value);
    }

    [Fact]
    public void Success_Has_Empty_Diagnostics()
    {
        var result = ParseResult<string>.Success("ok");
        Assert.Empty(result.Diagnostics);
    }
}
