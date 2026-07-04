ID: FD-006
Title: Technology Record Template
Type: Foundational Decision
Status: Approved
Version: 1.0
Owner: TODO
Approved By: TODO
Related Decisions: FD-001, FD-002, FD-003, FD-004, FD-005
Related Documents: [Technology-Template.md](../technologies/Technology-Template.md)
Last Updated: TODO

---

## Title

Technology Record Template

## Definition

A Technology represents an internal or external technology that is used, integrated, or depended upon by one or more Capabilities.

## Technology Record Template

| Field | Required | Description |
|--------|----------|-------------|
| Technology ID | Yes | Unique identifier (TECH-xxx) |
| Name | Yes | Technology name |
| Category | Yes | Database, Framework, AI, Search, Storage, Messaging, Identity, etc. |
| Classification | Yes | Build / Integrate / Extend / Compose |
| Status | Yes | Proposed / Approved / Active / Deprecated |
| Used By | No | Related Capabilities |
| Related Decisions | No | Related Architecture Decisions |
| Notes | No | Additional information |

## Governance

- Technology IDs use TECH-xxx.
- A Technology may be referenced by multiple Capabilities.
- Technologies do not own Capabilities.
- New fields require explicit ARB approval.

## Status

Approved
