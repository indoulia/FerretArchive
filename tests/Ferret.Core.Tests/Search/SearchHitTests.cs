using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchHitTests
{
    [Fact]
    public void FileSearchHit_Kind_Is_File()
    {
        Assert.Equal(SearchHitKind.File, MakeFileHit().Kind);
    }

    [Fact]
    public void FileSearchHit_Score_Preserved()
    {
        Assert.Equal(0.92f, MakeFileHit().Score);
    }

    [Fact]
    public void FileSearchHit_Explanation_Defaults_To_Null()
    {
        Assert.Null(MakeFileHit().Explanation);
    }

    [Fact]
    public void PassageSearchHit_Kind_Is_Passage()
    {
        Assert.Equal(SearchHitKind.Passage, MakePassageHit().Kind);
    }

    [Fact]
    public void PassageSearchHit_Heading_May_Be_Null()
    {
        var hit = MakePassageHit() with { Heading = null };
        Assert.Null(hit.Heading);
    }

    [Fact]
    public void PassageSearchHit_Preserves_Offsets()
    {
        var hit = MakePassageHit();
        Assert.Equal(10, hit.StartOffset);
        Assert.Equal(200, hit.EndOffset);
    }

    [Fact]
    public void SearchResult_Empty_Has_Zero_Hits()
    {
        var result = SearchResult.Empty;
        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalHits);
        Assert.Equal(0, result.ReturnedHits);
    }

    [Fact]
    public void SearchResult_ReturnedHits_Matches_Hits_Count()
    {
        var result = new SearchResult
        {
            Hits = [MakeFileHit()],
            TotalHits = 5,
            ReturnedHits = 1,
        };
        Assert.Equal(1, result.ReturnedHits);
        Assert.Equal(5, result.TotalHits);
    }

    private static FileSearchHit MakeFileHit() => new()
    {
        DocumentId = DocumentId.Create("filesystem:///src/Program.cs"),
        ConnectorInstanceId = new ConnectorInstanceId("src-root"),
        CanonicalUri = new Uri("filesystem:///src/Program.cs"),
        DisplayName = "src/Program.cs",
        Kind = SearchHitKind.File,
        Score = 0.92f,
        Snippet = HighlightedText.Plain("...the main entry point..."),
    };

    private static PassageSearchHit MakePassageHit() => new()
    {
        DocumentId = DocumentId.Create("filesystem:///src/Program.cs"),
        ConnectorInstanceId = new ConnectorInstanceId("src-root"),
        CanonicalUri = new Uri("filesystem:///src/Program.cs"),
        DisplayName = "src/Program.cs",
        Kind = SearchHitKind.Passage,
        Score = 0.85f,
        Snippet = HighlightedText.Plain("...authentication context..."),
        Heading = "Authentication",
        StartOffset = 10,
        EndOffset = 200,
    };
}
