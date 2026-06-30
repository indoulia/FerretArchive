namespace Ferret.Core.Runtime;

/// <summary>Configures and builds a runtime host from registered module descriptors.</summary>
public interface IRuntimeBuilder
{
    /// <summary>Registers a module descriptor with the builder.</summary>
    /// <param name="descriptor">The module descriptor to register.</param>
    /// <returns>The same builder instance, to allow call chaining.</returns>
    IRuntimeBuilder AddModule(IModuleDescriptor descriptor);

    /// <summary>Constructs the configured runtime host.</summary>
    /// <returns>A configured runtime host.</returns>
    IRuntimeHost Build();
}
