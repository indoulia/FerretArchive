using Ferret.Core.Ai.Prompts;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Prompts;

/// <summary>Registers Ferret.Prompts services into a DI container.</summary>
public static class PromptsModule
{
    /// <summary>Adds <see cref="IPromptRegistry"/> and <see cref="IPromptRenderer"/> to <paramref name="services"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPromptRegistry>(sp =>
        {
            var templates = sp.GetService<IEnumerable<PromptTemplate>>() ?? [];
            return new PromptRegistry(templates);
        });

        services.AddSingleton<IPromptRenderer, PromptRenderer>();
    }
}
