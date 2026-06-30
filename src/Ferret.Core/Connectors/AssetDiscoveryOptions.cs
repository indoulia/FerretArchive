namespace Ferret.Core.Connectors;

/// <summary>Options controlling asset discovery behaviour.</summary>
public sealed class AssetDiscoveryOptions
{
    /// <summary>Gets a shared default instance with no options set.</summary>
    public static AssetDiscoveryOptions Default { get; } = new();

    /// <summary>Gets an optional ignore policy applied per asset during discovery.</summary>
    public IIgnoreProvider? IgnoreProvider { get; init; }
}
