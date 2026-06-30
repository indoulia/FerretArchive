using Ferret.Core.Context;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that assembles a complete, deduplicated, token-budgeted context package for a query.</summary>
public sealed class ContextTool : IMcpTool
{
    private readonly IContextAssembler _assembler;

    /// <summary>Initializes a new instance of the <see cref="ContextTool"/> class.</summary>
    /// <param name="assembler">The context assembly pipeline.</param>
    public ContextTool(IContextAssembler assembler)
    {
        ArgumentNullException.ThrowIfNull(assembler);
        _assembler = assembler;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "ferret_context",
        Description = "Assemble a complete, deduplicated, token-budgeted context package for a query. Returns formatted document context ready for AI consumption.",
        InputSchemaJson = """
            {
              "type": "object",
              "properties": {
                "query": {
                  "type": "string",
                  "description": "The query to assemble context for"
                },
                "max_tokens": {
                  "type": "integer",
                  "description": "Maximum token budget for the assembled context (default: 8000)"
                },
                "max_documents": {
                  "type": "integer",
                  "description": "Maximum number of documents to include (default: 10)"
                }
              },
              "required": ["query"]
            }
            """,
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var query = arguments.GetRequiredString("query");
        var maxTokens = arguments.TryGetInt32("max_tokens", out var t) ? t : 8000;
        var maxDocuments = arguments.TryGetInt32("max_documents", out var d) ? d : 10;

        var request = new ContextRequest
        {
            Query = query,
            MaxTokens = maxTokens,
            MaxDocuments = maxDocuments,
        };

        try
        {
            var package = await _assembler.AssembleAsync(request, ct).ConfigureAwait(false);
            return McpToolResult.Success(package.ToPromptString());
        }
#pragma warning disable CA1031 // catch broad exception so tool can return MCP error instead of crashing the host
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return McpToolResult.Error($"Context assembly failed: {ex.Message}");
        }
#pragma warning restore CA1031
    }
}
