using Ferret.Core.Context;
using Ferret.Core.Primitives;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class ContextToolTests
{
    private static ContextPackage EmptyPackage(string query) => new()
    {
        Query = query,
        Documents = [],
        TotalTokenEstimate = 0,
        DocumentsConsidered = 0,
        DocumentsIncluded = 0,
        AssembledAt = DateTimeOffset.UtcNow,
    };

    private static ContextPackage PackageWithDoc(string query, string docId, string content) => new()
    {
        Query = query,
        Documents =
        [
            new ContextDocument
            {
                DocumentId = DocumentId.Create(docId),
                CanonicalUri = new Uri($"filesystem:///{docId}"),
                DisplayName = docId,
                Content = content,
                Score = 0.9f,
                TokenEstimate = (content.Length / 4) + 1,
                Source = ContextDocumentSource.FullDocument,
            }
        ],
        TotalTokenEstimate = (content.Length / 4) + 1,
        DocumentsConsidered = 1,
        DocumentsIncluded = 1,
        AssembledAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Descriptor_ToolName_IsFeretContext()
    {
        var tool = new ContextTool(new StubContextAssembler(EmptyPackage("q")));
        Assert.Equal("ferret_context", tool.Descriptor.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ValidQuery_ReturnsPromptString()
    {
        var pkg = PackageWithDoc("auth", "src/auth.cs", "public class Auth {}");
        var tool = new ContextTool(new StubContextAssembler(pkg));
        var args = McpArguments.FromDictionary(new Dictionary<string, object?> { ["query"] = "auth" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("auth", result.Content[0].Text, StringComparison.Ordinal);
        Assert.Contains("src/auth.cs", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NoResults_ReturnsSuccessWithMessage()
    {
        var tool = new ContextTool(new StubContextAssembler(EmptyPackage("nothing")));
        var args = McpArguments.FromDictionary(new Dictionary<string, object?> { ["query"] = "nothing" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_AssemblerThrows_ReturnsError()
    {
        var tool = new ContextTool(new FailingContextAssembler());
        var args = McpArguments.FromDictionary(new Dictionary<string, object?> { ["query"] = "test" });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("index offline", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_CustomMaxTokens_PassedToAssembler()
    {
        ContextRequest? captured = null;
        var assembler = new CapturingAssembler(req =>
        {
            captured = req;
            return EmptyPackage(req.Query);
        });
        var tool = new ContextTool(assembler);
        var args = McpArguments.FromDictionary(new Dictionary<string, object?>
        {
            ["query"] = "auth",
            ["max_tokens"] = 4000,
        });

        await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(4000, captured!.MaxTokens);
    }

    private sealed class StubContextAssembler(ContextPackage pkg) : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromResult(pkg);
    }

    private sealed class FailingContextAssembler : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromException<ContextPackage>(new InvalidOperationException("index offline"));
    }

    private sealed class CapturingAssembler(Func<ContextRequest, ContextPackage> factory) : IContextAssembler
    {
        public Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct) =>
            Task.FromResult(factory(request));
    }
}
