using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Providers.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Providers.OpenAi.Tests;

/// <summary>Runs the shared <see cref="ProviderComplianceTests"/> contract suite against <see cref="OpenAiModelProvider"/>.</summary>
public sealed class OpenAiProviderComplianceTests : ProviderComplianceTests
{
    protected override IModelProvider CreateProvider() =>
        new OpenAiModelProvider(
            new OpenAiOptions { Enabled = true, ApiKey = "sk-test" },
            NullLogger<OpenAiModelProvider>.Instance);
}
