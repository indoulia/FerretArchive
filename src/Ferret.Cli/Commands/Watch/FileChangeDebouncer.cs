using System.IO;

namespace Ferret.Cli.Commands.Watch;

/// <summary>Batches rapid filesystem events within a configurable debounce window, coalescing by path (last event wins).</summary>
internal sealed class FileChangeDebouncer : IDisposable
{
    private readonly TimeSpan _window;

    private readonly Lock _lock = new();

    private readonly Dictionary<string, WatcherChangeTypes> _pending =
        new(StringComparer.OrdinalIgnoreCase);

    private Timer? _timer;

    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="FileChangeDebouncer"/> class.</summary>
    /// <param name="debounceWindow">How long to wait after the last event before firing <see cref="ChangesReady"/>.</param>
    public FileChangeDebouncer(TimeSpan debounceWindow)
    {
        _window = debounceWindow;
    }

    /// <summary>Raised when the debounce window closes and at least one change is pending.</summary>
    public event EventHandler<DebouncedChangesEventArgs>? ChangesReady;

    /// <summary>Registers a file-change event for the given path, restarting the debounce window.</summary>
    /// <param name="path">Absolute path of the changed file.</param>
    /// <param name="changeType">The type of change observed.</param>
    public void Track(string path, WatcherChangeTypes changeType)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _pending[path] = changeType; // last event wins for same path
            _timer?.Dispose();
            _timer = new Timer(_ => Flush(), null, _window, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void Flush()
    {
        List<(string, WatcherChangeTypes)> snapshot;

        lock (_lock)
        {
            if (_pending.Count == 0)
            {
                return;
            }

            snapshot = _pending.Select(kv => (kv.Key, kv.Value)).ToList();
            _pending.Clear();
        }

        ChangesReady?.Invoke(this, new DebouncedChangesEventArgs { Changes = snapshot });
    }
}
