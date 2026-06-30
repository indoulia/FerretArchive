namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Tunable configuration values for the Ferret runtime host.
/// <para>Why: Centralises values that must be consistent across all runtime collaborators (e.g. version written to domain events).</para>
/// <para>Lifecycle: Created by the caller before RuntimeBuilder.Build(); owned by DI as a singleton after Build().</para>
/// <para>Layer: Ferret.Runtime only — callers access it through RuntimeBuilder.WithOptions(); never referenced by Core.</para>
/// <para>Thread Safety: Single Thread Only — set all properties before passing to Build(); treat as immutable afterward.</para>
/// </summary>
public sealed class RuntimeOptions
{
    /// <summary>Gets or sets the version string written to <c>RuntimeStarted</c> and <c>RuntimeStopped</c> events.</summary>
    public string RuntimeVersion { get; set; } = "0.5.0";
}
