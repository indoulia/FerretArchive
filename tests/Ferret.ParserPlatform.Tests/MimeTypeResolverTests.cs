using Ferret.Core.Documents;
using Ferret.ParserPlatform;

namespace Ferret.ParserPlatform.Tests;

public sealed class MimeTypeResolverTests
{
    private static readonly MimeTypeResolver Resolver = new();

    [Fact]
    public void Pdf_Resolves_To_ApplicationPdf_ParseableBinary()
    {
        var info = Resolver.Resolve("report.pdf");
        Assert.Equal("application/pdf", info.MediaType);
        Assert.Equal(MediaCategory.BinaryParseable, info.Category);
        Assert.Equal(DocumentKind.Prose, info.SuggestedKind);
    }

    [Fact]
    public void Docx_Resolves_To_Wordprocessing_ParseableBinary()
    {
        var info = Resolver.Resolve("spec.docx");
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", info.MediaType);
        Assert.Equal(MediaCategory.BinaryParseable, info.Category);
    }

    [Fact]
    public void Xlsx_Resolves_To_Spreadsheet_ParseableBinary_Data()
    {
        var info = Resolver.Resolve("jira-export.xlsx");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", info.MediaType);
        Assert.Equal(MediaCategory.BinaryParseable, info.Category);
        Assert.Equal(DocumentKind.Data, info.SuggestedKind);
    }

    [Theory]
    [InlineData("a.so")]
    [InlineData("a.class")]
    [InlineData("a.pyc")]
    [InlineData("a.nupkg")]
    [InlineData("a.psd")]
    public void Opaque_Binaries_Are_BinaryOpaque(string fileName)
    {
        Assert.Equal(MediaCategory.BinaryOpaque, Resolver.Resolve(fileName).Category);
    }

    [Theory]
    [InlineData("a.php", "text/x-php", DocumentKind.Code)]
    [InlineData("a.scala", "text/x-scala", DocumentKind.Code)]
    [InlineData("a.ini", "text/x-ini", DocumentKind.Config)]
    public void New_Text_Mappings_Have_Correct_Kind(string fileName, string mediaType, DocumentKind kind)
    {
        var info = Resolver.Resolve(fileName);
        Assert.Equal(mediaType, info.MediaType);
        Assert.Equal(MediaCategory.Text, info.Category);
        Assert.Equal(kind, info.SuggestedKind);
    }

    [Theory]
    [InlineData("Dockerfile")]
    [InlineData("Makefile")]
    public void Extensionless_Build_Files_Resolve_By_Name(string fileName)
    {
        var info = Resolver.Resolve(fileName);
        Assert.Equal(MediaCategory.Text, info.Category);
        Assert.Equal(DocumentKind.Config, info.SuggestedKind);
        Assert.Equal(1.0, info.Confidence);
    }

    [Fact]
    public void FileName_Lookup_Is_Case_Insensitive()
    {
        Assert.Equal(MediaCategory.Text, Resolver.Resolve("dockerfile").Category);
    }

    [Fact]
    public void FileName_Lookup_Uses_Base_Name_From_Path()
    {
        var info = Resolver.Resolve("/repo/build/Makefile");
        Assert.Equal(DocumentKind.Config, info.SuggestedKind);
    }

    [Fact]
    public void Known_Extension_Wins_Over_FileName_And_Unknown_Falls_Back_To_Text()
    {
        Assert.Equal("text/markdown", Resolver.Resolve("README.md").MediaType);
        var unknown = Resolver.Resolve("mystery.zzz");
        Assert.Equal("text/plain", unknown.MediaType);
        Assert.Equal(0.5, unknown.Confidence);
    }

    // Regression snapshot: a representative set of mappings across every category.
    // Guards the central resolver against accidental drift when entries are added later.
    [Theory]
    [InlineData("Program.cs", "text/x-csharp", MediaCategory.Text)]
    [InlineData("README.md", "text/markdown", MediaCategory.Text)]
    [InlineData("data.json", "application/json", MediaCategory.Text)]
    [InlineData("config.xml", "text/xml", MediaCategory.Text)]
    [InlineData("index.html", "text/html", MediaCategory.Text)]
    [InlineData("report.pdf", "application/pdf", MediaCategory.BinaryParseable)]
    [InlineData("spec.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", MediaCategory.BinaryParseable)]
    [InlineData("export.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", MediaCategory.BinaryParseable)]
    [InlineData("archive.zip", "application/octet-stream", MediaCategory.BinaryOpaque)]
    [InlineData("tool.exe", "application/octet-stream", MediaCategory.BinaryOpaque)]
    [InlineData("Dockerfile", "text/x-dockerfile", MediaCategory.Text)]
    [InlineData("Makefile", "text/x-makefile", MediaCategory.Text)]
    public void Representative_Mappings_Are_Stable(string fileName, string expectedMediaType, MediaCategory expectedCategory)
    {
        var info = Resolver.Resolve(fileName);
        Assert.Equal(expectedMediaType, info.MediaType);
        Assert.Equal(expectedCategory, info.Category);
    }
}
