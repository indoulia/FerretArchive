namespace Ferret.Core.Errors;

/// <summary>Base class for security-related platform exceptions.</summary>
public class SecurityException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="SecurityException"/> class.</summary>
    public SecurityException()
        : base("A security violation occurred.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SecurityException"/> class with a message.</summary>
    /// <param name="message">A message describing the security violation.</param>
    public SecurityException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SecurityException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the security violation.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SecurityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
