# Workflow: ReleaseSprint

## Trigger
All work items in the sprint reach Done status and CI is green on master.

## Inputs
- Sprint specification document (for sprint version and goal)
- ReleaseChecklist.md
- Git log for all commits since the previous sprint tag
- `docs/012-Releases/` (existing release notes)

## Preconditions
- All WIs in the sprint are in Done status
- CI is green on the master branch at the tip commit
- No open Blockers from any code or architecture review in this sprint
- ChiefArchitect and ProductManager confirm sprint is complete

## Execution Steps

1. **Run ReleaseChecklist.md**
   All items must pass. Do not proceed if any item fails.

2. **Write release notes**
   Create `docs/012-Releases/Sprint-N.md` with:
   - Sprint number and goal
   - Date range
   - Work items completed (WI-ID, title, brief description)
   - Architecture documents produced or updated
   - Known limitations or deferred items
   - Pending ADRs due in the next sprint

3. **Determine version tag**
   Sprint version follows the pattern `sprint-N` for pre-1.0 sprints (e.g., `sprint-1`). When the platform reaches v1.0, switch to SemVer tags (`v1.0.0`).

4. **Commit release notes**
   Stage and commit: `docs: Sprint N release notes`.

5. **Tag the commit**
   `git tag sprint-N -m "Sprint N: <goal>"` on the HEAD commit after the release notes commit.

6. **Push tag**
   `git push origin sprint-N`

7. **Update sprint spec status**
   In the sprint specification document, update the status from In Progress to Complete and add the completion date.

8. **Notify**
   Post sprint completion summary in the project discussion or communication channel. Include: sprint number, goal achieved, next sprint start date (if known).

## Validation
- ReleaseChecklist.md all items pass
- Release notes committed and readable
- Tag exists on origin
- Sprint spec marked Complete

## Outputs
- `docs/012-Releases/Sprint-N.md` release notes
- Git tag `sprint-N` on origin
- Updated sprint spec (status: Complete)

## Exit Criteria
Tag is on origin, release notes are committed, sprint spec is Complete.
