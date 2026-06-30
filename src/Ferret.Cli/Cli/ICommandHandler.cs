namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Command execution contract; resolved from DI so commands get constructor injection.
///      Enables telemetry, middleware, and authorization decorators without changing commands.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface ICommandHandler
{
    /// <summary>Executes the command and returns the result.</summary>
    /// <param name="context">The per-invocation context.</param>
    /// <returns>A task resolving to the command result.</returns>
    Task<CommandResult> ExecuteAsync(IFerretContext context);
}
