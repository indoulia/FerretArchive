# CLI Reference — [Command Group]

| Field | Value |
|---|---|
| **Status** | Draft \| Stable \| Deprecated |
| **Version** | 0.1.0 |
| **Author** | [name] |
| **Date** | YYYY-MM-DD |

---

## Overview

<!--
Brief description of this command group.
-->

## Global Options

| Option | Short | Type | Default | Description |
|---|---|---|---|---|
| `--config` | `-c` | `file` | `~/.ferret/config.json` | Config file path |
| `--output` | `-o` | `json\|table\|plain` | `table` | Output format |
| `--verbose` | `-v` | flag | false | Verbose logging |
| `--quiet` | `-q` | flag | false | Suppress all output except errors |
| `--help` | `-h` | flag | | Show help |

---

## Commands

### `ferret [group] [command]`

**Synopsis**

```
ferret [group] [command] [options] [arguments]
```

---

### `ferret [group] list`

**Description:** List all [resources].

**Usage**

```
ferret [group] list [--filter <expr>] [--page <n>] [--page-size <n>]
```

**Options**

| Option | Type | Default | Description |
|---|---|---|---|
| `--filter` | string | | JMESPath filter expression |
| `--page` | int | 1 | Page number |
| `--page-size` | int | 20 | Results per page |

**Example**

```powershell
ferret agent list --filter "status=='running'" --output json
```

**Output**

```json
[
  { "id": "uuid", "name": "my-agent", "status": "running" }
]
```

---

### `ferret [group] create`

<!--
Repeat for each command.
-->

---

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | Usage / argument error |
| `3` | Configuration error |
| `4` | Authentication error |
| `5` | Resource not found |

---

_Template version: 1.0 — stored in `/templates/cli.md`_
