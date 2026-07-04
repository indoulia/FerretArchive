using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class DependencyReferenceTests
{
    [Fact]
    public void References_With_Identical_Values_Are_Equal()
    {
        var a = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query" };
        var b = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query" };

        Assert.Equal(a, b);
    }

    [Fact]
    public void References_With_Different_RequestPath_Are_Not_Equal()
    {
        var a = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query-a" };
        var b = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query-b" };

        Assert.NotEqual(a, b);
    }
}
