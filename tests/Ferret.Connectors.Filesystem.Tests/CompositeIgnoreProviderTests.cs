using Ferret.Connectors.Filesystem.Ignore;
using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class CompositeIgnoreProviderTests
{
    [Fact]
    public void ShouldIgnore_Returns_True_When_Any_Provider_Returns_True()
    {
        var provider = new CompositeIgnoreProvider([new AlwaysIgnore(), new NeverIgnore()]);
        Assert.True(provider.ShouldIgnore(MakeAsset()));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_When_All_Providers_Return_False()
    {
        var provider = new CompositeIgnoreProvider([new NeverIgnore(), new NeverIgnore()]);
        Assert.False(provider.ShouldIgnore(MakeAsset()));
    }

    [Fact]
    public void ShouldIgnore_Returns_False_With_Empty_Provider_List()
    {
        var provider = new CompositeIgnoreProvider([]);
        Assert.False(provider.ShouldIgnore(MakeAsset()));
    }

    private static AssetDescriptor MakeAsset()
    {
        var uri = new Uri("filesystem:///any/file.cs");
        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("i"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = "file.cs",
            LastModified = DateTimeOffset.UtcNow,
        };
    }

    private sealed class AlwaysIgnore : IIgnoreProvider
    {
        public bool ShouldIgnore(AssetDescriptor asset) => true;
    }

    private sealed class NeverIgnore : IIgnoreProvider
    {
        public bool ShouldIgnore(AssetDescriptor asset) => false;
    }
}
