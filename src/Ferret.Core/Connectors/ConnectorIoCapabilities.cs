namespace Ferret.Core.Connectors;

/// <summary>Describes the raw I/O operations a connector supports.</summary>
public sealed class ConnectorIoCapabilities
{
    private ConnectorIoCapabilities(bool canRead, bool canWrite, bool canStream, bool supportsChangeDetection)
    {
        CanRead = canRead;
        CanWrite = canWrite;
        CanStream = canStream;
        SupportsChangeDetection = supportsChangeDetection;
    }

    /// <summary>Gets a value indicating whether this connector can read content.</summary>
    public bool CanRead { get; }

    /// <summary>Gets a value indicating whether this connector can write content.</summary>
    public bool CanWrite { get; }

    /// <summary>Gets a value indicating whether this connector supports streaming.</summary>
    public bool CanStream { get; }

    /// <summary>Gets a value indicating whether this connector can detect changes since last sync.</summary>
    public bool SupportsChangeDetection { get; }

    /// <summary>Creates a <see cref="ConnectorIoCapabilities"/> with explicit values.</summary>
    /// <param name="canRead">Whether the connector can read content.</param>
    /// <param name="canWrite">Whether the connector can write content.</param>
    /// <param name="canStream">Whether the connector supports streaming.</param>
    /// <param name="supportsChangeDetection">Whether the connector can detect changes.</param>
    /// <returns>A new <see cref="ConnectorIoCapabilities"/> instance.</returns>
    public static ConnectorIoCapabilities Create(bool canRead, bool canWrite, bool canStream, bool supportsChangeDetection) =>
        new(canRead, canWrite, canStream, supportsChangeDetection);

    /// <summary>Creates a read-only <see cref="ConnectorIoCapabilities"/>.</summary>
    /// <returns>A <see cref="ConnectorIoCapabilities"/> with only <see cref="CanRead"/> set to <see langword="true"/>.</returns>
    public static ConnectorIoCapabilities ReadOnly() => new(true, false, false, false);
}
