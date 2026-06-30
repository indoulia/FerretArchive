namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Rich command descriptor — Hidden, Experimental, Aliases, Examples reserved for Sprint 7 tooling.
/// Thread Safety: Thread Safe — immutable record.
/// </summary>
/// <param name="Name">The command name (e.g. "build").</param>
/// <param name="Description">The command description shown in help.</param>
/// <param name="Category">Optional grouping category for help display.</param>
/// <param name="Hidden">Whether the command is hidden from help output.</param>
/// <param name="Experimental">Whether the command is experimental.</param>
/// <param name="Aliases">Optional alternate names for the command.</param>
/// <param name="Examples">Optional usage examples for help display.</param>
internal sealed record CommandMetadata(
    string Name,
    string Description,
    string? Category = null,
    bool Hidden = false,
    bool Experimental = false,
    IReadOnlyList<string>? Aliases = null,
    IReadOnlyList<string>? Examples = null);
