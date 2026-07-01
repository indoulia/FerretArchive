using Ferret.Core.Documents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Parsers.Office;

/// <summary>DI registration for the Office parser package: Word (.docx) and Excel (.xlsx).</summary>
public static class OfficeParserModule
{
    /// <summary>Registers <see cref="WordParser"/> and <see cref="ExcelParser"/> as <see cref="IContentParser"/>s.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Default (unlimited) options unless a host has already registered a configured instance.
        services.TryAddSingleton(new ParserOptions());

        services.AddSingleton<IContentParser, WordParser>();
        services.AddSingleton<IContentParser, ExcelParser>();
    }
}
