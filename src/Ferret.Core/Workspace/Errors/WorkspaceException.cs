using Ferret.Core.Errors;

namespace Ferret.Core.Workspace.Errors;

/// <summary>Base class for all workspace-related platform exceptions.</summary>
public abstract class WorkspaceException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceException"/> class.</summary>
    protected WorkspaceException()
        : base("A workspace error occurred.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceException"/> class with a message.</summary>
    /// <param name="message">A message describing the workspace error.</param>
    protected WorkspaceException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the workspace error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected WorkspaceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
