#pragma warning disable CA1859 // Interface return types are intentional — tests verify the contract, not the concrete type
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Ferret.Models.Tests;

public sealed class ModelRegistryTests
{
    [Fact]
    public async Task CreateAsync_SingleProvider_ReturnsAllProviders()
    {
        var provider = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        var providers = registry.GetProviders();
        Assert.Single(providers);
        Assert.Equal("ollama", providers[0].Id.Value);
    }

    [Fact]
    public async Task CreateAsync_SingleProvider_ReturnsAggregatedModels()
    {
        var provider = new FakeModelProvider("ollama", [
            MakeDescriptor("ollama/llama3.2", "ollama"),
            MakeDescriptor("ollama/nomic-embed-text", "ollama"),
        ]);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        Assert.Equal(2, registry.GetModels().Count);
    }

    [Fact]
    public async Task CreateAsync_MultipleProviders_AggregatesAllModels()
    {
        var ollamaProvider = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var openAiProvider = new FakeModelProvider("openai", [MakeDescriptor("openai/gpt-4o", "openai")]);
        var registry = await ModelRegistry.CreateAsync([ollamaProvider, openAiProvider], NullLogger<ModelRegistry>.Instance);

        Assert.Equal(2, registry.GetModels().Count);
        Assert.Equal(2, registry.GetProviders().Count);
    }

    [Fact]
    public async Task CreateAsync_UnreachableProvider_ExcludesItsModelsAndContinues()
    {
        var unreachable = new ThrowingModelProvider("broken");
        var working = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var registry = await ModelRegistry.CreateAsync([unreachable, working], NullLogger<ModelRegistry>.Instance);

        var models = registry.GetModels();
        Assert.Single(models);
        Assert.Equal("ollama/llama3.2", models[0].Id.Value);
    }

    [Fact]
    public async Task GetModel_ExistingModelId_ReturnsDescriptor()
    {
        var provider = new FakeModelProvider("ollama", [MakeDescriptor("ollama/llama3.2", "ollama")]);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        var descriptor = registry.GetModel(ModelId.Create("ollama/llama3.2"));

        Assert.NotNull(descriptor);
        Assert.Equal("ollama/llama3.2", descriptor.Id.Value);
    }

    [Fact]
    public async Task GetModel_UnknownModelId_ReturnsNull()
    {
        var registry = await ModelRegistry.CreateAsync([], NullLogger<ModelRegistry>.Instance);

        Assert.Null(registry.GetModel(ModelId.Create("unknown/model")));
    }

    [Fact]
    public async Task GetProvider_ExistingProviderId_ReturnsProvider()
    {
        var provider = new FakeModelProvider("ollama", []);
        var registry = await ModelRegistry.CreateAsync([provider], NullLogger<ModelRegistry>.Instance);

        Assert.NotNull(registry.GetProvider(ProviderId.Create("ollama")));
    }

    [Fact]
    public async Task GetProvider_UnknownProviderId_ReturnsNull()
    {
        var registry = await ModelRegistry.CreateAsync([], NullLogger<ModelRegistry>.Instance);

        Assert.Null(registry.GetProvider(ProviderId.Create("unknown")));
    }

    private static ModelDescriptor MakeDescriptor(string modelId, string providerId) => new()
    {
        Id = ModelId.Create(modelId),
        ProviderId = ProviderId.Create(providerId),
        DisplayName = modelId,
        Capabilities = ModelCapabilities.Chat,
    };

    private sealed class FakeModelProvider(string providerId, IReadOnlyList<ModelDescriptor> models) : IModelProvider
    {
        public ProviderDescriptor Descriptor { get; } = new()
        {
            Id = ProviderId.Create(providerId),
            DisplayName = providerId,
            Capabilities = ModelCapabilities.Chat,
            Version = "1.0",
        };

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct) =>
            Task.FromResult(models);

        public IChatModel? GetChatModel(ModelId modelId) => null;

        public IEmbeddingModel? GetEmbeddingModel(ModelId modelId) => null;

        public IReranker? GetReranker(ModelId modelId) => null;
    }

    private sealed class ThrowingModelProvider(string providerId) : IModelProvider
    {
        public ProviderDescriptor Descriptor { get; } = new()
        {
            Id = ProviderId.Create(providerId),
            DisplayName = providerId,
            Capabilities = ModelCapabilities.Chat,
            Version = "1.0",
        };

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct) =>
            throw new HttpRequestException("Connection refused");

        public IChatModel? GetChatModel(ModelId modelId) => null;

        public IEmbeddingModel? GetEmbeddingModel(ModelId modelId) => null;

        public IReranker? GetReranker(ModelId modelId) => null;
    }
}
