namespace Ferret.Core.Runtime;

/// <summary>Provides read access to the set of modules registered with the runtime host.</summary>
public interface IModuleRegistry
{
    /// <summary>Gets all active modules.</summary>
    IReadOnlyCollection<IModule> Modules { get; }

    /// <summary>Attempts to retrieve a module by its identifier.</summary>
    /// <param name="moduleId">The identifier of the module to retrieve.</param>
    /// <param name="module">When this method returns, contains the module if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the module was found; otherwise <see langword="false"/>.</returns>
    bool TryGet(string moduleId, out IModule? module);

    /// <summary>Retrieves a module by its identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="moduleId">The identifier of the module to retrieve.</param>
    /// <returns>The module if found; otherwise <see langword="null"/>.</returns>
    IModule? GetById(string moduleId);
}
