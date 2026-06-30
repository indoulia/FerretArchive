using Ferret.Core.Abstractions;

namespace Ferret.Runtime.Tests.Fakes;

/// <summary>Test double for IHealthCheck returning a preset result.</summary>
public sealed class FakeHealthCheck : IHealthCheck
{
    private readonly HealthCheckResult _result;

    public FakeHealthCheck(HealthCheckResult result) => _result = result;

    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_result);
}
