namespace Ferret.Core.Runtime;

/// <summary>Defines the capabilities that a module can declare.</summary>
[Flags]
public enum ModuleCapability
{
    /// <summary>No capabilities declared.</summary>
    None = 0,

    /// <summary>The module provides file-indexing capability.</summary>
    Indexing = 1 << 0,

    /// <summary>The module provides knowledge-graph query capability.</summary>
    Knowledge = 1 << 1,

    /// <summary>The module provides AI-assisted review capability.</summary>
    Review = 1 << 2,

    /// <summary>The module provides specification management capability.</summary>
    Specification = 1 << 3,

    /// <summary>The module provides session memory capability.</summary>
    Memory = 1 << 4,

    /// <summary>The module provides workspace lifecycle capability.</summary>
    Workspace = 1 << 5,

    /// <summary>The module provides artefact provenance capability.</summary>
    Artifact = 1 << 6,
}
