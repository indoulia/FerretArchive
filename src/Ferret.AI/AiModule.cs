using Ferret.AI.Context;
using Ferret.Core.Context;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.AI;

/// <summary>Registers Ferret.AI services into the DI container.</summary>
public static class AiModule
{
    /// <summary>Configures Ferret.AI context assembly services.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<DocumentExpander>();
        services.AddSingleton<IContextAssembler, ContextAssembler>();

        return services;
    }
}
