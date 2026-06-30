# Release Checklist

Run before creating a sprint release tag. All items must pass. Mark: ✓ Pass | ✗ Fail | N/A.

## Work Item Completion
- [ ] All WIs in the sprint spec are in Done status
- [ ] No WI is in In Progress or Planned status
- [ ] All WI acceptance criteria have been explicitly verified (not assumed)

## Code Quality
- [ ] CI is green on master at the commit being tagged
- [ ] Zero open Blockers from any code or architecture review in this sprint
- [ ] No `//TODO` or `//FIXME` comments introduced in this sprint's commits

## Documentation
- [ ] `grep -rn "TODO\|TBD\|\[fill in\]" docs/` returns zero results in any document produced this sprint
- [ ] All README indexes reflect the documents produced this sprint
- [ ] All ARCH-NNN documents produced this sprint are in Accepted status (not Draft)
- [ ] `grep -rn "DOC-00[1-4]" docs/` returns zero results

## Cross-References
- [ ] All links in documents produced this sprint resolve to existing files
- [ ] All Mermaid diagrams in documents produced this sprint are valid syntax
- [ ] No reference to a Planned document as Accepted

## Architecture
- [ ] All Architecture Fitness Functions pass in CI
- [ ] No new lateral engine-to-engine dependencies introduced
- [ ] `Ferret.Core` has zero project references (verified by CI fitness function)

## Pending ADRs
- [ ] All ADRs due this sprint are Accepted and in `docs/adr/`
- [ ] Decision Register updated with all accepted decisions from this sprint

## Release Artefacts
- [ ] Release notes drafted in `docs/012-Releases/Sprint-N.md`
- [ ] Release notes include: sprint goal, WIs completed, architecture documents produced, known limitations, pending ADRs due next sprint
- [ ] Sprint spec status updated to Complete

## Tag
- [ ] Release notes commit is the HEAD commit on master
- [ ] `git tag sprint-N -m "Sprint N: <goal>"` created
- [ ] Tag pushed to origin
