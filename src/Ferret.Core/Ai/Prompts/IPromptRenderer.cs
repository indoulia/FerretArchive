namespace Ferret.Core.Ai.Prompts;

/// <summary>Renders a <see cref="PromptTemplate"/> by substituting variables.</summary>
public interface IPromptRenderer
{
    /// <summary>Renders the template body with the provided variable bindings.</summary>
    /// <param name="promptTemplate">The template to render.</param>
    /// <param name="variables">Variable bindings to substitute into the template.</param>
    /// <returns>The rendered string with all placeholders replaced.</returns>
    /// <exception cref="System.Exception">Thrown when a required variable is missing.</exception>
    string Render(PromptTemplate promptTemplate, PromptVariables variables);

    /// <summary>Returns the names of required variables that are absent from <paramref name="variables"/>.</summary>
    /// <param name="promptTemplate">The template to validate.</param>
    /// <param name="variables">Variable bindings to check against required variables.</param>
    /// <returns>A list of missing variable names; empty if all required variables are present.</returns>
    IReadOnlyList<string> Validate(PromptTemplate promptTemplate, PromptVariables variables);
}
