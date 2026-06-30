using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorConfigurationTests
{
    [Fact]
    public void GetValue_Returns_Null_For_Missing_Key()
    {
        var config = ConnectorConfiguration.Empty;

        Assert.Null(config.GetValue("missing"));
    }

    [Fact]
    public void GetValue_Is_Case_Insensitive()
    {
        var config = new ConnectorConfiguration(new Dictionary<string, string> { ["RootPath"] = "/src" });

        Assert.Equal("/src", config.GetValue("rootpath"));
        Assert.Equal("/src", config.GetValue("ROOTPATH"));
        Assert.Equal("/src", config.GetValue("RootPath"));
    }

    [Fact]
    public void GetValueOrDefault_Returns_Default_For_Missing_Key()
    {
        var config = ConnectorConfiguration.Empty;

        Assert.Equal("fallback", config.GetValueOrDefault("missing", "fallback"));
    }

    [Fact]
    public void With_Returns_New_Instance_With_Key_Set()
    {
        var original = ConnectorConfiguration.Empty;

        var updated = original.With("rootPath", "/src");

        Assert.Null(original.GetValue("rootPath"));
        Assert.Equal("/src", updated.GetValue("rootPath"));
    }

    [Fact]
    public void With_Overwrites_Existing_Key_Case_Insensitively()
    {
        var config = new ConnectorConfiguration(new Dictionary<string, string> { ["rootPath"] = "/old" });

        var updated = config.With("ROOTPATH", "/new");

        Assert.Equal("/new", updated.GetValue("rootPath"));
    }

    [Fact]
    public void Empty_Is_Shared_Singleton()
    {
        Assert.Same(ConnectorConfiguration.Empty, ConnectorConfiguration.Empty);
    }

    [Fact]
    public void AsReadOnlyDictionary_Returns_All_Keys()
    {
        var config = new ConnectorConfiguration(new Dictionary<string, string>
        {
            ["rootPath"] = ".",
            ["excludeExtensions"] = ".dll,.exe",
        });

        Assert.Equal(2, config.AsReadOnlyDictionary().Count);
    }

    [Fact]
    public void FromDictionary_Creates_Configuration_From_Dictionary()
    {
        var config = ConnectorConfiguration.FromDictionary(new Dictionary<string, string> { ["key"] = "val" });

        Assert.Equal("val", config.GetValue("key"));
    }
}
