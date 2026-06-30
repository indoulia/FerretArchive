using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Cli;

/// <summary>Why: No-op base so concrete modules only override what they contribute.</summary>
internal abstract class CliModuleBase : ICliModule
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public virtual IEnumerable<CommandDefinition> GetCommands() => [];

    /// <inheritdoc/>
    public virtual IEnumerable<Diagnostics.IDiagnosticCheck> GetDiagnosticChecks() => [];

    /// <inheritdoc/>
    public virtual void ConfigureServices(IServiceCollection services)
    {
        // No services registered by default.
    }
}
