namespace Ferret.Core.Context;

/// <summary>Indicates whether a <see cref="ContextDocument"/> contains a full document or a single section.</summary>
public enum ContextDocumentSource
{
    /// <summary>The document contains the full text of a source file or document.</summary>
    FullDocument = 0,

    /// <summary>The document contains a single section or excerpt from a larger document.</summary>
    Section = 1,
}
