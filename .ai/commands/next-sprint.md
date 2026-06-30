# Command: next-sprint

## When Invoked
The user types `/next-sprint` or asks to begin the next sprint.

## Behaviour

1. **Identify the next sprint**
   Read `docs/001-Product/` to find all sprint specification documents. Identify the highest sprint number that is Complete. The next sprint is that number + 1. If no sprint is Complete, the next sprint is Sprint 1.

2. **Load sprint context**
   Read the sprint specification document for the next sprint. If it does not exist, report that a sprint spec must be created first (using Sprint-Template.md and ProductManager agent guidance) and stop.

3. **Validate preconditions**
   Check that all pending ADRs due this sprint are Accepted (`docs/013-Governance/Decision-Register.md` Pending Decisions table). If any ADR is missing: list the missing ADRs and halt. Do not start the sprint.

4. **Load architecture context**
   Read `.ai/current-context.json`. Read the ARCH-NNN documents referenced by the sprint's work items. Note constraints, interfaces, and dependency rules relevant to this sprint.

5. **Report readiness**
   State: sprint number, goal, work item count, any precondition failures. If all preconditions pass: confirm ready to begin and invoke ExecuteSprint.md.

6. **Execute**
   Invoke ExecuteSprint.md with the loaded sprint context.

## What NOT to Do
- Do not start the sprint if any precondition fails — report what is missing first
- Do not skip reading the ARCH documents for the sprint's WIs before executing
- Do not create a new sprint spec without the ProductManager agent's involvement
- Do not run ReleaseSprint.md at the end automatically — confirm with the user before tagging
