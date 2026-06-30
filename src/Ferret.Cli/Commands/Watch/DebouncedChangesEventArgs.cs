using System.IO;

namespace Ferret.Cli.Commands.Watch;

/// <summary>Event arguments carrying a batch of coalesced file-change events.</summary>
internal sealed class DebouncedChangesEventArgs : EventArgs
{
    /// <summary>Gets the coalesced changes, one entry per distinct path.</summary>
    public required IReadOnlyList<(string Path, WatcherChangeTypes ChangeType)> Changes { get; init; }
}
