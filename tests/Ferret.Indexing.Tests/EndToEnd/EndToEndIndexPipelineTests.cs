using Ferret.ConnectorPlatform;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Helpers;
using Ferret.ParserPlatform;
using Ferret.ParserPlatform.Parsers;

using Xunit;

namespace Ferret.Indexing.Tests.EndToEnd;

/// <summary>
/// End-to-end tests: real filesystem, real parsers, real SQLite FTS5 engine.
/// Verifies that the full discover → parse → index pipeline works with no mocks.
/// The SQLite db and connectors.json are stored under .ferret/ which FilesystemConnector skips.
/// </summary>
public sealed class EndToEndIndexPipelineTests
{
    /// <summary>
    /// Full pipeline run discovers text files, indexes them, and records zero failures.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FullIndexRun_DiscoversTxtFiles_And_IndexesThem()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "readme.txt"),
            "hello world search test content");
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "notes.txt"),
            "ferret integration test content for FTS5 engine");

        // Place db inside .ferret/ which FilesystemConnector auto-skips.
        var dbPath = Path.Join(tempDir.Path, ".ferret", "test-index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var engine = new SqliteKeywordIndexEngine(dbPath);

        var pipeline = BuildRealPipeline(tempDir.Path, engine);
        var workspaceId = WorkspaceId.Create("e2e-test");

        var result = await pipeline.RunAsync(
            workspaceId,
            IndexPipelineOptions.Default,
            CancellationToken.None);

        Assert.Equal(0, result.Failures);
        Assert.True(result.DocumentsIndexed > 0, $"Expected > 0 indexed but got {result.DocumentsIndexed}");
    }

    /// <summary>
    /// Mixed-content directory: parseable text files are indexed, binary files are skipped.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FullIndexRun_Indexes_Parseable_And_Skips_Binaries()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "README.md"), "# Ferret\nEnd-to-end test.");
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "config.json"), "{\"name\":\"ferret\"}");
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "notes.txt"), "plain text content");
        await File.WriteAllBytesAsync(
            Path.Join(tempDir.Path, "image.png"), [0x89, 0x50, 0x4E, 0x47]);

        var dbPath = Path.Join(tempDir.Path, ".ferret", "mixed-index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var engine = new SqliteKeywordIndexEngine(dbPath);

        var pipeline = BuildRealPipeline(tempDir.Path, engine);

        var result = await pipeline.RunAsync(
            WorkspaceId.Create("e2e-mixed"),
            IndexPipelineOptions.Default,
            CancellationToken.None);

        Assert.Equal(0, result.Failures);
        Assert.True(
            result.DocumentsIndexed >= 3,
            $"Expected >= 3 indexed (md, json, txt) but got {result.DocumentsIndexed}");
        Assert.True(
            result.DocumentsSkipped >= 1,
            $"Expected >= 1 skipped (png) but got {result.DocumentsSkipped}");
    }

    /// <summary>
    /// ForceRebuild clears the index and re-indexes from scratch; result still shows correct counts.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FullIndexRun_ForceRebuild_ClearsAndReIndexes()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "doc.txt"), "rebuild test content");

        var dbPath = Path.Join(tempDir.Path, ".ferret", "rebuild-index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var engine = new SqliteKeywordIndexEngine(dbPath);

        var pipeline = BuildRealPipeline(tempDir.Path, engine);
        var workspaceId = WorkspaceId.Create("e2e-rebuild");

        // First run
        await pipeline.RunAsync(workspaceId, IndexPipelineOptions.Default, CancellationToken.None);

        // Second run with ForceRebuild
        var result = await pipeline.RunAsync(
            workspaceId,
            new IndexPipelineOptions { ForceRebuild = true },
            CancellationToken.None);

        Assert.Equal(0, result.Failures);
        Assert.True(
            result.DocumentsIndexed > 0,
            $"Expected > 0 indexed after rebuild but got {result.DocumentsIndexed}");
    }

    /// <summary>
    /// Subdirectories are structural, not readable assets. Walking into a nested
    /// directory must index the file it contains without recording the directory
    /// entry itself as a failure (a directory cannot be opened as a file stream).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FullIndexRun_WithSubdirectory_DoesNotFailOnDirectoryEntries()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "root.txt"), "root level content");
        Directory.CreateDirectory(Path.Join(tempDir.Path, "nested"));
        await File.WriteAllTextAsync(
            Path.Join(tempDir.Path, "nested", "child.txt"), "nested content");

        var dbPath = Path.Join(tempDir.Path, ".ferret", "subdir-index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var engine = new SqliteKeywordIndexEngine(dbPath);

        var pipeline = BuildRealPipeline(tempDir.Path, engine);

        var result = await pipeline.RunAsync(
            WorkspaceId.Create("e2e-subdir"),
            IndexPipelineOptions.Default,
            CancellationToken.None);

        Assert.Equal(0, result.Failures);
        Assert.True(
            result.DocumentsIndexed >= 2,
            $"Expected >= 2 indexed (root.txt, nested/child.txt) but got {result.DocumentsIndexed}");
    }

    // ── Shared pipeline builder ─────────────────────────────────────────────

    /// <summary>
    /// Builds a real <see cref="IndexPipeline"/> wired with real filesystem connector,
    /// real parsers, and the provided SQLite engine.
    /// The connector instance JSON is persisted to <c>rootPath/.ferret/connectors.json</c>,
    /// which is auto-skipped during file discovery.
    /// </summary>
    /// <param name="rootPath">The directory to scan for assets.</param>
    /// <param name="engine">The SQLite keyword index engine to write into.</param>
    /// <returns>A fully wired <see cref="IndexPipeline"/>.</returns>
    internal static IndexPipeline BuildRealPipeline(string rootPath, SqliteKeywordIndexEngine engine)
    {
        var mimeResolver = new MimeTypeResolver();

        var parserRegistry = ParserRegistryBuilder.Build(
            [new PlainTextParser(), new MarkdownParser(), new JsonParser()]);
        var dispatcher = new ParserDispatcher(parserRegistry);

        var store = new ConnectorInstanceStore();
        var workspacePath = WorkspacePath.Create(rootPath);
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("e2e-instance"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "E2E Test Filesystem",
            IsEnabled = true,
            Configuration = ConnectorConfiguration.FromDictionary(
                new Dictionary<string, string> { ["rootPath"] = rootPath }),
        };

        // SaveAsync writes to rootPath/.ferret/connectors.json — skipped during discovery.
        store.SaveAsync(workspacePath, [instance], CancellationToken.None).GetAwaiter().GetResult();

        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration { RootPath = rootPath },
            mimeResolver);

        var manager = ConnectorPlatformFactory.CreateConnectorManager(
            store,
            [factory],
            workspacePath);

        return new IndexPipeline(manager, dispatcher, engine, NullEventBus.Instance);
    }
}
