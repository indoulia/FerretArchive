# Command: release

## When Invoked
The user types `/release` or asks to release/close the current sprint.

## Behaviour

1. **Identify the active sprint**
   Read `docs/001-Product/` to find the sprint spec with status In Progress. If no sprint is In Progress, report "No active sprint." and stop.

2. **Verify all WIs are Done**
   Read the sprint spec WI table. If any WI is not in Done status: list them and stop. Do not proceed to release with incomplete work.

3. **Run ReleaseChecklist.md**
   Execute every item in `ReleaseChecklist.md`. Report each item's status. If any item fails, report what must be fixed before releasing and stop.

4. **Confirm with the user**
   Before creating the tag: state the sprint number, goal, and proposed tag name. Ask the user to confirm before tagging. Do not tag without explicit confirmation.

5. **Execute ReleaseSprint.md**
   After confirmation: invoke ReleaseSprint.md step by step:
   - Write release notes to `docs/012-Releases/Sprint-N.md`
   - Commit release notes
   - Create tag: `sprint-N`
   - Push tag to origin
   - Update sprint spec status to Complete

6. **Report**
   Confirm tag is on origin. State the tag name, sprint goal, and the next sprint number. Suggest running `/compact` to clear session context before starting the next sprint.

## What NOT to Do
- Do not create a release tag without user confirmation
- Do not release a sprint with any WI not in Done status
- Do not release if CI is not green
- Do not skip ReleaseChecklist.md
- Do not force-push tags
