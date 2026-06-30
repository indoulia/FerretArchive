using Ferret.Core.Runtime;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Concrete IFerretServices built once by RootCommandFactory from the DI container.
/// Lifecycle: Singleton per CLI invocation.
/// Thread Safety: Thread Safe — all members read-only after construction.
/// </summary>
internal sealed class FerretServices : IFerretServices
{
    /// <summary>Initializes a new instance of the <see cref="FerretServices"/> class.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="output">The output formatter.</param>
    internal FerretServices(
        IServiceProvider services,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IOutputFormatter output)
    {
        Services = services;
        Configuration = configuration;
        LoggerFactory = loggerFactory;
        Output = output;
    }

    /// <inheritdoc/>
    public IServiceProvider Services { get; }

    /// <inheritdoc/>
    public IConfiguration Configuration { get; }

    /// <inheritdoc/>
    public ILoggerFactory LoggerFactory { get; }

    /// <inheritdoc/>
    public IOutputFormatter Output { get; }

    /// <inheritdoc/>
    public IRuntimeHost? Runtime => null; // Sprint 7: resolve from Services
}
