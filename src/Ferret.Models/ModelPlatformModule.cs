using Ferret.Core.Ai.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ferret.Models;

/// <summary>Registers the AI model platform — <see cref="IModelRegistry"/> and <see cref="IModelRouter"/> — into the DI container.</summary>
public static class ModelPlatformModule
{
    /// <summary>Registers <see cref="IModelRegistry"/> and <see cref="IModelRouter"/> as singletons.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IModelRegistry>(sp =>
        {
            var providers = sp.GetRequiredService<IEnumerable<IModelProvider>>();
            var logger = sp.GetRequiredService<ILogger<ModelRegistry>>();
            return ModelRegistry.CreateAsync(providers, logger).GetAwaiter().GetResult();
        });

        services.AddSingleton<IModelRouter, ModelRouter>();

        return services;
    }
}
