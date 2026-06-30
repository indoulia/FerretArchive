using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class AssetDescriptorTests
{
    [Fact]
    public void AssetDescriptor_CanonicalUri_Is_Preserved()
    {
        var uri = new Uri("filesystem:///src/Program.cs");
        var desc = MakeDescriptor(uri);
        Assert.Equal(uri, desc.CanonicalUri);
    }

    [Fact]
    public void AssetDescriptor_Metadata_Defaults_To_Empty()
    {
        var desc = MakeDescriptor(new Uri("filesystem:///src/A.cs"));
        Assert.Empty(desc.Metadata);
    }

    [Fact]
    public void AssetDescriptor_Id_Matches_CanonicalUri()
    {
        var uri = new Uri("filesystem:///src/Program.cs");
        var desc = MakeDescriptor(uri);
        Assert.Equal(AssetId.From(uri), desc.Id);
    }

    private static AssetDescriptor MakeDescriptor(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = "Program.cs",
        LastModified = DateTimeOffset.UtcNow,
    };
}
