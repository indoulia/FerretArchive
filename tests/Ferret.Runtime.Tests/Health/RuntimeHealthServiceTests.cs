using Ferret.Core.Abstractions;
using Ferret.Core.Enumerations;
using Ferret.Runtime.Health;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Health;

public sealed class RuntimeHealthServiceTests
{
    [Fact]
    public async Task CheckAsync_NoChecks_ReturnsHealthy()
    {
        var sut = new RuntimeHealthService([]);

        RuntimeHealthReport report = await sut.CheckAsync();

        Assert.Equal(HealthStatus.Healthy, report.OverallStatus);
        Assert.Empty(report.Results);
    }

    [Fact]
    public async Task CheckAsync_AllHealthy_ReturnsHealthy()
    {
        var checks = new List<IHealthCheck>
        {
            new FakeHealthCheck(HealthCheckResult.Healthy("ok-1")),
            new FakeHealthCheck(HealthCheckResult.Healthy("ok-2")),
        };
        var sut = new RuntimeHealthService(checks);

        RuntimeHealthReport report = await sut.CheckAsync();

        Assert.Equal(HealthStatus.Healthy, report.OverallStatus);
        Assert.Equal(2, report.Results.Count);
    }

    [Fact]
    public async Task CheckAsync_OneDegraded_ReturnsDegraded()
    {
        var checks = new List<IHealthCheck>
        {
            new FakeHealthCheck(HealthCheckResult.Healthy("ok")),
            new FakeHealthCheck(HealthCheckResult.Degraded("slow")),
        };
        var sut = new RuntimeHealthService(checks);

        RuntimeHealthReport report = await sut.CheckAsync();

        Assert.Equal(HealthStatus.Degraded, report.OverallStatus);
    }

    [Fact]
    public async Task CheckAsync_OneUnhealthy_ReturnsUnhealthy()
    {
        var checks = new List<IHealthCheck>
        {
            new FakeHealthCheck(HealthCheckResult.Degraded("slow")),
            new FakeHealthCheck(HealthCheckResult.Unhealthy("down")),
        };
        var sut = new RuntimeHealthService(checks);

        RuntimeHealthReport report = await sut.CheckAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.OverallStatus);
    }

    [Fact]
    public async Task CheckAsync_CheckThrows_ResultIsUnhealthy()
    {
        var checks = new List<IHealthCheck> { new ThrowingCheck() };
        var sut = new RuntimeHealthService(checks);

        RuntimeHealthReport report = await sut.CheckAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.OverallStatus);
        Assert.NotNull(report.Results[0].Result.Exception);
    }

    private sealed class ThrowingCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated failure");
    }
}
