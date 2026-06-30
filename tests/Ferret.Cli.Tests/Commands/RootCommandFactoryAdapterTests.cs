using Ferret.Cli.Cli;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class RootCommandFactoryAdapterTests
{
    [Fact]
    public void Build_WithUnsupportedOptionType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            RootCommandFactory.Build([new UnsupportedOptionTypeModule()]));
    }

    private sealed class UnsupportedOptionTypeModule : CliModuleBase
    {
        public override string Name => "stub.unsupported";
        public override string Description => "Stub for unsupported option type.";

        public override IEnumerable<CommandDefinition> GetCommands()
        {
            yield return new CommandDefinition(
                new CommandMetadata("cmd", "A command."),
                HandlerType: typeof(object),
                Options:
                [
                    new OptionDefinition("--value", "A double value.", typeof(double)),
                ]);
        }
    }
}
