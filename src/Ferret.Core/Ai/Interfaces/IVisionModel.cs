using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Vision model contract — reserved for Sprint 15+.
/// No inference methods are declared in Sprint 12; implementations will accept
/// image inputs when the vision capability is introduced.
/// </summary>
public interface IVisionModel
{
    /// <summary>Gets the model's identity and capabilities.</summary>
    ModelDescriptor Descriptor { get; }
}
