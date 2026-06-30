using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Runtime.Extensions;

/// <summary>
/// IServiceCollection extension methods for registering the Ferret runtime in an existing DI container.
/// <para>Why: Allows application-layer hosts to add the Ferret runtime alongside other services without constructing RuntimeBuilder manually.</para>
/// <para>Lifecycle: Called once at application startup; the registered IRuntimeHost is a singleton.</para>
/// <para>Layer: Ferret.Runtime — consumed by the application layer; never referenced by Core.</para>
/// <para>Thread Safety: Single Thread Only — call during the service registration phase before the container is built.</para>
/// </summary>
public static class RuntimeServiceExtensions
{
    /// <summary>Registers <see cref="IRuntimeHost"/> as a singleton built from the optional <paramref name="configure"/> delegate.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional delegate to configure the <see cref="RuntimeBuilder"/> before it is built.</param>
    /// <returns>The same <see cref="IServiceCollection"/> to allow call chaining.</returns>
    public static IServiceCollection AddFerretRuntime(
        this IServiceCollection services,
        Action<RuntimeBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IRuntimeHost>(_ =>
        {
            var builder = new RuntimeBuilder();
            configure?.Invoke(builder);
            return builder.Build();
        });

        return services;
    }
}
