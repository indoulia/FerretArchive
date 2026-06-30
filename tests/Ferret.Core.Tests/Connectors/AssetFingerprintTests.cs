using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class AssetFingerprintTests
{
    [Fact]
    public void CreateLightweight_Is_Deterministic()
    {
        var t = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        var a = AssetFingerprint.CreateLightweight(t, 1024);
        var b = AssetFingerprint.CreateLightweight(t, 1024);
        Assert.Equal(a, b);
    }

    [Fact]
    public void CreateLightweight_Differs_For_Different_Size()
    {
        var t = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        var a = AssetFingerprint.CreateLightweight(t, 1024);
        var b = AssetFingerprint.CreateLightweight(t, 2048);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void CreateLightweight_Algorithm_Is_Lightweight()
    {
        var fp = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);
        Assert.Equal("lightweight", fp.Algorithm);
    }
}
