using Ferret.Core.Enumerations;

namespace Ferret.Core.Tests.Enumerations;

public sealed class EnumerationTests
{
    [Fact]
    public void HealthStatus_Has_Expected_Values()
    {
        Assert.Equal(0, (int)HealthStatus.Unknown);
        Assert.Equal(1, (int)HealthStatus.Healthy);
        Assert.Equal(2, (int)HealthStatus.Degraded);
        Assert.Equal(3, (int)HealthStatus.Unhealthy);
    }

    [Fact]
    public void Severity_Has_Expected_Values()
    {
        Assert.Equal(0, (int)Severity.None);
        Assert.Equal(1, (int)Severity.Low);
        Assert.Equal(2, (int)Severity.Medium);
        Assert.Equal(3, (int)Severity.High);
        Assert.Equal(4, (int)Severity.Critical);
    }

    [Fact]
    public void ValidationSeverity_Has_Expected_Values()
    {
        Assert.Equal(0, (int)ValidationSeverity.Info);
        Assert.Equal(1, (int)ValidationSeverity.Warning);
        Assert.Equal(2, (int)ValidationSeverity.Error);
    }

    [Fact]
    public void PluginState_Has_Expected_Values()
    {
        Assert.Equal(0, (int)PluginState.Unloaded);
        Assert.Equal(1, (int)PluginState.Loading);
        Assert.Equal(2, (int)PluginState.Active);
        Assert.Equal(3, (int)PluginState.Faulted);
        Assert.Equal(4, (int)PluginState.Unloading);
    }

    [Fact]
    public void SpecificationStatus_Has_Expected_Values()
    {
        Assert.Equal(0, (int)SpecificationStatus.Draft);
        Assert.Equal(1, (int)SpecificationStatus.UnderReview);
        Assert.Equal(2, (int)SpecificationStatus.Approved);
        Assert.Equal(3, (int)SpecificationStatus.Rejected);
        Assert.Equal(4, (int)SpecificationStatus.Superseded);
    }

    [Fact]
    public void ReviewStatus_Has_Expected_Values()
    {
        Assert.Equal(0, (int)ReviewStatus.Pending);
        Assert.Equal(1, (int)ReviewStatus.InProgress);
        Assert.Equal(2, (int)ReviewStatus.Complete);
        Assert.Equal(3, (int)ReviewStatus.Abandoned);
    }
}
