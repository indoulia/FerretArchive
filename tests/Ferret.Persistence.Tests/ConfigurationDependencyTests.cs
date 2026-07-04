using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class ConfigurationDependencyTests
{
    [Fact]
    public void Records_With_Identical_Values_Are_Equal()
    {
        var a = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
            Connector = new ComponentRegistrationIdentity { Id = "filesystem", Version = "1.0" },
        };
        var b = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
            Connector = new ComponentRegistrationIdentity { Id = "filesystem", Version = "1.0" },
        };

        Assert.Equal(a, b);
    }

    [Fact]
    public void Records_With_Different_Parser_Version_Are_Not_Equal()
    {
        var a = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
        };
        var b = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "2.0" },
        };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Parser_And_Connector_Are_Independently_Optional()
    {
        var parserOnly = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
        };

        Assert.NotNull(parserOnly.Parser);
        Assert.Null(parserOnly.Connector);
    }
}
