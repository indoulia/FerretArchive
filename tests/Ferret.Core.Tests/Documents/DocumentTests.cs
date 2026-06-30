using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class DocumentTests
{
    [Fact]
    public void Document_Id_Matches_SourceAssetId()
    {
        var assetId = new AssetId("filesystem:///src/Program.cs");
        var doc = MakeDocument(assetId);
        Assert.Equal(DocumentId.From(assetId), doc.Id);
    }

    [Fact]
    public void Document_Metadata_Defaults_To_Empty()
    {
        var doc = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        Assert.Empty(doc.Metadata);
    }

    [Fact]
    public void Document_Sections_Defaults_To_Empty()
    {
        var doc = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        Assert.Empty(doc.Sections);
    }

    [Fact]
    public void Document_SourceFingerprint_May_Be_Null()
    {
        var doc = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        Assert.Null(doc.SourceFingerprint);
    }

    [Fact]
    public void Document_Has_No_Public_Setters()
    {
        // init-only setters are public in reflection but not settable after construction;
        // distinguish them from regular public set by checking for IsExternalInit modifier.
        var isExternalInit = typeof(System.Runtime.CompilerServices.IsExternalInit);
        var props = typeof(Document).GetProperties();
        Assert.All(
            props,
            p =>
            {
                var setter = p.SetMethod;
                if (setter == null || !setter.IsPublic)
                {
                    return; // no setter or non-public — fine
                }

                var isInitOnly = setter.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(isExternalInit);
                Assert.True(
                    isInitOnly,
                    $"Property '{p.Name}' must not have a public set setter — Document is immutable (init-only is allowed)");
            });
    }

    [Fact]
    public void Document_With_Expression_Creates_New_Instance_Leaving_Original_Unchanged()
    {
        var original = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        var modified = original with { Title = "Updated Title" };

        Assert.NotSame(original, modified);
        Assert.Null(original.Title);
        Assert.Equal("Updated Title", modified.Title);
    }

    [Fact]
    public void ParseContext_For_Sets_Asset()
    {
        var asset = MakeAsset(new Uri("filesystem:///src/A.cs"));
        var ctx = ParseContext.For(asset);
        Assert.Same(asset, ctx.Asset);
    }

    private static Document MakeDocument(AssetId assetId) => new()
    {
        Id = DocumentId.From(assetId),
        SourceAssetId = assetId,
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        MediaType = "text/x-csharp",
        Kind = DocumentKind.Code,
        PlainText = "class Program { }",
        ProducedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
    };

    private static AssetDescriptor MakeAsset(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = "A.cs",
        LastModified = DateTimeOffset.UtcNow,
    };
}
