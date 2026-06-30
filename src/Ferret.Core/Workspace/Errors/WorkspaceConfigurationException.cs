namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when workspace configuration is invalid or cannot be loaded.</summary>
public sealed class WorkspaceConfigurationException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationException"/> class.</summary>
    public WorkspaceConfigurationException()
        : base("Workspace configuration is invalid or cannot be loaded.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationException"/> class with a message.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    public WorkspaceConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
