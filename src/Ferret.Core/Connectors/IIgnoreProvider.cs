namespace Ferret.Core.Connectors;

/// <summary>
/// Determines whether an asset should be excluded from discovery.
/// Implementations MUST return false for URI schemes they do not understand.
/// ShouldIgnore is pure — no I/O, no state mutation.
/// </summary>
public interface IIgnoreProvider
{
    /// <summary>Returns true if the asset should be excluded from discovery results.</summary>
    /// <param name="asset">The asset to evaluate.</param>
    /// <returns>True to exclude; false to include.</returns>
    bool ShouldIgnore(AssetDescriptor asset);
}
