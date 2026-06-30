using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Full extensibility contract — one module contributes commands, checks, and service registrations.
///      Sprint 7 WorkspaceCliModule, GitCliModule etc. implement this without changing RootCommandFactory.
/// Thread Safety: Thread Safe — called once during startup.
/// </summary>
internal interface ICliModule
{
    /// <summary>Gets the module name.</summary>
    string Name { get; }

    /// <summary>Gets the module description.</summary>
    string Description { get; }

    /// <summary>Returns the command definitions contributed by this module.</summary>
    /// <returns>An enumerable of command definitions.</returns>
    IEnumerable<CommandDefinition> GetCommands();

    /// <summary>Returns the diagnostic checks contributed by this module.</summary>
    /// <returns>An enumerable of diagnostic checks.</returns>
    IEnumerable<Diagnostics.IDiagnosticCheck> GetDiagnosticChecks();

    /// <summary>Registers services contributed by this module into the DI container.</summary>
    /// <param name="services">The service collection to register into.</param>
    void ConfigureServices(IServiceCollection services);
}
