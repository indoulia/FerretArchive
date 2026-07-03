# Parsers

Parsers extract searchable text from files. Ferret dispatches to parsers by MIME type (`MediaType`), not by file extension — extensions are resolved to MIME types once at the connector edge.

## Built-in Parsers

| MIME Type | Extensions | Description |
|---|---|---|
| `text/plain` | `.txt`, `.log`, `.env` | Raw text — no transformation |
| `text/x-csharp` | `.cs` | C# source files |
| `text/markdown` | `.md`, `.mdx` | Markdown (strips headings/code blocks) |
| `application/json` | `.json` | JSON (pretty-prints keys and values) |
| `text/x-yaml` | `.yml`, `.yaml` | YAML |
| `text/xml` | `.xml`, `.csproj`, `.props` | XML |
| `text/x-ini` | `.ini`, `.config`, `.editorconfig` | INI-style config |
| `text/html` | `.html`, `.htm` | HTML (strips tags) |

## Parser Dispatch

1. Connector assigns `MediaType` to each `AssetDescriptor` (based on file extension)
2. `IParserDispatcher` looks up the matching `IContentParser`
3. If no parser handles the MediaType, the asset is counted as **Skipped** (not an error)
4. If the parser returns content, the document is indexed
5. If the parser returns empty content, the asset is counted as **Skipped**
6. If the parser throws (it shouldn't) or returns Failed, the asset is counted as **Failed**

## Skipped vs Failed

After `ferret index`:
```
Discovered:  1,247 assets
Indexed:     1,189 documents
Skipped:        58    ← unsupported types or empty files
Failures:         0
```

Skipped is normal. Binary files (`.dll`, `.png`, `.db`) are skipped because no parser handles their MIME type.

## Checking what types are indexed

Run `ferret doctor`. Its **Parser Platform** report lists the installed parsers, the
extension coverage (how many extensions are indexable as Text or Parseable Binary vs.
treated as Opaque Binary), the parseable and opaque extension lists, and the loaded
parser packages:

```bash
ferret doctor
# Parser Platform
#
# Installed Parsers (7)
#   ✓ Plain Text Parser
#   ✓ Markdown Parser
#   ✓ JSON Parser
#   ✓ CSV Parser
#   ✓ PDF Parser
#   ✓ Word (DOCX) Parser
#   ✓ Excel (XLSX) Parser
#
# Extension Coverage
#   Text: 76
#   Parseable Binary: 3
#   Opaque Binary: 50
#   Known Extensions: 129
#   ...
```

Add `--verbose` for the complete opaque-extension list plus each parser's priority and
media type.

## Adding support for a new file type

See [Create a Parser](../developer-guide/create-parser) for a step-by-step guide to implementing `IContentParser`.

## Related

- [Connectors](connectors) — how files are discovered
- [Indexing](indexing) — how the index pipeline runs
- [Developer Guide: Create a Parser](../developer-guide/create-parser) — add a new format
