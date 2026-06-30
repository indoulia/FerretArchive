using System.Globalization;
using System.Text;

using Ferret.Cli.Commands.Indexing.ViewModels;

namespace Ferret.Cli.Commands.Indexing.Formatting;

/// <summary>Formats an <see cref="IndexSummaryViewModel"/> as human-readable plain text.</summary>
internal sealed class TextIndexSummaryFormatter
{
    /// <summary>Formats the given view model as a multiline plain-text summary string.</summary>
    /// <param name="vm">The view model to format.</param>
    /// <returns>A multiline string suitable for console output.</returns>
    public static string Format(IndexSummaryViewModel vm)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Index complete.");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Discovered:  {vm.AssetsDiscovered}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Indexed:     {vm.DocumentsIndexed}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Skipped:     {vm.DocumentsSkipped}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Failed:      {vm.Failures}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Duration:    {vm.Duration.TotalSeconds:F2}s");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Database:    {vm.DatabasePath}");

        if (vm.FailureMessages.Count > 0)
        {
            sb.AppendLine("Failures:");
            foreach (var message in vm.FailureMessages)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  - {message}");
            }
        }

        return sb.ToString();
    }
}
