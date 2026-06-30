using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Providers.Compliance;

/// <summary>
/// Shared behavioral contract suite for <see cref="IModelProvider"/> implementations.
/// Derive from this class in each provider's test project to inherit all expectations.
/// Future providers (Anthropic, Gemini, LM Studio, Azure, vLLM) inherit the same guarantees automatically.
/// </summary>
public abstract class ProviderComplianceTests
{
    /// <summary>Returns the provider under test, constructed with a minimal valid configuration.</summary>
    /// <returns>A fully initialized <see cref="IModelProvider"/> ready for behavioral assertions.</returns>
    protected abstract IModelProvider CreateProvider();

    // ── Descriptor ────────────────────────────────────────────────────────────

    [Fact]
    public void Descriptor_Id_IsNotEmpty()
    {
        var sut = CreateProvider();
        Assert.False(string.IsNullOrWhiteSpace(sut.Descriptor.Id.Value));
    }

    [Fact]
    public void Descriptor_DisplayName_IsNotEmpty()
    {
        var sut = CreateProvider();
        Assert.False(string.IsNullOrWhiteSpace(sut.Descriptor.DisplayName));
    }

    [Fact]
    public void Descriptor_Capabilities_IsNonZero()
    {
        var sut = CreateProvider();
        Assert.NotEqual(ModelCapabilities.None, sut.Descriptor.Capabilities);
    }

    [Fact]
    public void Descriptor_Version_IsNotEmpty()
    {
        var sut = CreateProvider();
        Assert.False(string.IsNullOrWhiteSpace(sut.Descriptor.Version));
    }

    // ── ListModelsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListModelsAsync_ReturnsNonNull()
    {
        var sut = CreateProvider();
        var result = await sut.ListModelsAsync(CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ListModelsAsync_AllModels_HaveNonEmptyIds()
    {
        var sut = CreateProvider();
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.All(models, m => Assert.False(string.IsNullOrWhiteSpace(m.Id.Value)));
    }

    [Fact]
    public async Task ListModelsAsync_AllModels_HaveNonEmptyDisplayNames()
    {
        var sut = CreateProvider();
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.All(models, m => Assert.False(string.IsNullOrWhiteSpace(m.DisplayName)));
    }

    [Fact]
    public async Task ListModelsAsync_AllModels_HaveNonZeroCapabilities()
    {
        var sut = CreateProvider();
        var models = await sut.ListModelsAsync(CancellationToken.None);
        Assert.All(models, m => Assert.NotEqual(ModelCapabilities.None, m.Capabilities));
    }

    // ── GetChatModel ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChatModel_ForChatCapabilityModel_ReturnsNonNull()
    {
        var sut = CreateProvider();
        var chatModelId = await FindFirstModelWithCapabilityAsync(sut, ModelCapabilities.Chat);
        if (chatModelId is null)
        {
            return; // provider has no chat models — compliance passes trivially
        }

        var model = sut.GetChatModel(chatModelId.Value);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task GetChatModel_ForChatCapabilityModel_ReturnedModel_HasChatCapability()
    {
        var sut = CreateProvider();
        var chatModelId = await FindFirstModelWithCapabilityAsync(sut, ModelCapabilities.Chat);
        if (chatModelId is null)
        {
            return;
        }

        var model = sut.GetChatModel(chatModelId.Value);
        Assert.NotNull(model);
        Assert.True(model.Descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
    }

    [Fact]
    public void GetChatModel_ForeignProvider_ReturnsNull()
    {
        var sut = CreateProvider();
        var foreignId = ModelId.Create("__foreign_provider__/__unknown_model__");
        Assert.Null(sut.GetChatModel(foreignId));
    }

    // ── GetEmbeddingModel ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetEmbeddingModel_ForEmbeddingCapabilityModel_ReturnsNonNull()
    {
        var sut = CreateProvider();
        var embModelId = await FindFirstModelWithCapabilityAsync(sut, ModelCapabilities.Embedding);
        if (embModelId is null)
        {
            return; // provider has no embedding models — compliance passes trivially
        }

        var model = sut.GetEmbeddingModel(embModelId.Value);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task GetEmbeddingModel_ForEmbeddingCapabilityModel_ReturnedModel_HasEmbeddingCapability()
    {
        var sut = CreateProvider();
        var embModelId = await FindFirstModelWithCapabilityAsync(sut, ModelCapabilities.Embedding);
        if (embModelId is null)
        {
            return;
        }

        var model = sut.GetEmbeddingModel(embModelId.Value);
        Assert.NotNull(model);
        Assert.True(model.Descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public void GetEmbeddingModel_ForeignProvider_ReturnsNull()
    {
        var sut = CreateProvider();
        var foreignId = ModelId.Create("__foreign_provider__/__unknown_model__");
        Assert.Null(sut.GetEmbeddingModel(foreignId));
    }

    // ── GetReranker ───────────────────────────────────────────────────────────

    [Fact]
    public void GetReranker_DoesNotThrow()
    {
        var sut = CreateProvider();
        var id = ModelId.Create("openai/gpt-4o");
        var ex = Record.Exception(() => sut.GetReranker(id));
        Assert.Null(ex);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<ModelId?> FindFirstModelWithCapabilityAsync(
        IModelProvider provider,
        ModelCapabilities capability)
    {
        var models = await provider.ListModelsAsync(CancellationToken.None);
        var match = models.FirstOrDefault(m => m.Capabilities.HasFlag(capability));
        return match?.Id;
    }
}
