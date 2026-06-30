using Ferret.Cli.Cli;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class RootCommandFactoryGroupingTests
{
    [Fact]
    public async Task Build_WithGroupedCommands_SubcommandsAppearUnderParent()
    {
        using var output = new StringWriter();
        await RootCommandFactory.Build([new StubGroupModule()], output).InvokeAsync(["grp", "--help"]);
        var text = output.ToString();
        Assert.Contains("sub1", text, StringComparison.Ordinal);
        Assert.Contains("sub2", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_EmptyGroupStub_StillShowsPlannedSubcommands()
    {
        // Sprint 10: "search" is now a real command (SearchCliModule); use "memory" stub instead.
        using var output = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], output).InvokeAsync(["memory"]);
        Assert.Contains("No commands are currently installed", output.ToString(), StringComparison.Ordinal);
    }

    private sealed class StubGroupModule : CliModuleBase
    {
        public override string Name => "stub";

        public override string Description => "Stub.";

        public override IEnumerable<CommandDefinition> GetCommands()
        {
            yield return new CommandDefinition(new CommandMetadata("grp", "A group."), HandlerType: null);
            yield return new CommandDefinition(new CommandMetadata("sub1", "Sub one."), typeof(object), Group: "grp");
            yield return new CommandDefinition(new CommandMetadata("sub2", "Sub two."), typeof(object), Group: "grp");
        }
    }
}
