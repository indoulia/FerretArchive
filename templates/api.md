# API Reference — [Service / Endpoint Group Name]

| Field | Value |
|---|---|
| **Status** | Draft \| Stable \| Deprecated |
| **Version** | v1 |
| **Base URL** | `https://api.ferret.dev/v1` |
| **Auth** | Bearer JWT \| API Key |
| **Date** | YYYY-MM-DD |
| **Last Updated** | YYYY-MM-DD |

---

## Overview

<!--
Brief description of what this API group does.
-->

## Authentication

```http
Authorization: Bearer <token>
# OR
X-Api-Key: <key>
```

## Common Headers

| Header | Required | Description |
|---|---|---|
| `Content-Type` | Yes | `application/json` |
| `Accept` | No | `application/json` (default) |
| `X-Request-Id` | No | Client-generated idempotency key |

## Error Response Schema

All errors follow [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457):

```json
{
  "type": "https://ferret.dev/errors/validation-failed",
  "title": "Validation Failed",
  "status": 400,
  "detail": "The 'name' field is required.",
  "instance": "/api/v1/agents"
}
```

---

## Endpoints

### `POST /[resource]`

**Summary:** [one line description]

**Request**

```json
{
  "field": "value"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `field` | string | Yes | |

**Response — 201 Created**

```json
{
  "id": "uuid",
  "createdAt": "2026-06-27T12:00:00Z"
}
```

**Error Codes**

| Status | Type | When |
|---|---|---|
| 400 | `validation-failed` | Invalid request body |
| 401 | `unauthorized` | Missing or invalid token |
| 409 | `conflict` | Resource already exists |

---

### `GET /[resource]/{id}`

<!--
Repeat for each endpoint.
-->

---

_Template version: 1.0 — stored in `/templates/api.md`_
