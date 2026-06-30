# Workflow: ExecuteSprint

## Trigger
Sprint specification document published in `docs/001-Product/` and sprint start date reached.

## Inputs
- Sprint specification document (Sprint-Template.md format)
- All ARCH-NNN documents referenced by the sprint's work items
- Previous sprint completion summary (if any)
- Decision Register — verify all pending ADRs due this sprint are Accepted

## Preconditions
- All pending ADRs listed in the sprint spec are in Accepted status
- All WI dependencies from prior sprints are in Done status
- CI is green on master before the sprint starts
- ChiefArchitect has signed off on the sprint spec

## Execution Steps

1. **Load sprint context**
   Read the sprint spec. List all WIs, their types, dependencies, and exit criteria. Load the dependency graph — identify which WIs can run in parallel and which must be sequential.

2. **Validate preconditions**
   For each pending ADR listed as due this sprint: confirm it exists in `docs/adr/` with status Accepted. If any ADR is missing, halt and notify ChiefArchitect before proceeding.

3. **Execute work items**
   For each WI in dependency order: invoke ExecuteWorkItem.md. Mark each WI In Progress before starting, Done when all exit criteria are verified.

4. **Validate sprint completion**
   Run the sprint-level validation defined in the sprint spec. Execute final cross-reference check (grep for broken links, DOC-xxx references if applicable). Confirm all README indexes are current.

5. **Run ReleaseSprint.md**
   Invoke ReleaseSprint.md to tag the sprint, write release notes, and close the sprint.

## Validation
- Zero open Blockers across all WIs
- CI green after all commits
- All sprint exit criteria checked and passed
- No placeholder text in any deliverable produced this sprint

## Outputs
- All WIs in Done status
- Git commits for each WI with conventional commit messages
- Release tag and release notes (from ReleaseSprint.md)
- Updated sprint spec with Done status

## Exit Criteria
All work items Done, sprint spec marked complete, CI green, release tag pushed.
