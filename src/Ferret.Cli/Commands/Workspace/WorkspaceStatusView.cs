namespace Ferret.Cli.Commands.Workspace;

/// <summary>Presentation model for the result of 'ferret workspace status'.</summary>
internal sealed record WorkspaceStatusView(
    bool IsInWorkspace,
    string? Name = null,
    string? Id = null,
    string? RootPath = null,
    string? CreatedAt = null,
    string? ErrorMessage = null);
