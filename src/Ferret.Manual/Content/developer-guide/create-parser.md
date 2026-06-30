# Create a Parser

A parser converts a file stream into a `Document` that Ferret can index. Ferret dispatches to parsers by `MediaType`, so your parser only needs to handle the types it declares.

## Step 1: Implement IContentParser

```csharp
using Ferret.Core.Indexing;

namespace Ferret.Parsers.Pdf;

public sealed class PdfParser : IContentParser
{
    // Declare which MediaTypes this parser handles
    public IReadOnlyList<string> SupportedMediaTypes =>
        ["application/pdf"];

    public async Task<ParseResult<Document>> ParseAsync(
        AssetDescriptor asset,
        Stream content,
        CancellationToken ct = default)
    {
        if (content.Length == 0)
            return ParseResult<Document>.Empty();

        try
        {
            var text = await ExtractTextAsync(content, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
                return ParseResult<Document>.Empty();

            var document = new Document
            {
                Id = DocumentId.FromAsset(asset),
                SourceAssetId = asset.Id,
                ConnectorId = asset.ConnectorId,
                CanonicalUri = asset.CanonicalUri,
                DisplayName = asset.DisplayName,
                Content = text,
                MediaType = "application/pdf",
                IndexedAt = DateTimeOffset.UtcNow
            };

            return ParseResult<Document>.Success(document);
        }
        catch (Exception ex)
        {
            return ParseResult<Document>.Failed(ex.Message);
        }
    }

    private static async Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken ct)
    {
        // Use a PDF library like PdfPig or iTextSharp here
        await Task.Yield();
        throw new NotImplementedException("PDF extraction not implemented");
    }
}
```

## Step 2: Register in DI

```csharp
services.AddSingleton<IContentParser, PdfParser>();
```

The parser dispatcher automatically picks up all `IContentParser` registrations and routes by MediaType.

## Step 3: Map the file extension

If your connector assigns MediaType via extension, ensure the MIME type resolver knows about your type:

```csharp
services.Configure<MimeTypeOptions>(opts =>
{
    opts.Extensions[".pdf"] = "application/pdf";
});
```

## ParseResult outcomes

| Outcome | Meaning | Effect |
|---|---|---|
| `Success(document)` | Parsed successfully | Document written to index |
| `Empty()` | File has no indexable content | Counted as Skipped |
| `Failed(message)` | Parse error | Counted as Failures; pipeline continues |

> **Note:** Parsers must never throw. Return `ParseResult.Failed(message)` instead. The pipeline expects all failure modes as explicit return values.

## Related

- [Extension Points](../architecture/extension-points) — parser interface diagram
- [Create a Connector](create-connector) — the connector that discovers assets your parser handles
- [Indexing](../user-guide/indexing) — how the full index pipeline runs
