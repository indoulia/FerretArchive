using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_Returns_Success_When_Parser_Registered()
    {
        var parser = new CapableParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("hello world");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ParseResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task DispatchAsync_Returns_Unsupported_When_No_Parser_Registered()
    {
        var registry = ParserRegistryBuilder.Build([]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("application/pdf"));

        Assert.Equal(ParseResultKind.Unsupported, result.Kind);
    }

    [Fact]
    public async Task DispatchAsync_Uses_OctetStream_When_Asset_Has_No_MediaType()
    {
        var registry = ParserRegistryBuilder.Build([]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");
        var asset = MakeAsset(null);

        var result = await dispatcher.DispatchAsync(stream, asset);

        Assert.Equal(ParseResultKind.Unsupported, result.Kind);
        Assert.Contains("application/octet-stream", result.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DispatchAsync_Returns_Empty_When_Stream_Is_Empty()
    {
        var parser = new CapableParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = new MemoryStream();

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.Equal(ParseResultKind.Empty, result.Kind);
    }

    [Fact]
    public async Task DispatchAsync_Returns_Failed_When_Parser_Throws()
    {
        var parser = new ThrowingParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.Equal(ParseResultKind.Failed, result.Kind);
        Assert.Contains("simulated failure", result.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DispatchAsync_Propagates_OperationCanceledException()
    {
        var parser = new CancellingParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.DispatchAsync(stream, MakeAsset("text/plain"), cts.Token).AsTask());
    }

    [Fact]
    public async Task DispatchAsync_Returns_Empty_When_Document_PlainText_Is_Whitespace()
    {
        var parser = new WhitespaceParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("   \n\t  ");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.Equal(ParseResultKind.Empty, result.Kind);
    }

    private static MemoryStream MakeStream(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }

    private static AssetDescriptor MakeAsset(string? mediaType) => new()
    {
        Id = AssetId.From(new Uri("filesystem:///src/test.txt")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///src/test.txt"),
        DisplayName = "test.txt",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = mediaType,
    };

    private sealed class CapableParser : IContentParser
    {
        internal CapableParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Capable",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct)
        {
            var doc = new Document
            {
                Id = DocumentId.From(context.Asset.Id),
                SourceAssetId = context.Asset.Id,
                ConnectorId = context.Asset.ConnectorId,
                InstanceId = context.Asset.InstanceId,
                MediaType = context.Asset.MediaType ?? "text/plain",
                Kind = DocumentKind.Unknown,
                PlainText = "parsed content",
                ProducedAt = DateTimeOffset.UtcNow,
            };
            return ValueTask.FromResult(doc);
        }
    }

    private sealed class ThrowingParser : IContentParser
    {
        internal ThrowingParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Throwing",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
            throw new InvalidOperationException("simulated failure");
    }

    private sealed class CancellingParser : IContentParser
    {
        internal CancellingParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Cancelling",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            throw new InvalidOperationException("should not reach here");
        }
    }

    private sealed class WhitespaceParser : IContentParser
    {
        internal WhitespaceParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Whitespace",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct)
        {
            var doc = new Document
            {
                Id = DocumentId.From(context.Asset.Id),
                SourceAssetId = context.Asset.Id,
                ConnectorId = context.Asset.ConnectorId,
                InstanceId = context.Asset.InstanceId,
                MediaType = context.Asset.MediaType ?? "text/plain",
                Kind = DocumentKind.Unknown,
                PlainText = "   \n\t  ",
                ProducedAt = DateTimeOffset.UtcNow,
            };
            return ValueTask.FromResult(doc);
        }
    }
}
