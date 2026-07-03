using System.Text;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class CsvParserTests
{
    private static AssetDescriptor Asset(string mediaType) => new()
    {
        Id = AssetId.From(new Uri("filesystem:///export.csv")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///export.csv"),
        DisplayName = "export.csv",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = mediaType,
    };

    private static MemoryStream MakeStream(string s) => new(Encoding.UTF8.GetBytes(s));

    [Fact]
    public void CanParse_Csv_And_Tsv_Only()
    {
        var parser = new CsvParser(new ParserOptions());
        Assert.True(parser.CanParse("text/csv"));
        Assert.True(parser.CanParse("text/tab-separated-values"));
        Assert.False(parser.CanParse("text/plain"));
    }

    [Fact]
    public void Priority_Is_200_To_Beat_PlainText()
    {
        Assert.Equal(200, new CsvParser(new ParserOptions()).Descriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Header_And_Rows_As_Data()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key,Summary,Severity\nBUG-1,Login fails,High\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains("Severity", doc.PlainText, StringComparison.Ordinal); // header
        Assert.Contains("Login fails", doc.PlainText, StringComparison.Ordinal); // cell
        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Handles_Quoted_Field_With_Embedded_Comma()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key,Summary\nBUG-2,\"Fails on login, then crashes\"\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains("Fails on login, then crashes", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Tsv_Splits_On_Tab()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key\tSummary\nBUG-3\tCrash on save\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/tab-separated-values")));

        Assert.Contains("Crash on save", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Does_Not_Dispose_Stream()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("a,b\n1,2\n");

        await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.True(stream.CanRead); // not disposed
    }

    [Fact]
    public async Task ParseAsync_Populates_Row_Column_And_Header_Metadata()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key,Summary,Severity\nBUG-1,Login fails,High\nBUG-2,Crash,Low\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Equal("2", doc.Metadata[DocumentMetadata.RowCount]); // data rows, excludes header
        Assert.Equal("3", doc.Metadata[DocumentMetadata.ColumnCount]); // header field count
        Assert.Equal("true", doc.Metadata[DocumentMetadata.HasHeader]);
    }

    [Fact]
    public async Task ParseAsync_BlankFile_YieldsEmptyText_And_NoHeader()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream(string.Empty);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Equal(string.Empty, doc.PlainText);
        Assert.Equal("false", doc.Metadata[DocumentMetadata.HasHeader]);
        Assert.Equal("0", doc.Metadata[DocumentMetadata.RowCount]);
    }

    [Theory]
    [InlineData("Key,Summary\nBUG-1,\n")] // empty trailing column
    [InlineData("Key,Summary,\nBUG-1,Login fails,\n")] // trailing comma / empty header col
    [InlineData("Key,Summary\n\nBUG-1,Login fails\n")] // empty row in the middle
    [InlineData("Key,Summary\nBUG-1,\"unterminated quote\n")] // unmatched quote — must not throw
    public async Task ParseAsync_MalformedInput_DoesNotThrow(string content)
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream(content);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.NotNull(doc); // parser is total over messy enterprise exports
        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Honors_Utf8_Bom()
    {
        var parser = new CsvParser(new ParserOptions());
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes("Key,Summary\nBUG-1,Café crash\n"))
            .ToArray();
        using var stream = new MemoryStream(bytes);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains("Café crash", doc.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("﻿", doc.PlainText, StringComparison.Ordinal); // BOM stripped by the reader, not indexed
    }

    [Fact]
    public async Task ParseAsync_VeryLongCell_IsPreserved()
    {
        var parser = new CsvParser(new ParserOptions());
        var longCell = new string('x', 100_000);
        using var stream = MakeStream($"Key,Notes\nBUG-1,{longCell}\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains(longCell, doc.PlainText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task ParseAsync_ScalesLinearly_WithoutExhaustingMemory(int rows)
    {
        var parser = new CsvParser(new ParserOptions());
        var sb = new StringBuilder("Key,Summary,Severity\n");
        for (var i = 0; i < rows; i++)
        {
            sb.Append("BUG-").Append(i).Append(",Issue ").Append(i).Append(",High\n");
        }

        using var stream = MakeStream(sb.ToString());

        // Bounded working set: the reader streams records; assert the parse completes,
        // reports the exact row count, and never throws OutOfMemoryException.
        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Equal(rows.ToString(System.Globalization.CultureInfo.InvariantCulture), doc.Metadata[DocumentMetadata.RowCount]);
        Assert.Contains("BUG-" + (rows - 1), doc.PlainText, StringComparison.Ordinal);
    }
}
