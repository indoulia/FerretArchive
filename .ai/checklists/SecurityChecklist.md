# Security Checklist

Run on any PR that touches: plugin host, `IPlugin`, permission enforcement, audit log, secret resolution, credential handling, or file system access from plugins. Mark: ✓ Pass | ✗ Fail | N/A.

## Secrets and Credentials
- [ ] No literal credential, API key, token, or password in any committed file
- [ ] No secret stored as a literal value in `workspace.json` or `config.json`
- [ ] Secrets referenced only as environment variable references (e.g., `${FERRET_API_KEY}`)
- [ ] `ISecretProvider` used for secret resolution — no `Environment.GetEnvironmentVariable` directly in engine code

## Logging
- [ ] No sensitive data logged at any level (credentials, file contents, PII, tokens)
- [ ] Log messages use named properties, not interpolated strings that could include sensitive values
- [ ] No stack traces logged below Error level

## Plugin Permissions
- [ ] Every capability the plugin uses is declared in `plugin.json`
- [ ] No capability declared that the plugin does not actually use (over-declaration is also a finding)
- [ ] `filesystem:write` declared if the plugin writes to the file system
- [ ] `network:outbound` declared if the plugin makes outbound network calls
- [ ] Plugin does not access `Ferret.Runtime` assemblies directly (Core only)
- [ ] No capability escalation pathway exists (plugin cannot acquire undeclared permissions at runtime)

## Input Validation
- [ ] All user-supplied input validated at the system boundary before use
- [ ] File paths from user input are canonicalised and validated to be within allowed directories
- [ ] No command injection risk in any external process invocation

## Audit
- [ ] All security-relevant actions (plugin activation, permission grant, artifact commit) produce an audit log entry
- [ ] Audit log entries include: actor, action, resource, timestamp, outcome

## Sensitive Files
- [ ] `SensitiveFileViolationException` is raised (not silently skipped) when a plugin attempts to read a sensitive file
- [ ] Sensitive file patterns include at minimum: `.env`, `*.pem`, `*.key`, `id_rsa`, `credentials`

## Severity of Findings
- **Critical**: Capability escalation, secret stored in plain text, sensitive data in logs
- **High**: Over-declared permissions, missing audit entries, unvalidated file path traversal
- **Medium**: Missing input validation at non-critical boundary, verbose error messages exposing internals
- **Low**: Defensive improvement suggestions

Critical and High findings are Blockers. Medium and Low are Suggestions.
