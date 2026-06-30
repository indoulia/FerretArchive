namespace Ferret.Core.Abstractions;

/// <summary>Abstracts the system clock to enable deterministic testing of time-dependent logic.</summary>
public interface IClock
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTimeOffset UtcNow { get; }
}
