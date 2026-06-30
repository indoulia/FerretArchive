# Plugin Design — [Plugin Name]

| Field | Value |
|---|---|
| **Status** | Draft \| Review \| Accepted |
| **Plugin ID** | `ferret.[vendor].[name]` |
| **Version** | 0.1.0 |
| **Author** | [name / org] |
| **Date** | YYYY-MM-DD |
| **Min Ferret Version** | 0.1.0 |

---

## Purpose

<!--
What capability does this plugin add to Ferret?
One paragraph.
-->

## Plugin Type

- [ ] Tool Plugin — exposes new tools to the agent runtime
- [ ] Provider Plugin — wraps an external AI model or service
- [ ] Storage Plugin — implements a storage backend
- [ ] Transport Plugin — adds a new communication channel
- [ ] Middleware Plugin — intercepts and transforms agent messages

## Manifest (plugin.json)

```json
{
  "id": "ferret.vendor.name",
  "version": "0.1.0",
  "displayName": "Plugin Display Name",
  "description": "One sentence description.",
  "author": "Author Name",
  "minRuntimeVersion": "0.1.0",
  "entryPoint": "Ferret.Plugin.Vendor.Name",
  "permissions": []
}
```

## Activation

<!--
How and when does this plugin activate?
What configuration is required?
-->

## Public Interface

```csharp
// Key interfaces implemented or consumed by this plugin
public interface IMyPlugin : IPlugin
{
    Task<Result> ExecuteAsync(PluginContext context, CancellationToken ct);
}
```

## Configuration Schema

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| | | | | |

## Events Raised / Consumed

| Event | Direction | Description |
|---|---|---|
| | Raised / Consumed | |

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| | | |

## Permissions Required

| Permission | Reason |
|---|---|
| | |

## Security Considerations

<!--
What access does this plugin need? What must be sandboxed?
-->

## Testing

<!--
How is this plugin unit-tested?
Does it require a running instance of the platform?
-->

---

_Template version: 1.0 — stored in `/templates/plugin.md`_
