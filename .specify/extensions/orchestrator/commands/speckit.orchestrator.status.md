---
description: "Multi-feature lifecycle dashboard — show every feature and its SDD phase"
---

# Orchestrator Status

Display a dashboard view of every feature in the project along with its current Spec-Driven Development (SDD) lifecycle phase, progress, and health.

## User Input

$ARGUMENTS

You **MUST** consider the user input before proceeding (if not empty). Optional flags:
- `--phase <name>` — filter to features in a specific phase (spec, plan, tasks, implementation)
- `--stalled` — show only features with no recent activity
- `--verbose` — include per-task progress details

## Prerequisites

- Working directory must contain a `specs/` folder
- Each feature must follow the standard layout: `specs/<id>-<name>/`

## Execution

### Step 1: Discover features

List all directories under `specs/`:

```bash
ls -d specs/*/
```

Each directory represents one feature.

### Step 2: Determine phase for each feature

For each feature directory, check which files exist to determine the current phase:

| Phase | Indicator |
|-------|-----------|
| `spec` | Only `spec.md` exists |
| `plan` | `spec.md` and `plan.md` exist |
| `tasks` | `spec.md`, `plan.md`, and `tasks.md` exist |
| `implementation` | `tasks.md` exists AND at least one task is marked complete |
| `complete` | All tasks in `tasks.md` are marked complete |

### Step 3: Calculate progress

For features in `implementation` or `complete`:

- Count total tasks in `tasks.md` (lines matching `^- \[[ x]\]`)
- Count completed tasks (lines matching `^- \[x\]`)
- Progress = completed / total

### Step 4: Detect stalled features

A feature is **stalled** if:
- Its most recent file modification is older than 5 days, AND
- It is not in phase `complete`

Use file mtime to determine last activity.

### Step 5: Apply filters

If `--phase <name>` is provided, show only features matching that phase.
If `--stalled` is provided, show only stalled features.

## Output

Print a structured dashboard:

```
Feature Lifecycle Overview
==========================

ID                       Phase             Progress    Last Activity
------------------------ ----------------- ----------- ---------------
001-user-auth            implementation    60% (9/15)  2 hours ago
002-payments             tasks             -           1 day ago
003-notifications        plan              -           6 days ago (STALLED)
004-admin-dashboard      spec              -           3 hours ago

Summary:
  Total features:      4
  In implementation:   1
  Ready to implement:  1
  In planning:         1
  In specification:    1
  Stalled:             1
```

If `--verbose` is used, expand each feature to list incomplete tasks:

```
001-user-auth [implementation, 60%]
  - [x] T001 Set up auth schema
  - [x] T002 Add password hashing
  - [ ] T010 Add OAuth provider support  (unblocked)
  - [ ] T011 Add session refresh logic   (blocked by T010)
```

## Notes

- This command is strictly read-only — it never modifies any file
- If no `specs/` directory exists, output a clear message and exit cleanly
- Phase detection is heuristic; a user note in a spec can override (future enhancement)
