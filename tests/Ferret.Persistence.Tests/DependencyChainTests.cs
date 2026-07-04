using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class DependencyChainTests
{
    [Fact]
    public void Empty_Has_No_References()
    {
        Assert.Empty(DependencyChain.Empty.References);
    }

    [Fact]
    public void Chains_With_The_Same_References_In_Different_List_Instances_Are_Equal()
    {
        var a = new DependencyChain
        {
            References =
            [
                new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query" },
            ],
        };
        var b = new DependencyChain
        {
            References =
            [
                new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query" },
            ],
        };

        // Deliberately two separate List<DependencyReference> instances with equal content —
        // proves DependencyChain has real structural equality, not the reference equality a
        // plain List<T>/array property on a record would silently fall back to.
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Chains_With_References_In_Different_Order_Are_Not_Equal()
    {
        var first = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo a" };
        var second = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo b" };
        var a = new DependencyChain { References = [first, second] };
        var b = new DependencyChain { References = [second, first] };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Chains_With_Different_Reference_Counts_Are_Not_Equal()
    {
        var reference = new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "search:/repo query" };
        var a = new DependencyChain { References = [reference] };
        var b = new DependencyChain { References = [reference, reference] };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Empty_Chain_Equals_A_Freshly_Constructed_Empty_Chain()
    {
        var freshlyEmpty = new DependencyChain { References = [] };

        Assert.Equal(DependencyChain.Empty, freshlyEmpty);
    }
}
