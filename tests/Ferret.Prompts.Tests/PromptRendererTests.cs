using Ferret.Core.Ai.Prompts;
using Ferret.Prompts;
using Ferret.Prompts.Exceptions;

namespace Ferret.Prompts.Tests;

public sealed class PromptRendererTests
{
    private readonly PromptRenderer _sut = new();

    private static PromptTemplate Make(string template, params string[] required) =>
        new PromptTemplate
        {
            Name = "test",
            Version = "1.0.0",
            Template = template,
            RequiredVariables = required,
        };

    [Fact]
    public void Render_AllVariablesProvided_ReturnsSubstitutedString()
    {
        var t = Make("Hello {{name}}!", "name");
        var vars = PromptVariables.Empty.Set("name", "World");
        Assert.Equal("Hello World!", _sut.Render(t, vars));
    }

    [Fact]
    public void Render_MissingRequiredVariable_ThrowsPromptRenderException()
    {
        var t = Make("Hello {{name}}!", "name");
        var ex = Assert.Throws<PromptRenderException>(
            () => _sut.Render(t, PromptVariables.Empty));
        Assert.Contains("name", ex.MissingVariables);
    }

    [Fact]
    public void Render_UnboundOptionalPlaceholder_LeftAsIs()
    {
        var t = Make("Hi {{name}} and {{other}}", "name");
        var vars = PromptVariables.Empty.Set("name", "Alice");
        Assert.Equal("Hi Alice and {{other}}", _sut.Render(t, vars));
    }

    [Fact]
    public void Validate_AllPresent_ReturnsEmpty()
    {
        var t = Make("{{a}} {{b}}", "a", "b");
        var vars = PromptVariables.Empty.Set("a", "x").Set("b", "y");
        Assert.Empty(_sut.Validate(t, vars));
    }

    [Fact]
    public void Validate_SomeMissing_ReturnsMissingNames()
    {
        var t = Make("{{a}} {{b}}", "a", "b");
        var vars = PromptVariables.Empty.Set("a", "x");
        var missing = _sut.Validate(t, vars);
        Assert.Single(missing);
        Assert.Equal("b", missing[0]);
    }
}
