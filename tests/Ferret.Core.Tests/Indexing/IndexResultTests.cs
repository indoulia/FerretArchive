using Ferret.Core.Indexing;
using Xunit;

namespace Ferret.Core.Tests.Indexing;

public sealed class IndexResultTests
{
    [Fact]
    public void IndexResult_FailureMessages_Defaults_To_Empty()
    {
        var result = MakeResult();
        Assert.Empty(result.FailureMessages);
        Assert.Empty(result.WarningMessages);
    }

    [Fact]
    public void IndexResult_Has_No_Public_Setters()
    {
        // init-only setters are public in reflection but not settable after construction;
        // distinguish them from regular public set by checking for IsExternalInit modifier.
        var isExternalInit = typeof(System.Runtime.CompilerServices.IsExternalInit);
        var props = typeof(IndexResult).GetProperties();
        Assert.All(
            props,
            p =>
            {
                var setter = p.SetMethod;
                if (setter == null || !setter.IsPublic)
                {
                    return; // no setter or non-public — fine
                }

                var isInitOnly = setter.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(isExternalInit);
                Assert.True(
                    isInitOnly,
                    $"Property '{p.Name}' must not have a public set setter — IndexResult is immutable (init-only is allowed)");
            });
    }

    [Fact]
    public void IndexStats_Has_No_Public_Setters()
    {
        var isExternalInit = typeof(System.Runtime.CompilerServices.IsExternalInit);
        var props = typeof(IndexStats).GetProperties();
        Assert.All(
            props,
            p =>
            {
                var setter = p.SetMethod;
                if (setter == null || !setter.IsPublic)
                {
                    return; // no setter or non-public — fine
                }

                var isInitOnly = setter.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(isExternalInit);
                Assert.True(
                    isInitOnly,
                    $"Property '{p.Name}' must not have a public set setter — IndexStats is immutable (init-only is allowed)");
            });
    }

    [Fact]
    public void IndexPipelineOptions_Default_Has_No_InstanceId_Filter_And_No_ForceRebuild()
    {
        Assert.Null(IndexPipelineOptions.Default.InstanceId);
        Assert.False(IndexPipelineOptions.Default.ForceRebuild);
    }

    private static IndexResult MakeResult() => new()
    {
        AssetsDiscovered = 10,
        AssetsProcessed = 10,
        DocumentsIndexed = 8,
        DocumentsSkipped = 2,
        Failures = 0,
        Warnings = 0,
        Duration = TimeSpan.FromSeconds(1.5),
    };
}
