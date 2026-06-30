using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

using Microsoft.Extensions.Logging;

namespace Ferret.Models;

/// <summary>Immutable registry of AI providers and their models, built once at startup via <see cref="CreateAsync"/>.</summary>
public sealed class ModelRegistry : IModelRegistry
{
    private static readonly Action<ILogger, string, Exception?> LogProviderExcluded =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, "ProviderExcluded"),
            "Provider '{ProviderId}' is unreachable — excluded from registry.");

    private readonly IReadOnlyList<ProviderDescriptor> _providers;
    private readonly IReadOnlyDictionary<string, IModelProvider> _providerById;
    private readonly IReadOnlyList<ModelDescriptor> _models;
    private readonly IReadOnlyDictionary<string, ModelDescriptor> _modelById;

    private ModelRegistry(
        IReadOnlyList<ProviderDescriptor> providers,
        IReadOnlyDictionary<string, IModelProvider> providerById,
        IReadOnlyList<ModelDescriptor> models,
        IReadOnlyDictionary<string, ModelDescriptor> modelById)
    {
        _providers = providers;
        _providerById = providerById;
        _models = models;
        _modelById = modelById;
    }

    /// <summary>
    /// Builds an immutable <see cref="ModelRegistry"/> by calling <see cref="IModelProvider.ListModelsAsync"/>
    /// on each provider. Providers that throw are excluded with a warning; they do not abort construction.
    /// </summary>
    /// <param name="providers">The set of providers to include in the registry.</param>
    /// <param name="logger">Logger for provider fault warnings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A fully constructed, immutable <see cref="ModelRegistry"/>.</returns>
    public static async Task<ModelRegistry> CreateAsync(
        IEnumerable<IModelProvider> providers,
        ILogger<ModelRegistry> logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(logger);

        var providerList = new List<ProviderDescriptor>();
        var providerById = new Dictionary<string, IModelProvider>(StringComparer.OrdinalIgnoreCase);
        var modelList = new List<ModelDescriptor>();
        var modelById = new Dictionary<string, ModelDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers)
        {
            IReadOnlyList<ModelDescriptor> discovered;
            try
            {
                discovered = await provider.ListModelsAsync(ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Intentional broad catch — provider implementations are arbitrary; any failure must isolate the provider, not abort registry construction
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogProviderExcluded(logger, provider.Descriptor.Id.Value, ex);
                continue;
            }

            providerList.Add(provider.Descriptor);
            providerById[provider.Descriptor.Id.Value] = provider;

            foreach (var model in discovered)
            {
                modelList.Add(model);
                modelById[model.Id.Value] = model;
            }
        }

        return new ModelRegistry(
            providerList.AsReadOnly(),
            providerById.AsReadOnly(),
            modelList.AsReadOnly(),
            modelById.AsReadOnly());
    }

    /// <inheritdoc/>
    public IReadOnlyList<ProviderDescriptor> GetProviders() => _providers;

    /// <inheritdoc/>
    public IModelProvider? GetProvider(ProviderId id)
    {
        _providerById.TryGetValue(id.Value, out var provider);
        return provider;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ModelDescriptor> GetModels() => _models;

    /// <inheritdoc/>
    public ModelDescriptor? GetModel(ModelId id)
    {
        _modelById.TryGetValue(id.Value, out var descriptor);
        return descriptor;
    }
}
