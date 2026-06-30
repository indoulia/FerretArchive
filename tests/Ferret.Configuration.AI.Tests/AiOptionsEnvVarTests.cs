using Ferret.Configuration.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ferret.Configuration.Ai.Tests;

public sealed class AiOptionsEnvVarTests
{
    [Fact]
    public void FERRET_AI_PROVIDER_Overrides_DefaultChatModel()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:DefaultChatModel"] = "ollama/llama3.2",
                ["FERRET_AI_PROVIDER"] = "openai",
            })
            .Build();

        var services = new ServiceCollection();
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        // When FERRET_AI_PROVIDER=openai, both model prefixes switch to openai/
        Assert.Equal("openai/llama3.2", options.DefaultChatModel);
        Assert.Equal("openai/nomic-embed-text", options.DefaultEmbeddingModel);
    }

    [Fact]
    public void FERRET_OPENAI_API_KEY_Overrides_OpenAi_Provider_ApiKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "file-key",
                ["FERRET_OPENAI_API_KEY"] = "env-key-123",
            })
            .Build();

        var services = new ServiceCollection();
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("env-key-123", options.Providers.GetValueOrDefault("OpenAi")?.ApiKey);
    }

    [Fact]
    public void FERRET_OLLAMA_BASE_URL_Overrides_Ollama_Provider_BaseUrl()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FERRET_OLLAMA_BASE_URL"] = "http://remote-host:11434",
            })
            .Build();

        var services = new ServiceCollection();
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("http://remote-host:11434", options.Providers.GetValueOrDefault("Ollama")?.BaseUrl);
    }

    [Fact]
    public void No_Env_Vars_Leaves_Config_Values_Unchanged()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:DefaultChatModel"] = "ollama/llama3.2",
            })
            .Build();

        var services = new ServiceCollection();
        AiConfigurationModule.ConfigureServices(services, config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("ollama/llama3.2", options.DefaultChatModel);
    }
}
