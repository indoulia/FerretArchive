using Ferret.Core.Ai.Models;
using Ferret.Core.Errors;

namespace Ferret.Models.Exceptions;

/// <summary>Thrown when a requested model ID is not available in the registry.</summary>
public sealed class ModelNotFoundException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="ModelNotFoundException"/> class.</summary>
    public ModelNotFoundException()
        : base("A requested AI model is not available.")
    {
        ModelId = default;
    }

    /// <summary>Initializes a new instance of the <see cref="ModelNotFoundException"/> class for a specific model.</summary>
    /// <param name="modelId">The model identifier that was not found.</param>
    public ModelNotFoundException(ModelId modelId)
        : base($"Model '{modelId.Value}' is not available. Run `ferret models list` to see available models.")
    {
        ModelId = modelId;
    }

    /// <summary>Initializes a new instance of the <see cref="ModelNotFoundException"/> class with a message.</summary>
    /// <param name="message">A message describing the error.</param>
    public ModelNotFoundException(string message)
        : base(message)
    {
        ModelId = default;
    }

    /// <summary>Initializes a new instance of the <see cref="ModelNotFoundException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ModelNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        ModelId = default;
    }

    /// <summary>Gets the model ID that was not found.</summary>
    public ModelId ModelId { get; }
}
