using Ferret.Core.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserPlatformModuleTests
{
    [Fact]
    public void ConfigureServices_Registers_IMimeTypeResolver()
    {
        var services = new ServiceCollection();

        ParserPlatformModule.ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        var resolver = provider.GetService<IMimeTypeResolver>();
        Assert.NotNull(resolver);
    }

    [Fact]
    public void ConfigureServices_Registers_IParserDispatcher()
    {
        var services = new ServiceCollection();

        ParserPlatformModule.ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetService<IParserDispatcher>();
        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void ConfigureServices_Registers_IParserRegistry()
    {
        var services = new ServiceCollection();

        ParserPlatformModule.ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<IParserRegistry>();
        Assert.NotNull(registry);
    }

    [Fact]
    public void ConfigureServices_Registers_Three_Parsers()
    {
        var services = new ServiceCollection();

        ParserPlatformModule.ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        var parsers = provider.GetServices<IContentParser>().ToList();
        Assert.Equal(3, parsers.Count);
    }

    [Fact]
    public void Registry_Contains_PlainText_Markdown_And_Json_Parsers()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IParserRegistry>();
        var all = registry.GetAll();

        var ids = all.Select(d => d.Id.Value).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("text/plain", ids);
        Assert.Contains("text/markdown", ids);
        Assert.Contains("application/json", ids);
    }

    [Fact]
    public void Dispatcher_Can_Handle_TextPlain()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IParserRegistry>();
        var parser = registry.GetParserFor("text/plain");

        Assert.NotNull(parser);
    }

    [Fact]
    public void Dispatcher_Can_Handle_TextMarkdown()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IParserRegistry>();
        var parser = registry.GetParserFor("text/markdown");

        Assert.NotNull(parser);
        Assert.Equal(200, parser.Descriptor.Priority);
    }
}
