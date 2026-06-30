// tests/Ferret.Core.Tests/Context/ContextPackageTests.cs
using Ferret.Core.Context;
using Ferret.Core.Primitives;

using Xunit;

namespace Ferret.Core.Tests.Context;

public sealed class ContextPackageTests
{
    private static ContextDocument MakeDoc(string id, string display, string content, float score) =>
        new()
        {
            DocumentId = DocumentId.Create(id),
            CanonicalUri = new Uri($"filesystem:///{display}"),
            DisplayName = display,
            Title = null,
            Content = content,
            Score = score,
            TokenEstimate = (content.Length / 4) + 1,
            Source = ContextDocumentSource.FullDocument,
        };

    [Fact]
    public void ContextPackage_PreservesFields()
    {
        var docs = new[]
        {
            MakeDoc("doc-1", "src/auth.cs", "public class Auth { }", 0.9f),
            MakeDoc("doc-2", "src/user.cs", "public class User { }", 0.7f),
        };
        var pkg = new ContextPackage
        {
            Query = "authentication",
            Documents = docs,
            TotalTokenEstimate = 12,
            DocumentsConsidered = 5,
            DocumentsIncluded = 2,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("authentication", pkg.Query);
        Assert.Equal(2, pkg.Documents.Count);
        Assert.Equal(12, pkg.TotalTokenEstimate);
        Assert.Equal(5, pkg.DocumentsConsidered);
        Assert.Equal(2, pkg.DocumentsIncluded);
    }

    [Fact]
    public void ToPromptString_ContainsQueryAndDocuments()
    {
        var docs = new[]
        {
            MakeDoc("doc-1", "src/auth.cs", "public class Auth { }", 0.9f),
        };
        var pkg = new ContextPackage
        {
            Query = "authentication",
            Documents = docs,
            TotalTokenEstimate = 6,
            DocumentsConsidered = 1,
            DocumentsIncluded = 1,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        var result = pkg.ToPromptString();

        Assert.Contains("authentication", result, StringComparison.Ordinal);
        Assert.Contains("src/auth.cs", result, StringComparison.Ordinal);
        Assert.Contains("public class Auth", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToPromptString_NumbersDocuments()
    {
        var docs = new[]
        {
            MakeDoc("doc-1", "src/a.cs", "content a", 0.9f),
            MakeDoc("doc-2", "src/b.cs", "content b", 0.7f),
        };
        var pkg = new ContextPackage
        {
            Query = "test",
            Documents = docs,
            TotalTokenEstimate = 10,
            DocumentsConsidered = 2,
            DocumentsIncluded = 2,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        var result = pkg.ToPromptString();

        Assert.Contains("[1]", result, StringComparison.Ordinal);
        Assert.Contains("[2]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToPromptString_EmptyDocuments_ReturnsQueryHeader()
    {
        var pkg = new ContextPackage
        {
            Query = "no results",
            Documents = [],
            TotalTokenEstimate = 0,
            DocumentsConsidered = 0,
            DocumentsIncluded = 0,
            AssembledAt = DateTimeOffset.UtcNow,
        };

        var result = pkg.ToPromptString();

        Assert.Contains("no results", result, StringComparison.Ordinal);
        Assert.DoesNotContain("[1]", result, StringComparison.Ordinal);
    }
}
