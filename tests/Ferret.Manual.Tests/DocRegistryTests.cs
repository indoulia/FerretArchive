namespace Ferret.Manual.Tests;

public sealed class DocRegistryTests
{
    [Fact]
    public void AllPages_ContainsExpectedSlugs()
    {
        var slugs = DocRegistry.AllPages.Select(p => p.Slug).ToHashSet();
        Assert.Contains("getting-started/installation", slugs);
        Assert.Contains("architecture/context-assembly", slugs);
        Assert.Contains("reference/cli", slugs);
        Assert.Contains("developer-guide/create-connector", slugs);
        Assert.Contains("design/why-sqlite", slugs);
        Assert.Contains("design/why-mcp", slugs);
    }

    [Fact]
    public void GetPage_KnownSlug_ReturnsPage()
    {
        var page = DocRegistry.GetPage("getting-started/installation");
        Assert.NotNull(page);
        Assert.Equal("Installation", page.Title);
    }

    [Fact]
    public void GetPage_UnknownSlug_ReturnsNull()
    {
        var page = DocRegistry.GetPage("does-not-exist");
        Assert.Null(page);
    }

    [Fact]
    public void GetMarkdown_KnownPage_ReturnsNonEmptyString()
    {
        var page = DocRegistry.GetPage("getting-started/installation")!;
        var markdown = DocRegistry.GetMarkdown(page);
        Assert.False(string.IsNullOrWhiteSpace(markdown));
        Assert.Contains("Installation", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void AllPages_OrderedBySection_ThenOrder()
    {
        var pages = DocRegistry.AllPages.ToList();
        var gsIndex = pages.IndexOf(pages.First(p => p.Section == "Getting Started"));
        var ugIndex = pages.IndexOf(pages.First(p => p.Section == "User Guide"));
        Assert.True(gsIndex < ugIndex);
    }

    [Fact]
    public void GetMarkdown_AllPages_ResourcesExist()
    {
        foreach (var page in DocRegistry.AllPages)
        {
            var ex = Record.Exception(() => DocRegistry.GetMarkdown(page));
            Assert.True(ex is null, $"Page '{page.Slug}' failed: {ex?.Message}");
        }
    }
}
