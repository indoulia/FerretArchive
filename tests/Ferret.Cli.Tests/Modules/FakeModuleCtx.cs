using Ferret.Core.Runtime;

namespace Ferret.Cli.Tests.Modules;

/// <summary>Test double for IModuleContext.</summary>
internal sealed class FakeModuleCtx : IModuleContext
{
    /// <inheritdoc/>
    public string ModuleId => "ferret.diagnostics";

    /// <inheritdoc/>
    public IExecutionContext ExecutionContext => throw new NotImplementedException();

    /// <inheritdoc/>
    public IModuleRegistry Registry => throw new NotImplementedException();
}
