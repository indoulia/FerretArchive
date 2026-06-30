# Create a Connector

A connector discovers assets from a data source and returns `AssetDescriptor` instances. Ferret ships with a `FilesystemConnector`; this guide shows how to add your own.

## Step 1: Create the project

```bash
dotnet new classlib -n Ferret.Connectors.MySource
cd Ferret.Connectors.MySource
dotnet add reference ../Ferret.Core/Ferret.Core.csproj
```

## Step 2: Implement IConnector

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Connectors.MySource;

public sealed class MySourceConnector : IConnector, IAssetSource
{
    private readonly MySourceOptions _options;

    public MySourceConnector(MySourceOptions options)
    {
        _options = options;
    }

    // --- IConnector ---

    public ConnectorType ConnectorType => ConnectorType.Remote;

    public ConnectorMetadata Metadata => new()
    {
        Id = "mysource",
        DisplayName = "My Source",
        Version = new SemanticVersion(1, 0, 0),
        Description = "Indexes content from My Source."
    };

    public ConnectorIoCapabilities Capabilities =>
        ConnectorIoCapabilities.Read;

    public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default)
    {
        // Check connectivity to your source
        return Task.FromResult(ConnectorHealth.Healthy("My Source reachable"));
    }

    public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default)
    {
        var session = new MySourceSession(_options);
        return Task.FromResult<IConnectorSession>(session);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    // --- IAssetSource ---

    public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        IConnectorSession session,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var items = await FetchItemsFromSource(ct).ConfigureAwait(false);

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return new AssetDescriptor
            {
                Id = AssetId.FromUri(item.Uri),
                CanonicalUri = new CanonicalUri(item.Uri),
                DisplayName = item.Title,
                MediaType = "text/plain",
                ContentLength = item.ContentLength,
                LastModified = item.UpdatedAt
            };
        }
    }

    private Task<IReadOnlyList<SourceItem>> FetchItemsFromSource(CancellationToken ct)
    {
        // Call your data source API here
        throw new NotImplementedException();
    }
}
```

## Step 3: Register in DI

In your module or `Program.cs`:

```csharp
services.AddSingleton<IConnector, MySourceConnector>();
services.Configure<MySourceOptions>(configuration.GetSection("connectors:mysource"));
```

## Step 4: Test

```csharp
[Fact]
public async Task DiscoverAsync_Returns_Assets()
{
    var connector = new MySourceConnector(new MySourceOptions { /* ... */ });
    var session = await connector.ConnectAsync();

    var assets = await connector.DiscoverAsync(session)
        .ToListAsync();

    assets.Should().NotBeEmpty();
    assets.Should().AllSatisfy(a => a.MediaType.Should().NotBeNullOrEmpty());
}
```

## Related

- [Extension Points](../architecture/extension-points) — connector interface diagram
- [Create a Parser](create-parser) — parse the content your connector discovers
- [Dependency Graph](../architecture/dependency-graph) — package reference rules
