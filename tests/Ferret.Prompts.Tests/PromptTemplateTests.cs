using Ferret.Core.Ai.Prompts;
using Xunit;

namespace Ferret.Prompts.Tests;

public sealed class PromptTemplateTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var template = new PromptTemplate
        {
            Name = "workspace-context",
            Version = "1.0.0",
            Template = "Hello {{name}}",
            RequiredVariables = ["name"],
            Description = "A greeting template",
        };

        Assert.Equal("workspace-context", template.Name);
        Assert.Equal("1.0.0", template.Version);
        Assert.Equal("Hello {{name}}", template.Template);
        Assert.Single(template.RequiredVariables);
        Assert.Equal("name", template.RequiredVariables[0]);
        Assert.Equal("A greeting template", template.Description);
    }

    [Fact]
    public void Description_IsOptional_DefaultsToNull()
    {
        var template = new PromptTemplate
        {
            Name = "t",
            Version = "1.0.0",
            Template = "hello",
            RequiredVariables = [],
        };

        Assert.Null(template.Description);
    }

    [Fact]
    public void RecordEquality_SameProperties_AreEqual()
    {
        var a = new PromptTemplate { Name = "t", Version = "1.0.0", Template = "x", RequiredVariables = [] };
        var b = new PromptTemplate { Name = "t", Version = "1.0.0", Template = "x", RequiredVariables = [] };

        Assert.Equal(a, b);
    }
}
