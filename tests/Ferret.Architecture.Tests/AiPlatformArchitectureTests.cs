using System.Reflection;

using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Providers.Ollama;
using Ferret.Providers.OpenAi;

namespace Ferret.Architecture.Tests;

/// <summary>Architectural compliance tests enforcing ADR-0019 AI platform isolation rules.</summary>
public sealed class AiPlatformArchitectureTests
{
    // Core contracts assembly — never changes regardless of which providers are registered.
    private static readonly Assembly CoreAiAssembly = typeof(IModelProvider).Assembly;

    // Discover all provider assemblies dynamically so new providers (e.g. Ferret.Providers.Anthropic)
    // are automatically covered without editing this file.
    private static readonly IReadOnlyList<Assembly> ProviderAssemblies =
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => (a.GetName().Name ?? string.Empty)
                .StartsWith("Ferret.Providers.", StringComparison.Ordinal))
            .ToList()
            .AsReadOnly();

    // Ensure the assemblies under test are loaded into the AppDomain before discovery runs.
    static AiPlatformArchitectureTests()
    {
        _ = typeof(OllamaModelProvider).Assembly;
        _ = typeof(OpenAiModelProvider).Assembly;
    }

    /// <summary>Ferret.Core must not reference any vendor AI SDK assemblies.</summary>
    [Fact]
    public void FerretCore_Must_Not_Reference_VendorSdks()
    {
        var vendorPrefixes = new[] { "OllamaSharp", "OpenAI", "Azure.AI", "Anthropic" };

        var violations = CoreAiAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => vendorPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Ferret.Core must not reference vendor AI SDKs. Found: {string.Join(", ", violations)}");
    }

    /// <summary>IModelProvider implementations must be sealed.</summary>
    [Fact]
    public void IModelProvider_Implementations_Must_Be_Sealed()
    {
        var nonSealed = ProviderAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IModelProvider).IsAssignableFrom(t) && t.IsClass && !t.IsSealed)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(
            nonSealed.Count == 0,
            $"IModelProvider implementations must be sealed. Non-sealed: {string.Join(", ", nonSealed)}");
    }

    /// <summary>ModelDescriptor must have no mutable public setters (only init-only or read-only).</summary>
    [Fact]
    public void ModelDescriptor_Must_Have_No_Mutable_Public_Setters()
    {
        var violations = typeof(ModelDescriptor).GetProperties()
            .Where(p =>
            {
                var setMethod = p.SetMethod;
                if (setMethod is null || !setMethod.IsPublic)
                {
                    return false;
                }

                var isInitOnly = setMethod.ReturnParameter?
                    .GetRequiredCustomModifiers()
                    .Any(m => m.Name == "IsExternalInit") ?? false;

                return !isInitOnly;
            })
            .Select(p => p.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"ModelDescriptor must have no mutable public setters. Violating: {string.Join(", ", violations)}");
    }

    /// <summary>No provider assembly may reference another provider assembly.</summary>
    [Fact]
    public void ProviderAssemblies_Must_Not_Cross_Reference_Each_Other()
    {
        var providerNames = ProviderAssemblies
            .Select(a => a.GetName().Name ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var violations = new List<string>();
        foreach (var assembly in ProviderAssemblies)
        {
            var crossRefs = assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(name => providerNames.Contains(name) &&
                               !string.Equals(name, assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var crossRef in crossRefs)
            {
                violations.Add($"{assembly.GetName().Name} → {crossRef}");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Provider assemblies must not reference each other. Violations: {string.Join(", ", violations)}");
    }
}
