# Workflow: CodeReview

## Trigger
PR opened against master, or review explicitly requested by PlatformEngineer.

## Inputs
- PR diff (all changed files)
- Work item specification (for acceptance criteria)
- CodeChecklist.md
- SecurityChecklist.md (if PR touches plugin host, permissions, auth, or secret handling)
- PerformanceChecklist.md (if PR touches index pipeline, context assembly, or model invocation)

## Preconditions
- CI is passing (build + unit tests green)
- PR description references the WI ID and describes what changed and why

## Execution Steps

1. **Read the PR description and WI spec**
   Understand what the PR is supposed to do. Note the acceptance criteria. If the PR description does not reference a WI, add a Blocker: "PR must reference a work item."

2. **Run CodeChecklist.md**
   Work through every item. For each item: pass, fail, or N/A (with reason). Any fail is a Blocker unless otherwise noted on the checklist item.

3. **Check architectural compliance**
   - Verify no new lateral engine-to-engine method calls (ARCH-001 §8)
   - Verify no `<ProjectReference>` from `Ferret.Core` to any other module
   - Verify `IClock` is used (not `DateTime.Now`) in any changed engine code (ARCH-012 §8)
   - Verify `CancellationToken` is propagated in any new async methods (ARCH-012 §9)

4. **Run SecurityChecklist.md** (if applicable)
   If the PR touches `Ferret.Plugins`, `IPlugin`, permission enforcement, audit log, or secret handling: run SecurityChecklist.md. Any Critical or High finding is a Blocker.

5. **Run PerformanceChecklist.md** (if applicable)
   If the PR touches index build pipeline, context assembly scoring, or model invocation: run PerformanceChecklist.md. A confirmed regression > 20% vs baseline is a Blocker.

6. **Verify acceptance criteria**
   For each acceptance criterion in the WI spec: verify the PR satisfies it. Note which test(s) cover each criterion. An unmet criterion is a Blocker.

7. **Produce review output**
   List all findings as Blocker / Suggestion / Question. Issue Approve or Request Changes.

## Validation
- All CodeChecklist.md items evaluated
- Zero open Blockers at approval time
- Every acceptance criterion verified

## Outputs
- Structured review with categorised findings
- Approve or Request Changes decision

## Exit Criteria
Zero open Blockers, Approve decision issued. PR author merges after approval.
