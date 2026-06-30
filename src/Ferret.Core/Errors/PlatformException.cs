namespace Ferret.Core.Errors;

/// <summary>Thrown when an unrecoverable platform-level error occurs.</summary>
public sealed class PlatformException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="PlatformException"/> class.</summary>
    public PlatformException()
        : base("A platform error occurred.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PlatformException"/> class with a message.</summary>
    /// <param name="message">A message describing the platform error.</param>
    public PlatformException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PlatformException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the platform error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public PlatformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
