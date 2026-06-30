# First Workspace

A workspace is a directory Ferret indexes and monitors. Initialise one with `ferret init`.

## Initialise

```bash
cd /path/to/my-project
ferret init
```

Creates:

```
my-project/
└── .ferret/
    ├── workspace.json    workspace configuration
    └── state.json        index state (auto-managed)
```

## Configure

Edit `.ferret/workspace.json` to add connector options:

```json
{
  "workspaceId": "my-project",
  "connectors": [
    {
      "type": "filesystem",
      "instanceId": "default",
      "root": ".",
      "include": ["**/*.cs", "**/*.md", "**/*.json"],
      "exclude": ["**/bin/**", "**/obj/**", "**/node_modules/**"]
    }
  ]
}
```

## .ferretignore

Create `.ferretignore` at the project root (same syntax as `.gitignore`):

```
bin/
obj/
*.generated.cs
*.designer.cs
```

## Related

- [First Index](first-index) — index your workspace
- [Configuration Reference](../reference/configuration) — full config schema
