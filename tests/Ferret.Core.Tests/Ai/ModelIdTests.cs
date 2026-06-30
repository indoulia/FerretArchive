using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ModelIdTests
{
    [Fact]
    public void Create_ReturnsModelIdWithValue()
    {
        var id = ModelId.Create("ollama/llama3.2");
        Assert.Equal("ollama/llama3.2", id.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("ollama/llama3.2", ModelId.Create("ollama/llama3.2").ToString());
    }

    [Fact]
    public void ProviderPrefix_SplitsOnSlash()
    {
        Assert.Equal("ollama", ModelId.Create("ollama/llama3.2").ProviderPrefix);
    }

    [Fact]
    public void LocalName_SplitsOnSlash()
    {
        Assert.Equal("llama3.2", ModelId.Create("ollama/llama3.2").LocalName);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(ModelId.Create("openai/gpt-4o"), ModelId.Create("openai/gpt-4o"));
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        Assert.NotEqual(ModelId.Create("openai/gpt-4o"), ModelId.Create("openai/gpt-4o-mini"));
    }

    [Fact]
    public void Create_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ModelId.Create(string.Empty));
    }

    [Fact]
    public void Create_Whitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ModelId.Create(" "));
    }
}
