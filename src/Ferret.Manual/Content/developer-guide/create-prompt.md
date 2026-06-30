# Create a Prompt

Prompt templates are registered at startup and rendered on demand. They use `{{variable}}` substitution with required variable validation.

## Step 1: Define the template

```csharp
using Ferret.Core.Prompts;

var template = new PromptTemplate
{
    Id = "summarise-file",
    Description = "Summarise a source file in three sentences",
    Version = new SemanticVersion(1, 0, 0),
    Template = """
        You are a senior software engineer reviewing a codebase.
        Summarise the following file in exactly three sentences.
        Focus on: purpose, key types or functions, and any important patterns.

        File: {{filename}}

        Content:
        {{content}}
        """,
    RequiredVariables = ["filename", "content"]
};
```

## Step 2: Register in DI

```csharp
// In your module's ConfigureServices method:
services.AddSingleton<PromptTemplate>(template);
```

All `PromptTemplate` instances registered in DI are collected by `PromptRegistry` at startup.

## Step 3: Render the template

```csharp
// Injected by DI:
private readonly IPromptRenderer _renderer;

public async Task<string> SummariseFileAsync(string filename, string content)
{
    var variables = new PromptVariables()
        .Set("filename", filename)
        .Set("content", content);

    // Throws PromptRenderException if a required variable is missing
    return _renderer.Render(_registry.Get("summarise-file"), variables);
}
```

## Step 4: Use via CLI

```bash
ferret prompt list
# ID                  Description
# summarise-file      Summarise a source file in three sentences

ferret prompt run summarise-file \
  --var filename=src/Ferret.Search/SearchService.cs \
  --var content="$(cat src/Ferret.Search/SearchService.cs)"
```

## Variable Rules

- `{{variable}}` placeholders are substituted by exact name match
- Variable names are case-sensitive
- Missing required variables throw `PromptRenderException` at render time
- Use `IPromptRenderer.Validate(template, variables)` for pre-flight checks without throwing

## Multiple Templates

Register as many templates as needed:

```csharp
services.AddSingleton<PromptTemplate>(reviewTemplate);
services.AddSingleton<PromptTemplate>(summariseTemplate);
services.AddSingleton<PromptTemplate>(explainTemplate);
```

## Related

- [AI Flow Architecture](../architecture/ai-flow) — how prompts connect to models
- [CLI Reference](../reference/cli) — `ferret prompt` commands
- [ADR-0020](../reference/architecture) — prompt platform architecture
