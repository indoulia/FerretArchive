namespace Ferret.Core.Documents;

/// <summary>
/// Classifies the semantic kind of a Document.
/// Assigned by the parser — not inferred from MediaType.
/// The parser has content-level context that MediaType alone cannot provide.
/// </summary>
public enum DocumentKind
{
    /// <summary>Source code in any programming language.</summary>
    Code = 0,

    /// <summary>Human-readable prose: documentation, README files, Markdown articles.</summary>
    Prose = 1,

    /// <summary>Structured data: JSON arrays, CSV datasets, tabular files.</summary>
    Data = 2,

    /// <summary>Configuration: JSON configs, TOML settings, YAML manifests.</summary>
    Config = 3,

    /// <summary>Kind could not be determined by the parser.</summary>
    Unknown = 99,
}
