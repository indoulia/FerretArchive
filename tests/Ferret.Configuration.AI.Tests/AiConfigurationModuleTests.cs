using Ferret.Configuration.Ai;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xunit;

namespace Ferret.Configuration.Ai.Tests;

public sealed class AiConfigurationModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersAiOptions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:DefaultChatModel"] = "ollama/llama3.2",
                ["Ferret:Ai:DefaultEmbeddingModel"] = "ollama/nomic-embed-text",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
        Assert.Equal("ollama/nomic-embed-text", options.DefaultEmbeddingModel);
    }

    [Fact]
    public void ConfigureServices_EmptyConfig_UsesDefaults()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
    }
}
