ID: FD-005
Title: Capability Record Template
Type: Foundational Decision
Status: Approved
Version: 1.0
Owner: TODO
Approved By: TODO
Related Decisions: FD-001, FD-002, FD-003, FD-004
Related Documents: [Capability-Template.md](../capabilities/Capability-Template.md)
Last Updated: TODO

---

## Title

Capability Record Template

## Definition

A Capability represents a business function provided by Ferret. Every Capability is the primary unit of product evolution.

## Capability Record Template

| Field | Required | Description |
|--------|----------|-------------|
| Capability ID | Yes | Unique identifier (CAP-xxx) |
| Name | Yes | Capability name |
| Purpose | Yes | Why the capability exists |
| Domain | Yes | Primary Product Domain |
| Classification | Yes | Build / Integrate / Extend / Compose |
| Status | Yes | Proposed / Planned / Active / Deprecated |
| Depends On | No | Other Capabilities |
| Technologies | No | Related Technologies |
| Related Decisions | No | Related Architecture Decisions |
| Notes | No | Additional information |

## Governance

- Capability IDs use CAP-xxx.
- Every Capability belongs to exactly one Product Domain.
- Every Capability has exactly one Classification.
- Capability records may reference Technologies and Architecture Decisions.
- Do not add additional fields without ARB approval.

## Status

Approved
