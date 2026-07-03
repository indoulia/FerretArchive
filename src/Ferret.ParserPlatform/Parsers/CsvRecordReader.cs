using System.Text;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>Minimal RFC-4180 reader: yields records of fields. Fields may be quoted with double
/// quotes and contain the delimiter or newlines; a doubled quote ("") is an escaped quote.</summary>
internal static class CsvRecordReader
{
    public static IEnumerable<IReadOnlyList<string>> ReadRecords(string text, char delimiter)
    {
        var field = new StringBuilder();
        var record = new List<string>();
        var inQuotes = false;
        var pending = false; // true once any char/field seen on the current record

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                pending = true;
            }
            else if (c == '\r')
            {
                // ignore; handled by \n
            }
            else if (c == delimiter)
            {
                record.Add(field.ToString());
                field.Clear();
                pending = true;
            }
            else if (c == '\n')
            {
                record.Add(field.ToString());
                field.Clear();
                yield return record;
                record = new List<string>();
                pending = false;
            }
            else
            {
                field.Append(c);
                pending = true;
            }
        }

        if (pending || field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            yield return record;
        }
    }
}
