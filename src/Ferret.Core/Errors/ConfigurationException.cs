namespace Ferret.Core.Errors;

/// <summary>Thrown when a configuration value is missing, malformed, or invalid.</summary>
public sealed class ConfigurationException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationException"/> class.</summary>
    public ConfigurationException()
        : base("A configuration error occurred.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException"/> class with a message.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ConfigurationException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
