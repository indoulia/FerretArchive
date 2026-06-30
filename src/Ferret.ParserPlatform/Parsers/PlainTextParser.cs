using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Fallback parser for any <c>text/*</c> media type. Reads content as UTF-8 and produces
/// a Document whose PlainText is the full file content.
/// Priority 100 — lower than format-specific parsers.
/// </summary>
public sealed class PlainTextParser : IContentParser
{
    /// <summary>Gets the static descriptor for the plain-text parser.</summary>
    public static readonly ParserDescriptor PlainTextDescriptor = new()
    {
        Id = new ParserId("text/plain"),
        Name = "Plain Text Parser",
        Version = "1.0",
        SupportedMediaTypes = ["text/*"],
        Capabilities = [ParserCapabilities.PlainTextExtraction],
        Priority = 100,
    };

    private static readonly HashSet<string> CodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/x-csharp", "text/x-python", "text/javascript", "text/typescript",
        "text/x-go", "text/x-rust", "text/x-java", "text/x-kotlin", "text/x-ruby",
        "text/x-swift", "text/x-c", "text/x-c++", "text/x-sh", "text/x-sql",
        "text/x-razor", "text/x-vue", "text/x-graphql", "text/css",
    };

    private static readonly HashSet<string> ConfigTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/yaml", "text/x-terraform", "text/x-powershell", "text/x-protobuf",
        "application/toml",
    };

    private static readonly HashSet<string> DataTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv", "text/tab-separated-values", "text/xml",
    };

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => PlainTextDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(
        Stream content,
        ParseContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        using var reader = new StreamReader(
            content,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var mediaType = context.Asset.MediaType ?? "text/plain";
        var kind = ResolveKind(mediaType);

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = mediaType,
            Kind = kind,
            PlainText = text,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
        };
    }

    private static DocumentKind ResolveKind(string mediaType)
    {
        if (CodeTypes.Contains(mediaType))
        {
            return DocumentKind.Code;
        }

        if (ConfigTypes.Contains(mediaType))
        {
            return DocumentKind.Config;
        }

        if (DataTypes.Contains(mediaType))
        {
            return DocumentKind.Data;
        }

        if (mediaType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentKind.Prose;
        }

        return DocumentKind.Unknown;
    }
}
