using System.Text;
using System.Text.Json;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Content parser for <c>application/json</c>. Priority 200 — higher than PlainTextParser.
/// Flattens JSON into dot-notation key-value pairs with deterministic (lexicographic) property ordering.
/// Uses System.Text.Json (BCL) — no external package dependency.
/// </summary>
public sealed class JsonParser : IContentParser
{
    private static readonly ParserDescriptor JsonDescriptor = new()
    {
        Id = new ParserId("application/json"),
        Name = "JSON Parser",
        Version = "1.0",
        SupportedMediaTypes = ["application/json"],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.MetadataExtraction],
        Priority = 200,
    };

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => JsonDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(
        Stream content,
        ParseContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        using var doc = await JsonDocument.ParseAsync(content, cancellationToken: ct)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        FlattenElement(doc.RootElement, string.Empty, sb);
        var plainText = sb.ToString().Trim();

        var title = ExtractTitle(doc.RootElement);

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = "application/json",
            Kind = DocumentKind.Data,
            PlainText = plainText,
            Title = title,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
        };
    }

    private static void FlattenElement(JsonElement element, string prefix, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var props = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ToList();
                foreach (var prop in props)
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenElement(prop.Value, key, sb);
                }

                break;

            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenElement(item, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{prefix}[{i}]"), sb);
                    i++;
                }

                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{prefix}: {element}"));
                break;

            case JsonValueKind.Null:
                break;

            default:
                break;
        }
    }

    private static string? ExtractTitle(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (root.TryGetProperty("name", out var nameProp) &&
            nameProp.ValueKind == JsonValueKind.String)
        {
            return nameProp.GetString();
        }

        if (root.TryGetProperty("title", out var titleProp) &&
            titleProp.ValueKind == JsonValueKind.String)
        {
            return titleProp.GetString();
        }

        return null;
    }
}
