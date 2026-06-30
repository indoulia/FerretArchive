namespace Ferret.Core.Events;

/// <summary>Carries source and schema information attached to an event.</summary>
public sealed class EventMetadata
{
    /// <summary>Initializes a new instance of the <see cref="EventMetadata"/> class.</summary>
    /// <param name="source">The module or component that emitted the event.</param>
    /// <param name="schemaVersion">The schema version of the event payload.</param>
    public EventMetadata(string source, string schemaVersion)
    {
        Source = source;
        SchemaVersion = schemaVersion;
    }

    /// <summary>Gets the module or component that emitted the event.</summary>
    public string Source { get; }

    /// <summary>Gets the schema version of the event payload.</summary>
    public string SchemaVersion { get; }
}
