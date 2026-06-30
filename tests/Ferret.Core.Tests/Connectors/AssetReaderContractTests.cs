using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class AssetReaderContractTests
{
    [Fact]
    public void IAssetReader_Is_An_Interface()
    {
        Assert.True(typeof(IAssetReader).IsInterface);
    }

    [Fact]
    public void IAssetReader_Has_OpenAsync_Method()
    {
        var method = typeof(IAssetReader).GetMethod("OpenAsync");

        Assert.NotNull(method);
    }

    [Fact]
    public void IAssetReader_OpenAsync_Returns_Task_Of_Stream()
    {
        var method = typeof(IAssetReader).GetMethod("OpenAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Stream>), method.ReturnType);
    }

    [Fact]
    public void IAssetReader_Is_Separate_From_IAssetSource()
    {
        Assert.NotEqual(typeof(IAssetReader), typeof(IAssetSource));
        Assert.False(typeof(IAssetReader).IsAssignableTo(typeof(IAssetSource)));
        Assert.False(typeof(IAssetSource).IsAssignableTo(typeof(IAssetReader)));
    }

    [Fact]
    public void IndexResult_Has_AssetsProcessed_Property()
    {
        var prop = typeof(Ferret.Core.Indexing.IndexResult).GetProperty("AssetsProcessed");

        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop.PropertyType);
    }

    [Fact]
    public void IndexPipelineOptions_Has_ForceRebuild_Property()
    {
        var prop = typeof(Ferret.Core.Indexing.IndexPipelineOptions).GetProperty("ForceRebuild");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void IndexPipelineOptions_Default_ForceRebuild_Is_False()
    {
        Assert.False(Ferret.Core.Indexing.IndexPipelineOptions.Default.ForceRebuild);
    }

    [Fact]
    public void IIndexEngine_Has_ClearAsync_Not_RebuildAsync()
    {
        var clearMethod = typeof(Ferret.Core.Indexing.IIndexEngine).GetMethod("ClearAsync");
        var rebuildMethod = typeof(Ferret.Core.Indexing.IIndexEngine).GetMethod("RebuildAsync");

        Assert.NotNull(clearMethod);
        Assert.Null(rebuildMethod);
    }

    [Fact]
    public void DocumentDiscoveredEvent_Exists_In_Events_Indexing_Namespace()
    {
        var type = typeof(Ferret.Core.Events.Indexing.DocumentDiscoveredEvent);

        Assert.NotNull(type);
    }

    [Fact]
    public void DocumentDiscoveredEvent_Has_AssetId_Property()
    {
        var prop = typeof(Ferret.Core.Events.Indexing.DocumentDiscoveredEvent)
            .GetProperty("AssetId");

        Assert.NotNull(prop);
        Assert.Equal(typeof(Ferret.Core.Connectors.AssetId), prop.PropertyType);
    }
}
