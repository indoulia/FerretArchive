using System.Reflection;
using System.Runtime.InteropServices;

namespace Ferret.Cli.Infrastructure;

/// <summary>
/// Why: Single source of truth for CLI version and runtime metadata; prevents version drift between assembly and output.
/// Lifecycle: Static; read once at process start.
/// Layer: Ferret.Cli only.
/// Thread Safety: Thread Safe — read-only after static initialization.
/// </summary>
internal static class FerretPlatform
{
    /// <summary>Gets the assembly informational version (e.g. "0.6.0+commit").</summary>
    internal static string Version { get; } =
        typeof(FerretPlatform).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "0.0.0";

    /// <summary>Gets a human-readable description of the current .NET runtime and OS.</summary>
    internal static string RuntimeInfo { get; } =
        $".NET {Environment.Version} / {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
}
