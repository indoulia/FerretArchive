using Ferret.Core.Documents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Parsers.Pdf;

/// <summary>DI registration for the PDF parser package.</summary>
public static class PdfParserModule
{
    /// <summary>Registers <see cref="PdfParser"/> as an <see cref="IContentParser"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(new ParserOptions()); // unlimited default unless a host configured one
        services.AddSingleton<IContentParser, PdfParser>();
    }
}
