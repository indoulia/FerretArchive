using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ValidationResultTests
{
    [Fact]
    public void IsValid_True_When_No_Issues()
    {
        var result = ValidationResult.Ok();

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void IsValid_False_When_Any_Error_Issue()
    {
        var result = ValidationResult.WithError("something went wrong", "instance-1");

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
    }

    [Fact]
    public void IsValid_True_When_Only_Warning_Issues()
    {
        var result = new ValidationResult
        {
            Issues = [new ValidationIssue { Message = "advisory", Severity = ValidationSeverity.Warning }],
        };

        Assert.True(result.IsValid);
    }

    [Fact]
    public void WithError_Sets_InstanceId()
    {
        var result = ValidationResult.WithError("msg", "my-instance");

        Assert.Equal("my-instance", result.Issues[0].InstanceId);
    }

    [Fact]
    public void Combine_Merges_All_Issues()
    {
        var a = ValidationResult.WithError("err-a", "inst-a");
        var b = new ValidationResult
        {
            Issues = [new ValidationIssue { Message = "warn-b", Severity = ValidationSeverity.Warning }],
        };

        var combined = ValidationResult.Combine([a, b]);

        Assert.Equal(2, combined.Issues.Count);
        Assert.False(combined.IsValid);
    }

    [Fact]
    public void Combine_Empty_Returns_Valid()
    {
        var combined = ValidationResult.Combine([]);

        Assert.True(combined.IsValid);
        Assert.Empty(combined.Issues);
    }
}
