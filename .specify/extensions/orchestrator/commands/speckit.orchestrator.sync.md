---
description: "Consolidate feature state into specs/status.json for external tooling"
---

# Orchestrator Sync

Write a consolidated project status snapshot to `specs/status.json` — a machine-readable summary of every feature, phase, progress, and task state. Useful for CI pipelines, dashboards, and external coordination tools.

## User Input

$ARGUMENTS

You **MUST** consider the user input before proceeding (if not empty). Optional flags:
- `--output <path>` — write to a custom path instead of `specs/status.json`
- `--compact` — omit task-level details, keep only per-feature summaries
- `--dry-run` — print the output to stdout without writing to disk

## Prerequisites

- `specs/` directory must exist
- Write access to the output path (unless `--dry-run`)

## Execution

### Step 1: Run the analysis

Internally perform the same discovery and parsing as:
- `/speckit.orchestrator.status` — phase and progress per feature
- `/speckit.orchestrator.next` — task queue with states
- `/speckit.orchestrator.conflicts` — file overlap detection

### Step 2: Build the JSON document

Construct an object with this shape:

```json
{
  "schema_version": "1.0",
  "generated_at": "2026-04-17T10:30:00Z",
  "generated_by": "speckit.orchestrator.sync",
  "summary": {
    "total_features": 4,
    "in_spec": 1,
    "in_plan": 1,
    "in_tasks": 1,
    "in_implementation": 1,
    "complete": 0,
    "stalled": 1,
    "total_conflicts": 2
  },
  "features": [
    {
      "id": "001-user-auth",
      "phase": "implementation",
      "progress": 0.60,
      "total_tasks": 15,
      "completed_tasks": 9,
      "blocked_tasks": 1,
      "last_activity": "2026-04-17T08:00:00Z",
      "files_touched": [
        "src/auth/session.ts",
        "src/db/users.sql"
      ]
    }
  ],
  "conflicts": [
    {
      "file": "src/api/users.ts",
      "severity": "warn",
      "features": ["001-user-auth", "004-admin-dashboard"]
    }
  ]
}
```

### Step 3: Write or print

- If `--dry-run` — print the JSON to stdout with 2-space indentation
- Otherwise — write to `specs/status.json` (or the path given by `--output`)

### Step 4: Confirm

Print a short confirmation message:

```
Wrote specs/status.json
  4 features
  2 conflicts
  generated_at: 2026-04-17T10:30:00Z
```

## Output Format

**Compact mode** (`--compact`) drops the `files_touched` array and per-feature detail arrays — useful when the JSON is consumed by dashboards that only need phase and progress.

## Notes

- This is the **only** file the orchestrator writes. All other orchestrator commands are strictly read-only.
- Safe to run in CI — idempotent, deterministic output for unchanged specs
- Pair with a pre-commit hook or GitHub Action to keep `specs/status.json` in sync automatically
- Consumers: project dashboards, stand-up bots, merge-queue orchestration tools, Grafana panels

## Integration Example

CI snippet (GitHub Actions):

```yaml
- name: Update spec status
  run: specify run speckit.orchestrator.sync
- name: Commit status
  run: |
    git add specs/status.json
    git diff --staged --quiet || git commit -m "chore: update spec status"
```
