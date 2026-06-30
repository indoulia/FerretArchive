using System.CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: IFerretContext implementation. From() builds from SC ParseResult (SC types stay in this method).
///      CreateTest() builds without SC for unit tests.
/// Layer: Ferret.Cli only.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class FerretContext : IFerretContext
{
    private readonly IReadOnlyDictionary<string, object?> _options;

    private FerretContext(
        VerbosityLevel verbosity,
        OutputFormat outputFormat,
        IFerretServices services,
        IReadOnlyDictionary<string, object?> options,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        Verbosity = verbosity;
        OutputFormat = outputFormat;
        Services = services;
        _options = options;
        WorkingDirectory = workingDirectory;
    }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public VerbosityLevel Verbosity { get; }

    /// <inheritdoc/>
    public OutputFormat OutputFormat { get; }

    /// <inheritdoc/>
    public IFerretServices Services { get; }

    /// <inheritdoc/>
    public string WorkingDirectory { get; }

    /// <inheritdoc/>
    public T? GetOption<T>(string name) =>
        _options.TryGetValue(name, out var v) && v is T typed ? typed : default;

    /// <summary>Builds from ParseResult — called only from RootCommandFactory.</summary>
    /// <param name="parseResult">The parsed command line result.</param>
    /// <param name="services">The platform services.</param>
    /// <param name="parsedOptions">Additional parsed option values keyed by name.</param>
    /// <param name="cancellationToken">The cancellation token for this invocation.</param>
    /// <returns>A fully initialised <see cref="FerretContext"/>.</returns>
    internal static FerretContext From(
        ParseResult parseResult,
        IFerretServices services,
        IReadOnlyDictionary<string, object?> parsedOptions,
        CancellationToken cancellationToken)
    {
        bool verbose = parseResult.GetValue(GlobalOptions.Verbose);
        bool quiet = parseResult.GetValue(GlobalOptions.Quiet);
        var verbosity = verbose ? VerbosityLevel.Verbose : quiet ? VerbosityLevel.Quiet : VerbosityLevel.Normal;
        bool jsonFlag = parseResult.GetValue(GlobalOptions.Json);
        return new FerretContext(
            verbosity,
            jsonFlag ? OutputFormat.Json : OutputFormat.Text,
            services,
            parsedOptions,
            Environment.CurrentDirectory,
            cancellationToken);
    }

    /// <summary>Builds without System.CommandLine — for unit tests.</summary>
    /// <param name="out">The output writer for the console formatter.</param>
    /// <param name="verbosity">The verbosity level. Defaults to Normal.</param>
    /// <param name="options">Optional dictionary of parsed option values.</param>
    /// <param name="workingDirectory">The working directory for this context. Defaults to the current directory.</param>
    /// <returns>A test-ready <see cref="FerretContext"/>.</returns>
    internal static FerretContext CreateTest(
        TextWriter @out,
        VerbosityLevel verbosity = VerbosityLevel.Normal,
        IReadOnlyDictionary<string, object?>? options = null,
        string? workingDirectory = null)
    {
        var formatter = new ConsoleFormatter(@out, verbosity);
        var services = new FerretServices(
            new EmptyServiceProvider(),
            new ConfigurationBuilder().Build(),
            NullLoggerFactory.Instance,
            formatter);
        return new FerretContext(
            verbosity,
            OutputFormat.Text,
            services,
            options ?? new Dictionary<string, object?>(),
            workingDirectory ?? Environment.CurrentDirectory,
            CancellationToken.None);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        /// <inheritdoc/>
        public object? GetService(Type serviceType) => null;
    }
}
