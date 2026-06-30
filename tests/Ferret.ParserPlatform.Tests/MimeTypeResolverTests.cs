using Ferret.Core.Documents;

namespace Ferret.ParserPlatform.Tests;

public sealed class MimeTypeResolverTests
{
    private readonly MimeTypeResolver _resolver = new();

    [Theory]
    [InlineData("README.md", "text/markdown")]
    [InlineData("index.html", "text/html")]
    [InlineData("app.ts", "text/typescript")]
    [InlineData("config.yaml", "text/yaml")]
    [InlineData("data.json", "application/json")]
    [InlineData("main.cs", "text/x-csharp")]
    [InlineData("script.py", "text/x-python")]
    [InlineData("build.rs", "text/x-rust")]
    public void Resolve_Returns_Correct_MediaType_For_Known_Extension(string fileName, string expectedMediaType)
    {
        var result = _resolver.Resolve(fileName);

        Assert.Equal(expectedMediaType, result.MediaType);
    }

    [Theory]
    [InlineData("binary.dll")]
    [InlineData("image.png")]
    [InlineData("archive.zip")]
    [InlineData("document.pdf")]
    public void Resolve_Returns_Binary_For_Binary_Extensions(string fileName)
    {
        var result = _resolver.Resolve(fileName);

        Assert.True(result.IsBinary);
        Assert.False(result.IsText);
        Assert.Equal("application/octet-stream", result.MediaType);
    }

    [Theory]
    [InlineData("README.md")]
    [InlineData("main.cs")]
    [InlineData("config.yaml")]
    public void Resolve_Returns_Text_For_Text_Extensions(string fileName)
    {
        var result = _resolver.Resolve(fileName);

        Assert.True(result.IsText);
        Assert.False(result.IsBinary);
    }

    [Fact]
    public void Resolve_Returns_PlainText_With_Low_Confidence_For_Unknown_Extension()
    {
        var result = _resolver.Resolve("file.unknown");

        Assert.Equal("text/plain", result.MediaType);
        Assert.True(result.Confidence < 1.0);
    }

    [Fact]
    public void Resolve_Is_Case_Insensitive_For_Extension()
    {
        var lower = _resolver.Resolve("file.md");
        var upper = _resolver.Resolve("file.MD");

        Assert.Equal(lower.MediaType, upper.MediaType);
    }

    [Fact]
    public void Resolve_Returns_PlainText_For_Empty_FileName()
    {
        var result = _resolver.Resolve(string.Empty);

        Assert.Equal("text/plain", result.MediaType);
    }

    [Fact]
    public void Resolve_Returns_PlainText_For_No_Extension()
    {
        var result = _resolver.Resolve("Makefile");

        Assert.Equal("text/plain", result.MediaType);
    }

    [Fact]
    public void Resolve_Returns_Confidence_1_For_Known_Extension()
    {
        var result = _resolver.Resolve("app.cs");

        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Resolve_Returns_SuggestedKind_For_Code_Files()
    {
        var result = _resolver.Resolve("main.cs");

        Assert.Equal(DocumentKind.Code, result.SuggestedKind);
    }

    [Fact]
    public void Resolve_Returns_SuggestedKind_Config_For_Yaml()
    {
        var result = _resolver.Resolve("config.yaml");

        Assert.Equal(DocumentKind.Config, result.SuggestedKind);
    }

    [Fact]
    public void Resolve_Returns_SuggestedKind_Prose_For_Markdown()
    {
        var result = _resolver.Resolve("readme.md");

        Assert.Equal(DocumentKind.Prose, result.SuggestedKind);
    }
}
