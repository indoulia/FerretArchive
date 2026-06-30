# Command: compact

## When Invoked
The user types `/compact` or context is approaching the 100K token warning threshold.

## Behaviour

1. **Save active work to current-context.json**
   Update `.ai/current-context.json` with:
   - Active sprint number and WI in progress (if any)
   - Files modified in this session that are not yet committed
   - Key architectural decisions made or confirmed in this session
   - Open questions or blockers identified in this session
   - Next action when session resumes

   Keep `current-context.json` under 100 entries. Remove stale entries (completed WIs, resolved questions).

2. **Update session.md**
   Write `.ai/session.md` with a brief (under 2KB) summary of:
   - What was accomplished in this session
   - Where execution was paused (sprint, WI, step)
   - What to do next when the session resumes

3. **Identify what can be cleared**
   Report to the user which loaded documents are no longer needed in context (e.g., ARCH documents for completed WIs, plan files that are executed). Suggest which can be cleared.

4. **Recommend compact or clear**
   - If the remaining work is related to the current sprint: recommend `/compact` (preserves conversation history summary)
   - If moving to unrelated work: recommend `/clear` (full context reset)

5. **Commit any uncommitted changes**
   If there are uncommitted changes to `.ai/` files: commit them with message `chore: update session context`.

## What NOT to Do
- Do not save code patterns, architecture details, or file structure to memory — those are in the files
- Do not save more than 100 entries to current-context.json — prune stale entries
- Do not skip saving context before a recommended /compact or /clear
