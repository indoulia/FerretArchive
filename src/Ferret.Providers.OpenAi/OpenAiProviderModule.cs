using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ferret.Providers.OpenAi;

/// <summary>DI registration module for the OpenAI provider.</summary>
public static class OpenAiProviderModule
{
    /// <summary>Registers <see cref="OpenAiModelProvider"/> as <see cref="IModelProvider"/> when OpenAI is enabled in configuration.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOpenAiProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetSection("Ferret:Ai:Providers:OpenAi")
            .Get<OpenAiOptions>() ?? new OpenAiOptions();

        if (!options.Enabled)
        {
            return services;
        }

        services.AddSingleton<IModelProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OpenAiModelProvider>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new OpenAiModelProvider(options, logger, loggerFactory);
        });

        return services;
    }
}
