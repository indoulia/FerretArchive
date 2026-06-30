using Ferret.Core.Events;
using Ferret.Core.Runtime.Events;

using Xunit;

namespace Ferret.Core.Tests.Runtime;

public sealed class RuntimeEventTests
{
    [Fact]
    public void RuntimeStarted_InheritsDomainEvent()
    {
        Assert.True(typeof(DomainEvent).IsAssignableFrom(typeof(RuntimeStarted)));
    }

    [Fact]
    public void RuntimeStarted_CarriesRuntimeVersion()
    {
        var evt = new RuntimeStarted("1.0.0");
        Assert.Equal("1.0.0", evt.RuntimeVersion);
    }

    [Fact]
    public void RuntimeStopped_CarriesRuntimeVersionAndModuleCount()
    {
        var evt = new RuntimeStopped("1.0.0", modulesActive: 3);
        Assert.Equal("1.0.0", evt.RuntimeVersion);
        Assert.Equal(3, evt.ModulesActive);
    }

    [Fact]
    public void ModuleLoaded_CarriesModuleInfo()
    {
        var evt = new ModuleLoaded("workspace", "Workspace Module", "1.0.0");
        Assert.Equal("workspace", evt.ModuleId);
        Assert.Equal("Workspace Module", evt.ModuleName);
        Assert.Equal("1.0.0", evt.Version);
    }

    [Fact]
    public void ModuleActivated_CarriesModuleInfo()
    {
        var evt = new ModuleActivated("workspace", "Workspace Module");
        Assert.Equal("workspace", evt.ModuleId);
        Assert.Equal("Workspace Module", evt.ModuleName);
    }

    [Fact]
    public void ModuleStopped_CarriesModuleInfo()
    {
        var evt = new ModuleStopped("workspace", "Workspace Module");
        Assert.Equal("workspace", evt.ModuleId);
        Assert.Equal("Workspace Module", evt.ModuleName);
    }
}
