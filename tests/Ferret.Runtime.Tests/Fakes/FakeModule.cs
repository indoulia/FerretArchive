using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Tests.Fakes;

/// <summary>Test double for DefaultModule. Tracks lifecycle call counts and optionally throws on start.</summary>
public sealed class FakeModule : DefaultModule
{
    private readonly Exception? _startException;

    public FakeModule(string id = "fake", Exception? startException = null)
        : base(ModuleMetadata.Create(id, id, SemanticVersion.Create(1, 0, 0), [], string.Empty, string.Empty))
    {
        _startException = startException;
    }

    public int OnStartingCalls { get; private set; }

    public int OnStartedCalls { get; private set; }

    public int OnStoppingCalls { get; private set; }

    public int OnStoppedCalls { get; private set; }

    public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        OnStartingCalls++;
        if (_startException is not null)
        {
            throw _startException;
        }

        return Task.CompletedTask;
    }

    public override Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        OnStartedCalls++;
        return Task.CompletedTask;
    }

    public override Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        OnStoppingCalls++;
        return Task.CompletedTask;
    }

    public override Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        OnStoppedCalls++;
        return Task.CompletedTask;
    }
}
