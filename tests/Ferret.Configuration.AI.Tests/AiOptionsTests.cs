using Ferret.Configuration.Ai;

using Xunit;

namespace Ferret.Configuration.Ai.Tests;

public sealed class AiOptionsTests
{
    [Fact]
    public void AiOptions_Defaults_AreCorrect()
    {
        var options = new AiOptions();

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
        Assert.Equal("ollama/nomic-embed-text", options.DefaultEmbeddingModel);
        Assert.Null(options.DefaultReranker);
    }

    [Fact]
    public void OllamaOptions_Defaults_AreCorrect()
    {
        var options = new OllamaOptions();

        Assert.True(options.Enabled);
        Assert.Equal("http://localhost:11434", options.BaseUrl);
        Assert.Equal(120, options.TimeoutSeconds);
        Assert.Null(options.ApiKey);
    }

    [Fact]
    public void OpenAiOptions_Defaults_AreCorrect()
    {
        var options = new OpenAiOptions();

        Assert.True(options.Enabled);
        Assert.Equal("https://api.openai.com/v1", options.BaseUrl);
        Assert.Equal(60, options.TimeoutSeconds);
    }

    [Fact]
    public void ProviderOptions_Defaults_AreCorrect()
    {
        var options = new ProviderOptions();

        Assert.True(options.Enabled);
        Assert.Equal(string.Empty, options.BaseUrl);
        Assert.Null(options.ApiKey);
        Assert.Equal(60, options.TimeoutSeconds);
    }
}
