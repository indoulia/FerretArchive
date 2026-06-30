namespace Ferret.Runtime.Modules;

/// <summary>
/// Optional interface that a module descriptor or DefaultModule subclass may implement to declare startup dependencies.
/// <para>Why: IModuleDescriptor has no Dependencies property (by design). This interface is the extension point for dependency ordering without forcing it into the Core contract.</para>
/// <para>Lifecycle: Checked once during ModuleDependencyGraph.Sort() at RuntimeBuilder.Build() time.</para>
/// <para>Layer: Ferret.Runtime — checked by ModuleDependencyGraph; never in Core.</para>
/// <para>Thread Safety: Single Thread Only — read-only at build time.</para>
/// </summary>
public interface IModuleWithDependencies
{
    /// <summary>Gets the module IDs that this module depends on. The runtime starts dependencies first.</summary>
    IReadOnlyList<string> DependsOn { get; }
}
