namespace Ferret.Core.Documents;

/// <summary>Canonical keys for <see cref="Document.Metadata"/>. Parsers MUST use these constants
/// (never raw strings) so keys never drift (PageCount vs Pagecount vs "Page Count") across parsers.</summary>
public static class DocumentMetadata
{
    /// <summary>Document author / creator.</summary>
    public const string Author = "Author";

    /// <summary>Document subject.</summary>
    public const string Subject = "Subject";

    /// <summary>Document keywords.</summary>
    public const string Keywords = "Keywords";

    /// <summary>Page count (PDF).</summary>
    public const string PageCount = "PageCount";

    /// <summary>Worksheet count (XLSX).</summary>
    public const string SheetCount = "SheetCount";

    /// <summary>Creation timestamp (ISO-8601).</summary>
    public const string Created = "Created";

    /// <summary>Last-modified timestamp (ISO-8601).</summary>
    public const string Modified = "Modified";

    /// <summary>Document category.</summary>
    public const string Category = "Category";

    /// <summary>Set to "true" when extracted text was truncated by the configured limit.</summary>
    public const string Truncated = "Truncated";

    /// <summary>Number of data rows (tabular formats; excludes the header row).</summary>
    public const string RowCount = "RowCount";

    /// <summary>Number of columns (tabular formats; header field count).</summary>
    public const string ColumnCount = "ColumnCount";

    /// <summary>Set to "true"/"false": whether a tabular document's first row is treated as a header.</summary>
    public const string HasHeader = "HasHeader";
}
