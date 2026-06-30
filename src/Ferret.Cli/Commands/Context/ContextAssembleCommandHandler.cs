using System.Globalization;

using Ferret.Cli.Cli;
using Ferret.Core.Context;

namespace Ferret.Cli.Commands.Context;

/// <summary>Handles the <c>ferret context &lt;query&gt;</c> command.</summary>
internal sealed class ContextAssembleCommandHandler : ICommandHandler
{
    private readonly IContextAssembler _assembler;

    /// <summary>Initializes a new instance of the <see cref="ContextAssembleCommandHandler"/> class.</summary>
    public ContextAssembleCommandHandler(IContextAssembler assembler)
    {
        ArgumentNullException.ThrowIfNull(assembler);
        _assembler = assembler;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var query = context.GetOption<string>("query");
        if (string.IsNullOrWhiteSpace(query))
        {
            context.Services.Output.WriteLine("Usage: ferret context <query>");
            return CommandResult.Failure;
        }

        var maxTokensRaw = context.GetOption<int?>("max-tokens");
        var maxDocumentsRaw = context.GetOption<int?>("max-documents");

        var request = new ContextRequest
        {
            Query = query,
            MaxTokens = maxTokensRaw is > 0 ? maxTokensRaw.Value : 8000,
            MaxDocuments = maxDocumentsRaw is > 0 ? maxDocumentsRaw.Value : 10,
        };

        try
        {
            var package = await _assembler.AssembleAsync(request, context.CancellationToken)
                .ConfigureAwait(false);

            await Console.Error.WriteLineAsync(string.Format(
                CultureInfo.InvariantCulture,
                "Assembled {0} document(s) (~{1} tokens) from {2} search hit(s).",
                package.DocumentsIncluded,
                package.TotalTokenEstimate,
                package.DocumentsConsidered)).ConfigureAwait(false);

            context.Services.Output.WriteLine(package.ToPromptString());

            return CommandResult.Success;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            context.Services.Output.WriteError($"Context assembly failed: {ex.Message}");
            return CommandResult.Failure;
        }
    }
}
