using Ferret.Core.Errors;

namespace Ferret.Core.Tests.Errors;

public sealed class ExceptionHierarchyTests
{
    [Fact]
    public void ValidationException_Is_FerretException() =>
        Assert.True(typeof(ValidationException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void ConfigurationException_Is_FerretException() =>
        Assert.True(typeof(ConfigurationException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void PlatformException_Is_FerretException() =>
        Assert.True(typeof(PlatformException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void SecurityException_Is_FerretException() =>
        Assert.True(typeof(SecurityException).IsSubclassOf(typeof(FerretException)));

    [Fact]
    public void PermissionDeniedException_Is_SecurityException() =>
        Assert.True(typeof(PermissionDeniedException).IsSubclassOf(typeof(SecurityException)));

    [Fact]
    public void ValidationException_Stores_Field_And_Constraint()
    {
        var ex = new ValidationException("name", "required", "Provide a name.");
        Assert.Equal("name", ex.Field);
        Assert.Equal("required", ex.Constraint);
        Assert.Equal("Provide a name.", ex.Guidance);
    }

    [Fact]
    public void PermissionDeniedException_Stores_Permission()
    {
        var ex = new PermissionDeniedException("workspace:read");
        Assert.Equal("workspace:read", ex.Permission);
    }
}
