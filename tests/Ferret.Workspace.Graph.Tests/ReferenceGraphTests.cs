namespace Ferret.Workspace.Graph.Tests;

public sealed class ReferenceGraphTests
{
    [Fact]
    public void WouldCreateCycle_SelfReference_ReturnsTrue()
    {
        var a = Guid.NewGuid();

        var result = ReferenceGraph.WouldCreateCycle([], a, a);

        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCycle_NoExistingReferences_ReturnsFalse()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var entries = new[]
        {
            new WorkspaceRegistryEntry { WorkspaceId = a, Name = "a" },
            new WorkspaceRegistryEntry { WorkspaceId = b, Name = "b" },
        };

        var result = ReferenceGraph.WouldCreateCycle(entries, a, b);

        Assert.False(result);
    }

    [Fact]
    public void WouldCreateCycle_DirectBackReference_ReturnsTrue()
    {
        // B already imports A; adding A -> B would close a 2-node cycle.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var entries = new[]
        {
            new WorkspaceRegistryEntry { WorkspaceId = a, Name = "a" },
            new WorkspaceRegistryEntry
            {
                WorkspaceId = b,
                Name = "b",
                References = [new WorkspaceReference { WorkspaceId = a }],
            },
        };

        var result = ReferenceGraph.WouldCreateCycle(entries, a, b);

        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCycle_TransitiveBackReference_ReturnsTrue()
    {
        // C already imports B, B already imports A; adding A -> C would close a 3-node cycle.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var entries = new[]
        {
            new WorkspaceRegistryEntry { WorkspaceId = a, Name = "a" },
            new WorkspaceRegistryEntry
            {
                WorkspaceId = b,
                Name = "b",
                References = [new WorkspaceReference { WorkspaceId = a }],
            },
            new WorkspaceRegistryEntry
            {
                WorkspaceId = c,
                Name = "c",
                References = [new WorkspaceReference { WorkspaceId = b }],
            },
        };

        var result = ReferenceGraph.WouldCreateCycle(entries, a, c);

        Assert.True(result);
    }

    [Fact]
    public void WouldCreateCycle_UnrelatedExistingReferences_ReturnsFalse()
    {
        // D imports E, unrelated to the A -> B edge being proposed.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var d = Guid.NewGuid();
        var e = Guid.NewGuid();
        var entries = new[]
        {
            new WorkspaceRegistryEntry { WorkspaceId = a, Name = "a" },
            new WorkspaceRegistryEntry { WorkspaceId = b, Name = "b" },
            new WorkspaceRegistryEntry
            {
                WorkspaceId = d,
                Name = "d",
                References = [new WorkspaceReference { WorkspaceId = e }],
            },
        };

        var result = ReferenceGraph.WouldCreateCycle(entries, a, b);

        Assert.False(result);
    }

    [Fact]
    public void WouldCreateCycle_DiamondSharedDependency_ReturnsFalse()
    {
        // A -> B, A -> C both importing D is not a cycle (a DAG allows a shared, non-cyclic dependency).
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var entries = new[]
        {
            new WorkspaceRegistryEntry
            {
                WorkspaceId = a,
                Name = "a",
                References = [new WorkspaceReference { WorkspaceId = b }],
            },
            new WorkspaceRegistryEntry
            {
                WorkspaceId = b,
                Name = "b",
                References = [new WorkspaceReference { WorkspaceId = d }],
            },
            new WorkspaceRegistryEntry
            {
                WorkspaceId = c,
                Name = "c",
                References = [new WorkspaceReference { WorkspaceId = d }],
            },
            new WorkspaceRegistryEntry { WorkspaceId = d, Name = "d" },
        };

        var result = ReferenceGraph.WouldCreateCycle(entries, a, c);

        Assert.False(result);
    }
}
