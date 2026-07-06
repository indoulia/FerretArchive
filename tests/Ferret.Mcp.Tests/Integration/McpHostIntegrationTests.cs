using Ferret.Core.Connectors;
using Ferret.Core.Context;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Mcp;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Runtime;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Ferret.Mcp.Tests.Integration;

/// <summary>Verifies that McpModule wires the DI container correctly.</summary>
public sealed class McpHostIntegrationTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);

        // Platform service fakes
        services.AddSingleton<ISearchService>(new FakeSearchService());
        services.AddSingleton<IDocumentService>(new FakeDocumentService());
        services.AddSingleton<IIndexEngine>(new FakeIndexEngine());
        services.AddSingleton<IWorkspaceContext>(new FakeWorkspaceContext());
        services.AddSingleton<IConnectorRegistry>(new FakeConnectorRegistry());
        services.AddSingleton<IContextAssembler>(new FakeContextAssembler());
        services.AddSingleton<IWorkspaceRegistry>(new FakeWorkspaceRegistry());

        McpModule.ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void McpModule_Registers_IMcpRuntime()
    {
        using var provider = BuildProvider();
        var runtime = provider.GetService<IMcpRuntime>();
        Assert.NotNull(runtime);
        Assert.IsType<McpRuntime>(runtime);
    }

    [Fact]
    public void McpModule_Registers_Five_Tools()
    {
        using var provider = BuildProvider();
        var tools = provider.GetServices<IMcpTool>().ToList();
        Assert.Equal(5, tools.Count);
    }

    [Fact]
    public void McpModule_Registers_Three_Resources()
    {
        using var provider = BuildProvider();
        var resources = provider.GetServices<IMcpResource>().ToList();
        Assert.Equal(3, resources.Count);
    }

    [Fact]
    public void McpModule_Registers_IMcpTransport()
    {
        using var provider = BuildProvider();
        var transport = provider.GetService<IMcpTransport>();
        Assert.NotNull(transport);
    }

    [Fact]
    public void McpModule_Tools_HaveUniqueNames()
    {
        using var provider = BuildProvider();
        var names = provider.GetServices<IMcpTool>().Select(t => t.Descriptor.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void McpModule_Resources_HaveUniqueUris()
    {
        using var provider = BuildProvider();
        var uris = provider.GetServices<IMcpResource>().Select(r => r.Descriptor.ResourceUri).ToList();
        Assert.Equal(uris.Count, uris.Distinct(StringComparer.Ordinal).Count());
    }

    // Minimal fakes for platform services

    private sealed class FakeSearchService : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
            Task.FromResult(SearchServiceResult.Success(
                new SearchQuery { OriginalText = rawQuery, Root = new KeywordExpression(rawQuery) },
                new SearchResult { Hits = [], TotalHits = 0, ReturnedHits = 0 },
                new SearchExecutionInfo { SessionId = Guid.Empty, ProviderName = "fake", Duration = TimeSpan.Zero, DocumentsScanned = 0, IndexVersion = "0" }));

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            SearchAsync(query.OriginalText, options);
    }

    private sealed class FakeDocumentService : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct) =>
            Task.FromResult<Document?>(null);
    }

    private sealed class FakeIndexEngine : IIndexEngine
    {
        public Task WriteAsync(Document document, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IndexStats> GetStatsAsync(CancellationToken ct = default) =>
            Task.FromResult(new IndexStats { DocumentCount = 0, TotalChars = 0, IndexSizeBytes = 0, LastIndexedAt = DateTimeOffset.MinValue });

        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Ferret.Core.Primitives.DocumentId documentId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test");

        public WorkspacePath WorkspaceRoot => WorkspacePath.Create(System.IO.Path.GetTempPath());
    }

    private sealed class FakeWorkspaceRegistry : IWorkspaceRegistry
    {
        public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default) =>
            Task.FromResult<WorkspaceRegistryEntry?>(null);

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WorkspaceRegistryEntry>>([]);

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeConnectorRegistry : IConnectorRegistry
    {
        public IReadOnlyList<ConnectorDescriptor> GetAll() => [];

        public ConnectorDescriptor? GetById(ConnectorId id) => null;

        public bool IsRegistered(ConnectorId id) => false;

        public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) => [];
    }

    private sealed class FakeContextAssembler : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromResult(new ContextPackage
            {
                Query = request.Query,
                Documents = [],
                TotalTokenEstimate = 0,
                DocumentsConsidered = 0,
                DocumentsIncluded = 0,
                AssembledAt = DateTimeOffset.UtcNow,
            });
    }
}
