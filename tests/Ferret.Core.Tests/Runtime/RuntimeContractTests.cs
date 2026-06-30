using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Xunit;

namespace Ferret.Core.Tests.Runtime;

public sealed class RuntimeContractTests
{
    [Fact]
    public void IRuntimeHost_ExistsAsInterface()
    {
        Assert.True(typeof(IRuntimeHost).IsInterface);
    }

    [Fact]
    public void IRuntimeBuilder_ExistsAsInterface()
    {
        Assert.True(typeof(IRuntimeBuilder).IsInterface);
    }

    [Fact]
    public void IModule_ExtendsILifecycleParticipant()
    {
        Assert.True(typeof(ILifecycleParticipant).IsAssignableFrom(typeof(IModule)));
    }

    [Fact]
    public void IModuleDescriptor_ExistsAsInterface()
    {
        Assert.True(typeof(IModuleDescriptor).IsInterface);
    }

    [Fact]
    public void IModuleRegistry_ExistsAsInterface()
    {
        Assert.True(typeof(IModuleRegistry).IsInterface);
    }

    [Fact]
    public void IModuleContext_ExistsAsInterface()
    {
        Assert.True(typeof(IModuleContext).IsInterface);
    }

    [Fact]
    public void IExecutionContext_ExistsAsInterface()
    {
        Assert.True(typeof(IExecutionContext).IsInterface);
    }

    [Fact]
    public void IRuntimeService_ExistsAsInterface()
    {
        Assert.True(typeof(IRuntimeService).IsInterface);
    }
}
