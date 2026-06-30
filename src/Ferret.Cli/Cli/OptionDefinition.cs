namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Describes a per-command option without System.CommandLine types; RootCommandFactory converts these.
/// Thread Safety: Thread Safe — immutable record.
/// </summary>
/// <param name="LongName">The long-form option name (e.g. "--output").</param>
/// <param name="Description">The option description shown in help.</param>
/// <param name="ValueType">The CLR type of the option value.</param>
/// <param name="IsHidden">Whether the option is hidden from help output.</param>
/// <param name="DefaultValue">The default value, or null.</param>
internal sealed record OptionDefinition(
    string LongName,
    string Description,
    Type ValueType,
    bool IsHidden = false,
    object? DefaultValue = null);
