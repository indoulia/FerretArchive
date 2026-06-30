namespace Ferret.Core.Runtime;

/// <summary>Provides a module with access to its execution context and the module registry.</summary>
public interface IModuleContext
{
    /// <summary>Gets the identifier of the module this context belongs to.</summary>
    string ModuleId { get; }

    /// <summary>Gets the execution context for the current operation.</summary>
    IExecutionContext ExecutionContext { get; }

    /// <summary>Gets the module registry, allowing this module to discover peer modules.</summary>
    IModuleRegistry Registry { get; }
}
