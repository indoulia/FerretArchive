using System.Text.Json;

using Ferret.Benchmarks.Corpus;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;

namespace Ferret.Benchmarks.Tests.Corpus;

public sealed class CorpusGeneratorTests
{
    // Text formats + the manifest must be byte-identical; binary formats compare extracted text + metadata.
    private static readonly string[] TextExtensions = [".md", ".html", ".cs", ".json"];

    [Fact]
    public void Same_Seed_Text_Formats_And_Manifest_Are_Byte_Identical()
    {
        Run((dirA, dirB) =>
        {
            foreach (var ext in TextExtensions.Append(".manifest"))
            {
                var pattern = ext == ".manifest" ? "corpus.json" : "*" + ext;
                var filesA = Directory.GetFiles(dirA, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirA)).ToList();
                var filesB = Directory.GetFiles(dirB, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirB)).ToList();
                Assert.Equal(filesA.Count, filesB.Count);
                for (var i = 0; i < filesA.Count; i++)
                {
                    Assert.Equal(File.ReadAllBytes(filesA[i]), File.ReadAllBytes(filesB[i]));
                }
            }
        });
    }

    [Fact]
    public async Task Same_Seed_Binary_Formats_Have_Identical_Extracted_Text_And_Metadata()
    {
        await Run(async (dirA, dirB) =>
        {
            await AssertBinaryEquivalent(dirA, dirB, "*.pdf", (s, a) => new PdfParser(new ParserOptions()).ParseAsync(s, a));
            await AssertBinaryEquivalent(dirA, dirB, "*.docx", (s, a) => new WordParser(new ParserOptions()).ParseAsync(s, a));
            await AssertBinaryEquivalent(dirA, dirB, "*.xlsx", (s, a) => new ExcelParser(new ParserOptions()).ParseAsync(s, a));
        });
    }

    [Fact]
    public void Small_Corpus_Emits_Enterprise_Hierarchy_And_Manifest()
    {
        var dir = NewDir();
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 1).Generate(CorpusSize.Small, dir);

            Assert.True(File.Exists(Path.Join(dir, "corpus.json")));
            Assert.True(Directory.Exists(Path.Join(dir, "Engineering")));
            Assert.True(Directory.Exists(Path.Join(dir, "Operations")));
            Assert.True(Directory.Exists(Path.Join(dir, "Quality")));
            Assert.True(Directory.Exists(Path.Join(dir, "Management")));

            Assert.NotEmpty(Directory.GetFiles(dir, "*.pdf", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.docx", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.xlsx", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories));

            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Join(dir, "corpus.json")));
            Assert.Equal(1, manifest.RootElement.GetProperty("seed").GetInt32());
            Assert.Equal("Small", manifest.RootElement.GetProperty("size").GetString());
            Assert.True(manifest.RootElement.GetProperty("documentCount").GetInt32() > 0);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    private static void Run(Action<string, string> assert)
    {
        var dirA = NewDir();
        var dirB = NewDir();
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirA);
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirB);
            assert(dirA, dirB);
        }
        finally
        {
            if (Directory.Exists(dirA))
            {
                Directory.Delete(dirA, true);
            }

            if (Directory.Exists(dirB))
            {
                Directory.Delete(dirB, true);
            }
        }
    }

    private static async Task Run(Func<string, string, Task> assert)
    {
        var dirA = NewDir();
        var dirB = NewDir();
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirA);
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirB);
            await assert(dirA, dirB);
        }
        finally
        {
            if (Directory.Exists(dirA))
            {
                Directory.Delete(dirA, true);
            }

            if (Directory.Exists(dirB))
            {
                Directory.Delete(dirB, true);
            }
        }
    }

    private static async Task AssertBinaryEquivalent(
        string dirA, string dirB, string pattern, Func<Stream, ParseContext, ValueTask<Document>> parse)
    {
        var filesA = Directory.GetFiles(dirA, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirA)).ToList();
        var filesB = Directory.GetFiles(dirB, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirB)).ToList();
        Assert.Equal(filesA.Count, filesB.Count);
        for (var i = 0; i < filesA.Count; i++)
        {
            var a = await ParseFile(filesA[i], parse);
            var b = await ParseFile(filesB[i], parse);
            Assert.Equal(a.PlainText, b.PlainText);
            Assert.Equal(Stable(a.Metadata), Stable(b.Metadata)); // exclude writer-stamped timestamps
        }
    }

    private static async Task<Document> ParseFile(string path, Func<Stream, ParseContext, ValueTask<Document>> parse)
    {
        await using var fs = File.OpenRead(path);
        var uri = new Uri("filesystem:///" + Path.GetFileName(path));
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("bench"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = Path.GetFileName(path),
            LastModified = DateTimeOffset.UnixEpoch,
            MediaType = "application/octet-stream",
        };
        return await parse(fs, ParseContext.For(asset));
    }

    // PDF writers stamp Created/Modified from the wall clock; exclude those keys from the comparison.
    private static IEnumerable<KeyValuePair<string, string>> Stable(IReadOnlyDictionary<string, string> m) =>
        m.Where(kv => kv.Key != DocumentMetadata.Created && kv.Key != DocumentMetadata.Modified)
         .OrderBy(kv => kv.Key, StringComparer.Ordinal);

    private static Func<string, string> RelPath(string root) => p => Path.GetRelativePath(root, p);

    private static string NewDir() => Path.Join(Path.GetTempPath(), "corpus-" + Guid.NewGuid().ToString("N"));
}
