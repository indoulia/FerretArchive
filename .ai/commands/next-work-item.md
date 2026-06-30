# Command: next-work-item

## When Invoked
The user types `/next-work-item` or asks to proceed to the next work item within an active sprint.

## Behaviour

1. **Identify current sprint**
   Read `docs/001-Product/` to find the In Progress sprint specification.

2. **Find the next WI**
   From the sprint spec WI table: find the first WI in Planned status where all dependencies are Done. If multiple WIs are eligible (no dependencies between them), present them to the user and ask which to start — or proceed with the first if the user says to continue.

3. **Load WI context**
   Read the WI specification section in the sprint doc. Read the ARCH-NNN documents referenced by the WI inputs. Check `.ai/current-context.json` for any relevant session context from prior WIs in this sprint.

4. **Mark WI in progress**
   Update the sprint spec WI table status to In Progress before starting any work.

5. **Execute**
   Invoke ExecuteWorkItem.md with the loaded WI context. Follow the workflow step by step. Do not skip steps — particularly the checklist step.

6. **Complete and advance**
   After all acceptance criteria are verified and the checklist passes: mark the WI Done in the sprint spec. Update `.ai/current-context.json`. Report what was done and what the next WI will be.

## What NOT to Do
- Do not start a WI whose dependencies are not Done
- Do not mark a WI Done based on assumption — verify each acceptance criterion explicitly
- Do not skip the checklist step even for documentation-only WIs
- Do not execute multiple WIs in one response without confirming between them
