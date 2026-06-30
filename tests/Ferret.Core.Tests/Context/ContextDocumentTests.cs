using Ferret.Core.Context;
using Ferret.Core.Primitives;

using Xunit;

namespace Ferret.Core.Tests.Context;

public sealed class ContextDocumentTests
{
    [Fact]
    public void ContextDocument_PreservesAllFields()
    {
        var id = DocumentId.Create("doc-1");
        var uri = new Uri("filesystem:///src/auth.cs");
        var doc = new ContextDocument
        {
            DocumentId = id,
            CanonicalUri = uri,
            DisplayName = "src/auth.cs",
            Title = "Authentication Service",
            Content = "public class AuthService { }",
            Score = 0.91f,
            TokenEstimate = 7,
            Source = ContextDocumentSource.FullDocument,
        };

        Assert.Equal("doc-1", doc.DocumentId.Value);
        Assert.Equal(uri, doc.CanonicalUri);
        Assert.Equal("src/auth.cs", doc.DisplayName);
        Assert.Equal("Authentication Service", doc.Title);
        Assert.Equal(0.91f, doc.Score);
        Assert.Equal(7, doc.TokenEstimate);
        Assert.Equal(ContextDocumentSource.FullDocument, doc.Source);
    }

    [Fact]
    public void ContextDocument_Title_CanBeNull()
    {
        var doc = new ContextDocument
        {
            DocumentId = DocumentId.Create("doc-2"),
            CanonicalUri = new Uri("filesystem:///src/util.cs"),
            DisplayName = "src/util.cs",
            Title = null,
            Content = "// utility",
            Score = 0.5f,
            TokenEstimate = 2,
            Source = ContextDocumentSource.Section,
        };

        Assert.Null(doc.Title);
        Assert.Equal(ContextDocumentSource.Section, doc.Source);
    }

    [Fact]
    public void ContextDocumentSource_HasExpectedValues()
    {
        Assert.Equal(0, (int)ContextDocumentSource.FullDocument);
        Assert.Equal(1, (int)ContextDocumentSource.Section);
    }
}
