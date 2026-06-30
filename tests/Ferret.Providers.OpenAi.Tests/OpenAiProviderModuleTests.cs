using Ferret.Core.Ai.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.Providers.OpenAi.Tests;

public sealed class OpenAiProviderModuleTests
{
    private static IServiceCollection BuildServices() =>
        new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

    [Fact]
    public void AddOpenAiProvider_WhenEnabled_RegistersIModelProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:Enabled"] = "true",
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "sk-test-key",
            })
            .Build();
        var services = BuildServices();
        services.AddOpenAiProvider(config);

        var provider = services.BuildServiceProvider();
        var providers = provider.GetServices<IModelProvider>().ToList();

        Assert.Contains(providers, p => p is OpenAiModelProvider);
    }

    [Fact]
    public void AddOpenAiProvider_WhenDisabled_DoesNotRegisterIModelProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:Enabled"] = "false",
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "sk-test-key",
            })
            .Build();
        var services = BuildServices();
        services.AddOpenAiProvider(config);

        var provider = services.BuildServiceProvider();
        var providers = provider.GetServices<IModelProvider>().ToList();

        Assert.DoesNotContain(providers, p => p is OpenAiModelProvider);
    }

    [Fact]
    public void AddOpenAiProvider_WhenEnabled_CanResolveOpenAiModelProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:OpenAi:Enabled"] = "true",
                ["Ferret:Ai:Providers:OpenAi:ApiKey"] = "sk-test-key",
            })
            .Build();
        var services = BuildServices();
        services.AddOpenAiProvider(config);

        var provider = services.BuildServiceProvider();
        var openAiProvider = provider.GetServices<IModelProvider>()
            .OfType<OpenAiModelProvider>()
            .SingleOrDefault();

        Assert.NotNull(openAiProvider);
        Assert.Equal("openai", openAiProvider.Descriptor.Id.Value);
    }
}
