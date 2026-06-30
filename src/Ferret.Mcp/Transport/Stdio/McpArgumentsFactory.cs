using System.Text.Json;

using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Transport.Stdio;

/// <summary>Converts SDK argument dictionaries to <see cref="McpArguments"/>.</summary>
internal static class McpArgumentsFactory
{
    /// <summary>Creates <see cref="McpArguments"/> from an SDK arguments dictionary.</summary>
    /// <param name="sdkArgs">Arguments dictionary from the MCP SDK, or <see langword="null"/>.</param>
    /// <returns>A populated <see cref="McpArguments"/> instance, or <see cref="McpArguments.Empty"/>.</returns>
    internal static McpArguments From(IDictionary<string, JsonElement>? sdkArgs)
    {
        if (sdkArgs is null || sdkArgs.Count == 0)
        {
            return McpArguments.Empty;
        }

        var values = new Dictionary<string, string>(sdkArgs.Count, StringComparer.Ordinal);
        foreach (var (key, element) in sdkArgs)
        {
            var strValue = element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : element.GetRawText();
            values[key] = strValue;
        }

        return new McpArguments(values);
    }
}
