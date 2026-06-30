# Command: commit

## When Invoked
The user types `/commit` or asks to commit current changes.

## Behaviour

1. **Check status**
   Run `git status`. List all modified and untracked files. If there are no changes, report "Nothing to commit." and stop.

2. **Identify the scope**
   Determine which files belong to the current WI. Do not stage files outside the WI scope without explicit user confirmation.

3. **Exclude sensitive and generated files**
   Do not stage:
   - `.env`, `*.key`, `*.pem`, `credentials.*`, `secrets.*`
   - `bin/`, `obj/`, `TestResults/`, `.ai/cache/`, `.ai/summaries/`
   - Files not in the WI scope

4. **Stage files**
   Stage the WI-scoped files explicitly by name — do not use `git add -A` or `git add .`.

5. **Write commit message**
   Follow the conventional commits format:
   ```
   type(scope): description (WI-XYYY)

   [optional body — only if the change needs context beyond the title]

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   ```

   Type values: `feat` (new feature), `fix` (bug fix), `docs` (documentation only), `chore` (build, config, tooling), `test` (test only), `refactor` (no behaviour change).

   Scope: the module or document category (e.g., `runtime`, `arch-012`, `std-005`).

6. **Commit**
   Use a bash heredoc for the commit message to avoid shell escaping issues:
   ```bash
   git commit -m "$(cat <<'EOF'
   type(scope): description (WI-XYYY)

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   EOF
   )"
   ```

7. **Verify**
   Run `git log --oneline -1` to confirm the commit was created with the correct message.

## What NOT to Do
- Do not use `git add -A` or `git add .` — always stage by name
- Do not commit `.env` or any file matching the sensitive file exclusion list
- Do not amend a previous commit — create a new commit
- Do not use `--no-verify`
- Do not push unless the user explicitly requests it
