namespace Ferret.Core.Workspace;

/// <summary>Controls how thorough a workspace health check is.</summary>
public enum HealthCheckDepth
{
    /// <summary>A fast structural check — verifies that required files are present and readable.</summary>
    Quick = 0,

    /// <summary>A full semantic check — validates file contents, schema consistency, and index integrity.</summary>
    Deep = 1,
}
