using Ferret.Core.Ai.Prompts;

using Xunit;

namespace Ferret.Prompts.Tests;

public sealed class PromptVariablesTests
{
    [Fact]
    public void Empty_HasNoKeys()
    {
        Assert.Empty(PromptVariables.Empty.Keys);
    }

    [Fact]
    public void Set_AddsBinding_ReturnsNewInstance()
    {
        var original = PromptVariables.Empty;
        var updated = original.Set("name", "Alice");

        Assert.Empty(original.Keys);
        Assert.Single(updated.Keys);
        Assert.Equal("Alice", updated.TryGet("name"));
    }

    [Fact]
    public void Set_ChainedCalls_AllBindingsPresent()
    {
        var vars = PromptVariables.Empty
            .Set("a", "1")
            .Set("b", "2")
            .Set("c", "3");

        Assert.Equal(3, vars.Keys.Count);
        Assert.Equal("1", vars.TryGet("a"));
        Assert.Equal("2", vars.TryGet("b"));
        Assert.Equal("3", vars.TryGet("c"));
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsNull()
    {
        Assert.Null(PromptVariables.Empty.TryGet("missing"));
    }

    [Fact]
    public void GetRequired_PresentKey_ReturnsValue()
    {
        var vars = PromptVariables.Empty.Set("key", "value");
        Assert.Equal("value", vars.GetRequired("key"));
    }

    [Fact]
    public void GetRequired_MissingKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PromptVariables.Empty.GetRequired("missing"));
    }

    [Fact]
    public void Contains_PresentKey_ReturnsTrue()
    {
        var vars = PromptVariables.Empty.Set("x", "1");
        Assert.True(vars.Contains("x"));
    }

    [Fact]
    public void Contains_AbsentKey_ReturnsFalse()
    {
        Assert.False(PromptVariables.Empty.Contains("x"));
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        var vars = PromptVariables.Empty.Set("key", "first").Set("key", "second");
        Assert.Equal("second", vars.TryGet("key"));
        Assert.Single(vars.Keys);
    }
}
