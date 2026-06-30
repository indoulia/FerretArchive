# Connectors

Connectors are the data sources Ferret indexes. RC1 ships with one built-in connector: the filesystem connector. Additional connectors can be added as extensions.

## Filesystem Connector

The filesystem connector walks a directory tree and discovers files. It is the default connector created by `ferret init`.

### Configuration

```json
{
  "connectors": [
    {
      "type": "filesystem",
      "instanceId": "default",
      "root": ".",
      "include": ["**/*.cs", "**/*.md", "**/*.json", "**/*.txt"],
      "exclude": ["**/bin/**", "**/obj/**", "**/node_modules/**", "**/.git/**"]
    }
  ]
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `type` | string | required | `"filesystem"` |
| `instanceId` | string | required | Unique ID for this connector instance |
| `root` | string | `"."` | Root directory to index (relative to workspace root) |
| `include` | string[] | all files | Glob patterns to include |
| `exclude` | string[] | none | Glob patterns to exclude |

### .ferretignore

Create `.ferretignore` at the workspace root for gitignore-style exclusion rules:

```
# Build output
bin/
obj/

# Generated files
*.generated.cs
*.designer.cs

# Test fixtures
tests/fixtures/

# Large data files
*.parquet
*.db
```

Files matching `.ferretignore` are excluded even if they match an `include` pattern. The file is applied automatically — no connector configuration required.

**Supported patterns:**

| Pattern | Meaning | Example |
|---|---|---|
| `*.ext` | All files with this extension in any directory | `*.db` |
| `dir/` | Entire directory tree | `bin/` |
| `**/dir/` | Directory at any depth | `**/node_modules/` |
| `**/*.ext` | Extension match across all subdirectories | `**/*.generated.cs` |
| `/path` | Root-anchored — only matches at workspace root | `/dist/` ignores top-level `dist/` only |

## Multiple Connector Instances

You can configure multiple filesystem instances to index different roots:

```json
{
  "connectors": [
    {
      "type": "filesystem",
      "instanceId": "src",
      "root": "src",
      "include": ["**/*.cs"]
    },
    {
      "type": "filesystem",
      "instanceId": "docs",
      "root": "docs",
      "include": ["**/*.md"]
    }
  ]
}
```

Index only a specific connector:
```bash
ferret index --connector docs
```

## Connector Health

```bash
ferret doctor
# Connector filesystem:default   OK   1,247 assets discovered
```

## Related

- [Indexing](indexing) — running the index pipeline
- [Parsers](parsers) — how connector output is parsed into documents
- [Developer Guide: Create a Connector](../developer-guide/create-connector) — build a custom connector
