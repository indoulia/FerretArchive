using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;
using Ferret.Core.Enumerations;
using Ferret.Core.Results;

namespace Ferret.Cli.Commands.Config;

/// <summary>Handles 'ferret config validate' — validates ferret.config.json and reports field errors.</summary>
internal sealed class ConfigValidateCommandHandler : ICommandHandler
{
    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var configPath = context.GetOption<string>("--config") ?? "ferret.config.json";

        if (!File.Exists(configPath))
        {
            context.Services.Output.WriteError($"Config file not found: {configPath}");
            return Task.FromResult(CommandResult.Failure);
        }

        List<ValidationFailure> failures;
        try
        {
            failures = Validate(configPath);
        }
        catch (InvalidDataException ex)
        {
            context.Services.Output.WriteError($"Configuration file contains invalid JSON: {ex.Message}");
            return Task.FromResult(CommandResult.Failure);
        }

        if (failures.Count == 0)
        {
            context.Services.Output.WriteSuccess("ferret.config.json is valid.");
            return Task.FromResult(CommandResult.Success);
        }

        context.Services.Output.WriteError($"ferret.config.json has {failures.Count} error(s):");
        foreach (var f in failures)
        {
            context.Services.Output.WriteLine($"  [{f.Field}] {f.Constraint} — {f.Guidance}");
        }

        return Task.FromResult(CommandResult.Failure);
    }

    private static List<ValidationFailure> Validate(string configPath)
    {
        var failures = new List<ValidationFailure>();
        var configuration = FerretConfigLoader.Load(configPath);

        var workspaceName = configuration["Ferret:Workspace:Name"];
        if (string.IsNullOrWhiteSpace(workspaceName))
        {
            failures.Add(new ValidationFailure(
                "Ferret:Workspace:Name",
                "required",
                "Set a non-empty workspace name in ferret.config.json under Ferret.Workspace.Name.",
                ValidationSeverity.Error));
        }

        var workspaceRoot = configuration["Ferret:Workspace:Root"];
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            failures.Add(new ValidationFailure(
                "Ferret:Workspace:Root",
                "required",
                "Set the workspace root directory in ferret.config.json under Ferret.Workspace.Root.",
                ValidationSeverity.Error));
        }
        else if (!Directory.Exists(workspaceRoot) && !workspaceRoot.Equals(".", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new ValidationFailure(
                "Ferret:Workspace:Root",
                "path-exists",
                $"Workspace root directory '{workspaceRoot}' does not exist.",
                ValidationSeverity.Error));
        }

        return failures;
    }
}
