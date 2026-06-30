namespace Ferret.Core.Errors;

/// <summary>Base class for all Ferret platform exceptions.</summary>
public abstract class FerretException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FerretException"/> class.</summary>
    protected FerretException()
        : base()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FerretException"/> class with a message.</summary>
    /// <param name="message">The exception message.</param>
    protected FerretException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FerretException"/> class with a message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected FerretException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
