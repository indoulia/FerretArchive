using Ferret.Core.Abstractions;
using Ferret.Core.Enumerations;

namespace Ferret.Core.Tests.Abstractions;

public sealed class HealthCheckResultTests
{
    [Fact]
    public void HealthCheckResult_Healthy_IsHealthy()
    {
        var r = HealthCheckResult.Healthy("All OK");
        Assert.Equal(HealthStatus.Healthy, r.Status);
        Assert.Equal("All OK", r.Description);
        Assert.Null(r.Exception);
    }

    [Fact]
    public void HealthCheckResult_Degraded_IsDegraded()
    {
        var r = HealthCheckResult.Degraded("Slow response");
        Assert.Equal(HealthStatus.Degraded, r.Status);
    }

    [Fact]
    public void HealthCheckResult_Unhealthy_WithException()
    {
        var ex = new InvalidOperationException("broken");
        var r = HealthCheckResult.Unhealthy("Connection failed", ex);
        Assert.Equal(HealthStatus.Unhealthy, r.Status);
        Assert.Equal(ex, r.Exception);
    }
}
