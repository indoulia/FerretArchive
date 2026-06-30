namespace Ferret.Core.Connectors;

/// <summary>Enriches an AssetDescriptor with additional metadata after discovery. Reserved for Sprint 9.</summary>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IAssetEnricher
{
    // Sprint 9: ValueTask<AssetDescriptor> EnrichAsync(AssetDescriptor asset, CancellationToken ct = default);
}
#pragma warning restore CA1040
