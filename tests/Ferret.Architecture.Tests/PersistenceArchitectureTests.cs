using System.Reflection;

using Ferret.Persistence;

using Xunit;

namespace Ferret.Architecture.Tests;

/// <summary>
/// S2-0 (Architecture Regression Protection): encodes the five invariants approved in
/// docs/archive/superpowers/plans/2026-07-04-v2-sprint-2-architecture-review.md §4.1. All five are GREEN
/// as of S2-1B: S2-1A fixed invariant 1 (dependency inversion), S2-1B fixed invariant 2 (assembly
/// direction) by relocating the vertical slice from <c>Ferret.Integration.Tests</c> to the new
/// non-test assembly <c>Ferret.VerticalSlice</c> — this is why invariant 1 now loads that
/// assembly instead. Reflection-only, same style as <c>ConnectorArchitectureTests</c>; no IL inspection.
/// </summary>
public sealed class PersistenceArchitectureTests
{
    private static readonly Assembly VerticalSliceHostAssembly = Assembly.Load("Ferret.VerticalSliceHost");
    private static readonly Assembly VerticalSliceAssembly = Assembly.Load("Ferret.VerticalSlice");

    private static readonly HashSet<Type> AllowedDependencyStateStoreDataTypes = new()
    {
        typeof(string),
        typeof(CancellationToken),
        typeof(DependencyRecord),
    };

    /// <summary>
    /// Invariant 1 (GREEN as of S2-1A): VerticalSliceCommandHandler must receive
    /// IDependencyStateStore via constructor injection rather than constructing
    /// SpikeDependencyStateStore directly. Fixed in S2-1A; the type now lives in
    /// <c>Ferret.VerticalSlice</c> as of S2-1B, so this loads that assembly.
    /// </summary>
    [Fact]
    public void VerticalSliceCommandHandler_Must_Depend_On_IDependencyStateStore_Via_Constructor()
    {
        var handlerType = VerticalSliceAssembly.GetTypes()
            .Single(t => t.Name == "VerticalSliceCommandHandler");

        var hasStoreConstructorParameter = handlerType
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IDependencyStateStore)));

        var message = "VerticalSliceCommandHandler must receive IDependencyStateStore through its constructor " +
            "instead of constructing SpikeDependencyStateStore directly. Fixed by S2-1A.";
        Assert.True(hasStoreConstructorParameter, message);
    }

    /// <summary>
    /// Invariant 2 (GREEN as of S2-1B): the vertical-slice host (production-shaped code)
    /// must not reference a test assembly. Fixed by relocating the vertical slice out of
    /// <c>Ferret.Integration.Tests</c> into the new <c>Ferret.VerticalSlice</c> assembly.
    /// </summary>
    [Fact]
    public void VerticalSliceHost_Assembly_Must_Not_Reference_A_Test_Assembly()
    {
        var testAssemblyReferences = VerticalSliceHostAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var message = "Ferret.VerticalSliceHost must not reference a test assembly. Found: " +
            string.Join(", ", testAssemblyReferences) + ". Fixed by S2-1B.";
        Assert.True(testAssemblyReferences.Count == 0, message);
    }

    /// <summary>
    /// Invariant 3 (expected GREEN): RequestEquivalence.AreEquivalent must remain a static,
    /// non-awaitable pure function (ARCH-028 §3, ARCH-033 §11).
    /// </summary>
    [Fact]
    public void RequestEquivalence_AreEquivalent_Must_Be_Static_And_Not_Awaitable()
    {
        var method = typeof(RequestEquivalence).GetMethod(nameof(RequestEquivalence.AreEquivalent));

        Assert.NotNull(method);
        Assert.True(method!.IsStatic, "RequestEquivalence.AreEquivalent must remain static.");
        Assert.False(
            IsAwaitableReturnType(method.ReturnType),
            $"RequestEquivalence.AreEquivalent must not become awaitable (found {method.ReturnType.FullName}).");
    }

    /// <summary>
    /// Invariant 4 (expected GREEN): ResolutionCheck.Compare must remain a static, non-awaitable
    /// pure function (ARCH-033 §5).
    /// </summary>
    [Fact]
    public void ResolutionCheck_Compare_Must_Be_Static_And_Not_Awaitable()
    {
        var method = typeof(ResolutionCheck).GetMethod(nameof(ResolutionCheck.Compare));

        Assert.NotNull(method);
        Assert.True(method!.IsStatic, "ResolutionCheck.Compare must remain static.");
        Assert.False(
            IsAwaitableReturnType(method.ReturnType),
            $"ResolutionCheck.Compare must not become awaitable (found {method.ReturnType.FullName}).");
    }

    /// <summary>
    /// Invariant 5 (expected GREEN): IDependencyStateStore must name no storage technology, file
    /// format, or key structure in its method signatures (T2's original acceptance criterion,
    /// now enforced automatically instead of by one-time inspection).
    /// </summary>
    [Fact]
    public void IDependencyStateStore_Methods_Must_Not_Reference_Storage_Technology_Types()
    {
        var violations = new List<string>();

        foreach (var method in typeof(IDependencyStateStore).GetMethods())
        {
            foreach (var parameter in method.GetParameters())
            {
                if (!IsAllowedDependencyStateStoreDataType(parameter.ParameterType))
                {
                    violations.Add($"{method.Name}({parameter.Name}: {parameter.ParameterType.FullName})");
                }
            }

            var unwrappedReturn = UnwrapAwaitableValueType(method.ReturnType);
            if (unwrappedReturn is not null && !IsAllowedDependencyStateStoreDataType(unwrappedReturn))
            {
                violations.Add($"{method.Name} return {unwrappedReturn.FullName}");
            }
        }

        var message = "IDependencyStateStore must name no storage technology, file format, or key structure. " +
            "Violations: " + string.Join(", ", violations);
        Assert.True(violations.Count == 0, message);
    }

    private static bool IsAllowedDependencyStateStoreDataType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return AllowedDependencyStateStoreDataTypes.Contains(underlying);
    }

    private static bool IsAwaitableReturnType(Type returnType)
    {
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            return true;
        }

        if (returnType.IsGenericType)
        {
            var definition = returnType.GetGenericTypeDefinition();
            return definition == typeof(Task<>) || definition == typeof(ValueTask<>);
        }

        return false;
    }

    private static Type? UnwrapAwaitableValueType(Type returnType)
    {
        if (!returnType.IsGenericType)
        {
            return null;
        }

        var definition = returnType.GetGenericTypeDefinition();
        return definition == typeof(Task<>) || definition == typeof(ValueTask<>)
            ? returnType.GetGenericArguments()[0]
            : null;
    }
}
