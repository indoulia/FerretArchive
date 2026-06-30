using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Ignore;

/// <summary>Chains multiple IIgnoreProvider instances. Returns true if any provider returns true.</summary>
public sealed class CompositeIgnoreProvider : IIgnoreProvider
{
    private readonly IReadOnlyList<IIgnoreProvider> _providers;

    /// <summary>Initializes a new instance of the <see cref="CompositeIgnoreProvider"/> class.</summary>
    /// <param name="providers">The collection of providers to chain.</param>
    public CompositeIgnoreProvider(IReadOnlyList<IIgnoreProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers;
    }

    /// <inheritdoc/>
    public bool ShouldIgnore(AssetDescriptor asset) =>
        _providers.Any(p => p.ShouldIgnore(asset));
}
