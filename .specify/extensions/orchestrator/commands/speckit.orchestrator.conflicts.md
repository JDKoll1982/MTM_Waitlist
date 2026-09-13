---
description: "Detect when parallel features touch the same files to prevent merge conflicts"
---

# Orchestrator Conflicts

Analyze all active feature specs and detect file overlaps — places where two or more parallel features plan to modify the same file. Catches merge conflicts before they happen.

## User Input

$ARGUMENTS

You **MUST** consider the user input before proceeding (if not empty). Optional flags:
- `--feature <id>` — limit analysis to conflicts involving a specific feature
- `--severity <level>` — filter to `warn` or `info` only
- `--paths-only` — output just the conflicting file paths (for piping to other tools)

## Prerequisites

- `specs/` directory must exist with at least two features
- Feature `tasks.md` files should mention file paths in task descriptions

## Execution

### Step 1: Extract file paths from every feature

For each feature in `specs/*/`:
- Read `tasks.md`
- For each task, extract file paths mentioned in the description
- File paths are identified by common patterns:
  - Relative paths with known extensions: `src/api/users.ts`, `db/schema.sql`
  - Backtick-wrapped paths: `` `src/components/Login.tsx` ``
  - Explicit "modify:", "create:", "edit:" prefixes

Build a map: `file_path → [list of (feature_id, task_id, action)]`

### Step 2: Classify each file reference

For each mentioned file, classify the action as:
- **additive** — creating a new file, or appending a new section (e.g., new table, new function)
- **modifying** — changing existing logic in a shared location
- **replacing** — rewriting or removing existing code

Heuristic cues:
- Words like "create", "add", "new" → additive
- Words like "update", "refactor", "fix", "change" → modifying
- Words like "remove", "replace", "rewrite" → replacing

### Step 3: Detect overlaps

For every file touched by 2+ features, classify severity:

| Severity | Condition |
|----------|-----------|
| **WARN** | 2+ features modifying or replacing the same file |
| **INFO** | 2+ features with additive-only changes (low conflict risk) |
| **OK** | Only 1 feature touches the file (not reported) |

### Step 4: Apply filters

- If `--feature <id>` — restrict output to conflicts where that feature is involved
- If `--severity <level>` — filter to specified severity

## Output

Print a conflict report grouped by severity:

```
File Conflict Analysis
======================

Features analyzed:  4
Files touched:      23
Conflicts found:    3

WARN: src/api/users.ts
  modified by: 001-user-auth (T012) — "refactor session validation"
  modified by: 004-admin-dashboard (T005) — "add admin-only user endpoints"
  Recommendation: merge 001-user-auth first, then rebase 004

WARN: src/db/migrations/current.sql
  replacing by: 002-payments (T014) — "rewrite migration runner"
  modifying by: 003-notifications (T003) — "add notifications table migration"
  Recommendation: coordinate ordering before either lands

INFO: src/db/schema.sql
  additive by: 002-payments (T011) — "add payments table"
  additive by: 001-user-auth (T008) — "add user sessions table"
  No conflict expected (both add new tables, no overlap)
```

If `--paths-only` is used, output just paths:

```
src/api/users.ts
src/db/migrations/current.sql
src/db/schema.sql
```

If no conflicts are found:

```
No file conflicts detected across 4 features.
```

## Notes

- This command is strictly read-only — it never modifies any file
- Classification is heuristic; use as a signal, not a guarantee
- Run before starting implementation to sequence feature merges correctly
- Pair with `/speckit.orchestrator.next` to pick unblocked, conflict-free tasks
