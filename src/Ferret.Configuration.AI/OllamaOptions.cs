namespace Ferret.Configuration.Ai;

/// <summary>Configuration for the Ollama provider.</summary>
public sealed class OllamaOptions : ProviderOptions
{
    /// <summary>Initializes a new instance of the <see cref="OllamaOptions"/> class with Ollama defaults.</summary>
    public OllamaOptions()
    {
        BaseUrl = "http://localhost:11434";
        TimeoutSeconds = 120;
    }
}
