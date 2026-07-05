using Ferret.Cli.Cli;
using Ferret.Core.Ai.Models;
using Ferret.Models;

namespace Ferret.Cli.Commands.Models;

/// <summary>Handles <c>ferret models list</c> — prints all registered models in tabular form.</summary>
internal sealed class ModelsListCommandHandler : ICommandHandler
{
    private readonly IModelRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="ModelsListCommandHandler"/> class.</summary>
    /// <param name="registry">The model registry to read from.</param>
    public ModelsListCommandHandler(IModelRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var models = _registry.GetModels();

        if (models.Count == 0)
        {
            context.Services.Output.WriteLine("No models are registered. Configure providers in ferret.json.");
            return Task.FromResult(CommandResult.Success);
        }

        var rows = models.Select(m => new ModelsListViewModel
        {
            Provider = m.ProviderId.Value,
            ModelId = m.Id.Value,
            Capabilities = FormatCapabilities(m.Capabilities),
            ContextWindow = FormatContext(m.ContextWindow),
        }).ToList();

        var providerW = Math.Max("Provider".Length, rows.Max(r => r.Provider.Length));
        var idW = Math.Max("Model".Length, rows.Max(r => r.ModelId.Length));
        var capW = Math.Max("Capabilities".Length, rows.Max(r => r.Capabilities.Length));
        var ctxW = Math.Max("Context".Length, rows.Max(r => r.ContextWindow.Length));

        var header = $"{"Provider".PadRight(providerW)}   {"Model".PadRight(idW)}   {"Capabilities".PadRight(capW)}   {"Context".PadRight(ctxW)}";
        var sep = $"{new string('-', providerW)}   {new string('-', idW)}   {new string('-', capW)}   {new string('-', ctxW)}";

        context.Services.Output.WriteLine(header);
        context.Services.Output.WriteLine(sep);

        foreach (var row in rows)
        {
            context.Services.Output.WriteLine(
                $"{row.Provider.PadRight(providerW)}   {row.ModelId.PadRight(idW)}   {row.Capabilities.PadRight(capW)}   {row.ContextWindow.PadRight(ctxW)}");
        }

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

    private static string FormatContext(long? tokens) =>
        tokens is null ? "—" :
        tokens >= 1000 ? $"{tokens / 1000}k" :
        tokens.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
