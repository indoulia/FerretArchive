namespace Ferret.Cli.Commands.Workspace;

/// <summary>Presentation model for the result of 'ferret workspace init'.</summary>
internal sealed record WorkspaceInitView(
    bool Succeeded,
    string? RootPath,
    string? FerretPath,
    string? ErrorMessage);
