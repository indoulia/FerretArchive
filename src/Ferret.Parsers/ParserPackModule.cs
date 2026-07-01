using Ferret.ParserPlatform;
using Ferret.Parsers.Pdf;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers;

/// <summary>
/// Single composition entry point for the parser pack: the platform (registry, dispatcher,
/// MimeTypeResolver, built-in text/CSV parsers) plus the PDF parser package. Hosts call this once
/// instead of wiring each parser module individually. Sprint 3 adds the Office package here.
/// </summary>
public static class ParserPackModule
{
    /// <summary>Registers the parser platform and all bundled format parsers.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ParserPlatformModule.ConfigureServices(services);
        PdfParserModule.ConfigureServices(services);
    }
}
