# Workspace

A workspace is the central unit of organisation in Ferret. It is a directory with a `.ferret/` folder containing configuration and index state. Every Ferret command operates within a workspace.

## Initialisation

```bash
cd /path/to/my-project
ferret init
```

`ferret init` creates:
- `.ferret/workspace.json` — configuration (edit this)
- `.ferret/state.json` — index state (auto-managed)

Optionally specify a workspace ID:
```bash
ferret init --id my-project
```

## workspace.json

The user-editable configuration file. The minimal version created by `ferret init`:

```json
{
  "workspaceId": "my-project",
  "schemaVersion": 1,
  "connectors": [
    {
      "type": "filesystem",
      "instanceId": "default",
      "root": "."
    }
  ]
}
```

See [Configuration Reference](../reference/configuration) for the full schema.

## Health Checks

```bash
ferret doctor
```

Runs health checks on:
- Workspace configuration validity
- Index file existence and integrity
- Connector connectivity
- Provider availability (if configured)

Use `--verbose` for detailed output on each check.

## Workspace Upgrade

When you update Ferret, run `ferret doctor` first. If the workspace schema version has changed, Ferret will prompt you to upgrade:

```bash
ferret doctor
# WARNING: Workspace schema v1 found; current version is v2.
# Run: ferret workspace upgrade
```

## Multiple Workspaces

Each project has its own workspace. Run `ferret init` in each project directory. Ferret uses the nearest `.ferret/` ancestor directory when running commands.

## Related

- [First Workspace](../getting-started/first-workspace) — getting started walkthrough
- [Connectors](connectors) — configure what gets indexed
- [Configuration Reference](../reference/configuration) — full schema
