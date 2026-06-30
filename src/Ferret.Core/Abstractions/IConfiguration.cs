namespace Ferret.Core.Abstractions;

/// <summary>Provides read access to a typed configuration value.</summary>
/// <typeparam name="T">The type of the configuration value.</typeparam>
public interface IConfiguration<out T>
{
    /// <summary>Gets the configuration value.</summary>
    T Value { get; }
}
