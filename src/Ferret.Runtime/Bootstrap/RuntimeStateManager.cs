using Ferret.Core.Runtime;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Atomic state machine for the host-level runtime lifecycle.
/// <para>Why: Provides a single authority for RuntimeState so all runtime collaborators read from one source.</para>
/// <para>Lifecycle: Created inside RuntimeBuilder.Build() and registered as a DI singleton; lives until the RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime internal — only RuntimeHost and ModuleLifecycleService may use this directly.</para>
/// <para>Thread Safety: Thread Safe — all transitions use Interlocked.CompareExchange.</para>
/// </summary>
internal sealed class RuntimeStateManager
{
    private int _state = (int)RuntimeState.Stopped;

    /// <summary>Gets the current runtime state.</summary>
    public RuntimeState Current => (RuntimeState)Volatile.Read(ref _state);

    /// <summary>Atomically transitions from <paramref name="from"/> to <paramref name="to"/>. Returns <c>true</c> on success.</summary>
    public bool TryTransition(RuntimeState from, RuntimeState to)
        => Interlocked.CompareExchange(ref _state, (int)to, (int)from) == (int)from;

    /// <summary>Unconditionally sets the state (used for Faulted transitions where CAS may race).</summary>
    public void ForceSet(RuntimeState state)
        => Volatile.Write(ref _state, (int)state);
}
