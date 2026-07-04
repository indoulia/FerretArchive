using Ferret.Cli.Cli;
using Ferret.Cli.Commands;
using Ferret.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.VerticalSlice;

/// <summary>
/// Test-only <see cref="ICliModule"/> exercising T8's execution-policy layer. Never passed to
/// <c>Program.cs</c>'s real module list — building a <see cref="RootCommandFactory"/> app requires
/// explicitly including this module, which only test code does. Registering
/// <see cref="VerticalSliceCommandHandler"/> this way keeps ARCH-034 §5's "no new CLI
/// command/tool/flag" boundary intact: nothing is added to the shipped <c>ferret</c> command
/// surface, while the handler still runs through the same real <see cref="ICommandHandler"/>,
/// <see cref="IFerretContext"/>, and <see cref="CommandResult"/> contracts a real command uses.
/// S2-1A: the dependency-record store's backing file path is a construction-time concern of this
/// module (known to its caller before <see cref="RootCommandFactory"/> builds the DI container),
/// not a per-invocation CLI argument — this is what lets <see cref="VerticalSliceCommandHandler"/>
/// receive a working <see cref="IDependencyStateStore"/> through constructor injection.
/// S2-1B: lives in <c>Ferret.VerticalSlice</c> (a non-test assembly), not
/// <c>Ferret.Integration.Tests</c>, so that <c>Ferret.VerticalSliceHost</c> (production-shaped
/// code) no longer depends on a test assembly to obtain it.
/// S2-2: this composition root now wires up <see cref="FileDependencyStateStore"/> (ADR-0022),
/// not <see cref="SpikeDependencyStateStore"/> — the spike remains in the codebase as a reference
/// implementation but is no longer the one this module registers.
/// S2-4 (ADR-0024): <see cref="FileDependencyStateStore"/> now treats its constructor argument as
/// a root directory rather than a single file, so this module's own <c>_storePath</c> field is
/// unchanged but is now the store's root directory, not one target file.
/// </summary>
#pragma warning disable CA1812 // constructed by Ferret.VerticalSliceHost's Program.cs and by test code in other assemblies; this compilation has no visible `new` of its own
internal sealed class VerticalSliceCliModule : CliModuleBase
{
    private readonly string _storePath;

    /// <summary>Initializes a new instance of the <see cref="VerticalSliceCliModule"/> class.</summary>
    /// <param name="storePath">Absolute path to the directory under which this module's command's dependency-record store keeps its keyed record files (ADR-0024).</param>
    public VerticalSliceCliModule(string storePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storePath);
        _storePath = storePath;
    }

    /// <inheritdoc/>
    public override string Name => "ferret.vslice-test";

    /// <inheritdoc/>
    public override string Description => "Sprint 1 proof-only command; not part of the real CLI.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("vslice-resolve", "Sprint 1 proof: resolve-or-recompute one file.", Hidden: true),
            typeof(VerticalSliceCommandHandler))
            .WithArgument("path", "Absolute path to the file to resolve.");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IDependencyStateStore>(_ => new FileDependencyStateStore(_storePath));
        services.AddTransient<VerticalSliceCommandHandler>();
    }
}
#pragma warning restore CA1812
