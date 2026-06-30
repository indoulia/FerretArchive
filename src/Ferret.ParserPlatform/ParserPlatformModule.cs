using Ferret.Core.Documents;
using Ferret.ParserPlatform.Parsers;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.ParserPlatform;

/// <summary>
/// DI registration module for the Parser Platform. Registers all parser services so any host
/// with this module has a fully wired <see cref="IParserDispatcher"/>.
/// No CLI commands are registered in Sprint 9.
/// </summary>
public sealed class ParserPlatformModule
{
    /// <summary>Registers all Parser Platform services into the service collection.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMimeTypeResolver, MimeTypeResolver>();
        services.AddSingleton<IContentParser, PlainTextParser>();
        services.AddSingleton<IContentParser, MarkdownParser>();
        services.AddSingleton<IContentParser, JsonParser>();
        services.AddSingleton<IParserRegistry>(sp =>
            ParserRegistryBuilder.Build(sp.GetServices<IContentParser>()));
        services.AddSingleton<IParserDispatcher, ParserDispatcher>();
    }
}
