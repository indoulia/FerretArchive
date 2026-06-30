using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Primitives;

public sealed class TypedIdTests
{
    // WorkspaceId
    [Fact]
    public void WorkspaceId_Create_ReturnsInstance() =>
        Assert.Equal("ws-1", WorkspaceId.Create("ws-1").Value);

    [Fact]
    public void WorkspaceId_Create_ThrowsOnEmpty() =>
        Assert.Throws<ArgumentException>(() => WorkspaceId.Create(string.Empty));

    [Fact]
    public void WorkspaceId_Equality_SameValue_IsEqual()
    {
        var a = WorkspaceId.Create("ws-1");
        var b = WorkspaceId.Create("ws-1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void WorkspaceId_Equality_DifferentValue_IsNotEqual()
    {
        var a = WorkspaceId.Create("ws-1");
        var b = WorkspaceId.Create("ws-2");
        Assert.NotEqual(a, b);
    }

    // DocumentId
    [Fact]
    public void DocumentId_Create_ReturnsInstance() =>
        Assert.Equal("doc-1", DocumentId.Create("doc-1").Value);

    [Fact]
    public void DocumentId_Create_ThrowsOnWhiteSpace() =>
        Assert.Throws<ArgumentException>(() => DocumentId.Create("   "));

    // SpecificationId
    [Fact]
    public void SpecificationId_Create_ReturnsInstance() =>
        Assert.Equal("spec-1", SpecificationId.Create("spec-1").Value);

    // ReviewId
    [Fact]
    public void ReviewId_Create_ReturnsInstance() =>
        Assert.Equal("rv-1", ReviewId.Create("rv-1").Value);

    // PluginId
    [Fact]
    public void PluginId_Create_ReturnsInstance() =>
        Assert.Equal("plugin-foo", PluginId.Create("plugin-foo").Value);

    // ArtifactId
    [Fact]
    public void ArtifactId_Create_ReturnsInstance() =>
        Assert.Equal("art-1", ArtifactId.Create("art-1").Value);

    // CorrelationId
    [Fact]
    public void CorrelationId_Create_ReturnsInstance() =>
        Assert.Equal("corr-abc", CorrelationId.Create("corr-abc").Value);

    // ExecutionId
    [Fact]
    public void ExecutionId_Create_ReturnsInstance() =>
        Assert.Equal("exec-1", ExecutionId.Create("exec-1").Value);

    // ToString
    [Fact]
    public void WorkspaceId_ToString_ReturnsValue() =>
        Assert.Equal("ws-42", WorkspaceId.Create("ws-42").ToString());

    // GetHashCode consistency
    [Fact]
    public void WorkspaceId_SameValue_SameHashCode()
    {
        var a = WorkspaceId.Create("ws-1");
        var b = WorkspaceId.Create("ws-1");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
