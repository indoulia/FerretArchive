using Ferret.Core.Workspace;
using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspacePathTests
{
    [Fact]
    public void Create_WithValidPath_ReturnsInstance()
    {
        var path = WorkspacePath.Create(@"C:\repos\myproject");
        Assert.Equal(@"C:\repos\myproject", path.FullPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankPath_ThrowsArgumentException(string? path)
    {
        Assert.Throws<ArgumentException>(() => WorkspacePath.Create(path!));
    }

    [Fact]
    public void Combine_WithRelativePath_ReturnsCombinedPath()
    {
        var root = WorkspacePath.Create(@"C:\repos\myproject");
        var combined = root.Combine(".ai");
        Assert.Equal(@"C:\repos\myproject\.ai", combined.FullPath);
    }

    [Fact]
    public void IsUnder_WhenChildIsUnderParent_ReturnsTrue()
    {
        var parent = WorkspacePath.Create(@"C:\repos\myproject");
        var child = WorkspacePath.Create(@"C:\repos\myproject\src\file.cs");
        Assert.True(child.IsUnder(parent));
    }

    [Fact]
    public void IsUnder_WhenPathIsSameAsParent_ReturnsFalse()
    {
        var path = WorkspacePath.Create(@"C:\repos\myproject");
        Assert.False(path.IsUnder(path));
    }

    [Fact]
    public void IsUnder_WhenPathIsNotUnderParent_ReturnsFalse()
    {
        var parent = WorkspacePath.Create(@"C:\repos\myproject");
        var other = WorkspacePath.Create(@"C:\repos\otherproject");
        Assert.False(other.IsUnder(parent));
    }

    [Fact]
    public void Equality_SameFullPath_AreEqual()
    {
        var a = WorkspacePath.Create(@"C:\repos\project");
        var b = WorkspacePath.Create(@"C:\repos\project");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentFullPath_AreNotEqual()
    {
        var a = WorkspacePath.Create(@"C:\repos\projectA");
        var b = WorkspacePath.Create(@"C:\repos\projectB");
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void ToString_ReturnsFullPath()
    {
        var path = WorkspacePath.Create(@"C:\repos\project");
        Assert.Equal(@"C:\repos\project", path.ToString());
    }

    [Fact]
    public void GetHashCode_EqualPaths_HaveSameHashCode()
    {
        var a = WorkspacePath.Create(@"C:\repos\project");
        var b = WorkspacePath.Create(@"C:\repos\project");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
