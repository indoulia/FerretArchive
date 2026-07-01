using System.Globalization;

using Ferret.Cli.Cli;
using Ferret.Core.Documents;
using Ferret.ParserPlatform;

namespace Ferret.Cli.Diagnostics;

/// <summary>Renders the informational "Parser Platform" section of `ferret doctor`: installed
/// parsers, extension coverage, parseable/opaque extensions, and loaded parser packages.</summary>
internal sealed class ParserPlatformReport
{
    private const int OpaqueSampleSize = 8;

    private readonly IReadOnlyList<IContentParser> _parsers;

    /// <summary>Initializes a new instance of the <see cref="ParserPlatformReport"/> class.</summary>
    /// <param name="parsers">The composed content parsers, in registration order.</param>
    internal ParserPlatformReport(IReadOnlyList<IContentParser> parsers)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        _parsers = parsers;
    }

    /// <summary>Renders the report to the output.</summary>
    /// <param name="output">The output formatter.</param>
    /// <param name="verbose">When true, shows all opaque extensions plus per-parser priority/media
    /// type and parseable-extension MIME mappings.</param>
    internal void Render(IOutputFormatter output, bool verbose)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.WriteLine();
        output.WriteLine("Parser Platform");
        output.WriteLine();

        RenderInstalledParsers(output, _parsers, verbose);
        output.WriteLine();
        RenderExtensionCoverage(output);
        output.WriteLine();
        RenderParseableBinary(output, verbose);
        output.WriteLine();
        RenderOpaqueBinary(output, verbose);
        output.WriteLine();
        RenderPackages(output, _parsers);
    }

    private static void RenderInstalledParsers(IOutputFormatter output, IReadOnlyList<IContentParser> parsers, bool verbose)
    {
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Installed Parsers ({parsers.Count})"));
        if (parsers.Count == 0)
        {
            output.WriteLine("  No parsers are registered.");
            return;
        }

        foreach (var parser in parsers)
        {
            var descriptor = parser.Descriptor;
            output.WriteLine("  ✓ " + descriptor.Name);
            if (verbose)
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"      Priority: {descriptor.Priority}"));
                output.WriteLine("      Media Type: " + string.Join(", ", descriptor.SupportedMediaTypes));
            }
        }
    }

    private static void RenderExtensionCoverage(IOutputFormatter output)
    {
        var text = MimeTypeResolver.ExtensionsInCategory(MediaCategory.Text).Count;
        var parseable = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable).Count;
        var opaque = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryOpaque).Count;

        output.WriteLine("Extension Coverage");
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Text: {text}"));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Parseable Binary: {parseable}"));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Opaque Binary: {opaque}"));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Known Extensions: {text + parseable + opaque}"));
    }

    private static void RenderParseableBinary(IOutputFormatter output, bool verbose)
    {
        var entries = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryParseable);
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Parseable Binary ({entries.Count})"));
        if (verbose)
        {
            foreach (var entry in entries)
            {
                output.WriteLine("  " + entry.Extension + " → " + entry.MediaType);
            }
        }
        else
        {
            output.WriteLine("  " + string.Join("  ", entries.Select(e => e.Extension)));
        }
    }

    private static void RenderOpaqueBinary(IOutputFormatter output, bool verbose)
    {
        var extensions = MimeTypeResolver.ExtensionsInCategory(MediaCategory.BinaryOpaque)
            .Select(e => e.Extension).ToList();
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Opaque Binary ({extensions.Count}) — currently treated as opaque binary"));

        if (verbose || extensions.Count <= OpaqueSampleSize)
        {
            output.WriteLine("  " + string.Join(" ", extensions));
        }
        else
        {
            output.WriteLine("  " + string.Join(" ", extensions.Take(OpaqueSampleSize)) + " ...");
            output.WriteLine("  run `ferret doctor --verbose` for the full list");
        }
    }

    private static void RenderPackages(IOutputFormatter output, IReadOnlyList<IContentParser> parsers)
    {
        var packages = parsers
            .Select(p => p.GetType().Assembly.GetName().Name ?? "(unknown)")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Parser Packages ({packages.Count})"));
        if (packages.Count == 0)
        {
            output.WriteLine("  No parser packages loaded.");
            return;
        }

        foreach (var package in packages)
        {
            output.WriteLine("  " + package);
        }
    }
}
