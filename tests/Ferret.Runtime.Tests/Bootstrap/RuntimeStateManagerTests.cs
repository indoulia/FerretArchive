using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;

namespace Ferret.Runtime.Tests.Bootstrap;

public sealed class RuntimeStateManagerTests
{
    [Fact]
    public void Current_InitialState_IsStopped()
    {
        var mgr = new RuntimeStateManager();
        Assert.Equal(RuntimeState.Stopped, mgr.Current);
    }

    [Fact]
    public void TryTransition_FromMatchingState_Succeeds()
    {
        var mgr = new RuntimeStateManager();
        bool result = mgr.TryTransition(RuntimeState.Stopped, RuntimeState.Starting);
        Assert.True(result);
        Assert.Equal(RuntimeState.Starting, mgr.Current);
    }

    [Fact]
    public void TryTransition_FromWrongState_Fails()
    {
        var mgr = new RuntimeStateManager();
        bool result = mgr.TryTransition(RuntimeState.Running, RuntimeState.Stopping);
        Assert.False(result);
        Assert.Equal(RuntimeState.Stopped, mgr.Current);
    }

    [Fact]
    public void TryTransition_DoesNotChangeStateOnFailure()
    {
        var mgr = new RuntimeStateManager();
        mgr.TryTransition(RuntimeState.Stopped, RuntimeState.Starting);
        mgr.TryTransition(RuntimeState.Running, RuntimeState.Stopping); // wrong from
        Assert.Equal(RuntimeState.Starting, mgr.Current);
    }

    [Fact]
    public void ForceSet_OverridesCurrentState()
    {
        var mgr = new RuntimeStateManager();
        mgr.ForceSet(RuntimeState.Faulted);
        Assert.Equal(RuntimeState.Faulted, mgr.Current);
    }

    [Fact]
    public void TryTransition_IsThreadSafe_OnlyOneWinner()
    {
        var mgr = new RuntimeStateManager();
        int successCount = 0;

        var threads = Enumerable.Range(0, 20)
            .Select(_ => new Thread(() =>
            {
                if (mgr.TryTransition(RuntimeState.Stopped, RuntimeState.Starting))
                {
                    Interlocked.Increment(ref successCount);
                }
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        Assert.Equal(1, successCount);
        Assert.Equal(RuntimeState.Starting, mgr.Current);
    }

    [Theory]
    [InlineData(RuntimeState.Stopped, RuntimeState.Starting)]
    [InlineData(RuntimeState.Starting, RuntimeState.Running)]
    [InlineData(RuntimeState.Running, RuntimeState.Stopping)]
    [InlineData(RuntimeState.Stopping, RuntimeState.Stopped)]
    public void TryTransition_ValidTransitions_Succeed(RuntimeState from, RuntimeState to)
    {
        var mgr = new RuntimeStateManager();
        mgr.ForceSet(from);
        Assert.True(mgr.TryTransition(from, to));
    }
}
