using Ferret.Core.Ai.Models;

using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ProviderIdTests
{
    [Fact]
    public void Create_ReturnsProviderIdWithValue()
    {
        var id = ProviderId.Create("ollama");
        Assert.Equal("ollama", id.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("openai", ProviderId.Create("openai").ToString());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(ProviderId.Create("ollama"), ProviderId.Create("ollama"));
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        Assert.NotEqual(ProviderId.Create("ollama"), ProviderId.Create("openai"));
    }

    [Fact]
    public void Create_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProviderId.Create(string.Empty));
    }
}
