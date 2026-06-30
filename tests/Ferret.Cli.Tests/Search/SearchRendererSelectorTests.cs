using System.Text.Json;

using Ferret.Cli.Search;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;

using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class SearchRendererSelectorTests
{
    private readonly SearchRendererSelector _ansiRenderer =
        new SearchRendererSelector(new AnsiTextStyler());

    private readonly SearchRendererSelector _plainRenderer =
        new SearchRendererSelector(new NullTextStyler());

    // ── Text format — zero results ────────────────────────────────────────────

    [Fact]
    public void Text_NoHits_Contains_Query_In_Output()
    {
        var vm = MakeViewModel("authentication", []);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("authentication", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_NoHits_Does_Not_Contain_Table_Rows()
    {
        var vm = MakeViewModel("auth", []);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.DoesNotContain("doc-", output, StringComparison.Ordinal);
    }

    // ── Text format — with results ────────────────────────────────────────────

    [Fact]
    public void Text_WithHits_Contains_DisplayName()
    {
        var vm = MakeViewModel("auth", [MakeHit("auth-service.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("auth-service.cs", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_WithHits_Contains_Snippet_Text()
    {
        var vm = MakeViewModel("token", [MakeHit("file.cs", "token")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("token", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_WithHits_Contains_Provider_Name_In_Footer()
    {
        var vm = MakeViewModel("auth", [MakeHit("f.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("bm25-fts5", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_AnsiRenderer_Match_Spans_Get_Bold_Escape_Sequences()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _ansiRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("\x1B[1m", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_NullRenderer_Contains_No_Escape_Sequences()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.DoesNotContain("\x1B[", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_Footer_Contains_Hit_Count()
    {
        FileSearchHit[] hits = [MakeHit("a.cs", "auth"), MakeHit("b.cs", "auth")];
        var vm = MakeViewModel("auth", hits);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("2", output, StringComparison.Ordinal);
    }

    // ── JSON format ───────────────────────────────────────────────────────────

    [Fact]
    public void Json_Output_Is_Valid_Json()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        var doc = JsonDocument.Parse(output);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Json_Output_Contains_Query_Field()
    {
        var vm = MakeViewModel("authentication", []);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        Assert.Contains("authentication", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_Output_Contains_Hits_Array()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        var doc = JsonDocument.Parse(output);
        Assert.True(doc.RootElement.TryGetProperty("hits", out var hits));
        Assert.Equal(JsonValueKind.Array, hits.ValueKind);
    }

    [Fact]
    public void Json_Output_Contains_Total_Field()
    {
        FileSearchHit[] hits = [MakeHit("a.cs", "auth"), MakeHit("b.cs", "auth")];
        var vm = MakeViewModel("auth", hits);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        var doc = JsonDocument.Parse(output);
        Assert.True(doc.RootElement.TryGetProperty("total", out var total));
        Assert.Equal(2, total.GetInt32());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchViewModel MakeViewModel(string query, FileSearchHit[] hits) =>
        new()
        {
            OriginalQuery = query,
            Hits = hits,
            ExecutionInfo = new SearchExecutionInfo
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "bm25-fts5",
                Duration = TimeSpan.FromMilliseconds(12),
                DocumentsScanned = hits.Length,
                IndexVersion = "fts5",
            },
        };

    private static FileSearchHit MakeHit(string displayName, string matchText) =>
        new()
        {
            DocumentId = DocumentId.Create(displayName),
            ConnectorInstanceId = new ConnectorInstanceId(string.Empty),
            CanonicalUri = new Uri($"file:///{displayName}"),
            DisplayName = displayName,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = new HighlightedText
            {
                Spans =
                [
                    new TextSpan("before ", TextSpanKind.Normal),
                    new TextSpan(matchText, TextSpanKind.Match),
                    new TextSpan(" after", TextSpanKind.Normal),
                ],
            },
        };
}
