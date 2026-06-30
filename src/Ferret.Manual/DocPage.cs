namespace Ferret.Manual;

/// <summary>Metadata for a single manual page.</summary>
public sealed record DocPage
{
    /// <summary>Gets the URL-safe slug, e.g. "getting-started/installation".</summary>
    public required string Slug { get; init; }

    /// <summary>Gets the human-readable page title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the top-level section, e.g. "Getting Started".</summary>
    public required string Section { get; init; }

    /// <summary>Gets the sort order within section.</summary>
    public required int Order { get; init; }

    /// <summary>Gets the assembly embedded resource name for the Markdown file.</summary>
    public required string ResourceName { get; init; }
}
