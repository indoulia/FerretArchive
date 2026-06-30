using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Bootstrap;

/// <summary>Tests for <see cref="RuntimeBuilder"/>.</summary>
public sealed class RuntimeBuilderTests
{
    [Fact]
    public void Build_NoModules_ReturnsRuntimeHost()
    {
        IRuntimeHost host = new RuntimeBuilder().Build();

        Assert.NotNull(host);
        Assert.Equal(RuntimeState.Stopped, host.State);
    }

    [Fact]
    public void AddModule_NullDescriptor_ThrowsArgumentNull()
    {
        var builder = new RuntimeBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddModule(null!));
    }

    [Fact]
    public async Task AddModule_ModuleAppearsInRegistry()
    {
        var module = new FakeModule("reg-test");
        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(module)
            .Build();

        await using (host as IAsyncDisposable)
        {
            await host.StartAsync();

            Assert.NotNull(host.Modules.GetById("reg-test"));
        }
    }

    [Fact]
    public void AddModule_DuplicateId_ThrowsAtAddModule()
    {
        var builder = new RuntimeBuilder()
            .AddModule(new FakeModule("dup"));

        // Duplicate throws at AddModule time, not deferred to Build
        Assert.Throws<InvalidOperationException>(() => builder.AddModule(new FakeModule("dup")));
    }
}
