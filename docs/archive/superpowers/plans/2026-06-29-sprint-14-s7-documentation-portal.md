# Sprint 14 S7: The Ferret Manual Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build The Ferret Manual — a first-class embedded product manual served by `ferret manual`. Modelled on Microsoft Learn, Rust Book, and Kubernetes Docs. Not API documentation. A complete manual covering every audience: new users, daily users, contributors, and integrators.

**Architecture:** `Ferret.Manual` package hosts Markdown content files as embedded resources. At runtime, `Markdig` converts Markdown to HTML on demand. `ManualServer` (System.Net.HttpListener) serves pages on port 7070. A single-file HTML template with inline CSS and Lunr.js provides navigation, syntax highlighting, and full-text search. `ferret manual` starts the server and opens the browser.

**Tech Stack:** .NET 9, Markdig (NuGet), System.Net.HttpListener, Lunr.js 2.3.9 (inlined), xUnit

## Global Constraints

- Package name: `Ferret.Manual` (not `Ferret.Docs`)
- CLI command: `ferret manual [--port N]`
- No ASP.NET Core — `System.Net.HttpListener` only
- No external CSS/JS requests — everything inline
- Content source: Markdown files in `src/Ferret.Manual/Content/` as `EmbeddedResource`
- Follows existing CLI module pattern: `CliModuleBase`, `CommandDefinition`, `ICommandHandler.ExecuteAsync`
- TDD: failing test → red → implement → green → commit
- Commit prefix: `feat(sprint-14):`

---

## Content Structure (33 pages)

```
Content/
├── getting-started/
│   ├── index.md              Getting Started overview
│   ├── installation.md       Install Ferret (win/mac/linux)
│   ├── first-workspace.md    ferret init walkthrough
│   ├── first-index.md        ferret index walkthrough
│   ├── first-search.md       ferret search walkthrough
│   └── connect-claude.md     Connect to Claude Desktop / Cursor
├── user-guide/
│   ├── index.md              User Guide overview
│   ├── workspace.md          .ferret/ tree, config, workspace lifecycle
│   ├── connectors.md         What connectors do, list of built-in connectors
│   ├── parsers.md            Parser platform, supported file types
│   ├── indexing.md           Full index, incremental index, ferret watch
│   ├── search.md             BM25 search, ranking, filters
│   ├── context.md            Context Assembly, ContextPackage, token budgets
│   ├── ai.md                 AI providers, model routing, prompt platform
│   └── watch.md              File watching, debounce, auto re-index
├── reference/
│   ├── index.md              Reference overview
│   ├── cli.md                Every command, flag, exit code, example
│   ├── configuration.md      ferret.config.json full schema + env vars
│   ├── mcp.md                Every MCP tool: schema, example, error codes
│   └── architecture.md       Layer diagram, dependency rules, ADR index
├── architecture/
│   ├── index.md              Architecture Explorer overview
│   ├── platform-overview.md  Platform layer stack with ASCII diagram
│   ├── dependency-graph.md   Package dependency diagram
│   ├── storage.md            SQLite schema, FTS5 tables, state files
│   ├── search-flow.md        Query → BM25 → Result flow diagram
│   ├── ai-flow.md            AI provider chain flow diagram
│   ├── context-assembly.md   Context pipeline diagram
│   ├── mcp-runtime.md        MCP server lifecycle, tool dispatch
│   ├── configuration.md      Config layering, binding, env overrides
│   └── extension-points.md   IConnector, IParser, IAiProvider, IPromptTemplate
├── developer-guide/
│   ├── index.md              Developer Guide overview
│   ├── create-connector.md   Build a custom IConnector
│   ├── create-parser.md      Build a custom IParser
│   ├── create-ai-provider.md Implement IAiProvider
│   └── create-prompt.md      Create a custom prompt template
├── design/
│   ├── index.md              Design Decisions overview
│   ├── why-sqlite.md         Why SQLite for the index
│   ├── why-bm25.md           Why BM25 before semantic/vector search
│   ├── why-mcp.md            Why MCP before REST
│   ├── why-providers.md      Why the provider abstraction
│   ├── why-context-assembly.md  Why context assembly as a pipeline
│   ├── why-platform-first.md   Why platform-first architecture
│   └── why-manual.md           Why Ferret.Manual, not Ferret.Docs
├── troubleshooting.md        Common errors with ferret doctor messages + fixes
├── faq.md                    Frequently asked questions
└── release-notes.md          Version history: Sprint 8 → Sprint 14 (RC1)
```

---

## File Structure After S7

```
src/
├── Ferret.Manual/
│   ├── Ferret.Manual.csproj
│   ├── Content/**/*.md          (33 content files)
│   ├── DocPage.cs               Page metadata record
│   ├── DocRegistry.cs           In-memory page catalogue + resource loading
│   ├── HtmlTemplate.cs          Inline CSS + Lunr.js + page render
│   ├── ManualServer.cs          HttpListener request handler
│   └── ManualCommandHandler.cs  ferret manual command logic
tests/
├── Ferret.Manual.Tests/
│   ├── Ferret.Manual.Tests.csproj
│   ├── DocRegistryTests.cs
│   └── ManualServerTests.cs
```

---

### Task 1: `Ferret.Manual.csproj` — project scaffold

**Files:**
- Create: `src/Ferret.Manual/Ferret.Manual.csproj`
- Modify: `src/Ferret.sln` — add project

**Interfaces:**
- Produces: compilable project with Markdig NuGet reference and embedded content resources

- [ ] **Step 1: Create `Ferret.Manual.csproj`**

Create `src/Ferret.Manual/Ferret.Manual.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Ferret.Manual</RootNamespace>
    <AssemblyName>Ferret.Manual</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Markdig" Version="0.38.0" />
  </ItemGroup>

  <!-- Embed all Markdown content files as assembly resources -->
  <ItemGroup>
    <EmbeddedResource Include="Content\**\*.md" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add project to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Manual/Ferret.Manual.csproj
```

- [ ] **Step 3: Add project reference from Ferret.Cli**

```
dotnet add src/Ferret.Cli/Ferret.Cli.csproj reference src/Ferret.Manual/Ferret.Manual.csproj
```

- [ ] **Step 4: Build to verify scaffold**

```
dotnet build src/Ferret.sln
```

Expected: Build succeeded

- [ ] **Step 5: Commit**

```
git add src/Ferret.Manual/Ferret.Manual.csproj src/Ferret.sln src/Ferret.Cli/Ferret.Cli.csproj
git commit -m "feat(sprint-14): Ferret.Manual project scaffold"
```

---

### Task 2: `DocPage` + `DocRegistry` — page catalogue and resource loading

**Files:**
- Create: `src/Ferret.Manual/DocPage.cs`
- Create: `src/Ferret.Manual/DocRegistry.cs`
- Test: `tests/Ferret.Manual.Tests/DocRegistryTests.cs`

**Interfaces:**
- Produces:
  - `DocPage` — `Slug`, `Title`, `Section`, `Order`, `ResourceName`
  - `DocRegistry.GetPage(slug) → DocPage?`
  - `DocRegistry.GetMarkdown(page) → string`
  - `DocRegistry.AllPages → IReadOnlyList<DocPage>`
  - `DocRegistry.GetPreviousPage(page) → DocPage?`
  - `DocRegistry.GetNextPage(page) → DocPage?`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Manual.Tests/DocRegistryTests.cs`:

```csharp
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
        Assert.Contains("Installation", markdown);
    }

    [Fact]
    public void AllPages_OrderedBySection_ThenOrder()
    {
        var pages = DocRegistry.AllPages;
        var gsIndex = pages.IndexOf(pages.First(p => p.Section == "Getting Started"));
        var ugIndex = pages.IndexOf(pages.First(p => p.Section == "User Guide"));
        Assert.True(gsIndex < ugIndex);
    }
}
```

- [ ] **Step 2: Create the content directory structure**

Create the directory tree under `src/Ferret.Manual/Content/`:

```powershell
$dirs = @(
  "src\Ferret.Manual\Content\getting-started",
  "src\Ferret.Manual\Content\user-guide",
  "src\Ferret.Manual\Content\reference",
  "src\Ferret.Manual\Content\architecture",
  "src\Ferret.Manual\Content\developer-guide",
  "src\Ferret.Manual\Content\design"
)
$dirs | ForEach-Object { New-Item -ItemType Directory -Path $_ -Force }
```

Create a one-line placeholder for each of the 33 slugs listed in the Content Structure section above. Each file must contain at least `# [Title]` so the registry test passes.

- [ ] **Step 3: Create `DocPage.cs`**

Create `src/Ferret.Manual/DocPage.cs`:

```csharp
namespace Ferret.Manual;

/// <summary>Metadata for a single manual page.</summary>
public sealed record DocPage
{
    /// <summary>URL-safe slug, e.g. "getting-started/installation".</summary>
    public required string Slug { get; init; }

    /// <summary>Human-readable page title.</summary>
    public required string Title { get; init; }

    /// <summary>Top-level section, e.g. "Getting Started".</summary>
    public required string Section { get; init; }

    /// <summary>Sort order within section.</summary>
    public required int Order { get; init; }

    /// <summary>Assembly embedded resource name for the Markdown file.</summary>
    public required string ResourceName { get; init; }
}
```

- [ ] **Step 4: Create `DocRegistry.cs`**

Create `src/Ferret.Manual/DocRegistry.cs`:

```csharp
using System.Reflection;

namespace Ferret.Manual;

/// <summary>In-memory catalogue of all manual pages with resource-backed Markdown loading.</summary>
public static class DocRegistry
{
    private static readonly Assembly _assembly = typeof(DocRegistry).Assembly;
    private const string ResourcePrefix = "Ferret.Manual.Content.";

    public static IReadOnlyList<DocPage> AllPages { get; } = BuildCatalogue();

    public static DocPage? GetPage(string slug) =>
        AllPages.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public static string GetMarkdown(DocPage page)
    {
        using var stream = _assembly.GetManifestResourceStream(page.ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource not found: {page.ResourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Returns pages grouped by section in navigation order.</summary>
    public static IReadOnlyList<(string Section, IReadOnlyList<DocPage> Pages)> GetNavTree()
    {
        return AllPages
            .GroupBy(p => p.Section)
            .OrderBy(g => g.Min(p => p.Order))
            .Select(g => (g.Key, (IReadOnlyList<DocPage>)g.OrderBy(p => p.Order).ToList()))
            .ToList();
    }

    public static DocPage? GetPreviousPage(DocPage page)
    {
        var index = AllPages.IndexOf(page);
        return index > 0 ? AllPages[index - 1] : null;
    }

    public static DocPage? GetNextPage(DocPage page)
    {
        var index = AllPages.IndexOf(page);
        return index >= 0 && index < AllPages.Count - 1 ? AllPages[index + 1] : null;
    }

    private static IReadOnlyList<DocPage> BuildCatalogue() =>
    [
        // ── Getting Started ──────────────────────────────────────────────────
        P("getting-started/index",          "Getting Started",     "Getting Started", 100),
        P("getting-started/installation",   "Installation",        "Getting Started", 101),
        P("getting-started/first-workspace","First Workspace",     "Getting Started", 102),
        P("getting-started/first-index",    "First Index",         "Getting Started", 103),
        P("getting-started/first-search",   "First Search",        "Getting Started", 104),
        P("getting-started/connect-claude", "Connect Claude",      "Getting Started", 105),

        // ── User Guide ───────────────────────────────────────────────────────
        P("user-guide/index",       "User Guide",   "User Guide", 200),
        P("user-guide/workspace",   "Workspace",    "User Guide", 201),
        P("user-guide/connectors",  "Connectors",   "User Guide", 202),
        P("user-guide/parsers",     "Parsers",       "User Guide", 203),
        P("user-guide/indexing",    "Indexing",     "User Guide", 204),
        P("user-guide/search",      "Search",       "User Guide", 205),
        P("user-guide/context",     "Context",      "User Guide", 206),
        P("user-guide/ai",          "AI",           "User Guide", 207),
        P("user-guide/watch",       "Watch",        "User Guide", 208),

        // ── Reference ────────────────────────────────────────────────────────
        P("reference/index",         "Reference",     "Reference", 300),
        P("reference/cli",           "CLI Reference", "Reference", 301),
        P("reference/configuration", "Configuration", "Reference", 302),
        P("reference/mcp",           "MCP Reference", "Reference", 303),
        P("reference/architecture",  "Architecture",  "Reference", 304),

        // ── Architecture Explorer ────────────────────────────────────────────
        P("architecture/index",             "Architecture Explorer", "Architecture", 400),
        P("architecture/platform-overview", "Platform Overview",     "Architecture", 401),
        P("architecture/dependency-graph",  "Dependency Graph",      "Architecture", 402),
        P("architecture/storage",           "Storage",               "Architecture", 403),
        P("architecture/search-flow",       "Search Flow",           "Architecture", 404),
        P("architecture/ai-flow",           "AI Flow",               "Architecture", 405),
        P("architecture/context-assembly",  "Context Assembly",      "Architecture", 406),
        P("architecture/mcp-runtime",       "MCP Runtime",           "Architecture", 407),
        P("architecture/configuration",     "Configuration",         "Architecture", 408),
        P("architecture/extension-points",  "Extension Points",      "Architecture", 409),

        // ── Developer Guide ──────────────────────────────────────────────────
        P("developer-guide/index",               "Developer Guide",       "Developer Guide", 500),
        P("developer-guide/create-connector",    "Create a Connector",    "Developer Guide", 501),
        P("developer-guide/create-parser",       "Create a Parser",       "Developer Guide", 502),
        P("developer-guide/create-ai-provider",  "Create an AI Provider", "Developer Guide", 503),
        P("developer-guide/create-prompt",       "Create a Prompt",       "Developer Guide", 504),

        // ── Design Decisions ────────────────────────────────────────────────
        P("design/index",                "Design Decisions",       "Design Decisions", 550),
        P("design/why-sqlite",           "Why SQLite?",            "Design Decisions", 551),
        P("design/why-bm25",             "Why BM25 Before Vectors?","Design Decisions", 552),
        P("design/why-mcp",              "Why MCP Before REST?",   "Design Decisions", 553),
        P("design/why-providers",        "Why Providers?",         "Design Decisions", 554),
        P("design/why-context-assembly", "Why Context Assembly?",  "Design Decisions", 555),
        P("design/why-platform-first",   "Why Platform-First?",    "Design Decisions", 556),
        P("design/why-manual",           "Why Manual, Not Docs?",  "Design Decisions", 557),

        // ── Standalone pages ─────────────────────────────────────────────────
        P("troubleshooting", "Troubleshooting", "Troubleshooting", 600),
        P("faq",             "FAQ",             "FAQ",             700),
        P("release-notes",   "Release Notes",   "Release Notes",   800),
    ];

    private static DocPage P(string slug, string title, string section, int order)
    {
        // "getting-started/installation" → "Ferret.Manual.Content.getting-started.installation.md"
        var resourceSuffix = slug.Replace('/', '.') + ".md";
        return new DocPage
        {
            Slug         = slug,
            Title        = title,
            Section      = section,
            Order        = order,
            ResourceName = ResourcePrefix + resourceSuffix,
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

```
dotnet test tests/Ferret.Manual.Tests/ --filter "DocRegistryTests" -v
```

Expected: FAIL — project doesn't compile (no placeholder content files yet)

- [ ] **Step 6: Verify embedded resource names**

After creating placeholder content files:

```
dotnet build src/Ferret.Manual/Ferret.Manual.csproj -v detailed 2>&1 | Select-String "EmbeddedResource"
```

Expected: 41 resource entries

- [ ] **Step 7: Run tests to verify they pass**

```
dotnet test tests/Ferret.Manual.Tests/ --filter "DocRegistryTests" -v
```

Expected: PASS — 5 tests pass

- [ ] **Step 8: Commit**

```
git add src/Ferret.Manual/
git add tests/Ferret.Manual.Tests/
git commit -m "feat(sprint-14): DocPage + DocRegistry — 41-page manual catalogue with embedded resource loading"
```

---

### Task 3: `HtmlTemplate` — layout, CSS, and client-side search

**Files:**
- Create: `src/Ferret.Manual/HtmlTemplate.cs`

**Interfaces:**
- Produces: `HtmlTemplate.Render(DocPage page, string contentHtml, IReadOnlyList<DocPage> allPages, DocPage? prev = null, DocPage? next = null) → string`

- [ ] **Step 1: Download Lunr.js 2.3.9 minified source**

```powershell
$lunr = (Invoke-WebRequest -Uri "https://unpkg.com/lunr@2.3.9/lunr.min.js" -UseBasicParsing).Content
```

You will paste this content into `HtmlTemplate.cs` as the `LunrJs` constant.

- [ ] **Step 2: Create `HtmlTemplate.cs`**

Create `src/Ferret.Manual/HtmlTemplate.cs`:

```csharp
using System.Text;

namespace Ferret.Manual;

/// <summary>Renders complete HTML pages for the manual. All CSS and JS are inline — no external requests.</summary>
public static class HtmlTemplate
{
    private const string SourceBaseUrl =
        "https://github.com/indoulia/Ferret/src/master/src/Ferret.Manual/Content/";
    private const string IssueUrl =
        "https://github.com/indoulia/Ferret/issues/new";

    public static string Render(DocPage page, string contentHtml, IReadOnlyList<DocPage> allPages,
        DocPage? prev = null, DocPage? next = null)
    {
        var nav    = BuildNav(allPages, page.Slug);
        var search = BuildSearchData(allPages);
        var footer = BuildFooter(page, prev, next);
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{Escape(page.Title)} — The Ferret Manual</title>
              <style>{Css}</style>
            </head>
            <body>
              <div class="layout">
                <nav class="sidebar">
                  <div class="sidebar-header">
                    <a href="/manual" class="logo">🦡 The Ferret Manual</a>
                    <input type="search" id="search" placeholder="Search the manual…" aria-label="Search">
                  </div>
                  <div id="search-results" class="search-results" hidden></div>
                  {nav}
                </nav>
                <main class="content">
                  <article class="prose">
                    {contentHtml}
                  </article>
                  {footer}
                </main>
              </div>
              <script>{LunrJs}</script>
              <script>{SearchScript(search)}</script>
            </body>
            </html>
            """;
    }

    private static string BuildNav(IReadOnlyList<DocPage> allPages, string activeSlug)
    {
        var navTree = DocRegistry.GetNavTree();
        var sb = new StringBuilder();
        sb.AppendLine("<ul class=\"nav-tree\">");
        foreach (var (section, pages) in navTree)
        {
            sb.AppendLine("  <li class=\"nav-section\">");
            sb.AppendLine($"    <span class=\"nav-section-label\">{Escape(section)}</span>");
            sb.AppendLine("    <ul>");
            foreach (var p in pages)
            {
                var active = p.Slug == activeSlug ? " class=\"active\"" : "";
                sb.AppendLine($"      <li><a href=\"/manual/{p.Slug}\"{active}>{Escape(p.Title)}</a></li>");
            }
            sb.AppendLine("    </ul>");
            sb.AppendLine("  </li>");
        }
        sb.AppendLine("</ul>");
        return sb.ToString();
    }

    private static string BuildSearchData(IReadOnlyList<DocPage> allPages)
    {
        var entries = allPages.Select(p =>
            $"{{\"id\":\"{Escape(p.Slug)}\",\"title\":\"{Escape(p.Title)}\",\"section\":\"{Escape(p.Section)}\"}}");
        return "[" + string.Join(",", entries) + "]";
    }

    private static string BuildFooter(DocPage page, DocPage? prev, DocPage? next)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<footer class=\"page-footer\">");

        // Previous / Next navigation
        sb.AppendLine("  <nav class=\"page-nav\">");
        if (prev is not null)
            sb.AppendLine($"    <a class=\"prev-page\" href=\"/manual/{prev.Slug}\">← {Escape(prev.Title)}</a>");
        else
            sb.AppendLine("    <span class=\"prev-page placeholder\"></span>");
        if (next is not null)
            sb.AppendLine($"    <a class=\"next-page\" href=\"/manual/{next.Slug}\">{Escape(next.Title)} →</a>");
        else
            sb.AppendLine("    <span class=\"next-page placeholder\"></span>");
        sb.AppendLine("  </nav>");

        // Edit / Report actions
        var editUrl = $"{SourceBaseUrl}{page.Slug.Replace('/', '.')}.md";
        sb.AppendLine("  <div class=\"page-actions\">");
        sb.AppendLine($"    <a href=\"{editUrl}\" target=\"_blank\" rel=\"noopener\">Edit source</a>");
        sb.AppendLine($"    <span class=\"sep\">·</span>");
        sb.AppendLine($"    <a href=\"{IssueUrl}\" target=\"_blank\" rel=\"noopener\">Report issue</a>");
        sb.AppendLine($"    <span class=\"sep\">·</span>");
        sb.AppendLine("    <a href=\"/manual/architecture/index\">Architecture</a>");
        sb.AppendLine($"    <span class=\"sep\">·</span>");
        sb.AppendLine("    <a href=\"/manual/reference/cli\">CLI Reference</a>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <div class=\"page-footer-brand\">The Ferret Manual · RC1</div>");
        sb.AppendLine("</footer>");
        return sb.ToString();
    }

    private static string SearchScript(string pagesJson) => $$"""
        (function() {
          var pages = {{pagesJson}};
          var idx = lunr(function() {
            this.ref('id');
            this.field('title', { boost: 10 });
            this.field('section');
            pages.forEach(function(p) { this.add(p); }, this);
          });
          var pagesMap = {};
          pages.forEach(function(p) { pagesMap[p.id] = p; });
          var input = document.getElementById('search');
          var box   = document.getElementById('search-results');
          input.addEventListener('input', function() {
            var q = input.value.trim();
            box.hidden = !q;
            if (!q) return;
            var results = idx.search(q + '*');
            if (!results.length) {
              box.innerHTML = '<div class="no-results">No results for "' + q + '"</div>';
              return;
            }
            box.innerHTML = results.slice(0, 8).map(function(r) {
              var p = pagesMap[r.ref];
              return '<a href="/manual/' + p.id + '" class="search-hit">'
                + '<span class="hit-section">' + p.section + '</span>'
                + '<span class="hit-title">'   + p.title   + '</span></a>';
            }).join('');
          });
          document.addEventListener('click', function(e) {
            if (!e.target.closest('#search') && !e.target.closest('#search-results'))
              box.hidden = true;
          });
        })();
        """;

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private const string Css = """
        *,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
        :root{
          --sidebar-bg:#1e1e2e;--sidebar-fg:#cdd6f4;--sidebar-dim:#6c7086;
          --active-bg:#313244;--active-fg:#89b4fa;--accent:#89b4fa;
          --bg:#ffffff;--fg:#1e1e2e;--code-bg:#f5f5f5;--border:#e0e0e0;
          --sidebar-w:280px;
        }
        body{font-family:system-ui,-apple-system,sans-serif;font-size:16px;
             line-height:1.6;color:var(--fg);background:var(--bg)}
        .layout{display:flex;min-height:100vh}
        .sidebar{width:var(--sidebar-w);min-height:100vh;background:var(--sidebar-bg);
                 color:var(--sidebar-fg);display:flex;flex-direction:column;
                 position:sticky;top:0;height:100vh;overflow-y:auto;flex-shrink:0}
        .sidebar-header{padding:1.5rem 1rem 1rem;border-bottom:1px solid #313244}
        .logo{color:var(--sidebar-fg);text-decoration:none;font-size:1.1rem;
              font-weight:600;display:block;margin-bottom:1rem}
        #search{width:100%;padding:.5rem .75rem;background:#313244;border:1px solid #45475a;
                border-radius:6px;color:var(--sidebar-fg);font-size:.9rem;outline:none}
        #search:focus{border-color:var(--accent)}
        .search-results{background:#181825;border:1px solid #45475a;border-radius:6px;
                        margin:.5rem 1rem}
        .search-hit{display:flex;flex-direction:column;padding:.6rem 1rem;text-decoration:none;
                    color:var(--sidebar-fg);border-bottom:1px solid #313244}
        .search-hit:hover{background:var(--active-bg)}
        .hit-section{font-size:.75rem;color:var(--sidebar-dim);text-transform:uppercase;
                     letter-spacing:.05em}
        .hit-title{font-size:.9rem}
        .no-results{padding:.75rem 1rem;color:var(--sidebar-dim);font-size:.9rem}
        .nav-tree{list-style:none;padding:1rem 0}
        .nav-section{margin-bottom:.5rem}
        .nav-section-label{display:block;padding:.35rem 1rem;font-size:.7rem;font-weight:700;
                           text-transform:uppercase;letter-spacing:.1em;color:var(--sidebar-dim)}
        .nav-section ul{list-style:none}
        .nav-section ul li a{display:block;padding:.35rem 1rem .35rem 1.75rem;color:var(--sidebar-fg);
                             text-decoration:none;font-size:.9rem;border-left:2px solid transparent;
                             transition:background .1s}
        .nav-section ul li a:hover{background:var(--active-bg)}
        .nav-section ul li a.active{background:var(--active-bg);color:var(--active-fg);
                                    border-left-color:var(--accent);font-weight:500}
        .content{flex:1;min-width:0;display:flex;flex-direction:column}
        .prose{max-width:860px;padding:3rem 4rem;flex:1}
        .prose h1{font-size:2rem;font-weight:700;margin-bottom:.5rem}
        .prose h2{font-size:1.4rem;font-weight:600;margin:2.5rem 0 .75rem;
                  padding-bottom:.4rem;border-bottom:1px solid var(--border)}
        .prose h3{font-size:1.1rem;font-weight:600;margin:1.75rem 0 .5rem}
        .prose p{margin-bottom:1rem}
        .prose ul,.prose ol{margin:.5rem 0 1rem 1.5rem}
        .prose li{margin-bottom:.25rem}
        .prose a{color:var(--accent)}
        .prose pre{background:var(--code-bg);border:1px solid var(--border);border-radius:6px;
                   padding:1.25rem;overflow-x:auto;margin:1rem 0;font-size:.875rem}
        .prose code{background:var(--code-bg);border-radius:3px;padding:.15em .35em;
                    font-size:.875em;font-family:'Cascadia Code','Fira Code',monospace}
        .prose pre code{background:none;padding:0;border-radius:0}
        .prose blockquote{border-left:3px solid var(--accent);padding-left:1rem;
                          color:#555;margin:1rem 0}
        .prose table{width:100%;border-collapse:collapse;margin:1rem 0;font-size:.9rem}
        .prose th{background:var(--code-bg);padding:.6rem 1rem;text-align:left;
                  border:1px solid var(--border);font-weight:600}
        .prose td{padding:.6rem 1rem;border:1px solid var(--border)}
        .prose tr:nth-child(even) td{background:#fafafa}
        .page-footer{padding:1.5rem 4rem 2rem;border-top:1px solid var(--border)}
        .page-nav{display:flex;justify-content:space-between;margin-bottom:1.25rem}
        .page-nav a{color:var(--accent);text-decoration:none;font-size:.9rem;font-weight:500}
        .page-nav a:hover{text-decoration:underline}
        .page-nav .placeholder{flex:1}
        .page-nav .next-page{text-align:right}
        .page-actions{font-size:.8rem;color:#888;margin-bottom:.5rem}
        .page-actions a{color:#888}
        .page-actions a:hover{color:var(--accent)}
        .page-actions .sep{margin:0 .4rem;color:#bbb}
        .page-footer-brand{font-size:.75rem;color:#bbb;margin-top:.35rem}
        @media(max-width:768px){.sidebar{display:none}.prose{padding:1.5rem}.page-footer{padding:1.5rem}}
        """;

    // Paste full Lunr.js 2.3.9 minified source here.
    // Download: Invoke-WebRequest "https://unpkg.com/lunr@2.3.9/lunr.min.js" | Select-Object -ExpandProperty Content
    private const string LunrJs = "/* lunr.js 2.3.9 — paste full minified source here */";
}
```

**After creating the file, replace the `LunrJs` placeholder** with the actual Lunr.js 2.3.9 minified source downloaded in Step 1.

- [ ] **Step 3: Build to verify**

```
dotnet build src/Ferret.Manual/Ferret.Manual.csproj
```

Expected: Build succeeded

- [ ] **Step 4: Commit**

```
git add src/Ferret.Manual/HtmlTemplate.cs
git commit -m "feat(sprint-14): HtmlTemplate — Catppuccin dark sidebar, inline CSS, Lunr.js client-side search"
```

---

### Task 4: `ManualServer` — HttpListener request handler

**Files:**
- Create: `src/Ferret.Manual/ManualServer.cs`
- Test: `tests/Ferret.Manual.Tests/ManualServerTests.cs`

**Interfaces:**
- Produces: `ManualServer(int port = 7070)`, `Task StartAsync(CancellationToken ct)`, `string BaseUrl`, `IDisposable`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Manual.Tests/ManualServerTests.cs`:

```csharp
using System.Net.Http;

namespace Ferret.Manual.Tests;

public sealed class ManualServerTests : IAsyncDisposable
{
    private readonly ManualServer _server;
    private readonly HttpClient _client;
    private readonly CancellationTokenSource _cts;

    public ManualServerTests()
    {
        _server = new ManualServer(17070); // non-default port avoids conflicts
        _client = new HttpClient { BaseAddress = new Uri("http://localhost:17070") };
        _cts    = new CancellationTokenSource();
        _ = _server.StartAsync(_cts.Token);
        Thread.Sleep(200); // allow listener to bind
    }

    [Fact]
    public async Task Get_Root_Redirects_To_Manual()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:17070") };
        var response = await client.GetAsync("/");
        Assert.Equal(302, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_Manual_Returns_Html()
    {
        var response = await _client.GetAsync("/manual");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<html", body);
        Assert.Contains("Ferret Manual", body);
    }

    [Fact]
    public async Task Get_KnownPage_Returns_Html()
    {
        var response = await _client.GetAsync("/manual/getting-started/installation");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Installation", body);
    }

    [Fact]
    public async Task Get_UnknownPage_Returns_404()
    {
        var response = await _client.GetAsync("/manual/nonexistent-page");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SearchApi_Returns_Json()
    {
        var response = await _client.GetAsync("/manual/search?q=install");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("[", body.Trim());
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _client.Dispose();
        _server.Dispose();
        await ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Manual.Tests/ --filter "ManualServerTests" -v
```

Expected: FAIL — type not found

- [ ] **Step 3: Implement `ManualServer`**

Create `src/Ferret.Manual/ManualServer.cs`:

```csharp
using Markdig;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Ferret.Manual;

public sealed class ManualServer : IDisposable
{
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private readonly HttpListener _listener;
    private readonly int _port;
    private bool _disposed;

    public string BaseUrl => $"http://localhost:{_port}/manual";

    public ManualServer(int port = 7070)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _listener.Start();
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            _ = Task.Run(() => HandleAsync(ctx), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var query = ctx.Request.Url?.Query ?? "";

            if (path == "/" || path == "/manual" || path == "/manual/")
            {
                Redirect(ctx, "/manual/getting-started/index");
                return;
            }

            if (path.StartsWith("/manual/search", StringComparison.OrdinalIgnoreCase))
            {
                await ServeSearchAsync(ctx, query).ConfigureAwait(false);
                return;
            }

            if (path.StartsWith("/manual/", StringComparison.OrdinalIgnoreCase))
            {
                var slug = path["/manual/".Length..].TrimEnd('/');
                await ServePageAsync(ctx, slug).ConfigureAwait(false);
                return;
            }

            Respond(ctx, 404, "text/plain", "Not found");
        }
        catch (Exception ex)
        {
            try { Respond(ctx, 500, "text/plain", $"Error: {ex.Message}"); }
            catch { /* listener closed */ }
        }
    }

    private static async Task ServePageAsync(HttpListenerContext ctx, string slug)
    {
        var page = DocRegistry.GetPage(slug);
        if (page is null)
        {
            Respond(ctx, 404, "text/html",
                "<html><body><h1>404 — Page not found</h1><p><a href=\"/manual\">Back to manual</a></p></body></html>");
            return;
        }
        var markdown    = DocRegistry.GetMarkdown(page);
        var contentHtml = Markdown.ToHtml(markdown, _pipeline);
        var prev        = DocRegistry.GetPreviousPage(page);
        var next        = DocRegistry.GetNextPage(page);
        var html        = HtmlTemplate.Render(page, contentHtml, DocRegistry.AllPages, prev, next);
        Respond(ctx, 200, "text/html; charset=utf-8", html);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task ServeSearchAsync(HttpListenerContext ctx, string rawQuery)
    {
        var q = System.Web.HttpUtility.ParseQueryString(rawQuery)["q"] ?? "";
        var results = DocRegistry.AllPages
            .Where(p => p.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || p.Section.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(p => new { slug = p.Slug, title = p.Title, section = p.Section });
        var json = JsonSerializer.Serialize(results);
        Respond(ctx, 200, "application/json", json);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static void Redirect(HttpListenerContext ctx, string location)
    {
        ctx.Response.StatusCode = 302;
        ctx.Response.Headers["Location"] = location;
        ctx.Response.Close();
    }

    private static void Respond(HttpListenerContext ctx, int statusCode, string contentType, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode      = statusCode;
        ctx.Response.ContentType     = contentType;
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes);
        ctx.Response.Close();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _listener.Stop(); } catch { /* already stopped */ }
        _listener.Close();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Manual.Tests/ --filter "ManualServerTests" -v
```

Expected: PASS — 5 tests pass

- [ ] **Step 5: Commit**

```
git add src/Ferret.Manual/ManualServer.cs
git add tests/Ferret.Manual.Tests/ManualServerTests.cs
git commit -m "feat(sprint-14): ManualServer — HttpListener with page serve, search API, redirect"
```

---

### Task 5: Write all 33 content pages

**Files:** All files under `src/Ferret.Manual/Content/`

Write real, useful content for every page. These become the canonical user-facing manual for RC1.

**Style guide:**
- First H1 is the page title
- 2–3 sentence intro paragraph
- `## ` H2 for major sections; `### ` H3 for subsections
- Fenced code blocks with language specifiers: ` ```bash `, ` ```json `, ` ```csharp `
- `> **Note:**` blockquotes for tips and warnings
- Every page ends with `## Related` linking to 2–3 related pages

---

#### Getting Started (6 pages)

- [ ] **Write `Content/getting-started/index.md`**

```markdown
# Getting Started

Ferret indexes your codebase and documentation and makes it available to AI assistants
like Claude via MCP. This guide takes you from zero to a working Claude integration
in under five minutes.

## Steps

1. [Install Ferret](installation) — download the binary for your platform
2. [Create a workspace](first-workspace) — run `ferret init` in your project directory
3. [Index your code](first-index) — run `ferret index` to build the search index
4. [Search your workspace](first-search) — run `ferret search` to verify results
5. [Connect Claude](connect-claude) — wire up Claude Desktop or Cursor via MCP

## Prerequisites

- Windows 10/11, macOS 12+, or Linux (Ubuntu 20.04+)
- No .NET SDK required — Ferret ships as a self-contained binary

## Related

- [User Guide](../user-guide/index) — deeper coverage of every capability
- [CLI Reference](../reference/cli) — every command, flag, and exit code
```

- [ ] **Write `Content/getting-started/installation.md`**

```markdown
# Installation

Ferret ships as a single self-contained binary. No .NET SDK, no runtime, no dependencies.

## Windows

Run `publish.ps1` from the repo root to build locally:

```powershell
.\publish.ps1 -Rid win-x64
```

Copy `artifacts\win-x64\ferret.exe` to a directory on your PATH (e.g. `C:\tools\`).

## macOS

```bash
# Apple Silicon (M1/M2/M3)
chmod +x ferret-osx-arm64
sudo mv ferret-osx-arm64 /usr/local/bin/ferret

# Intel
chmod +x ferret-osx-x64
sudo mv ferret-osx-x64 /usr/local/bin/ferret
```

## Linux

```bash
chmod +x ferret-linux-x64
sudo mv ferret-linux-x64 /usr/local/bin/ferret
```

## Verify

```bash
ferret --version
# ferret 0.14.0
```

> **Note:** If `ferret` is not found after installation, ensure the directory is on your PATH
> and restart your terminal.

## Related

- [First Workspace](first-workspace) — initialise your first workspace
- [Troubleshooting](../troubleshooting) — installation errors
```

- [ ] **Write `Content/getting-started/first-workspace.md`**

```markdown
# First Workspace

A workspace is a directory Ferret indexes and monitors. Initialise one with `ferret init`.

## Initialise

```bash
cd /path/to/my-project
ferret init
```

Creates:

```
my-project/
└── .ferret/
    ├── workspace.json    workspace configuration
    └── state.json        index state (auto-managed)
```

## Configure

Edit `.ferret/workspace.json` to add connector options:

```json
{
  "workspaceId": "my-project",
  "connectors": [
    {
      "type": "filesystem",
      "instanceId": "default",
      "root": ".",
      "include": ["**/*.cs", "**/*.md", "**/*.json"],
      "exclude": ["**/bin/**", "**/obj/**", "**/node_modules/**"]
    }
  ]
}
```

## .ferretignore

Create `.ferretignore` at the project root (same syntax as `.gitignore`):

```
bin/
obj/
*.generated.cs
*.designer.cs
```

## Related

- [First Index](first-index) — index your workspace
- [Configuration Reference](../reference/configuration) — full config schema
```

- [ ] **Write `Content/getting-started/first-index.md`**

```markdown
# First Index

Run `ferret index` from your workspace root:

```bash
ferret index
```

## What happens

1. **Discover** — connector walks the directory tree
2. **Filter** — `.ferretignore` and `exclude` patterns applied
3. **Parse** — each file converted to searchable text
4. **Index** — content written to the SQLite FTS5 index

## Sample output

```
Indexing workspace: my-project
  Connectors: filesystem (default)
  Discovered:  1,247 assets
  Indexed:     1,231 documents
  Skipped:        16
  Failures:        0
  Duration:     4.2s
Index complete.
```

## Incremental re-index

After the first full index, subsequent runs only re-index changed files:

```bash
ferret index
# Discovered: 1,247  Indexed: 3  Skipped: 1,244  Duration: 0.3s
```

## Force full rebuild

```bash
ferret index --rebuild
```

## Related

- [First Search](first-search) — search the indexed workspace
- [Indexing](../user-guide/indexing) — incremental indexing, watching
```

- [ ] **Write `Content/getting-started/first-search.md`**

```markdown
# First Search

Search your indexed workspace:

```bash
ferret search "IIndexPipeline"
```

## Sample output

```
Results for "IIndexPipeline" (4 found, 120ms)

1. src/Ferret.Core/Indexing/IIndexPipeline.cs           score: 0.94
   Orchestrates a complete discover → parse → index pipeline run.

2. src/Ferret.Indexing/IndexPipeline.cs                 score: 0.87
   public sealed class IndexPipeline : IIndexPipeline

3. tests/Ferret.Indexing.Tests/IndexPipelineTests.cs    score: 0.71
   [Fact] public async Task RunAsync_Returns_Correct_Counts()
```

## Useful flags

| Flag | Effect |
|---|---|
| `--top N` | Return at most N results (default: 10) |
| `--json` | Output as JSON (for scripting) |

## Related

- [Connect Claude](connect-claude) — use Ferret from within Claude
- [Search](../user-guide/search) — ranking, advanced queries
- [MCP Reference](../reference/mcp) — `ferret_search` MCP tool
```

- [ ] **Write `Content/getting-started/connect-claude.md`**

```markdown
# Connect Claude

Ferret exposes your workspace to Claude Desktop and Cursor via MCP (Model Context Protocol).

## Start the MCP server

```bash
ferret serve
```

Output:
```
Ferret MCP server running.
Tools: ferret_search, ferret_read_document, ferret_context, ferret_workspace_status
```

## Claude Desktop

Open the config file:
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

Add:

```json
{
  "mcpServers": {
    "ferret": {
      "command": "ferret",
      "args": ["serve"],
      "cwd": "/absolute/path/to/your/project"
    }
  }
}
```

Restart Claude Desktop. Ferret will appear in the tools panel.

## Cursor

In Cursor → Settings → MCP → Add server:

```json
{
  "name": "ferret",
  "command": "ferret serve",
  "workingDirectory": "/absolute/path/to/your/project"
}
```

## Test

In Claude, type: **"Search my codebase for IIndexPipeline"**

Claude will call `ferret_search` and return grounded results from your workspace.

## Related

- [MCP Reference](../reference/mcp) — all MCP tools and schemas
- [Context Assembly](../architecture/context-assembly) — how context is built
- [Troubleshooting](../troubleshooting) — MCP connection errors
```

---

#### Architecture Explorer (10 pages)

- [ ] **Write `Content/architecture/index.md`** — overview linking all 9 subsection pages

- [ ] **Write `Content/architecture/platform-overview.md`** — platform layer stack with full ASCII diagram (see Task 4b outline above for the diagram — use that diagram exactly)

- [ ] **Write `Content/architecture/search-flow.md`** — query → BM25 → result flow diagram (see Task 4b outline above)

- [ ] **Write `Content/architecture/context-assembly.md`** — 6-step context pipeline diagram (see Task 4b outline above)

- [ ] **Write `Content/architecture/dependency-graph.md`** — ASCII package dependency diagram showing which packages reference which

- [ ] **Write `Content/architecture/storage.md`** — `.ferret/` directory tree, SQLite `documents` table schema, `documents_fts` FTS5 virtual table, `index-state.json` format

- [ ] **Write `Content/architecture/ai-flow.md`** — `IModelRouter` → provider selection → `IAiProvider.CompleteAsync` → response flow

- [ ] **Write `Content/architecture/mcp-runtime.md`** — MCP server startup, `IMcpTool` registration, stdio request/response lifecycle, tool dispatch

- [ ] **Write `Content/architecture/configuration.md`** — config layering: `ferret.config.json` → environment variables → defaults; `IConfiguration` binding; `PostConfigure` env var overrides

- [ ] **Write `Content/architecture/extension-points.md`** — the four extension interfaces (`IConnector`/`IAssetSource`/`IAssetReader`, `IParser`, `IAiProvider`, `IPromptTemplate`) with minimal working examples for each

---

#### Remaining pages

- [ ] **Write all User Guide pages** (`user-guide/index`, `workspace`, `connectors`, `parsers`, `indexing`, `search`, `context`, `ai`, `watch`) — follow style guide; each page 1–2 pages of real content

- [ ] **Write `reference/cli.md`** — every command (`init`, `index`, `search`, `serve`, `watch`, `doctor`, `config`, `manual`, `models`, `prompt`) in a consistent format: usage, flags, exit codes, one example

- [ ] **Write `reference/configuration.md`** — full `ferret.config.json` schema as a table (field, type, default, description) + all environment variable overrides

- [ ] **Write `reference/mcp.md`** — all 4 MCP tools (`ferret_search`, `ferret_read_document`, `ferret_context`, `ferret_workspace_status`) with JSON input/output schemas and one example each

- [ ] **Write `reference/architecture.md`** — ADR index: list all ADRs in `docs/adr/` with their number, title, status, and one-line decision

- [ ] **Write all Developer Guide pages** (`developer-guide/index`, `create-connector`, `create-parser`, `create-ai-provider`, `create-prompt`) — each with a working 30-line C# example

- [ ] **Write `troubleshooting.md`** — top 10 errors, each with: symptom, `ferret doctor` output if applicable, root cause, fix command

- [ ] **Write `faq.md`** — 15 questions and answers covering: "What file types does Ferret index?", "How is Ferret different from GitHub Copilot?", "Does Ferret send my code to the cloud?", and 12 more

- [ ] **Write `release-notes.md`** — version history from Sprint 8 to Sprint 14 (RC1), matching the CHANGELOG.md in S9

---

#### Design Decisions (8 pages)

These are not ADRs. They are human-readable explanations of *why* Ferret is designed the way it is. The audience is contributors and curious users. Tone: direct, honest, historical. Acknowledge trade-offs.

- [ ] **Write `Content/design/index.md`**

```markdown
# Design Decisions

Every significant choice in Ferret has a reason. This section explains the *why* behind the major design decisions — not as formal ADRs, but as readable explanations of the thinking, the trade-offs, and what we decided against.

These pages exist because architecture without rationale is just code. Understanding why things are the way they are makes it easier to extend, adapt, and challenge them.

## Decisions

- [Why SQLite?](why-sqlite) — why a file-based database, not a server
- [Why BM25 Before Vectors?](why-bm25) — why keyword search ships first
- [Why MCP Before REST?](why-mcp) — why we target AI tools, not HTTP clients
- [Why Providers?](why-providers) — why the AI provider abstraction exists
- [Why Context Assembly?](why-context-assembly) — why search results are not enough
- [Why Platform-First?](why-platform-first) — why we build foundations before features
- [Why Manual, Not Docs?](why-manual) — why this is called The Ferret Manual

## Related

- [Architecture Explorer](../architecture/index) — how the pieces fit together
- [Developer Guide](../developer-guide/index) — how to extend the platform
```

- [ ] **Write `Content/design/why-sqlite.md`**

```markdown
# Why SQLite?

Ferret uses SQLite as its index store. This was not the default choice — it was a deliberate decision after evaluating several alternatives.

## What we considered

- **Embedded key-value stores** (LevelDB, RocksDB): fast, but no full-text search without a separate layer
- **Server-based databases** (PostgreSQL, MySQL): powerful, but require a running server — a non-starter for a local CLI tool
- **In-memory indexes** (Lucene, Elasticsearch): excellent search, but heavyweight and require JVM or a separate process
- **SQLite + FTS5**: embedded, zero-dependency, ACID, and ships with a production-quality full-text search engine built in

## Why SQLite won

**Zero deployment cost.** SQLite is a single file. Every workspace gets its own `.ferret/indexes/keyword/keyword-index.db`. No service to start, no port to manage, no credentials to configure.

**FTS5 is genuinely good.** SQLite's FTS5 extension supports BM25 ranking, prefix queries, phrase matching, and column weighting. It is not a toy.

**Single-file durability.** The entire index is one file. Backup means `cp`. Migration means `rm` and re-index. Recovery is trivial.

**Transactional correctness.** Every batch write is a transaction. Interrupted indexing leaves the database consistent, not corrupted.

## What we gave up

- **Distributed scale**: SQLite is single-writer. For a local developer tool, this is irrelevant.
- **Advanced vector search**: FTS5 has no native vector similarity. Sprint 16 will add hybrid search via a separate vector store.

## Related

- [Storage Architecture](../architecture/storage) — the SQLite schema
- [Why BM25 Before Vectors?](why-bm25) — why vectors come later
```

- [ ] **Write `Content/design/why-bm25.md`**

```markdown
# Why BM25 Before Vectors?

Ferret RC1 ships with BM25 keyword search only. No embeddings, no semantic similarity, no vector database. This was intentional.

## BM25 is underrated

BM25 (Best Match 25) is a probabilistic keyword ranking function that has been the backbone of production search engines for 30 years. When you search for `IIndexPipeline`, BM25 finds it. When you search for `ferret index --rebuild`, BM25 finds it.

For code search, BM25 is excellent. Code has high identifier density. Identifiers are exact or near-exact. BM25's term frequency weighting rewards documents that use your search terms heavily — which is exactly what you want when searching for a class name or a CLI flag.

## Why vectors are not in RC1

**Vectors require an embedding model.** Every document must be embedded before indexing. This means either bundling a model (large binary, licensing concerns) or calling an external API (requires credentials, network, cost).

**Vectors are opaque.** BM25 results are explainable: "this document ranks high because it contains 'IIndexPipeline' 4 times." Vector results are not: "this document ranks high because its embedding is similar in 768 dimensions." Debugging is hard.

**Vectors are not always better.** For exact identifier lookup, BM25 outperforms semantic search. Semantic search excels for natural-language questions ("how does Ferret handle file deletions?") — and that use case belongs in the Context Assembly layer, not the raw search layer.

## The plan

Sprint 16 will add hybrid search: BM25 + vector similarity, combined with Reciprocal Rank Fusion. The SQLite index store will be joined by a second vector store. Keyword search results will improve with semantic re-ranking.

RC1 ships BM25 because it works, it's fast, and it requires zero external dependencies.

## Related

- [Why SQLite?](why-sqlite) — the index store choice
- [Search Architecture](../architecture/search-flow) — the search flow
```

- [ ] **Write `Content/design/why-mcp.md`**

```markdown
# Why MCP Before REST?

Ferret's primary integration surface is the Model Context Protocol (MCP), not a REST API. We built `ferret serve` before we built any HTTP endpoint. This surprised some early reviewers.

## MCP is where AI assistants already live

Claude Desktop, Cursor, and VS Code with GitHub Copilot all speak MCP natively. When you add Ferret as an MCP server, your AI assistant can immediately call `ferret_search` and `ferret_context` without any glue code, API client, or authentication token.

A REST API would require a second integration step: someone has to write a plugin, an extension, or an AI tool wrapper that bridges HTTP to the AI assistant's tool protocol. MCP eliminates that step.

## REST is for humans (and machines that aren't AI)

REST APIs are excellent for human-operated workflows: CI scripts, dashboards, integrations with other services. They are the right choice when the consumer is deterministic and needs structured data.

AI assistants are not REST clients. They use tool-calling protocols where the AI decides which tool to call and what arguments to pass. MCP is that protocol. REST is not.

## What we gave up

Building REST first would have given us a simpler server implementation and a more testable interface. We addressed this by designing `IMcpTool` as a clean interface and testing tools as units before wiring up the MCP runtime.

REST will come in a post-RC1 sprint for users who need programmatic non-AI access.

## Related

- [MCP Runtime Architecture](../architecture/mcp-runtime) — how the server works
- [MCP Reference](../reference/mcp) — all MCP tools
```

- [ ] **Write `Content/design/why-providers.md`**

```markdown
# Why Providers?

Ferret abstracts AI model access behind `IAiProvider`. You configure a provider in `ferret.config.json`; the rest of the system never knows which one is running.

## The problem providers solve

AI model APIs are not stable. OpenAI changes pricing. Ollama changes its HTTP API. New providers appear. Users have different needs: some want local-only (Ollama), some want cloud (OpenAI), some will want Anthropic directly.

If we hardcoded OpenAI calls throughout the codebase, every model change would require code changes. If we hardcoded Ollama, enterprise users couldn't use their existing API access.

The provider abstraction means the switching cost is one config file change.

## The design

`IAiProvider` has one meaningful method: `CompleteAsync(CompletionRequest, CancellationToken) → CompletionResponse`. Every provider implements this contract. `IModelRouter` selects the right provider and model for each request based on the configuration.

`Ferret.Providers.Ollama` implements `IAiProvider` using Ollama's HTTP API.
`Ferret.Providers.OpenAI` implements `IAiProvider` using the OpenAI-compatible API.

Adding a new provider is a new package, a new implementation, and a DI registration. Nothing else changes.

## What we decided against

- **Separate provider per use case**: routing by task type (summarisation vs. classification vs. embedding) is desirable but adds complexity. Sprint 12 ships one provider per workspace, which covers RC1 needs.
- **Auto-discovery**: dynamically loading provider DLLs would add startup complexity. Explicit registration is simpler and safer.

## Related

- [AI Flow Architecture](../architecture/ai-flow) — the provider chain
- [Configuration Reference](../reference/configuration) — provider config
```

- [ ] **Write `Content/design/why-context-assembly.md`**

```markdown
# Why Context Assembly?

Context Assembly is the pipeline that transforms search results into a `ContextPackage` suitable for an AI prompt. It is not search. It is a separate, ordered pipeline.

## Search results ≠ context

When an AI assistant asks Ferret for context about "how does file watching work?", a raw keyword search returns the top-10 BM25 matches. That is not yet context. Context requires:

1. **Deduplication** — two results from the same file should not both appear
2. **Expansion** — a function definition is more useful with its callers
3. **Content filtering** — binary files, generated code, and test fixtures are usually noise
4. **Token budgeting** — the context must fit within the AI model's context window

Without this pipeline, every AI-assisted query would either overflow the context window (too much) or return meaningless fragments (too little).

## Why a pipeline, not a function

The stages are independently composable, testable, and replaceable. A user can configure a `ContentFilter` to exclude `*.generated.cs`. A team can add a custom `Expander` that includes related ADRs. The pipeline model makes this extensible without touching the search layer.

Each stage in `Ferret.Core.Context` is an `IContextStage` with a single method: `ProcessAsync(ContextPackage, CancellationToken)`. The pipeline runs them in order.

## What it costs

Context Assembly adds latency. For most queries on a 1,000-document workspace, the pipeline runs in under 100ms. For large workspaces with aggressive expansion, it can take several hundred milliseconds. The token budget stage short-circuits early when the limit is reached.

## Related

- [Context Assembly Architecture](../architecture/context-assembly) — the pipeline diagram
- [MCP Reference](../reference/mcp) — how `ferret_context` triggers the pipeline
```

- [ ] **Write `Content/design/why-platform-first.md`**

```markdown
# Why Platform-First?

Ferret spent Sprints 1–12 building a platform before shipping significant user-facing features. The first `ferret search` command did not appear until Sprint 10. This was intentional.

## The platform compounds

Every feature built on a stable platform costs less than one built without one. The connector platform (Sprint 8) means adding a new file type is one `IParser` implementation. The provider platform (Sprint 12) means switching AI models is one config change.

A platform is not overhead. It is deferred feature cost paid upfront.

## The alternative was fragility

We evaluated an "implement features first, extract platform later" approach. The risk: every feature built without abstractions becomes a dependency that must be unentangled when the platform arrives. We have seen this pattern cause 2-3x rework cost in previous projects.

Ferret's architecture is frozen at v1.0 (ADR-0012). This means contributors know exactly which interfaces to implement, which boundaries to respect, and which changes require an ADR.

## What we gave up

Platform-first means later user-facing features. Ferret could not be used by real users until Sprint 10. For a developer tool this is acceptable; for a consumer product it would not be.

The trade-off is: slow start, fast execution. Every post-RC1 sprint delivers user value on a foundation that does not need to be rebuilt.

## Related

- [Architecture Explorer](../architecture/index) — the resulting platform
- [Extension Points](../architecture/extension-points) — what the platform exposes
```

- [ ] **Write `Content/design/why-manual.md`**

```markdown
# Why Manual, Not Docs?

This application is called *The Ferret Manual*, not *The Ferret Docs* or *Ferret Documentation*. The distinction is intentional.

## The word "docs" is overloaded

"Docs" has come to mean API reference: generated class listings, method signatures, parameter tables. It is what most projects ship because it is easy to generate from code comments.

Ferret does not need API documentation for end users. End users do not call `DocRegistry.GetPage()`. They run `ferret search`. What they need is a manual.

## A manual teaches

A manual explains how to accomplish goals. It has a Getting Started section that takes you from zero to working in five minutes. It has a User Guide that covers daily workflows. It has a Design Decisions section (this one) that teaches the *why*, not just the *what*.

The Rust Book is a manual. The Kubernetes documentation is (mostly) a manual. Microsoft Learn is a manual. These are the references we modelled this on.

## The manual is also a dogfooding opportunity

After RC1, Ferret will index its own manual. Searching for "how does context assembly work?" will return results from `architecture/context-assembly.md`. The documentation becomes part of the knowledge base.

This is only possible because the manual is authored in Markdown and embedded in the binary — not generated from code, not hosted externally.

## Related

- [Getting Started](../getting-started/index) — the manual's starting point
- [Architecture Explorer](../architecture/index) — the platform behind the manual
```

---

- [ ] **Commit all content**

```
git add src/Ferret.Manual/Content/
git commit -m "feat(sprint-14): Ferret Manual — 41 content pages, Getting Started, Architecture Explorer, Reference, Developer Guide, Design Decisions"
```

---

### Task 6: `ManualCommandHandler` + `ManualCliModule` — `ferret manual` command

**Files:**
- Create: `src/Ferret.Manual/ManualCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Manual/ManualCliModule.cs`
- Modify: `src/Ferret.Cli/Program.cs`

- [ ] **Step 1: Read `src/Ferret.Cli/Commands/Serve/ServeCliModule.cs`**

Read the file to understand the exact `CliModuleBase` + `ICommandHandler` pattern used. Note how the `--port` option is defined if one exists.

- [ ] **Step 2: Create `ManualCommandHandler.cs`**

Create `src/Ferret.Manual/ManualCommandHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ferret.Manual;

public sealed class ManualCommandHandler
{
    private readonly ILogger<ManualCommandHandler> _logger;

    public ManualCommandHandler(ILogger<ManualCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<int> HandleAsync(int port = 7070, CancellationToken ct = default)
    {
        using var server = new ManualServer(port);
        var url = server.BaseUrl;

        Console.WriteLine($"The Ferret Manual → {url}");
        Console.WriteLine("Press Ctrl+C to stop.");
        _logger.LogInformation("The Ferret Manual is running at {Url}", url);

        _ = server.StartAsync(ct);

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not open browser: {Message}", ex.Message);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* Ctrl+C — graceful shutdown */ }

        return 0;
    }
}
```

- [ ] **Step 3: Create `ManualCliModule.cs`**

Following the exact pattern from `ServeCliModule.cs`, create `src/Ferret.Cli/Commands/Manual/ManualCliModule.cs`. The command:
- Name: `manual`
- Description: `"Open The Ferret Manual in your browser"`
- Option: `--port` (int, default 7070, description "Port for the manual server")
- Handler calls: `manualCommandHandler.HandleAsync(port, ctx.GetCancellationToken())`
- `ConfigureServices`: `services.AddSingleton<ManualCommandHandler>()`

- [ ] **Step 4: Register in `Program.cs`**

Add to `Program.cs`:

```csharp
services.AddSingleton<ICliModule, ManualCliModule>();
```

Add the `using` at the top:

```csharp
using Ferret.Cli.Commands.Manual;
```

- [ ] **Step 5: Build and smoke test**

```
dotnet build src/Ferret.sln
dotnet run --project src/Ferret.Cli -- manual --port 7071
```

Open `http://localhost:7071/manual` in a browser. Verify:
- Left nav shows all 8 sections (including Design Decisions)
- Getting Started / Installation page renders correctly
- Search box returns results for "install"
- Architecture / Context Assembly page shows ASCII diagram
- Every page footer shows Previous / Next navigation + Edit source / Report issue links

- [ ] **Step 6: Commit**

```
git add src/Ferret.Manual/ManualCommandHandler.cs
git add src/Ferret.Cli/Commands/Manual/
git add src/Ferret.Cli/Program.cs
git commit -m "feat(sprint-14): ferret manual — The Ferret Manual at http://localhost:7070/manual"
```

---

### Task 7: Test project and solution wiring

**Files:**
- Create: `tests/Ferret.Manual.Tests/Ferret.Manual.Tests.csproj`
- Modify: `src/Ferret.sln`

- [ ] **Step 1: Create test project**

```
dotnet new xunit -n Ferret.Manual.Tests -o tests/Ferret.Manual.Tests --framework net9.0
dotnet add tests/Ferret.Manual.Tests/Ferret.Manual.Tests.csproj reference src/Ferret.Manual/Ferret.Manual.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Manual.Tests/Ferret.Manual.Tests.csproj
```

- [ ] **Step 2: Run the full test suite**

```
dotnet test tests/Ferret.Manual.Tests/ -v
```

Expected: all tests pass

- [ ] **Step 3: Run full solution tests**

```
dotnet test tests/ -v
```

Expected: all tests pass, no regressions

- [ ] **Step 4: Commit**

```
git add tests/Ferret.Manual.Tests/
git add src/Ferret.sln
git commit -m "test(sprint-14): Ferret.Manual.Tests — DocRegistry + ManualServer coverage"
```

---

## Completion Checklist

- [ ] `ferret manual` starts HTTP server and opens browser at `http://localhost:7070/manual`
- [ ] `ferret manual --port 8080` overrides the default port
- [ ] All 41 pages accessible and render without errors (no 404, no 500)
- [ ] Left navigation present on all pages with correct active-state highlighting and 8 top-level sections
- [ ] Full-text search returns relevant results for queries matching page titles
- [ ] All 8 top-level sections present: Getting Started, User Guide, Reference, Architecture, Developer Guide, Design Decisions, Troubleshooting, FAQ
- [ ] Architecture Explorer contains ASCII diagrams for platform overview, search flow, and context assembly (minimum 3 of 9)
- [ ] Developer Guide pages contain working C# code examples for all 4 extension types
- [ ] Design Decisions section contains all 7 pages (why-sqlite, why-bm25, why-mcp, why-providers, why-context-assembly, why-platform-first, why-manual)
- [ ] Every page shows persistent footer: Previous / Next navigation, Edit source, Report issue, Architecture, CLI Reference links
- [ ] Previous / Next links navigate correctly between adjacent pages (no broken links at first/last page)
- [ ] CLI Reference documents every shipped command with flags and exit codes
- [ ] `ferret manual` exits cleanly on Ctrl+C
- [ ] All tests pass: `dotnet test tests/`
- [ ] Build passes: `dotnet build src/Ferret.sln`
