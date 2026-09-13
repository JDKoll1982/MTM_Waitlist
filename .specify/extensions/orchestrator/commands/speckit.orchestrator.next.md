---
description: "Query and prioritize tasks across all features by tag, layer, or blocker status"
---

# Orchestrator Next

Find and prioritize the next task to work on across all active features. Filter by tag, layer, or blocker status to coordinate work across parallel specs.

## User Input

$ARGUMENTS

You **MUST** consider the user input before proceeding (if not empty). Optional flags:
- `--layer <name>` — filter tasks tagged with a specific layer (e.g., `database`, `api`, `ui`)
- `--tag <name>` — filter tasks tagged with a specific keyword
- `--unblocked` — show only tasks with no pending dependencies
- `--feature <id>` — limit to a specific feature
- `--top <n>` — show only the top N recommendations (default 10)

## Prerequisites

- `specs/` directory must exist with at least one feature
- Each feature directory may optionally contain `tasks.md`

## Execution

### Step 1: Collect all tasks

For each feature in `specs/*/`:
- Read `tasks.md` if it exists
- Parse every line matching `^- \[[ x]\]` as a task entry
- Extract: task ID (e.g., `T007`), description, completion status, and any inline metadata

### Step 2: Parse task metadata

Tasks may include inline tags in their descriptions. Look for patterns like:

```markdown
- [ ] T014 [database] Create payments schema (blocked by T013)
- [ ] T015 [api, security] Add auth middleware
```

Extract:
- **Layer/tag markers** — text inside square brackets (e.g., `[database]`, `[api]`)
- **Blocker references** — `blocked by T###` patterns
- **Priority hints** — optional `!high`, `!medium`, `!low` markers

### Step 3: Determine task state

A task is:
- **Completed** if marked `[x]`
- **Blocked** if it references a blocker that is not yet completed
- **Unblocked** otherwise

### Step 4: Apply filters

1. If `--feature <id>` — restrict to that feature only
2. If `--layer <name>` or `--tag <name>` — match against extracted tags
3. If `--unblocked` — exclude blocked tasks
4. Always exclude completed tasks

### Step 5: Rank results

Sort remaining tasks by:
1. Explicit priority (high > medium > low)
2. Unblocked status (unblocked first)
3. Feature phase (implementation-phase features first, since they're actively moving)
4. Feature ID (ascending, as proxy for age)

Limit output to `--top N` (default 10).

## Output

Print a ranked task list:

```
Cross-Feature Task Queue
========================

Filters: --layer database --unblocked
Showing top 5 of 12 matching tasks

Feature             Task   Description                      State
------------------- ------ -------------------------------- -----------
002-payments        T014   Create payments schema           unblocked
001-user-auth       T023   Add user session indexes         unblocked
004-admin-dashboard T008   Add audit log table              unblocked
001-user-auth       T025   Add password reset token table   unblocked
002-payments        T019   Add transaction history index    unblocked

Summary:
  Total matching:    12
  Unblocked:         8
  Blocked:           4
```

If no tasks match the filters:

```
No tasks match the current filters.
Try removing --unblocked or broadening --layer/--tag.
```

## Notes

- This command is strictly read-only — it does not modify any task files
- Works across all features simultaneously; no need to switch branches
- Pair with `/speckit.orchestrator.conflicts` before starting work to avoid file overlaps
