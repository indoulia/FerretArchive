using System.Text.RegularExpressions;

using Ferret.Core.Ai.Prompts;
using Ferret.Prompts.Exceptions;

namespace Ferret.Prompts;

/// <summary>Stateless renderer that substitutes <c>{{variable}}</c> placeholders in prompt templates.</summary>
public sealed class PromptRenderer : IPromptRenderer
{
    private static readonly Regex PlaceholderPattern =
        new(@"\{\{([a-zA-Z0-9_\-]+)\}\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <inheritdoc/>
    public string Render(PromptTemplate promptTemplate, PromptVariables variables)
    {
        ArgumentNullException.ThrowIfNull(promptTemplate);
        var missing = Validate(promptTemplate, variables);
        if (missing.Count > 0)
        {
            throw new PromptRenderException(promptTemplate.Name, missing);
        }

        return PlaceholderPattern.Replace(
            promptTemplate.Template,
            match =>
            {
                var name = match.Groups[1].Value;
                return variables.TryGet(name) ?? match.Value;
            });
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> Validate(PromptTemplate promptTemplate, PromptVariables variables)
    {
        ArgumentNullException.ThrowIfNull(promptTemplate);
        return promptTemplate.RequiredVariables.Where(v => !variables.Contains(v)).ToList();
    }
}
