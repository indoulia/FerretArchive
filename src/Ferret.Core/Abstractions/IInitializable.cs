namespace Ferret.Core.Abstractions;

/// <summary>Represents a component that requires explicit asynchronous initialization before use.</summary>
public interface IInitializable
{
    /// <summary>Initializes this component asynchronously.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that represents the initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
