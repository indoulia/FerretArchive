using Ferret.Cli.Cli;
using Ferret.Persistence;

namespace Ferret.VerticalSlice;

/// <summary>
/// T8: the first layer that owns execution policy. Composes T7's resolution outcome with T2's
/// fetch and T6's recompute — Satisfied reuses the persisted record's output; Not-satisfied or
/// Indeterminate recomputes via <see cref="VerticalSliceDriver.ScanAndPersistAsync"/>. Introduces
/// no comparison or matching logic of its own; T4/T5 remain the sole source of the outcome.
/// S2-1A: receives its <see cref="IDependencyStateStore"/> through the constructor — resolved by
/// DI (<see cref="VerticalSliceCliModule.ConfigureServices"/>) — instead of constructing
/// <see cref="SpikeDependencyStateStore"/> directly.
/// S2-1B: lives in <c>Ferret.VerticalSlice</c> (a non-test assembly), not
/// <c>Ferret.Integration.Tests</c>, so that <c>Ferret.VerticalSliceHost</c> (production-shaped
/// code) no longer depends on a test assembly to obtain it.
/// </summary>
#pragma warning disable CA1812 // resolved via DI by CommandDefinition.HandlerType through reflection, not a direct `new` the analyzer can see
internal sealed class VerticalSliceCommandHandler : ICommandHandler
{
    private readonly IDependencyStateStore _store;

    /// <summary>Initializes a new instance of the <see cref="VerticalSliceCommandHandler"/> class.</summary>
    /// <param name="store">The dependency-state store to resolve and persist through.</param>
    public VerticalSliceCommandHandler(IDependencyStateStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var filePath = context.GetOption<string>("path")
            ?? throw new InvalidOperationException("'path' argument is required.");

        var rootPath = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException($"Cannot determine directory for '{filePath}'.");
        var fileName = Path.GetFileName(filePath);
        var requestPath = Path.Join(rootPath, fileName);

        var outcome = await VerticalSliceDriver.ResolveAndReuseAsync(rootPath, fileName, _store, context.CancellationToken)
            .ConfigureAwait(false);

        var record = outcome == ResolutionOutcome.Satisfied
            ? await _store.GetRecordAsync(VerticalSliceDriver.EngineResponsibility, requestPath, context.CancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Satisfied outcome reported but no record could be fetched.")
            : await VerticalSliceDriver.ScanAndPersistAsync(rootPath, fileName, _store, context.CancellationToken).ConfigureAwait(false);

        context.Services.Output.WriteLine(record.PlainText ?? string.Empty);
        return CommandResult.Success;
    }
}
#pragma warning restore CA1812
