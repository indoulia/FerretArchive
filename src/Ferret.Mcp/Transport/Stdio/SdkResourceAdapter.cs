using Ferret.Mcp.Registry;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Ferret.Mcp.Transport.Stdio;

/// <summary>Adapts <see cref="IMcpResourceRegistry"/> to MCP SDK handler delegates.</summary>
internal static class SdkResourceAdapter
{
    /// <summary>Creates the SDK list-resources handler backed by <paramref name="registry"/>.</summary>
    /// <param name="registry">Resource registry to enumerate.</param>
    /// <returns>An SDK handler delegate.</returns>
    internal static McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> CreateListHandler(
        IMcpResourceRegistry registry)
    {
        return (_, _) =>
        {
            var resources = registry.GetAll()
                .Select(d => new Resource
                {
                    Uri = d.ResourceUri,
                    Name = d.Name,
                    Description = d.Description,
                    MimeType = d.MimeType,
                })
                .ToList();

            return ValueTask.FromResult(new ListResourcesResult { Resources = resources });
        };
    }

    /// <summary>Creates the SDK read-resource handler backed by <paramref name="registry"/>.</summary>
    /// <param name="registry">Resource registry to dispatch reads.</param>
    /// <returns>An SDK handler delegate.</returns>
    internal static McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> CreateReadHandler(
        IMcpResourceRegistry registry)
    {
        return async (context, ct) =>
        {
            var uri = context.Params?.Uri ?? string.Empty;
            var resource = registry.GetByUri(uri);

            if (resource is null)
            {
                return new ReadResourceResult
                {
                    Contents = [new TextResourceContents { Uri = uri, Text = $"Resource not found: {uri}", MimeType = "text/plain" }],
                };
            }

            var content = await resource.ReadAsync(uri, ct).ConfigureAwait(false);
            return new ReadResourceResult
            {
                Contents = [new TextResourceContents { Uri = content.ResourceUri, Text = content.Text, MimeType = content.MimeType }],
            };
        };
    }
}
