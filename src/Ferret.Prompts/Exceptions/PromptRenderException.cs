using Ferret.Core.Errors;

namespace Ferret.Prompts.Exceptions;

/// <summary>Thrown when a prompt template cannot be rendered due to missing required variables.</summary>
public sealed class PromptRenderException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="PromptRenderException"/> class.</summary>
    public PromptRenderException()
        : base()
    {
        TemplateName = string.Empty;
        MissingVariables = [];
    }

    /// <summary>Initializes a new instance of the <see cref="PromptRenderException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    public PromptRenderException(string message)
        : base(message)
    {
        TemplateName = string.Empty;
        MissingVariables = [];
    }

    /// <summary>Initializes a new instance of the <see cref="PromptRenderException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PromptRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
        TemplateName = string.Empty;
        MissingVariables = [];
    }

    /// <summary>Initializes a new instance of the <see cref="PromptRenderException"/> class with render context.</summary>
    /// <param name="templateName">The name of the template that failed to render.</param>
    /// <param name="missingVariables">The required variable names that were absent.</param>
    public PromptRenderException(string templateName, IReadOnlyList<string> missingVariables)
        : base(BuildMessage(templateName, missingVariables))
    {
        TemplateName = templateName;
        MissingVariables = missingVariables;
    }

    /// <summary>Gets the name of the template that failed to render.</summary>
    public string TemplateName { get; }

    /// <summary>Gets the required variable names that were absent.</summary>
    public IReadOnlyList<string> MissingVariables { get; }

    private static string BuildMessage(string name, IReadOnlyList<string> missing) =>
        $"Prompt template '{name}' is missing required variables: {string.Join(", ", missing.Select(v => $"'{v}'"))}.";
}
