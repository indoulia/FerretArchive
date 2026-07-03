using Ferret.Core.Documents;

namespace Ferret.Core.Tests.Documents;

public sealed class MediaTypeInfoTests
{
    [Fact]
    public void Text_Category_IsText_True_IsBinary_False()
    {
        var info = new MediaTypeInfo { MediaType = "text/plain", Category = MediaCategory.Text };
        Assert.True(info.IsText);
        Assert.False(info.IsBinary);
    }

    [Fact]
    public void BinaryParseable_IsText_False_IsBinary_True()
    {
        var info = new MediaTypeInfo { MediaType = "application/pdf", Category = MediaCategory.BinaryParseable };
        Assert.False(info.IsText);
        Assert.True(info.IsBinary);
    }

    [Fact]
    public void BinaryOpaque_IsText_False_IsBinary_True()
    {
        var info = new MediaTypeInfo { MediaType = "application/octet-stream", Category = MediaCategory.BinaryOpaque };
        Assert.False(info.IsText);
        Assert.True(info.IsBinary);
    }

    [Fact]
    public void Unknown_Is_BinaryOpaque()
    {
        Assert.Equal(MediaCategory.BinaryOpaque, MediaTypeInfo.Unknown.Category);
    }
}
