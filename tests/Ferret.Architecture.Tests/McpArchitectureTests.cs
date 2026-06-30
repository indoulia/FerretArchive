using System.Reflection;

using Ferret.Mcp;
using Ferret.Mcp.Transport.Stdio;

using Xunit;

namespace Ferret.Architecture.Tests;

/// <summary>Architectural compliance tests enforcing ADR-0018 MCP isolation rules.</summary>
public sealed class McpArchitectureTests
{
    private const string McpSdkPrefix = "ModelContextProtocol";

    private static readonly Assembly McpAssembly = typeof(McpModule).Assembly;

    /// <summary>Only Transport.Stdio types may reference the MCP SDK.</summary>
    [Fact]
    public void OnlyStdioNamespace_MayReference_McpSdk()
    {
        var violations = McpAssembly.GetTypes()
            .Where(t =>
                t.Namespace is not null &&
                !t.Namespace.StartsWith("Ferret.Mcp.Transport.Stdio", StringComparison.Ordinal) &&
                TypeReferencesMcpSdk(t))
            .Select(t => t.FullName!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Only Transport.Stdio may reference ModelContextProtocol SDK. Violating types: {string.Join(", ", violations)}");
    }

    /// <summary>IMcpTool implementations must be sealed.</summary>
    [Fact]
    public void IMcpTool_Implementations_Must_Be_Sealed()
    {
        var toolInterface = typeof(Ferret.Mcp.Protocol.IMcpTool);
        var violations = McpAssembly.GetTypes()
            .Where(t => toolInterface.IsAssignableFrom(t) && t.IsClass && !t.IsSealed)
            .Select(t => t.FullName!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"IMcpTool implementations must be sealed. Non-sealed: {string.Join(", ", violations)}");
    }

    /// <summary>IMcpResource implementations must be sealed.</summary>
    [Fact]
    public void IMcpResource_Implementations_Must_Be_Sealed()
    {
        var resourceInterface = typeof(Ferret.Mcp.Protocol.IMcpResource);
        var violations = McpAssembly.GetTypes()
            .Where(t => resourceInterface.IsAssignableFrom(t) && t.IsClass && !t.IsSealed)
            .Select(t => t.FullName!)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"IMcpResource implementations must be sealed. Non-sealed: {string.Join(", ", violations)}");
    }

    /// <summary>Ferret.Mcp must not reference Ferret.Cli.</summary>
    [Fact]
    public void McpAssembly_Must_Not_Reference_Ferret_Cli()
    {
        var references = McpAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(
            references,
            name => name.Equals("Ferret.Cli", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>StdioTransport is the only type with MCP SDK references in the public surface.</summary>
    [Fact]
    public void StdioTransport_Should_Reference_McpSdk()
    {
        Assert.True(
            TypeReferencesMcpSdk(typeof(StdioTransport)),
            "StdioTransport is expected to reference the MCP SDK for stdio transport.");
    }

    private static bool TypeReferencesMcpSdk(Type type)
    {
        try
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Any(f => f.FieldType.FullName?.StartsWith(McpSdkPrefix, StringComparison.Ordinal) ?? false)
                || type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .SelectMany(m => m.GetParameters())
                    .Any(p => p.ParameterType.FullName?.StartsWith(McpSdkPrefix, StringComparison.Ordinal) ?? false)
                || type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Any(m => m.ReturnType.FullName?.StartsWith(McpSdkPrefix, StringComparison.Ordinal) ?? false);
        }
        catch (ReflectionTypeLoadException)
        {
            return false;
        }
    }
}
