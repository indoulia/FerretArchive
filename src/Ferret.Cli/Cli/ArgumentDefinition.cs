namespace Ferret.Cli.Cli;

/// <summary>Defines a positional argument for a CLI command.</summary>
/// <param name="Name">Argument name — used as the key in context.GetOption&lt;string&gt;("name").</param>
/// <param name="Description">Human-readable description shown in help text.</param>
/// <param name="IsRequired">Whether the argument is required. Defaults to true.</param>
internal sealed record ArgumentDefinition(string Name, string Description, bool IsRequired = true);
