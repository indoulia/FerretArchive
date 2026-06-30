namespace Ferret.Core.Connectors;

/// <summary>Opaque fingerprint used for change detection. Never expose the raw value directly.</summary>
/// <param name="Algorithm">The algorithm used to produce this fingerprint (e.g. "lightweight", "sha256").</param>
/// <param name="Value">The opaque fingerprint value.</param>
public sealed record AssetFingerprint(string Algorithm, string Value)
{
    /// <summary>Creates a lightweight fingerprint from last-write-time and file size. No I/O required.</summary>
    /// <param name="lastWrite">The file's last-write timestamp.</param>
    /// <param name="sizeBytes">The file size in bytes.</param>
    /// <returns>A deterministic lightweight fingerprint.</returns>
    public static AssetFingerprint CreateLightweight(DateTimeOffset lastWrite, long sizeBytes) =>
        new("lightweight", $"{lastWrite.ToUnixTimeMilliseconds()}:{sizeBytes}");
}
