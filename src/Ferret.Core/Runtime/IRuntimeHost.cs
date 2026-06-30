namespace Ferret.Core.Runtime;

/// <summary>Manages the platform module lifecycle from startup through shutdown.</summary>
public interface IRuntimeHost
{
    /// <summary>Gets the current state of the runtime.</summary>
    RuntimeState State { get; }

    /// <summary>Gets the module registry for the active runtime.</summary>
    IModuleRegistry Modules { get; }

    /// <summary>Starts the runtime and activates all registered modules.</summary>
    /// <param name="cancellationToken">A token to cancel the startup sequence.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the runtime and deactivates all active modules.</summary>
    /// <param name="cancellationToken">A token to cancel the shutdown sequence.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
