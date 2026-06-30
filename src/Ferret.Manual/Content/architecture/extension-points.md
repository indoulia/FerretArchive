# Extension Points

Ferret exposes four primary extension interfaces. Each interface has a single responsibility, requires no base class inheritance, and is registered via dependency injection.

## 1. IConnector — Add a new data source

Implement `IConnector` (and optionally `IAssetSource`) to index content from any source.

```csharp
using Ferret.Core.Connectors;

public sealed class GitHubConnector : IConnector, IAssetSource
{
    public ConnectorType ConnectorType => ConnectorType.Remote;
    public ConnectorMetadata Metadata => new()
    {
        Id = "github",
        DisplayName = "GitHub",
        Version = new SemanticVersion(1, 0, 0)
    };
    public ConnectorIoCapabilities Capabilities =>
        ConnectorIoCapabilities.Read;

    public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct) =>
        Task.FromResult(ConnectorHealth.Healthy("GitHub reachable"));

    public Task<IConnectorSession> ConnectAsync(CancellationToken ct) =>
        Task.FromResult<IConnectorSession>(new GitHubSession(_options));

    public Task DisconnectAsync(CancellationToken ct) =>
        Task.CompletedTask;

    // IAssetSource
    public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        ConnectorSession session,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // yield return AssetDescriptor for each file in the repo
    }
}
```

Register in DI: `services.AddSingleton<IConnector, GitHubConnector>();`

## 2. IContentParser — Add a new file format

Implement `IContentParser` to extract text from a file type the default parsers do not handle.

```csharp
using Ferret.Core.Indexing;

public sealed class PdfParser : IContentParser
{
    public IReadOnlyList<string> SupportedMediaTypes =>
        ["application/pdf"];

    public Task<ParseResult<Document>> ParseAsync(
        AssetDescriptor asset,
        Stream content,
        CancellationToken ct)
    {
        // Extract text from PDF stream
        var text = ExtractText(content);
        var document = new Document
        {
            Id = DocumentId.FromAsset(asset),
            SourceAssetId = asset.Id,
            Content = text,
            MediaType = "application/pdf"
        };
        return Task.FromResult(ParseResult<Document>.Success(document));
    }
}
```

Register in DI: `services.AddSingleton<IContentParser, PdfParser>();`

## 3. IModelProvider — Add a new AI provider

Implement `IModelProvider` to integrate with a new AI vendor.

```csharp
using Ferret.Core.Ai;

public sealed class AnthropicModelProvider : IModelProvider
{
    public ProviderId ProviderId => new("anthropic");

    public IReadOnlyList<ModelDescriptor> Models => [
        new ModelDescriptor(new ModelId("anthropic/claude-3-5-sonnet"),
            ModelCapabilities.Chat, "Claude 3.5 Sonnet")
    ];

    public IChatModel? GetChatModel(ModelId modelId) =>
        modelId.Value == "anthropic/claude-3-5-sonnet"
            ? new AnthropicChatModel(_client, modelId)
            : null;

    public IEmbeddingModel? GetEmbeddingModel(ModelId modelId) => null;
}
```

Register in DI: `services.AddSingleton<IModelProvider, AnthropicModelProvider>();`

## 4. IPromptTemplate — Add a new prompt

Register a `PromptTemplate` instance in DI to make it available via `ferret prompt run`:

```csharp
services.AddSingleton<PromptTemplate>(new PromptTemplate
{
    Id = "summarise-file",
    Description = "Summarise the content of a source file",
    Template = """
        Summarise the following file in 3 sentences.

        File: {{filename}}
        Content:
        {{content}}
        """,
    RequiredVariables = ["filename", "content"]
});
```

> **Note:** Templates use `{{variable}}` substitution. Missing required variables throw `PromptRenderException` at render time.

## Related

- [Developer Guide](../developer-guide/index) — step-by-step walkthroughs
- [Platform Overview](platform-overview) — where extensions fit in the stack
- [Dependency Graph](dependency-graph) — package reference rules
