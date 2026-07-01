namespace Ferret.Core.Documents;

/// <summary>Classifies how a media type's content can be consumed by the parser platform.</summary>
public enum MediaCategory
{
    /// <summary>Human-readable text, consumable directly by a text/* parser.</summary>
    Text = 0,

    /// <summary>Binary, but a registered parser can extract text from it (e.g. PDF, DOCX).</summary>
    BinaryParseable = 1,

    /// <summary>Binary with no extractable text (images, executables, fonts, archives).</summary>
    BinaryOpaque = 2,
}
