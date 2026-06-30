namespace Ferret.Core.Runtime;

/// <summary>Implemented by types that participate in the module lifecycle.</summary>
public interface ILifecycleParticipant
{
    /// <summary>Called before the module starts up. Use for pre-start validation or resource acquisition.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>Called after the module has fully started.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>Called before the module shuts down. Use for graceful termination of in-flight work.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>Called after the module has fully stopped.</summary>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default);
}
