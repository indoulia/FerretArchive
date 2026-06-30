namespace Ferret.Core.Ai.Prompts;

/// <summary>Read-only catalogue of registered <see cref="PromptTemplate"/> instances.</summary>
public interface IPromptRegistry
{
    /// <summary>Returns the template with the given name and exact version, or <see langword="null"/>.</summary>
    /// <param name="name">The template name.</param>
    /// <param name="version">The exact semver version string.</param>
    /// <returns>The matching template, or <see langword="null"/> if not found.</returns>
    PromptTemplate? GetByVersion(string name, string version);

    /// <summary>Returns the latest version of the named template, or <see langword="null"/>.</summary>
    /// <param name="name">The template name.</param>
    /// <returns>The highest-version template, or <see langword="null"/> if no template with that name exists.</returns>
    PromptTemplate? GetLatest(string name);

    /// <summary>Returns all registered templates.</summary>
    /// <returns>An immutable list of all templates in registration order.</returns>
    IReadOnlyList<PromptTemplate> GetAll();
}
