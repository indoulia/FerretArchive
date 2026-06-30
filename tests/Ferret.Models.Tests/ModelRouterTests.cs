#pragma warning disable CA1859 // Interface return types are intentional — tests verify the contract, not the concrete type
using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models;
using Ferret.Models.Exceptions;

using Microsoft.Extensions.Options;

using Xunit;

namespace Ferret.Models.Tests;

public sealed class ModelRouterTests
{
    [Fact]
    public void GetDefaultChatModel_WhenConfigured_ReturnsModel()
    {
        var chatModel = new FakeChatModel();
        var registry = new FakeModelRegistry(chatModel: chatModel);
        var options = Options.Create(new AiOptions { DefaultChatModel = "ollama/llama3.2" });
        var router = new ModelRouter(registry, options);

        var result = router.GetDefaultChatModel();

        Assert.Same(chatModel, result);
    }

    [Fact]
    public void GetDefaultChatModel_WhenModelNotFound_ThrowsModelNotFoundException()
    {
        var registry = new FakeModelRegistry(chatModel: null);
        var options = Options.Create(new AiOptions { DefaultChatModel = "ollama/missing" });
        var router = new ModelRouter(registry, options);

        var ex = Assert.Throws<ModelNotFoundException>(() => router.GetDefaultChatModel());
        Assert.Equal("ollama/missing", ex.ModelId.Value);
        Assert.Contains("ollama/missing", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ferret models list", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetChatModel_ExistingModelId_ReturnsModel()
    {
        var chatModel = new FakeChatModel();
        var registry = new FakeModelRegistry(chatModel: chatModel);
        var options = Options.Create(new AiOptions());
        var router = new ModelRouter(registry, options);

        var result = router.GetChatModel(ModelId.Create("ollama/llama3.2"));

        Assert.Same(chatModel, result);
    }

    [Fact]
    public void GetChatModel_UnknownModelId_ReturnsNull()
    {
        var registry = new FakeModelRegistry(chatModel: null);
        var options = Options.Create(new AiOptions());
        var router = new ModelRouter(registry, options);

        Assert.Null(router.GetChatModel(ModelId.Create("unknown/model")));
    }

    [Fact]
    public void GetDefaultEmbeddingModel_WhenConfigured_ReturnsModel()
    {
        var embeddingModel = new FakeEmbeddingModel();
        var registry = new FakeModelRegistry(embeddingModel: embeddingModel);
        var options = Options.Create(new AiOptions { DefaultEmbeddingModel = "ollama/nomic-embed-text" });
        var router = new ModelRouter(registry, options);

        var result = router.GetDefaultEmbeddingModel();

        Assert.Same(embeddingModel, result);
    }

    [Fact]
    public void GetDefaultEmbeddingModel_WhenModelNotFound_ThrowsModelNotFoundException()
    {
        var registry = new FakeModelRegistry(embeddingModel: null);
        var options = Options.Create(new AiOptions { DefaultEmbeddingModel = "ollama/missing-embed" });
        var router = new ModelRouter(registry, options);

        var ex = Assert.Throws<ModelNotFoundException>(() => router.GetDefaultEmbeddingModel());
        Assert.Equal("ollama/missing-embed", ex.ModelId.Value);
    }

    [Fact]
    public void GetEmbeddingModel_UnknownModelId_ReturnsNull()
    {
        var registry = new FakeModelRegistry(embeddingModel: null);
        var options = Options.Create(new AiOptions());
        var router = new ModelRouter(registry, options);

        Assert.Null(router.GetEmbeddingModel(ModelId.Create("unknown/embed")));
    }

    // ---- Fakes ----

    private sealed class FakeModelRegistry(IChatModel? chatModel = null, IEmbeddingModel? embeddingModel = null)
        : IModelRegistry
    {
        private readonly FakeProvider _fakeProvider = new(chatModel, embeddingModel);

        public IReadOnlyList<ProviderDescriptor> GetProviders() => [];

        public IModelProvider? GetProvider(ProviderId id) =>
            id.Value is "ollama" or "openai" or "unknown" ? _fakeProvider : null;

        public IReadOnlyList<ModelDescriptor> GetModels() => [];

        public ModelDescriptor? GetModel(ModelId id) => null;
    }

    private sealed class FakeProvider(IChatModel? chatModel, IEmbeddingModel? embeddingModel) : IModelProvider
    {
        public ProviderDescriptor Descriptor => new()
        {
            Id = ProviderId.Create("fake"),
            DisplayName = "Fake",
            Capabilities = ModelCapabilities.Chat,
            Version = "1.0",
        };

        public IChatModel? GetChatModel(ModelId modelId) => chatModel;

        public IEmbeddingModel? GetEmbeddingModel(ModelId modelId) => embeddingModel;

        public IReranker? GetReranker(ModelId modelId) => null;

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ModelDescriptor>>([]);
    }

    private sealed class FakeChatModel : IChatModel
    {
        public ModelDescriptor Descriptor => throw new NotSupportedException();

        public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");

        public IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(ChatRequest request, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");
    }

    private sealed class FakeEmbeddingModel : IEmbeddingModel
    {
        public ModelDescriptor Descriptor => throw new NotSupportedException();

        public Task<EmbeddingResult> EmbedAsync(EmbeddingRequest request, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");

        public Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(IReadOnlyList<EmbeddingRequest> requests, CancellationToken ct) =>
            throw new NotSupportedException("Fake — not for calling");
    }
}
