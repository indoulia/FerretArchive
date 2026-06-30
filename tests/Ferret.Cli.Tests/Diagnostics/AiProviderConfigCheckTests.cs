using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Diagnostics.Checks;
using Microsoft.Extensions.Configuration;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class AiProviderConfigCheckTests
{
    [Fact]
    public async Task Pass_WhenAiProvidersConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ferret:Ai:Providers:Ollama:BaseUrl"] = "http://localhost:11434",
            })
            .Build();
        using var sw = new StringWriter();
        var ctx = FerretContext.CreateTest(sw);
        var check = new AiProviderConfigCheck(config);
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Fail_WhenNoAiProvidersConfigured()
    {
        var config = new ConfigurationBuilder().Build();
        using var sw = new StringWriter();
        var ctx = FerretContext.CreateTest(sw);
        var check = new AiProviderConfigCheck(config);
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void Name_IsStable()
    {
        var check = new AiProviderConfigCheck(new ConfigurationBuilder().Build());
        Assert.Equal("AI provider configured", check.Name);
    }
}
