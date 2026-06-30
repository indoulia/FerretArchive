using Ferret.Core.Runtime;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Platform service bag — commands access all services through one stable interface.
///      Runtime is nullable Sprint 6 (no daemon); non-null Sprint 7+ when daemon is introduced.
/// Thread Safety: Thread Safe — services are singletons.
/// </summary>
internal interface IFerretServices
{
    /// <summary>Gets the DI service provider.</summary>
    IServiceProvider Services { get; }

    /// <summary>Gets the application configuration.</summary>
    IConfiguration Configuration { get; }

    /// <summary>Gets the logger factory.</summary>
    ILoggerFactory LoggerFactory { get; }

    /// <summary>Gets the output formatter.</summary>
    IOutputFormatter Output { get; }

    /// <summary>Gets the runtime host; null in Sprint 6, non-null Sprint 7+ daemon.</summary>
    IRuntimeHost? Runtime { get; }
}
