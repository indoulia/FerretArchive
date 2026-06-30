using Ferret.Core.Ai.Models;

using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ModelCapabilitiesTests
{
    [Fact]
    public void None_IsZero()
    {
        Assert.Equal(0, (int)ModelCapabilities.None);
    }

    [Fact]
    public void Flags_CanBeCombined()
    {
        var caps = ModelCapabilities.Chat | ModelCapabilities.Vision;
        Assert.True(caps.HasFlag(ModelCapabilities.Chat));
        Assert.True(caps.HasFlag(ModelCapabilities.Vision));
        Assert.False(caps.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public void Chat_IsOne()
    {
        Assert.Equal(1, (int)ModelCapabilities.Chat);
    }

    [Fact]
    public void Embedding_IsTwo()
    {
        Assert.Equal(2, (int)ModelCapabilities.Embedding);
    }

    [Fact]
    public void Reranking_IsFour()
    {
        Assert.Equal(4, (int)ModelCapabilities.Reranking);
    }

    [Fact]
    public void Vision_IsEight()
    {
        Assert.Equal(8, (int)ModelCapabilities.Vision);
    }
}
