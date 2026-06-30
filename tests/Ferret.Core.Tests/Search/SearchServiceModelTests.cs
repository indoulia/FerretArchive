using Ferret.Core.Search;

using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchServiceModelTests
{
    [Fact]
    public void SearchExecutionInfo_SessionId_Is_Guid()
    {
        var info = MakeExecutionInfo();
        Assert.IsType<Guid>(info.SessionId);
        Assert.NotEqual(Guid.Empty, info.SessionId);
    }

    [Fact]
    public void SearchExecutionInfo_Duration_Preserved()
    {
        var info = MakeExecutionInfo();
        Assert.Equal(TimeSpan.FromMilliseconds(27), info.Duration);
    }

    [Fact]
    public void SearchCapabilities_SupportsKeyword_True()
    {
        var caps = new SearchCapabilities
        {
            SupportsKeyword = true,
            SupportsPhrase = true,
            SupportsPrefix = true,
        };
        Assert.True(caps.SupportsKeyword);
    }

    [Fact]
    public void SearchCapabilities_SupportsSemantic_Defaults_To_False()
    {
        var caps = new SearchCapabilities
        {
            SupportsKeyword = true,
            SupportsPhrase = true,
            SupportsPrefix = true,
        };
        Assert.False(caps.SupportsSemantic);
        Assert.False(caps.SupportsHybrid);
    }

    [Fact]
    public void SearchServiceStatus_Has_Five_Values()
    {
        Assert.Equal(5, Enum.GetValues<SearchServiceStatus>().Length);
    }

    [Fact]
    public void SearchServiceStatus_Success_Is_Zero()
    {
        Assert.Equal(0, (int)SearchServiceStatus.Success);
    }

    [Fact]
    public void SearchServiceResult_Diagnostics_Defaults_To_Empty()
    {
        var result = MakeServiceResult();
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SearchServiceResult_ProviderDescriptor_May_Be_Null()
    {
        var result = MakeServiceResult() with { ProviderDescriptor = null };
        Assert.Null(result.ProviderDescriptor);
    }

    [Fact]
    public void SearchServiceResult_Result_May_Be_Null_When_Status_Is_Not_Success()
    {
        var result = MakeServiceResult() with
        {
            Status = SearchServiceStatus.IndexNotFound,
            Result = null,
        };
        Assert.Equal(SearchServiceStatus.IndexNotFound, result.Status);
        Assert.Null(result.Result);
    }

    [Fact]
    public void SearchServiceResult_IsSuccess_True_When_Status_Success()
    {
        var result = MakeServiceResult();
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SearchServiceResult_IsSuccess_False_When_Status_Not_Success()
    {
        var result = MakeServiceResult() with { Status = SearchServiceStatus.IndexNotFound };
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void SearchServiceResult_Hits_Returns_Empty_When_No_Result()
    {
        var result = MakeServiceResult() with { Result = null };
        Assert.Empty(result.Hits);
    }

    [Fact]
    public void SearchServiceResult_Factory_Success_Produces_Success_Status()
    {
        var query = MakeQuery();
        var result = SearchServiceResult.Success(query, SearchResult.Empty, MakeExecutionInfo());
        Assert.True(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.Success, result.Status);
        Assert.Same(query, result.Query);
    }

    [Fact]
    public void SearchServiceResult_Factory_Failure_Produces_Failure_Status()
    {
        var query = MakeQuery();
        var diagnostics = new[] { new SearchDiagnostic(SearchDiagnosticSeverity.Error, "bad query") };
        var result = SearchServiceResult.Failure(query, SearchServiceStatus.InvalidQuery, diagnostics);
        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.InvalidQuery, result.Status);
        Assert.Single(result.Diagnostics);
        Assert.Null(result.ExecutionInfo);
    }

    [Fact]
    public void SearchProviderResult_Success_IsSuccess_True()
    {
        var providerResult = SearchProviderResult.Success([], 0, "1.0");
        Assert.True(providerResult.IsSuccess);
        Assert.Empty(providerResult.Hits);
    }

    [Fact]
    public void SearchProviderResult_Failure_IsSuccess_False()
    {
        var providerResult = SearchProviderResult.Failure(SearchServiceStatus.IndexNotFound);
        Assert.False(providerResult.IsSuccess);
        Assert.Equal(SearchServiceStatus.IndexNotFound, providerResult.Status);
        Assert.Empty(providerResult.Hits);
    }

    private static SearchExecutionInfo MakeExecutionInfo() => new()
    {
        SessionId = Guid.NewGuid(),
        ProviderName = "BM25",
        Duration = TimeSpan.FromMilliseconds(27),
        DocumentsScanned = 150,
        IndexVersion = "1.0",
    };

    private static SearchQuery MakeQuery() =>
        new() { OriginalText = "auth", Root = new KeywordExpression("auth") };

    private static SearchServiceResult MakeServiceResult() => new()
    {
        Query = MakeQuery(),
        Result = SearchResult.Empty,
        Status = SearchServiceStatus.Success,
        ExecutionInfo = MakeExecutionInfo(),
    };
}
