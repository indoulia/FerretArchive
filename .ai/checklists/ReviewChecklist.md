# Review Checklist

Use at the start of any review to ensure review quality. Mark each item: ✓ Done | ✗ Not Done | N/A.

## Before Starting
- [ ] CI is passing on the PR (do not review a failing build)
- [ ] PR description references a WI ID
- [ ] PR description explains what changed and why (not just what)
- [ ] Author has confirmed the PR is ready for review (not a draft/WIP)

## Checklist Execution
- [ ] CodeChecklist.md run (for code PRs) — all items evaluated
- [ ] ArchitectureChecklist.md run (for ARCH document PRs) — all items evaluated
- [ ] SecurityChecklist.md run (if PR touches plugins, permissions, auth, or secrets)
- [ ] PerformanceChecklist.md run (if PR touches index pipeline, context assembly, or model invocation)

## Findings Quality
- [ ] Every finding is categorised as Blocker / Suggestion / Question
- [ ] Every Blocker has a clear, actionable remediation suggestion
- [ ] Suggestions are not presented as blockers
- [ ] No finding is vague ("this seems wrong") — every finding cites a specific rule or standard

## Acceptance Criteria Verification
- [ ] Each WI acceptance criterion verified explicitly (not assumed)
- [ ] Each acceptance criterion is covered by at least one test
- [ ] No acceptance criterion is met only by manual testing with no automated coverage

## Consistency
- [ ] Changes are consistent with the ARCH-NNN document for the affected module
- [ ] No new behaviour contradicts AC-001 through AC-014
- [ ] No documentation changed without updating cross-references and README index

## Decision
- [ ] Decision is Approve or Request Changes — never "Approve with comments" when Blockers exist
- [ ] If Request Changes: all Blockers listed clearly
- [ ] If Approve: all Blockers confirmed resolved, zero open items

## After Review
- [ ] Review comments posted to PR (not just stored locally)
- [ ] For ARCH reviews: AR-NNN document created and committed to `docs/Reviews/`
