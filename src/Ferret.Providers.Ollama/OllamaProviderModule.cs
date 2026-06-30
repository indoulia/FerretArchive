using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ferret.Providers.Ollama;

/// <summary>DI registration module for the Ollama provider.</summary>
public static class OllamaProviderModule
{
    /// <summary>Registers <see cref="OllamaModelProvider"/> as <see cref="IModelProvider"/> when Ollama is enabled in configuration.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetSection("Ferret:Ai:Providers:Ollama")
            .Get<OllamaOptions>() ?? new OllamaOptions();

        if (!options.Enabled)
        {
            return services;
        }

        services.AddSingleton<IModelProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OllamaModelProvider>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new OllamaModelProvider(options, logger, httpClient: null, loggerFactory);
        });

        return services;
    }
}
