# Contract: `MTM_Waitlist.Mock.Service` HTTP API

**Feature**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09
**Implements**: FR-010, FR-011, FR-013, FR-023, FR-026
**Verified by**: SC-009, SC-010, SC-011

Small network HTTP interface hosted in-process by the service app (Kestrel via the `Microsoft.AspNetCore.App`
framework reference), bound to a configurable address/port. The application and operators use it for on-demand refresh
and status. **Emergency restore is deliberately absent from this surface** (FR-023) and is only reachable from the
service's own UI on the database host.

---

## 1. Transport and authentication

| Aspect | Requirement |
|---|---|
| Scheme | HTTP on the configured bind address/port (`ApiSettings.BindAddress`, `ApiSettings.Port`; see `mock-service-configuration.md`) |
| Credential header | `X-MTM-Mock-Token: <shared credential>` |
| Comparison | Constant-time comparison against the DPAPI-protected stored credential |
| Missing/incorrect token | `401` with `{"error":"unauthorized"}` — for **every** endpoint; there is no unauthenticated endpoint at all (SC-010) |
| Leakage | The credential is never echoed in any response, header, error message, log line, or status payload (FR-026, SC-010) |
| Logging | Unauthorized attempts are logged with source address, method, path, and timestamp only — never the presented value |
| Content type | `application/json` for all request and response bodies |

## 2. `POST /api/refresh`

Request optional; body selects shapes and, if omitted, refreshes **all enabled shapes**.

```json
{ "shapeKeys": ["work_order_lookup", "operation_sequences"] }
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `shapeKeys` | string[] | no | Omit or send `null` for all enabled shapes. Unknown keys ⇒ `400` |

**Response — `200 OK`** (a completed cycle, including a cycle that skipped unreachable shapes):

```json
{
  "runId": "8f2c…",
  "startedUtc": "2026-09-09T14:02:11Z",
  "finishedUtc": "2026-09-09T14:02:23Z",
  "results": [
    { "shapeKey": "work_order_lookup", "outcome": "succeeded", "rowCount": 184, "durationMs": 1180 },
    { "shapeKey": "operation_sequences", "outcome": "skippedSourceUnreachable", "rowCount": null, "durationMs": 3021,
      "errorMessage": "Visual connection timeout" }
  ]
}
```

| `outcome` value | Meaning | Effect on the mirror |
|---|---|---|
| `succeeded` | Complete new snapshot loaded and swapped in | Live table replaced atomically |
| `skippedSourceUnreachable` | Visual unreachable — a **normal** result, not an error (FR-008, US3 acceptance 3) | None; previous snapshot intact |
| `failedSchemaMismatch` | Source returned an unexpected column set (edge case "Source schema drift") | None; previous snapshot intact; shape marked failed |
| `failedLoad` / `failedUnknown` | Load or procedural failure | None; previous snapshot intact |

| Status | When |
|---|---|
| `400` | Malformed body, or one or more unknown `shapeKeys` |
| `401` | Missing/incorrect credential |
| `409` | A refresh cycle is already running; body reports the in-flight `runId` |

Because `skippedSourceUnreachable` is `200`, a caller can never mistake an unreachable source for a service failure.
The refresh **never** returns partial per-shape data — it reports outcomes only.

## 3. `GET /api/status`

**Response — `200 OK`:**

```json
{
  "serviceVersion": "1.0.0",
  "startedUtc": "2026-09-09T06:00:02Z",
  "visualSourceReachable": false,
  "credentialConfigured": true,
  "refreshIntervalMinutes": 15,
  "shapes": [
    { "shapeKey": "work_order_lookup", "isEnabled": true, "lastRunUtc": "2026-09-09T13:50:00Z",
      "lastOutcome": "succeeded", "lastRowCount": 184, "refreshedUtc": "2026-09-09T13:50:02Z", "isSeedContentOnly": false }
  ],
  "backups": [
    { "store": "mtm_waitlist", "isEnabled": true, "lastRunUtc": "2026-09-09T01:00:11Z",
      "lastOutcome": "succeeded", "lastArtifactPath": "…\\backups\\mtm_waitlist\\mtm_waitlist_20260909T010011.sql",
      "artifactCount": 14, "toolAvailable": true }
  ]
}
```

| Rule | Requirement |
|---|---|
| Content | Per-item last-run outcome and timestamp for **every** refresh shape and **every** backup store (FR-013) |
| Tooling | `toolAvailable: false` reports the missing `mysqldump` condition clearly and accompanies a `toolUnavailable` backup outcome (FR-013, edge case "Backup facility missing") |
| Secrets | The payload contains no credential and no connection-string password |
| Side effects | None — status never triggers a refresh or a backup |

## 4. Backup endpoints (used by the service's own settings UI, and available to an authorized caller)

| Method | Path | Body / query | Response |
|---|---|---|---|
| `POST` | `/api/backup` | `{ "store": "mtm_waitlist" }` | `200` with `{ "artifact": { "store", "createdUtc", "filePath", "sizeBytes", "isRetained" } }`; `400` unknown store, `409` store already backing up |
| `GET` | `/api/backups` | `?store=mtm_waitlist` (optional) | `200` with `{ "artifacts": [ … ] }` ordered newest first, each including `isRetained` |

A backup whose tool is unavailable returns `200` with `outcome: "toolUnavailable"` and **no** artifact — a partial or
zero-length file is never reported as successful (FR-013, SC-008).

## 5. Restore — explicitly NOT on this surface (FR-010, FR-023)

| Method | Path | Response |
|---|---|---|
| any | `/api/restore` | **Not routed.** The API returns `404` for `/api/restore` and for any equivalent path |

Constraints this contract encodes:

- Emergency restore is performed **only** from the service UI running on the database host.
- Restore requires an explicit human confirmation before any data changes; an unconfirmed request changes nothing.
- Restore is a **full replacement** of the selected store from the selected artifact.
- Restore is never bound to a network listener, even with a valid token — there is no code path from an HTTP request to
  `RestoreService`.
- The API host must therefore be able to start with `RestoreService` unregistered/absent; the two are independent
  registrations in the service DI container.

## 6. Error model

```json
{ "error": "unauthorized" | "invalidRequest" | "refreshInProgress" | "unknownShape" | "unknownStore" | "internalError",
  "message": "human-readable, secret-free detail" }
```

No response ever includes a stack trace, a connection string, a credential, or a raw database error containing
connection details; server-side details go to the service log only.
