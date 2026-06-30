using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Configuration.Ai;

/// <summary>Registers AI configuration options into the DI container.</summary>
public static class AiConfigurationModule
{
    /// <summary>Binds <see cref="AiOptions"/> from <c>Ferret:Ai</c> and registers it as <c>IOptions&lt;AiOptions&gt;</c>.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection("Ferret:Ai"))
            .PostConfigure(options =>
            {
                // FERRET_AI_PROVIDER — rewrite DefaultChatModel / DefaultEmbeddingModel prefix.
                var aiProvider = configuration["FERRET_AI_PROVIDER"];
                if (!string.IsNullOrWhiteSpace(aiProvider))
                {
                    var slashIndex = options.DefaultChatModel.IndexOf('/', StringComparison.Ordinal);
                    var modelName = slashIndex >= 0 ? options.DefaultChatModel[(slashIndex + 1)..] : options.DefaultChatModel;
                    options.DefaultChatModel = $"{aiProvider}/{modelName}";

                    var embSlash = options.DefaultEmbeddingModel.IndexOf('/', StringComparison.Ordinal);
                    var embName = embSlash >= 0 ? options.DefaultEmbeddingModel[(embSlash + 1)..] : options.DefaultEmbeddingModel;
                    options.DefaultEmbeddingModel = $"{aiProvider}/{embName}";
                }

                // FERRET_OPENAI_API_KEY — override OpenAi provider ApiKey.
                var openAiKey = configuration["FERRET_OPENAI_API_KEY"];
                if (!string.IsNullOrWhiteSpace(openAiKey))
                {
                    if (!options.Providers.TryGetValue("OpenAi", out var openAiOpts))
                    {
                        openAiOpts = new OpenAiOptions();
                        options.Providers["OpenAi"] = openAiOpts;
                    }

                    openAiOpts.ApiKey = openAiKey;
                }

                // FERRET_OLLAMA_BASE_URL — override Ollama provider BaseUrl.
                var ollamaUrl = configuration["FERRET_OLLAMA_BASE_URL"];
                if (!string.IsNullOrWhiteSpace(ollamaUrl))
                {
                    if (!options.Providers.TryGetValue("Ollama", out var ollamaOpts))
                    {
                        ollamaOpts = new OllamaOptions();
                        options.Providers["Ollama"] = ollamaOpts;
                    }

                    ollamaOpts.BaseUrl = ollamaUrl;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
