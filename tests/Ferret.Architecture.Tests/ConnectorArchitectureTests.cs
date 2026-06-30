using System.Reflection;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Architecture.Tests;

/// <summary>Architectural compliance tests enforcing ADR-0013 rules.</summary>
public sealed class ConnectorArchitectureTests
{
    private static readonly Assembly FilesystemAssembly = typeof(FilesystemConnector).Assembly;

    /// <summary>IConnector implementations must be sealed.</summary>
    [Fact]
    public void IConnector_Implementations_Must_Be_Sealed()
    {
        var nonSealed = FilesystemAssembly.GetTypes()
            .Where(t => typeof(IConnector).IsAssignableFrom(t) && t.IsClass && !t.IsSealed)
            .Select(t => t.FullName)
            .ToList();

        Assert.True(
            nonSealed.Count == 0,
            $"IConnector implementations must be sealed. Non-sealed: {string.Join(", ", nonSealed)}");
    }

    /// <summary>AssetDescriptor must have no public setters.</summary>
    [Fact]
    public void AssetDescriptor_Must_Have_No_Public_Setters()
    {
        var violations = typeof(AssetDescriptor).GetProperties()
            .Where(p =>
            {
                var setMethod = p.SetMethod;
                var hasPublicSetter = setMethod?.IsPublic ?? false;
                var isInitOnly = setMethod?.ReturnParameter?.GetRequiredCustomModifiers()
                    .Any(m => m.Name == "IsExternalInit") ?? false;
                return hasPublicSetter && !isInitOnly;
            })
            .Select(p => p.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"AssetDescriptor must have no public setters. Violating properties: {string.Join(", ", violations)}");
    }

    /// <summary>ConnectorDescriptor must have no public setters.</summary>
    [Fact]
    public void ConnectorDescriptor_Must_Have_No_Public_Setters()
    {
        var violations = typeof(ConnectorDescriptor).GetProperties()
            .Where(p =>
            {
                var setMethod = p.SetMethod;
                var hasPublicSetter = setMethod?.IsPublic ?? false;
                var isInitOnly = setMethod?.ReturnParameter?.GetRequiredCustomModifiers()
                    .Any(m => m.Name == "IsExternalInit") ?? false;
                return hasPublicSetter && !isInitOnly;
            })
            .Select(p => p.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"ConnectorDescriptor must have no public setters. Violating properties: {string.Join(", ", violations)}");
    }

    /// <summary>Filesystem assembly must not reference Ferret.Cli.</summary>
    [Fact]
    public void Filesystem_Assembly_Must_Not_Reference_Ferret_Cli()
    {
        var references = FilesystemAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(
            references,
            name => name.Equals("Ferret.Cli", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>IAssetSource.DiscoverAsync must return IAsyncEnumerable of AssetDescriptor.</summary>
    [Fact]
    public void IAssetSource_DiscoverAsync_Must_Return_IAsyncEnumerable_Of_AssetDescriptor()
    {
        var method = typeof(IAssetSource).GetMethod(nameof(IAssetSource.DiscoverAsync));

        Assert.NotNull(method);

        var returnType = method!.ReturnType;
        Assert.True(
            returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
            returnType.GetGenericArguments()[0] == typeof(AssetDescriptor),
            $"IAssetSource.DiscoverAsync must return IAsyncEnumerable<AssetDescriptor> but returns {returnType.FullName}");
    }

    /// <summary>IIgnoreProvider.ShouldIgnore must return bool.</summary>
    [Fact]
    public void IIgnoreProvider_ShouldIgnore_Must_Return_Bool()
    {
        var method = typeof(IIgnoreProvider).GetMethod(nameof(IIgnoreProvider.ShouldIgnore));

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }
}
