namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Pure metadata + HandlerType; DI resolves the handler. No delegate lambdas —
///      enables constructor injection, telemetry, middleware, and decorators in Sprint 7+.
/// Thread Safety: Thread Safe — immutable record.
/// </summary>
/// <param name="Metadata">The command metadata.</param>
/// <param name="HandlerType">The DI-resolved handler type; null for group-only commands.</param>
/// <param name="Group">Optional group name for subcommand organisation.</param>
/// <param name="Options">Per-command option definitions.</param>
/// <param name="PlannedSubcommands">Subcommand names planned for a future sprint.</param>
/// <param name="PlannedSprint">The sprint in which planned subcommands will be added.</param>
/// <param name="Arguments">Positional argument definitions, in order.</param>
internal sealed record CommandDefinition(
    CommandMetadata Metadata,
    Type? HandlerType,
    string? Group = null,
    IReadOnlyList<OptionDefinition>? Options = null,
    IReadOnlyList<string>? PlannedSubcommands = null,
    string? PlannedSprint = null,
    IReadOnlyList<ArgumentDefinition>? Arguments = null)
{
    /// <summary>Creates a placeholder group command with no handler.</summary>
    /// <param name="name">The group command name.</param>
    /// <param name="description">The group command description.</param>
    /// <param name="plannedSprint">The sprint that will add subcommands.</param>
    /// <param name="plannedSubcommands">The planned subcommand names.</param>
    /// <returns>A group-level <see cref="CommandDefinition"/> with no handler.</returns>
    internal static CommandDefinition EmptyGroup(
        string name,
        string description,
        string plannedSprint,
        string[] plannedSubcommands) =>
        new(
            new CommandMetadata(name, description),
            HandlerType: null,
            PlannedSubcommands: plannedSubcommands,
            PlannedSprint: plannedSprint);

    /// <summary>Returns a copy of this definition with the given positional argument added.</summary>
    /// <param name="name">The argument name, used as the key in <see cref="IFerretContext.GetOption{T}"/>.</param>
    /// <param name="description">Human-readable description shown in help text.</param>
    /// <param name="isRequired">Whether the argument is required. Defaults to true.</param>
    /// <returns>A new <see cref="CommandDefinition"/> with the argument appended.</returns>
    internal CommandDefinition WithArgument(string name, string description, bool isRequired = true) =>
        this with { Arguments = [.. Arguments ?? [], new ArgumentDefinition(name, description, isRequired)] };
}
