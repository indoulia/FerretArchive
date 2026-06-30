namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Per-invocation context — every ICommandHandler receives exactly this.
///      Adding options, trace IDs, or user identity is a one-property change here.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface IFerretContext
{
    /// <summary>Gets the cancellation token for this invocation.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Gets the verbosity level for this invocation.</summary>
    VerbosityLevel Verbosity { get; }

    /// <summary>Gets the output format for this invocation.</summary>
    OutputFormat OutputFormat { get; }

    /// <summary>Gets the platform services for this invocation.</summary>
    IFerretServices Services { get; }

    /// <summary>Gets the working directory at the time this context was created.</summary>
    string WorkingDirectory { get; }

    /// <summary>Gets a parsed option value by name, or default if not present.</summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="name">The option name key.</param>
    /// <returns>The option value, or default if not found.</returns>
    T? GetOption<T>(string name);
}
