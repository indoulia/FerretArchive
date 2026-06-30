namespace Ferret.Configuration.Ai;

/// <summary>Configuration for the OpenAI provider.</summary>
public sealed class OpenAiOptions : ProviderOptions
{
    /// <summary>Initializes a new instance of the <see cref="OpenAiOptions"/> class with OpenAI defaults.</summary>
    public OpenAiOptions()
    {
        BaseUrl = "https://api.openai.com/v1";
    }
}
