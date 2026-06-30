using Ferret.Cli.Cli;
using Ferret.Core.Ai.Models;
using Ferret.Models;

namespace Ferret.Cli.Commands.Models;

/// <summary>Handles <c>ferret models info &lt;model-id&gt;</c> — prints detail for a single model.</summary>
internal sealed class ModelsInfoCommandHandler : ICommandHandler
{
    private readonly IModelRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="ModelsInfoCommandHandler"/> class.</summary>
    /// <param name="registry">The model registry to read from.</param>
    public ModelsInfoCommandHandler(IModelRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelIdArg = context.GetOption<string>("model-id") ?? string.Empty;
        var modelId = ModelId.Create(modelIdArg);
        var descriptor = _registry.GetModel(modelId);

        if (descriptor is null)
        {
            context.Services.Output.WriteLine($"Model '{modelIdArg}' not found. Run 'ferret models list' to see available models.");
            return Task.FromResult(CommandResult.Failure);
        }

        var vm = new ModelsInfoViewModel
        {
            ModelId = descriptor.Id.Value,
            Provider = descriptor.ProviderId.Value,
            Capabilities = FormatCapabilities(descriptor.Capabilities),
            ContextWindow = FormatContext(descriptor.ContextWindow),
            Status = "Registered",
        };

        context.Services.Output.WriteLine($"Model:        {vm.ModelId}");
        context.Services.Output.WriteLine($"Provider:     {vm.Provider}");
        context.Services.Output.WriteLine($"Capabilities: {vm.Capabilities}");
        context.Services.Output.WriteLine($"Context:      {vm.ContextWindow}");
        context.Services.Output.WriteLine($"Status:       {vm.Status}");

        return Task.FromResult(CommandResult.Success);
    }

    private static string FormatCapabilities(ModelCapabilities caps)
    {
        if (caps == ModelCapabilities.None)
        {
            return "None";
        }

        var parts = new List<string>();
        if (caps.HasFlag(ModelCapabilities.Chat))
        {
            parts.Add("Chat");
        }

        if (caps.HasFlag(ModelCapabilities.Embedding))
        {
            parts.Add("Embedding");
        }

        if (caps.HasFlag(ModelCapabilities.Reranking))
        {
            parts.Add("Reranking");
        }

        if (caps.HasFlag(ModelCapabilities.Vision))
        {
            parts.Add("Vision");
        }

        return string.Join(", ", parts);
    }

    private static string FormatContext(long? tokens)
    {
        if (tokens is null)
        {
            return "—";
        }

        return $"{tokens.Value:N0} tokens";
    }
}
