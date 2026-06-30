using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

namespace Ferret.Models;

/// <summary>Read-only view of all registered AI providers and their models.</summary>
public interface IModelRegistry
{
    /// <summary>Returns all registered provider descriptors.</summary>
    /// <returns>A list of all known <see cref="ProviderDescriptor"/> values.</returns>
    IReadOnlyList<ProviderDescriptor> GetProviders();

    /// <summary>Returns the provider instance for <paramref name="id"/>, or <see langword="null"/> if not registered.</summary>
    /// <param name="id">The provider identifier to look up.</param>
    /// <returns>The matching <see cref="IModelProvider"/>, or <see langword="null"/>.</returns>
    IModelProvider? GetProvider(ProviderId id);

    /// <summary>Returns all cached model descriptors across all providers.</summary>
    /// <returns>A list of all known <see cref="ModelDescriptor"/> values.</returns>
    IReadOnlyList<ModelDescriptor> GetModels();

    /// <summary>Returns the descriptor for <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The model identifier to look up.</param>
    /// <returns>The matching <see cref="ModelDescriptor"/>, or <see langword="null"/>.</returns>
    ModelDescriptor? GetModel(ModelId id);
}
