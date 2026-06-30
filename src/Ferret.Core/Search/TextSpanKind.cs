namespace Ferret.Core.Search;

/// <summary>
/// Classifies a <see cref="TextSpan"/> within a <see cref="HighlightedText"/>.
/// Providers assign span kinds; renderers apply formatting based on them.
/// </summary>
public enum TextSpanKind
{
    /// <summary>Ordinary text — no special formatting.</summary>
    Normal = 0,

    /// <summary>Text that matched the search query — highlighted by the renderer.</summary>
    Match = 1,

    /// <summary>Reserved: text deleted in a diff context (Sprint 11+).</summary>
    Deleted = 2,

    /// <summary>Reserved: text inserted in a diff context (Sprint 11+).</summary>
    Inserted = 3,

    /// <summary>Reserved: text flagged with a warning annotation (Sprint 11+).</summary>
    Warning = 4,

    /// <summary>Reserved: text referenced by an AI-generated answer (Sprint 11+).</summary>
    AIReference = 5,
}
