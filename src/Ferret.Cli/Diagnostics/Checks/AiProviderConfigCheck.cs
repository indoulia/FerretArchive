using Ferret.Cli.Cli;

using Microsoft.Extensions.Configuration;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Checks that at least one AI provider is present in the <c>Ferret:Ai:Providers</c> config section.</summary>
internal sealed class AiProviderConfigCheck : IDiagnosticCheck
{
    private readonly IConfiguration _configuration;

    internal AiProviderConfigCheck(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public string Name => "AI provider configured";

    /// <inheritdoc/>
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("Ferret:Ai:Providers");
        bool hasProviders = section.GetChildren().Any();
        var result = hasProviders
            ? DiagnosticCheckResult.Pass()
            : DiagnosticCheckResult.Fail("No AI providers found under 'Ferret:Ai:Providers'. Add Ollama or OpenAi config.");
        return Task.FromResult(result);
    }
}
