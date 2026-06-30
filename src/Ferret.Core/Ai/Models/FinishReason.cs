namespace Ferret.Core.Ai.Models;

/// <summary>Reason a model stopped generating tokens.</summary>
public enum FinishReason
{
    /// <summary>The model reached a natural stopping point.</summary>
    Stop,

    /// <summary>The model stopped because it reached the configured token limit.</summary>
    Length,

    /// <summary>The model stopped to invoke one or more tools.</summary>
    ToolCalls,

    /// <summary>The model stopped because the output was filtered by a content policy.</summary>
    ContentFilter,

    /// <summary>The model stopped due to an error condition.</summary>
    Error,
}
