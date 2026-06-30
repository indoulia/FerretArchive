# Create an AI Provider

An AI provider wraps a vendor SDK and exposes it through Ferret's `IModelProvider` interface. The rest of Ferret never imports the vendor SDK — only your provider package does.

## Step 1: Create the project

```bash
dotnet new classlib -n Ferret.Providers.Anthropic
cd Ferret.Providers.Anthropic
dotnet add reference ../Ferret.Core/Ferret.Core.csproj
dotnet add package Anthropic.SDK   # or your vendor's SDK
```

## Step 2: Implement IModelProvider

```csharp
using Ferret.Core.Ai;

namespace Ferret.Providers.Anthropic;

public sealed class AnthropicModelProvider : IModelProvider
{
    private readonly AnthropicClient _client;

    public AnthropicModelProvider(AnthropicClient client)
    {
        _client = client;
    }

    public ProviderId ProviderId => new("anthropic");

    public IReadOnlyList<ModelDescriptor> Models =>
    [
        new ModelDescriptor(
            Id: new ModelId("anthropic/claude-3-5-sonnet-20241022"),
            Capabilities: ModelCapabilities.Chat,
            DisplayName: "Claude 3.5 Sonnet"),
        new ModelDescriptor(
            Id: new ModelId("anthropic/claude-3-haiku-20240307"),
            Capabilities: ModelCapabilities.Chat,
            DisplayName: "Claude 3 Haiku")
    ];

    public IChatModel? GetChatModel(ModelId modelId) =>
        Models.Any(m => m.Id == modelId)
            ? new AnthropicChatModel(_client, modelId)
            : null;

    public IEmbeddingModel? GetEmbeddingModel(ModelId modelId) => null;
}
```

## Step 3: Implement IChatModel

```csharp
internal sealed class AnthropicChatModel : IChatModel
{
    private readonly AnthropicClient _client;

    public AnthropicChatModel(AnthropicClient client, ModelId modelId)
    {
        _client = client;
        Descriptor = new ModelDescriptor(modelId, ModelCapabilities.Chat,
            "Anthropic Chat Model");
    }

    public ModelDescriptor Descriptor { get; }

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken ct = default)
    {
        // Translate CompletionRequest to vendor SDK call
        var response = await _client.Messages.CreateAsync(
            new MessageRequest
            {
                Model = Descriptor.Id.Value.Replace("anthropic/", ""),
                MaxTokens = request.MaxTokens ?? 1024,
                Messages = [new() { Role = "user", Content = request.Prompt }]
            }, ct).ConfigureAwait(false);

        return new CompletionResponse
        {
            Content = response.Content[0].Text,
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens
        };
    }
}
```

## Step 4: Register in DI

```csharp
services.AddSingleton<AnthropicClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
    return new AnthropicClient(opts.ApiKey);
});
services.AddSingleton<IModelProvider, AnthropicModelProvider>();
```

## Step 5: Verify isolation

Architecture tests will fail if `Anthropic.*` types leak into other packages. Run:

```bash
dotnet test Ferret.Architecture.Tests
```

## Related

- [AI Flow Architecture](../architecture/ai-flow) — how the provider chain works
- [Why Providers?](../design/why-providers) — the design rationale
- [Configuration Reference](../reference/configuration) — how to set the default model
