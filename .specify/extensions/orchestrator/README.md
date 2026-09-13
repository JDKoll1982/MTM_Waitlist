# Spec Orchestrator — Cross-Feature Coordination for Spec Kit

Track state, select tasks, and detect conflicts across parallel spec-driven features — without waiting for merges.

> Solves [spec-kit#2238](https://github.com/github/spec-kit/issues/2238) — cross-feature orchestration as an extension.

## The Problem

Spec Kit's philosophy supports parallel feature branches, but each feature lives in isolation:

- No unified view of **which features are where** in the SDD lifecycle
- No way to query **tasks across features** (e.g., "all database-layer tasks")
- No detection when **two features plan to modify the same files**

As features accumulate, complexity grows exponentially and merge conflicts become inevitable.

## The Solution

Four read-only commands that give you a project-wide view of every active spec:

| Command | Purpose |
|---------|---------|
| `/speckit.orchestrator.status` | Dashboard of all features and their SDD phase (spec → plan → tasks → implementation) |
| `/speckit.orchestrator.next` | Query tasks across all features — filter by tag, layer, blocker status, or priority |
| `/speckit.orchestrator.conflicts` | Detect file overlaps between features before they become merge conflicts |
| `/speckit.orchestrator.sync` | Write consolidated state to `specs/status.json` for CI, dashboards, or external tools |

## Installation

```bash
specify install https://github.com/Quratulain-bilal/spec-kit-orchestrator/archive/refs/tags/v1.0.0.zip
```

## Usage

### See every feature at a glance

```
/speckit.orchestrator.status
```

Output:
```
Feature Lifecycle Overview
==========================
001-user-auth        [implementation]  60% tasks done
002-payments         [tasks]           ready to implement
003-notifications    [plan]            awaiting tasks
004-admin-dashboard  [spec]            needs planning

Stalled: 1 feature (plan phase, no updates in 5 days)
```

### Find your next task across features

```
/speckit.orchestrator.next --layer database
/speckit.orchestrator.next --tag security
/speckit.orchestrator.next --unblocked
```

Output:
```
Cross-Feature Task Queue
========================
[002-payments]      T014  Create payments schema         (unblocked)
[001-user-auth]     T023  Add user session indexes       (unblocked)
[003-notifications] T008  Design notification queue      (blocked by T007)
```

### Detect file conflicts before they happen

```
/speckit.orchestrator.conflicts
```

Output:
```
File Conflict Analysis
======================
WARN: src/api/users.ts
  modified by: 001-user-auth (T012)
  modified by: 004-admin-dashboard (T005)
  Recommendation: merge 001-user-auth first, then rebase 004

INFO: src/db/schema.sql
  modified by: 002-payments (additive)
  modified by: 001-user-auth (additive)
  No conflict expected (both add new tables)
```

### Export state for CI or dashboards

```
/speckit.orchestrator.sync
```

Writes `specs/status.json`:
```json
{
  "generated_at": "2026-04-17T10:00:00Z",
  "features": [
    {
      "id": "001-user-auth",
      "phase": "implementation",
      "progress": 0.60,
      "blocked_tasks": [],
      "files_touched": ["src/auth/*", "src/db/users.sql"]
    }
  ]
}
```

## How It Works

The extension is **100% read-only** on your specs:

1. Scans all `specs/*/` directories
2. Reads `spec.md`, `plan.md`, `tasks.md`, and checks implementation status
3. Parses task headers for tags, layers, and dependencies
4. Cross-references file paths mentioned in tasks to detect overlaps
5. Optionally writes a single consolidated `specs/status.json` (the only file it writes)

No hooks. No spec modifications. No surprises.

## Requirements

- spec-kit >= 0.4.0
- Standard `specs/` directory structure (one folder per feature)

## License

MIT
