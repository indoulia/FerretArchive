using System.Globalization;

namespace Ferret.Benchmarks.Corpus;

/// <summary>The value kind of a table cell. Minimal by design — parser coverage, not spreadsheet semantics.</summary>
public enum CorpusCellKind
{
    /// <summary>A text value (shared string in Excel).</summary>
    Text,

    /// <summary>A numeric value.</summary>
    Number,

    /// <summary>A boolean value.</summary>
    Boolean,

    /// <summary>A date value.</summary>
    Date,

    /// <summary>An empty cell.</summary>
    Empty,
}

/// <summary>A single typed table cell. <see cref="Value"/> is the canonical text form; renderers that
/// support types (Excel) emit typed cells, text renderers emit <see cref="Value"/> verbatim.</summary>
/// <param name="Kind">The cell value kind.</param>
/// <param name="Value">The canonical text form of the value.</param>
public sealed record CorpusCell(CorpusCellKind Kind, string Value)
{
    /// <summary>An empty cell.</summary>
    public static readonly CorpusCell Empty = new(CorpusCellKind.Empty, string.Empty);

    /// <summary>Creates a text cell.</summary>
    /// <param name="value">The text value.</param>
    /// <returns>A text cell.</returns>
    public static CorpusCell Text(string value) => new(CorpusCellKind.Text, value);

    /// <summary>Creates a numeric cell.</summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>A numeric cell.</returns>
    public static CorpusCell Number(double value) =>
        new(CorpusCellKind.Number, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Creates a boolean cell.</summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A boolean cell.</returns>
    public static CorpusCell Boolean(bool value) => new(CorpusCellKind.Boolean, value ? "true" : "false");

    /// <summary>Creates a date cell (ISO yyyy-MM-dd canonical form).</summary>
    /// <param name="value">The date value.</param>
    /// <returns>A date cell.</returns>
    public static CorpusCell Date(DateOnly value) =>
        new(CorpusCellKind.Date, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
