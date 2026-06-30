using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorContractTests
{
    [Fact]
    public void ConnectorType_HasExpectedValues()
    {
        Assert.Equal(0, (int)ConnectorType.Filesystem);
        Assert.Equal(1, (int)ConnectorType.Git);
        Assert.Equal(99, (int)ConnectorType.Custom);
    }

    [Fact]
    public void ConnectorMetadata_Create_StoresValues()
    {
        var meta = ConnectorMetadata.Create("fs-001", "Filesystem", "Local filesystem connector", ConnectorType.Filesystem, "1.0");
        Assert.Equal("fs-001", meta.Id);
        Assert.Equal("Filesystem", meta.Name);
        Assert.Equal(ConnectorType.Filesystem, meta.ConnectorType);
        Assert.Equal("1.0", meta.Version);
    }

    [Fact]
    public void ConnectorIoCapabilities_Create_StoresValues()
    {
        var caps = ConnectorIoCapabilities.Create(canRead: true, canWrite: false, canStream: true, supportsChangeDetection: true);
        Assert.True(caps.CanRead);
        Assert.False(caps.CanWrite);
        Assert.True(caps.SupportsChangeDetection);
    }

    [Fact]
    public void ConnectorIoCapabilities_ReadOnly_OnlyCanRead()
    {
        var caps = ConnectorIoCapabilities.ReadOnly();
        Assert.True(caps.CanRead);
        Assert.False(caps.CanWrite);
        Assert.False(caps.CanStream);
        Assert.False(caps.SupportsChangeDetection);
    }

    [Fact]
    public void ConnectorHealth_Connected_IsConnected()
    {
        var health = ConnectorHealth.Connected(DateTimeOffset.UtcNow);
        Assert.True(health.IsConnected);
        Assert.Null(health.ErrorMessage);
    }

    [Fact]
    public void ConnectorHealth_Disconnected_HasErrorMessage()
    {
        var health = ConnectorHealth.Disconnected("timeout", DateTimeOffset.UtcNow);
        Assert.False(health.IsConnected);
        Assert.Equal("timeout", health.ErrorMessage);
    }
}
