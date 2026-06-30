namespace Ferret.Core.Abstractions;

/// <summary>Marks an entity as having a stable string identifier.</summary>
public interface IIdentifiable
{
    /// <summary>Gets the unique identifier of this entity.</summary>
    string Id { get; }
}
