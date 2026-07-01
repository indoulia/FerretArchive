using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>Builds an <see cref="AssetDescriptor"/> from a filesystem path for benchmark/dispatch use.</summary>
internal static class TestAsset
{
    /// <summary>Creates an asset descriptor for the given file path and resolved media type.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="mediaType">The resolved media type.</param>
    /// <returns>An <see cref="AssetDescriptor"/>.</returns>
    public static AssetDescriptor For(string path, string mediaType)
    {
        var name = Path.GetFileName(path);
        var uri = new Uri("filesystem:///" + name);
        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("bench"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = name,
            LastModified = DateTimeOffset.UnixEpoch, // fixed: benchmark inputs are deterministic
            MediaType = mediaType,
        };
    }
}
