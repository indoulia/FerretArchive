using System.Globalization;
using System.Text;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that retrieves the full content of a document by ID.</summary>
public sealed class ReadDocumentTool : IMcpTool
{
    private readonly IDocumentService _documentService;

    /// <summary>Initializes a new instance of the <see cref="ReadDocumentTool"/> class.</summary>
    /// <param name="documentService">Platform document retrieval service.</param>
    public ReadDocumentTool(IDocumentService documentService)
    {
        ArgumentNullException.ThrowIfNull(documentService);
        _documentService = documentService;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "read_document",
        Description = "Retrieve the full text content of a document by its ID (obtained from the search tool).",
        InputSchemaJson = """{"type":"object","properties":{"document_id":{"type":"string","description":"Document ID from a search result"}},"required":["document_id"]}""",
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var rawId = arguments.GetRequiredString("document_id");
        var id = DocumentId.Create(rawId);

        var document = await _documentService.GetAsync(id, ct).ConfigureAwait(false);
        if (document is null)
        {
            return McpToolResult.Error($"Document not found: {rawId}");
        }

        var sb = new StringBuilder();
        if (document.Title is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"# {document.Title}");
            sb.AppendLine();
        }

        sb.Append(document.PlainText);
        return McpToolResult.Success(sb.ToString().TrimEnd());
    }
}
