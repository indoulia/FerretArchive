namespace Ferret.Core.Tests;

public sealed class CoreModuleTests
{
    [Fact]
    public void Core_Assembly_Loads() =>
        Assert.NotNull(typeof(Ferret.Core.Enumerations.HealthStatus).Assembly);
}
