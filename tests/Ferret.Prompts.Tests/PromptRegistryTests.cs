using Ferret.Core.Ai.Prompts;
using Ferret.Prompts;

namespace Ferret.Prompts.Tests;

public sealed class PromptRegistryTests
{
    private static PromptTemplate Make(string name, string version) =>
        new PromptTemplate
        {
            Name = name,
            Version = version,
            Template = "Hello {{name}}",
            RequiredVariables = ["name"],
        };

    [Fact]
    public void GetAll_Empty_ReturnsEmptyList()
    {
        var sut = new PromptRegistry([]);
        Assert.Empty(sut.GetAll());
    }

    [Fact]
    public void GetAll_WithTemplates_ReturnsAll()
    {
        var t1 = Make("greet", "1.0.0");
        var t2 = Make("farewell", "1.0.0");
        var sut = new PromptRegistry([t1, t2]);
        Assert.Equal(2, sut.GetAll().Count);
    }

    [Fact]
    public void GetByVersion_ExactMatch_ReturnsTemplate()
    {
        var t = Make("greet", "1.0.0");
        var sut = new PromptRegistry([t]);
        Assert.Equal(t, sut.GetByVersion("greet", "1.0.0"));
    }

    [Fact]
    public void GetByVersion_NoMatch_ReturnsNull()
    {
        var sut = new PromptRegistry([Make("greet", "1.0.0")]);
        Assert.Null(sut.GetByVersion("greet", "2.0.0"));
    }

    [Fact]
    public void GetLatest_MultipleVersions_ReturnsHighest()
    {
        var t1 = Make("greet", "1.0.0");
        var t2 = Make("greet", "2.0.0");
        var t3 = Make("greet", "1.5.0");
        var sut = new PromptRegistry([t1, t2, t3]);
        Assert.Equal("2.0.0", sut.GetLatest("greet")?.Version);
    }

    [Fact]
    public void GetLatest_UnknownName_ReturnsNull()
    {
        var sut = new PromptRegistry([Make("greet", "1.0.0")]);
        Assert.Null(sut.GetLatest("unknown"));
    }

    [Fact]
    public void Constructor_DuplicateNameVersion_Throws()
    {
        var t1 = Make("greet", "1.0.0");
        var t2 = Make("greet", "1.0.0");
        Assert.Throws<InvalidOperationException>(() => new PromptRegistry([t1, t2]));
    }
}
