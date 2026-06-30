using System.Text.Json;

using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Ferret.Mcp.Transport.Stdio;

/// <summary>Adapts <see cref="IMcpToolRegistry"/> to MCP SDK handler delegates.</summary>
internal static class SdkToolAdapter
{
    private static readonly JsonElement EmptySchema =
        JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>Creates the SDK list-tools handler backed by <paramref name="registry"/>.</summary>
    /// <param name="registry">Tool registry to enumerate.</param>
    /// <returns>An SDK handler delegate.</returns>
    internal static McpRequestHandler<ListToolsRequestParams, ListToolsResult> CreateListHandler(
        IMcpToolRegistry registry)
    {
        return (_, _) =>
        {
            var tools = registry.GetAll()
                .Select(d => new Tool
                {
                    Name = d.Name,
                    Description = d.Description,
                    InputSchema = d.InputSchemaJson is not null
                        ? JsonDocument.Parse(d.InputSchemaJson).RootElement.Clone()
                        : EmptySchema,
                })
                .ToList();

            return ValueTask.FromResult(new ListToolsResult { Tools = tools });
        };
    }

    /// <summary>Creates the SDK call-tool handler backed by <paramref name="registry"/>.</summary>
    /// <param name="registry">Tool registry to dispatch calls.</param>
    /// <param name="errorMapper">Mapper for exception-to-result conversion.</param>
    /// <returns>An SDK handler delegate.</returns>
    internal static McpRequestHandler<CallToolRequestParams, CallToolResult> CreateCallHandler(
        IMcpToolRegistry registry,
        IMcpErrorMapper errorMapper)
    {
        return async (context, ct) =>
        {
            var name = context.Params?.Name ?? string.Empty;
            var tool = registry.GetByName(name);

            if (tool is null)
            {
                return BuildError($"Tool not found: {name}");
            }

            var args = McpArgumentsFactory.From(context.Params?.Arguments);
            McpToolResult result;
            try
            {
                result = await tool.ExecuteAsync(args, ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // catch broad exception so error mapper can classify it
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result = errorMapper.MapException(ex);
            }
#pragma warning restore CA1031

            return new CallToolResult
            {
                Content = result.Content
                    .Select(c => (ContentBlock)new TextContentBlock { Text = c.Text ?? string.Empty })
                    .ToList(),
                IsError = result.IsError,
            };
        };
    }

    private static CallToolResult BuildError(string message) => new()
    {
        Content = [new TextContentBlock { Text = message }],
        IsError = true,
    };
}
