using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class ModelDescriptorTests
{
    [Fact]
    public void ModelDescriptor_PreservesId()
    {
        var id = ModelId.Create("ollama/llama3.2");
        var descriptor = new ModelDescriptor
        {
            Id = id,
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat,
        };
        Assert.Equal(id, descriptor.Id);
    }

    [Fact]
    public void ModelDescriptor_ContextWindow_DefaultsToNull()
    {
        var descriptor = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat,
        };
        Assert.Null(descriptor.ContextWindow);
    }

    [Fact]
    public void ModelDescriptor_Description_DefaultsToNull()
    {
        var descriptor = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat,
        };
        Assert.Null(descriptor.Description);
    }

    [Fact]
    public void ModelDescriptor_PreservesCapabilities()
    {
        var descriptor = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat | ModelCapabilities.Vision,
        };
        Assert.True(descriptor.Capabilities.HasFlag(ModelCapabilities.Chat));
        Assert.True(descriptor.Capabilities.HasFlag(ModelCapabilities.Vision));
        Assert.False(descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public void ProviderDescriptor_PreservesCapabilities()
    {
        var descriptor = new ProviderDescriptor
        {
            Id = ProviderId.Create("ollama"),
            DisplayName = "Ollama",
            Capabilities = ModelCapabilities.Chat | ModelCapabilities.Embedding,
            Version = "0.1.0",
        };
        Assert.True(descriptor.Capabilities.HasFlag(ModelCapabilities.Embedding));
    }

    [Fact]
    public void ProviderDescriptor_PreservesVersion()
    {
        var descriptor = new ProviderDescriptor
        {
            Id = ProviderId.Create("ollama"),
            DisplayName = "Ollama",
            Capabilities = ModelCapabilities.Chat,
            Version = "0.6.1",
        };
        Assert.Equal("0.6.1", descriptor.Version);
    }

    [Fact]
    public void ModelDescriptor_Equality_SameValues_AreEqual()
    {
        var a = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat,
        };
        var b = new ModelDescriptor
        {
            Id = ModelId.Create("ollama/llama3.2"),
            ProviderId = ProviderId.Create("ollama"),
            DisplayName = "Llama 3.2",
            Capabilities = ModelCapabilities.Chat,
        };
        Assert.Equal(a, b);
    }
}
