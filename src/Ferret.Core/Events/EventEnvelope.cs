namespace Ferret.Core.Events;

/// <summary>Wraps an event payload with routing and versioning metadata.</summary>
public sealed class EventEnvelope
{
    /// <summary>Initializes a new instance of the <see cref="EventEnvelope"/> class.</summary>
    /// <param name="payload">The event payload to wrap.</param>
    /// <param name="schemaVersion">The schema version of the payload.</param>
    public EventEnvelope(object payload, string schemaVersion)
    {
        EnvelopeId = Guid.NewGuid().ToString("N");
        Payload = payload;
        SchemaVersion = schemaVersion;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the unique identifier of this envelope.</summary>
    public string EnvelopeId { get; }

    /// <summary>Gets the event payload.</summary>
    public object Payload { get; }

    /// <summary>Gets the schema version of the payload.</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the UTC timestamp at which this envelope was created.</summary>
    public DateTimeOffset CreatedOn { get; }
}
