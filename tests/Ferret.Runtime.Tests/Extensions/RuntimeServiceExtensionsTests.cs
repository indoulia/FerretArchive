using Ferret.Core.Runtime;
using Ferret.Runtime.Extensions;
using Ferret.Runtime.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Runtime.Tests.Extensions;

/// <summary>Tests for <see cref="RuntimeServiceExtensions"/>.</summary>
public sealed class RuntimeServiceExtensionsTests
{
    [Fact]
    public async Task AddFerretRuntime_RegistersIRuntimeHost()
    {
        ServiceCollection services = new();
        services.AddFerretRuntime();

        await using ServiceProvider provider = services.BuildServiceProvider();

        IRuntimeHost host = provider.GetRequiredService<IRuntimeHost>();

        Assert.NotNull(host);
    }

    [Fact]
    public async Task AddFerretRuntime_WithConfigure_ModuleRegistered()
    {
        ServiceCollection services = new();
        services.AddFerretRuntime(b => b.AddModule(new FakeModule("m")));

        await using ServiceProvider provider = services.BuildServiceProvider();

        IRuntimeHost host = provider.GetRequiredService<IRuntimeHost>();

        await using (host as IAsyncDisposable)
        {
            await host.StartAsync();

            Assert.NotNull(host.Modules.GetById("m"));
        }
    }

    [Fact]
    public void AddFerretRuntime_CalledTwice_RegistersOnlyOnce()
    {
        ServiceCollection services = new();
        services.AddFerretRuntime();
        services.AddFerretRuntime();

        int count = services.Count(sd => sd.ServiceType == typeof(IRuntimeHost));

        Assert.Equal(1, count);
    }
}
