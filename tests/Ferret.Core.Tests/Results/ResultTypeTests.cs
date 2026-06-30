using Ferret.Core.Enumerations;
using Ferret.Core.Results;

namespace Ferret.Core.Tests.Results;

public sealed class ResultTypeTests
{
    [Fact]
    public void OperationResult_Success_IsSuccessful()
    {
        var r = OperationResult.Success();
        Assert.True(r.IsSuccess);
        Assert.Null(r.ErrorMessage);
    }

    [Fact]
    public void OperationResult_Failure_IsNotSuccessful()
    {
        var r = OperationResult.Failure("Something went wrong.");
        Assert.False(r.IsSuccess);
        Assert.Equal("Something went wrong.", r.ErrorMessage);
    }

    [Fact]
    public void OperationResult_Generic_Success_HasValue()
    {
        var r = OperationResult.Success(42);
        Assert.True(r.IsSuccess);
        Assert.Equal(42, r.Value);
    }

    [Fact]
    public void OperationResult_Generic_Failure_HasNoValue()
    {
        var r = OperationResult.Failure<int>("error");
        Assert.False(r.IsSuccess);
        Assert.Equal(default, r.Value);
    }

    [Fact]
    public void ValidationFailure_Properties_AreStored()
    {
        var f = new ValidationFailure("field", "required", "Provide a value.", ValidationSeverity.Error);
        Assert.Equal("field", f.Field);
        Assert.Equal("required", f.Constraint);
        Assert.Equal("Provide a value.", f.Guidance);
        Assert.Equal(ValidationSeverity.Error, f.Severity);
    }

    [Fact]
    public void ValidationResult_Valid_HasNoFailures()
    {
        var r = ValidationResult.Valid();
        Assert.True(r.IsValid);
        Assert.Empty(r.Failures);
    }

    [Fact]
    public void ValidationResult_Invalid_HasFailures()
    {
        var f = new ValidationFailure("name", "required", "Provide a name.", ValidationSeverity.Error);
        var r = ValidationResult.Invalid([f]);
        Assert.False(r.IsValid);
        Assert.Single(r.Failures);
    }

    [Fact]
    public void DiscoveryResult_Stores_Items()
    {
        var r = new DiscoveryResult<string>(["a", "b", "c"], true);
        Assert.Equal(3, r.Items.Count);
        Assert.True(r.IsComplete);
    }

    [Fact]
    public void ParseResult_Success_HasValue()
    {
        var r = ParseResult.Success(99);
        Assert.True(r.IsSuccess);
        Assert.Equal(99, r.Value);
    }

    [Fact]
    public void ParseResult_Failure_HasMessage()
    {
        var r = ParseResult.Failure<int>("parse error");
        Assert.False(r.IsSuccess);
        Assert.Equal("parse error", r.ErrorMessage);
    }

    [Fact]
    public void ReviewResult_Stores_Status()
    {
        var r = new ReviewResult(ReviewStatus.Complete, [], "All good.");
        Assert.Equal(ReviewStatus.Complete, r.Status);
        Assert.Equal("All good.", r.Summary);
    }

    [Fact]
    public void IndexResult_Stores_Count()
    {
        var r = new IndexResult(42, 2, false);
        Assert.Equal(42, r.IndexedCount);
        Assert.Equal(2, r.FailedCount);
        Assert.False(r.IsComplete);
    }
}
