using Ferret.Core.Ai.Models;

using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class EmbeddingResultTests
{
    [Fact]
    public void EmbeddingResult_PreservesVector()
    {
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var result = new EmbeddingResult
        {
            Vector = vector,
            ModelId = ModelId.Create("ollama/nomic-embed-text"),
            TokenCount = 5,
        };
        Assert.Equal(3, result.Vector.Length);
    }

    [Fact]
    public void EmbeddingRequest_ModelId_DefaultsToNull()
    {
        var req = new EmbeddingRequest { Text = "hello" };
        Assert.Null(req.ModelId);
    }
}
