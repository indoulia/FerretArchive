namespace Ferret.Core.Errors;

/// <summary>Thrown when an operation is denied because the caller lacks the required permission.</summary>
public sealed class PermissionDeniedException : SecurityException
{
    /// <summary>Initializes a new instance of the <see cref="PermissionDeniedException"/> class.</summary>
    public PermissionDeniedException()
        : base("Permission denied.")
    {
        Permission = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="PermissionDeniedException"/> class with the required permission identifier.</summary>
    /// <param name="permission">The permission identifier that was required but not held.</param>
    public PermissionDeniedException(string permission)
        : base($"Permission denied: '{permission}' is required to perform this operation.")
    {
        Permission = permission;
    }

    /// <summary>Initializes a new instance of the <see cref="PermissionDeniedException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the permission denial.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public PermissionDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
        Permission = string.Empty;
    }

    /// <summary>Gets the permission identifier that was required but not held.</summary>
    public string Permission { get; }
}
