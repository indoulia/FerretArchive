using Ferret.Cli.Cli;
using Ferret.Core.Ai.Prompts;

namespace Ferret.Cli.Commands.Prompt;

/// <summary>Handles the <c>ferret prompt list</c> command.</summary>
internal sealed class PromptListCommandHandler : ICommandHandler
{
    private readonly IPromptRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="PromptListCommandHandler"/> class.</summary>
    /// <param name="registry">The prompt registry to query.</param>
    public PromptListCommandHandler(IPromptRegistry registry) => _registry = registry;

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var templates = _registry.GetAll();
        if (templates.Count == 0)
        {
            context.Services.Output.WriteLine("No prompt templates are registered.");
            return Task.FromResult(CommandResult.Success);
        }

        var nameWidth = templates.Max(t => t.Name.Length);
        var versionWidth = templates.Max(t => t.Version.Length);

        context.Services.Output.WriteLine(
            "Name".PadRight(nameWidth + 2) +
            "Version".PadRight(versionWidth + 2) +
            "Required Variables");

        context.Services.Output.WriteLine(
            new string('-', nameWidth + 2) +
            new string('-', versionWidth + 2) +
            new string('-', 20));

        foreach (var t in templates)
        {
            var vars = t.RequiredVariables.Count == 0
                ? "(none)"
                : string.Join(", ", t.RequiredVariables);

            context.Services.Output.WriteLine(
                t.Name.PadRight(nameWidth + 2) +
                t.Version.PadRight(versionWidth + 2) +
                vars);
        }

        return Task.FromResult(CommandResult.Success);
    }
}
