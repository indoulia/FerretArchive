using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Registry;

/// <summary>
/// Performs topological sort over a set of DefaultModule entries using DFS, respecting IModuleWithDependencies edges.
/// <para>Why: Modules must start in dependency order; the sort is computed once at build time, not at every startup.</para>
/// <para>Lifecycle: Stateless static utility — called once by RuntimeBuilder.Build().</para>
/// <para>Layer: Ferret.Runtime internal — used only by RuntimeBuilder.</para>
/// <para>Thread Safety: Thread Safe — stateless static method.</para>
/// </summary>
internal static class ModuleDependencyGraph
{
    /// <summary>Returns modules sorted in dependency order (dependencies first). Throws on cycles or missing IDs.</summary>
    public static IReadOnlyList<DefaultModule> Sort(IReadOnlyList<DefaultModule> modules)
    {
        var byId = modules.ToDictionary(m => m.Id);
        var sorted = new List<DefaultModule>(modules.Count);
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        foreach (var module in modules)
        {
            Visit(module, byId, sorted, visited, inStack);
        }

        return sorted;
    }

    private static void Visit(
        DefaultModule module,
        Dictionary<string, DefaultModule> byId,
        List<DefaultModule> sorted,
        HashSet<string> visited,
        HashSet<string> inStack)
    {
        if (visited.Contains(module.Id))
        {
            return;
        }

        if (!inStack.Add(module.Id))
        {
            throw new InvalidOperationException(
                $"Dependency cycle detected involving module '{module.Id}'.");
        }

        if (module is IModuleWithDependencies deps)
        {
            foreach (var depId in deps.DependsOn)
            {
                if (!byId.TryGetValue(depId, out var dep))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Id}' depends on '{depId}' which is not registered.");
                }

                Visit(dep, byId, sorted, visited, inStack);
            }
        }

        inStack.Remove(module.Id);
        visited.Add(module.Id);
        sorted.Add(module);
    }
}
