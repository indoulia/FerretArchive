using System.Reflection;

namespace Ferret.Manual;

/// <summary>In-memory catalogue of all manual pages with resource-backed Markdown loading.</summary>
public static class DocRegistry
{
    private const string ResourcePrefix = "Ferret.Manual.Content.";

    private static readonly Assembly _assembly = typeof(DocRegistry).Assembly;

    private static readonly List<DocPage> _pages = BuildCatalogue();

    /// <summary>Gets all registered manual pages in catalogue order.</summary>
    public static IReadOnlyList<DocPage> AllPages { get; } = _pages;

    /// <summary>Returns the page with the given slug, or <see langword="null"/> if not found.</summary>
    /// <param name="slug">The URL-safe slug to look up.</param>
    /// <returns>The matching <see cref="DocPage"/>, or <see langword="null"/>.</returns>
    public static DocPage? GetPage(string slug) =>
        AllPages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the Markdown source for the given page.</summary>
    /// <param name="page">The page whose Markdown should be loaded.</param>
    /// <returns>The raw Markdown string from the embedded resource.</returns>
    public static string GetMarkdown(DocPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        using var stream = _assembly.GetManifestResourceStream(page.ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource not found: {page.ResourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Returns pages grouped by section in navigation order.</summary>
    /// <returns>A list of (section, pages) tuples ordered by the minimum page order within each section.</returns>
    public static IReadOnlyList<(string Section, IReadOnlyList<DocPage> Pages)> GetNavTree()
    {
        return AllPages
            .GroupBy(p => p.Section)
            .OrderBy(g => g.Min(p => p.Order))
            .Select(g => (g.Key, (IReadOnlyList<DocPage>)g.OrderBy(p => p.Order).ToList()))
            .ToList();
    }

    /// <summary>Returns the page that precedes <paramref name="page"/> in catalogue order, or <see langword="null"/>.</summary>
    /// <param name="page">The reference page.</param>
    /// <returns>The preceding <see cref="DocPage"/>, or <see langword="null"/>.</returns>
    public static DocPage? GetPreviousPage(DocPage page)
    {
        var idx = _pages.FindIndex(p => p.Slug == page.Slug);
        return idx > 0 ? _pages[idx - 1] : null;
    }

    /// <summary>Returns the page that follows <paramref name="page"/> in catalogue order, or <see langword="null"/>.</summary>
    /// <param name="page">The reference page.</param>
    /// <returns>The following <see cref="DocPage"/>, or <see langword="null"/>.</returns>
    public static DocPage? GetNextPage(DocPage page)
    {
        var idx = _pages.FindIndex(p => p.Slug == page.Slug);
        return idx >= 0 && idx < _pages.Count - 1 ? _pages[idx + 1] : null;
    }

    private static List<DocPage> BuildCatalogue() =>
    new List<DocPage>
    {
        P("getting-started/index", "Getting Started", "Getting Started", 100),
        P("getting-started/installation", "Installation", "Getting Started", 101),
        P("getting-started/first-workspace", "First Workspace", "Getting Started", 102),
        P("getting-started/first-index", "First Index", "Getting Started", 103),
        P("getting-started/first-search", "First Search", "Getting Started", 104),
        P("getting-started/connect-claude", "Connect Claude", "Getting Started", 105),

        P("user-guide/index", "User Guide", "User Guide", 200),
        P("user-guide/workspace", "Workspace", "User Guide", 201),
        P("user-guide/connectors", "Connectors", "User Guide", 202),
        P("user-guide/parsers", "Parsers", "User Guide", 203),
        P("user-guide/indexing", "Indexing", "User Guide", 204),
        P("user-guide/search", "Search", "User Guide", 205),
        P("user-guide/context", "Context", "User Guide", 206),
        P("user-guide/ai", "AI", "User Guide", 207),
        P("user-guide/watch", "Watch", "User Guide", 208),

        P("reference/index", "Reference", "Reference", 300),
        P("reference/cli", "CLI Reference", "Reference", 301),
        P("reference/configuration", "Configuration", "Reference", 302),
        P("reference/mcp", "MCP Reference", "Reference", 303),
        P("reference/architecture", "Architecture", "Reference", 304),

        P("architecture/index", "Architecture Explorer", "Architecture", 400),
        P("architecture/platform-overview", "Platform Overview", "Architecture", 401),
        P("architecture/dependency-graph", "Dependency Graph", "Architecture", 402),
        P("architecture/storage", "Storage", "Architecture", 403),
        P("architecture/search-flow", "Search Flow", "Architecture", 404),
        P("architecture/ai-flow", "AI Flow", "Architecture", 405),
        P("architecture/context-assembly", "Context Assembly", "Architecture", 406),
        P("architecture/mcp-runtime", "MCP Runtime", "Architecture", 407),
        P("architecture/configuration", "Configuration", "Architecture", 408),
        P("architecture/extension-points", "Extension Points", "Architecture", 409),

        P("developer-guide/index", "Developer Guide", "Developer Guide", 500),
        P("developer-guide/create-connector", "Create a Connector", "Developer Guide", 501),
        P("developer-guide/create-parser", "Create a Parser", "Developer Guide", 502),
        P("developer-guide/create-ai-provider", "Create an AI Provider", "Developer Guide", 503),
        P("developer-guide/create-prompt", "Create a Prompt", "Developer Guide", 504),

        P("design/index", "Design Decisions", "Design Decisions", 550),
        P("design/why-sqlite", "Why SQLite?", "Design Decisions", 551),
        P("design/why-bm25", "Why BM25 Before Vectors?", "Design Decisions", 552),
        P("design/why-mcp", "Why MCP Before REST?", "Design Decisions", 553),
        P("design/why-providers", "Why Providers?", "Design Decisions", 554),
        P("design/why-context-assembly", "Why Context Assembly?", "Design Decisions", 555),
        P("design/why-platform-first", "Why Platform-First?", "Design Decisions", 556),
        P("design/why-manual", "Why Manual, Not Docs?", "Design Decisions", 557),

        P("troubleshooting", "Troubleshooting", "Troubleshooting", 600),
        P("faq", "FAQ", "FAQ", 700),
        P("release-notes", "Release Notes", "Release Notes", 800),
    };

    private static DocPage P(string slug, string title, string section, int order)
    {
        // MSBuild converts hyphens to underscores in directory segments, not in the filename.
        // e.g. slug "getting-started/installation" → resource "…getting_started.installation.md"
        var slashIdx = slug.LastIndexOf('/');
        string resourceSuffix;
        if (slashIdx < 0)
        {
            resourceSuffix = slug + ".md";
        }
        else
        {
            var dirPart = slug[..slashIdx].Replace('-', '_').Replace('/', '.');
            var filePart = slug[(slashIdx + 1)..];
            resourceSuffix = dirPart + "." + filePart + ".md";
        }

        return new DocPage
        {
            Slug = slug,
            Title = title,
            Section = section,
            Order = order,
            ResourceName = ResourcePrefix + resourceSuffix,
        };
    }
}
