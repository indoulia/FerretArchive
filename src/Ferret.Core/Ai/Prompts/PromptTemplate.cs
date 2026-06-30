namespace Ferret.Core.Ai.Prompts;

/// <summary>An immutable prompt template with metadata and a list of required variable names.</summary>
public sealed record PromptTemplate
{
    /// <summary>Gets the unique name of this template (e.g. "workspace-context").</summary>
    public required string Name { get; init; }

    /// <summary>Gets the semantic version string (e.g. "1.0.0").</summary>
    public required string Version { get; init; }

    /// <summary>Gets the raw template body containing <c>{{variable}}</c> placeholders.</summary>
    public required string Template { get; init; }

    /// <summary>Gets the variable names that must be supplied before rendering.</summary>
    public required IReadOnlyList<string> RequiredVariables { get; init; }

    /// <summary>Gets a human-readable description of what this template produces, or <see langword="null"/> if none provided.</summary>
    public string? Description { get; init; }
}
