using System.CommandLine;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Centralises all global option definitions; hidden Sprint 6; Sprint 7 wires their values into FerretContext.
/// Layer: Ferret.Cli only — System.CommandLine types confined here.
/// Thread Safety: Thread Safe — read-only after static initialization.
/// </summary>
internal static class GlobalOptions
{
    /// <summary>Gets the --verbose option.</summary>
    internal static Option<bool> Verbose { get; } = Hidden(new Option<bool>("--verbose") { Description = "Verbose output." });

    /// <summary>Gets the --quiet option.</summary>
    internal static Option<bool> Quiet { get; } = Hidden(new Option<bool>("--quiet") { Description = "Suppress output." });

    /// <summary>Gets the --json option (Sprint 7).</summary>
    internal static Option<bool> Json { get; } = Hidden(new Option<bool>("--json") { Description = "JSON output (Sprint 7)." });

    /// <summary>Gets the --no-color option (Sprint 7).</summary>
    internal static Option<bool> NoColor { get; } = Hidden(new Option<bool>("--no-color") { Description = "Disable color (Sprint 7)." });

    /// <summary>Gets the --log-level option.</summary>
    internal static Option<string> LogLevel { get; } = new Option<string>("--log-level")
    {
        Description = "Minimum log level: Trace, Debug, Information, Warning, Error, Critical (default: Information).",
        DefaultValueFactory = _ => "Information",
    };

    /// <summary>Adds all global options to the root command.</summary>
    /// <param name="root">The root command to add options to.</param>
    internal static void AddAll(RootCommand root)
    {
        root.Add(Verbose);
        root.Add(Quiet);
        root.Add(Json);
        root.Add(NoColor);
        root.Add(LogLevel);
    }

    private static Option<bool> Hidden(Option<bool> opt)
    {
        opt.Hidden = true;
        return opt;
    }
}
