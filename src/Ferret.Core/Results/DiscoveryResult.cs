namespace Ferret.Core.Results;

/// <summary>Represents the result of a discovery operation that finds items of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of items discovered.</typeparam>
public sealed class DiscoveryResult<T>
{
    /// <summary>Initializes a new instance of the <see cref="DiscoveryResult{T}"/> class.</summary>
    /// <param name="items">The discovered items.</param>
    /// <param name="isComplete">Indicates whether discovery is complete or was truncated.</param>
    public DiscoveryResult(IReadOnlyList<T> items, bool isComplete)
    {
        Items = items;
        IsComplete = isComplete;
    }

    /// <summary>Gets the discovered items.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets a value indicating whether discovery completed without truncation.</summary>
    public bool IsComplete { get; }
}
