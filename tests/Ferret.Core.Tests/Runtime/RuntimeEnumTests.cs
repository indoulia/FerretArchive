using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Xunit;

namespace Ferret.Core.Tests.Runtime;

#pragma warning disable SA1402 // File may only contain a single type

public sealed class RuntimeEnumTests
{
    [Fact]
    public void RuntimeState_HasExpectedValues()
    {
        Assert.Equal(0, (int)RuntimeState.Stopped);
        Assert.Equal(1, (int)RuntimeState.Starting);
        Assert.Equal(2, (int)RuntimeState.Running);
        Assert.Equal(3, (int)RuntimeState.Stopping);
        Assert.Equal(4, (int)RuntimeState.Faulted);
    }

    [Fact]
    public void ModuleState_HasExpectedValues()
    {
        Assert.Equal(0, (int)ModuleState.Unloaded);
        Assert.Equal(1, (int)ModuleState.Loading);
        Assert.Equal(2, (int)ModuleState.Active);
        Assert.Equal(3, (int)ModuleState.Deactivating);
        Assert.Equal(4, (int)ModuleState.Stopped);
        Assert.Equal(5, (int)ModuleState.Faulted);
    }

    [Fact]
    public void ModuleCapability_IsFlags()
    {
        Assert.Equal(0, (int)ModuleCapability.None);
        Assert.Equal(1, (int)ModuleCapability.Indexing);
        Assert.Equal(2, (int)ModuleCapability.Knowledge);
        Assert.Equal(4, (int)ModuleCapability.Review);
        Assert.Equal(8, (int)ModuleCapability.Specification);
        Assert.Equal(16, (int)ModuleCapability.Memory);
        Assert.Equal(32, (int)ModuleCapability.Workspace);
        Assert.Equal(64, (int)ModuleCapability.Artifact);
    }

    [Fact]
    public void ModuleCapability_CanCombineFlags()
    {
        var combined = ModuleCapability.Indexing | ModuleCapability.Knowledge;
        Assert.True(combined.HasFlag(ModuleCapability.Indexing));
        Assert.True(combined.HasFlag(ModuleCapability.Knowledge));
        Assert.False(combined.HasFlag(ModuleCapability.Review));
    }

    [Fact]
    public void ModuleCapability_NoneIsZero()
    {
        Assert.Equal(ModuleCapability.None, (ModuleCapability)0);
    }
}

public sealed class ModuleMetadataTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsMetadata()
    {
        var version = SemanticVersion.Create(1, 0, 0);
        var caps = new[] { ModuleCapability.Workspace };

        var metadata = ModuleMetadata.Create("workspace", "Workspace Module", version, caps, "Manages workspace lifecycle.", "Ferret Core Team");

        Assert.Equal("workspace", metadata.Id);
        Assert.Equal("Workspace Module", metadata.Name);
        Assert.Equal(version, metadata.Version);
        Assert.Contains(ModuleCapability.Workspace, metadata.Capabilities);
        Assert.Equal("Manages workspace lifecycle.", metadata.Description);
        Assert.Equal("Ferret Core Team", metadata.Author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankId_ThrowsArgumentException(string id)
    {
        var version = SemanticVersion.Create(1, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            ModuleMetadata.Create(id, "Name", version, Array.Empty<ModuleCapability>(), string.Empty, string.Empty));
    }
}
