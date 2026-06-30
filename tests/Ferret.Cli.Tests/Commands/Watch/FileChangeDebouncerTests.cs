using System.IO;

using Ferret.Cli.Commands.Watch;

namespace Ferret.Cli.Tests.Commands.Watch;

public sealed class FileChangeDebouncerTests : IDisposable
{
    private readonly FileChangeDebouncer _debouncer;

    public FileChangeDebouncerTests()
    {
        // 50ms window for fast tests
        _debouncer = new FileChangeDebouncer(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task Track_SingleChange_FiresAfterDebounceWindow()
    {
        var fired = new TaskCompletionSource<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => fired.TrySetResult(e);

        _debouncer.Track("/workspace/file.cs", WatcherChangeTypes.Changed);

        var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Single(result.Changes);
        Assert.Equal("/workspace/file.cs", result.Changes[0].Path);
    }

    [Fact]
    public async Task Track_RapidChangesToSamePath_CoalescesIntoOne()
    {
        var batches = new List<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => batches.Add(e);

        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed);
        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed); // duplicate
        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed); // duplicate

        await Task.Delay(200);

        Assert.Single(batches);
        Assert.Single(batches[0].Changes); // a.cs deduplicated
    }

    [Fact]
    public async Task Track_MultipleDistinctPaths_AllIncluded()
    {
        var fired = new TaskCompletionSource<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => fired.TrySetResult(e);

        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed);
        _debouncer.Track("/workspace/b.cs", WatcherChangeTypes.Created);

        var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Equal(2, result.Changes.Count);
    }

    [Fact]
    public async Task Track_DeleteOverridesChange_KeepsDeleteType()
    {
        var fired = new TaskCompletionSource<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => fired.TrySetResult(e);

        _debouncer.Track("/workspace/file.cs", WatcherChangeTypes.Changed);
        _debouncer.Track("/workspace/file.cs", WatcherChangeTypes.Deleted); // last event wins

        var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Single(result.Changes);
        Assert.Equal(WatcherChangeTypes.Deleted, result.Changes[0].ChangeType);
    }

    public void Dispose() => _debouncer.Dispose();
}
