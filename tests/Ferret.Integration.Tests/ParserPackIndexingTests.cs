using Ferret.Benchmarks.Corpus;
using Ferret.Core.Documents;
using Ferret.ParserPlatform;
using Ferret.Parsers;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Integration.Tests;

public sealed class ParserPackIndexingTests
{
    [Fact]
    public async Task Pdf_Docx_Xlsx_Csv_Parsed_And_Opaque_Binaries_Excluded()
    {
        // 1. Generate a Small corpus (laid out under a realistic enterprise tree).
        var root = Path.Join(Path.GetTempPath(), "pp-int-" + Guid.NewGuid().ToString("N"));
        new SyntheticEnterpriseCorpusGenerator(seed: 7).Generate(CorpusSize.Small, root);

        // 2. Drop a loose opaque binary into the tree (must NOT be parseable).
        var soPath = Path.Join(root, "Engineering", "Source", "native.so");
        await File.WriteAllBytesAsync(soPath, [0x7F, 0x45, 0x4C, 0x46, 0x00, 0x01]);

        // 3. Drop a CSV export into the tree (structure-aware CsvParser in the platform).
        var csvPath = Path.Join(root, "Management", "Notes", "jira-export.csv");
        await File.WriteAllTextAsync(csvPath, "Key,Summary,Severity\nBUG-1,SSO login fails,High\n");

        // 4. Resolve the full parser pack dispatcher (the public API production uses).
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IParserDispatcher>();
        var resolver = new MimeTypeResolver();

        // 5. Parse one document of each format through the dispatcher (recurse the hierarchy by extension).
        var pdfPath = Directory.GetFiles(root, "*.pdf", SearchOption.AllDirectories).OrderBy(p => p).First();
        var docxPath = Directory.GetFiles(root, "*.docx", SearchOption.AllDirectories).OrderBy(p => p).First();
        var xlsxPath = Directory.GetFiles(root, "*.xlsx", SearchOption.AllDirectories).OrderBy(p => p).First();

        var pdfResult = await DispatchFile(dispatcher, resolver, pdfPath);
        var docxResult = await DispatchFile(dispatcher, resolver, docxPath);
        var xlsxResult = await DispatchFile(dispatcher, resolver, xlsxPath);
        var csvResult = await DispatchFile(dispatcher, resolver, csvPath);
        var soResult = await DispatchFile(dispatcher, resolver, soPath);

        // PDF and DOCX: parsed as prose with non-empty text.
        Assert.Equal(ParseResultKind.Success, pdfResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(pdfResult.Value!.PlainText));
        Assert.Equal(ParseResultKind.Success, docxResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(docxResult.Value!.PlainText));

        // XLSX: parsed as Data; a header token from the enterprise archetype is present.
        Assert.Equal(ParseResultKind.Success, xlsxResult.Kind);
        Assert.Equal(DocumentKind.Data, xlsxResult.Value!.Kind);
        Assert.Contains("Priority", xlsxResult.Value!.PlainText, StringComparison.Ordinal);

        // CSV: structure-aware, Data kind, cell value searchable (CsvParser beats PlainTextParser).
        Assert.Equal(ParseResultKind.Success, csvResult.Kind);
        Assert.Equal(DocumentKind.Data, csvResult.Value!.Kind);
        Assert.Contains("SSO login fails", csvResult.Value!.PlainText, StringComparison.Ordinal);

        // Opaque binary: resolver yields application/octet-stream, dispatcher finds no parser.
        Assert.Equal(ParseResultKind.Unsupported, soResult.Kind);

        // Metadata round-trips renderer -> package/document info -> parser, verified per format.
        Assert.True(pdfResult.Value!.Metadata.ContainsKey(DocumentMetadata.PageCount));
        Assert.True(docxResult.Value!.Metadata.ContainsKey(DocumentMetadata.Author));
        Assert.True(xlsxResult.Value!.Metadata.ContainsKey(DocumentMetadata.SheetCount));

        Directory.Delete(root, true);
    }

    [Fact]
    public void Manifest_Matches_Generated_Corpus()
    {
        var root = Path.Join(Path.GetTempPath(), "pp-manifest-" + Guid.NewGuid().ToString("N"));
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 11).Generate(CorpusSize.Small, root);

            using var manifest = System.Text.Json.JsonDocument.Parse(
                File.ReadAllText(Path.Join(root, "corpus.json")));
            var documentCount = manifest.RootElement.GetProperty("documentCount").GetInt32();

            // Every generated file except the manifest itself is a document.
            var actual = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Count(p => !string.Equals(Path.GetFileName(p), "corpus.json", StringComparison.Ordinal));
            Assert.Equal(documentCount, actual);

            // formatCounts must sum to the document count.
            var formatSum = manifest.RootElement.GetProperty("formatCounts")
                .EnumerateObject().Sum(p => p.Value.GetInt32());
            Assert.Equal(documentCount, formatSum);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static async Task<ParseResult<Document>> DispatchFile(
        IParserDispatcher dispatcher, MimeTypeResolver resolver, string path)
    {
        var mediaType = resolver.Resolve(Path.GetFileName(path)).MediaType;
        var asset = TestAsset.For(path, mediaType);
        await using var fs = File.OpenRead(path);
        return await dispatcher.DispatchAsync(fs, asset);
    }
}
