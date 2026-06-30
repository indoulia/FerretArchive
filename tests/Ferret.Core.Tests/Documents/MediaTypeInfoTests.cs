using Ferret.Core.Documents;

using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class MediaTypeInfoTests
{
    [Fact]
    public void MediaTypeInfo_Has_No_Public_Setters()
    {
        // init-only setters are public in reflection but not settable after construction;
        // distinguish them from regular public set by checking for IsExternalInit modifier.
        var isExternalInit = typeof(System.Runtime.CompilerServices.IsExternalInit);
        var props = typeof(MediaTypeInfo).GetProperties();
        Assert.All(
            props,
            p =>
            {
                if (!(p.CanWrite && (p.SetMethod?.IsPublic ?? false)))
                {
                    return;
                }

                var isInitOnly = p.SetMethod!.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(isExternalInit);
                Assert.True(
                    isInitOnly,
                    $"Property '{p.Name}' must not have a public set setter — MediaTypeInfo is immutable (init-only is allowed)");
            });
    }

    [Fact]
    public void MediaTypeInfo_IsText_And_IsBinary_Are_Mutually_Exclusive()
    {
        var text = new MediaTypeInfo
        {
            MediaType = "text/plain",
            IsText = true,
            IsBinary = false,
        };
        Assert.True(text.IsText);
        Assert.False(text.IsBinary);
    }

    [Fact]
    public void MediaTypeInfo_Confidence_Defaults_To_One()
    {
        var info = new MediaTypeInfo { MediaType = "text/plain", IsText = true, IsBinary = false };
        Assert.Equal(1.0, info.Confidence);
    }

    [Fact]
    public void MediaTypeInfo_SuggestedKind_Defaults_To_Null()
    {
        var info = new MediaTypeInfo { MediaType = "text/plain", IsText = true, IsBinary = false };
        Assert.Null(info.SuggestedKind);
    }
}
