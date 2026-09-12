# `specs/001/tasks.md` — phase and task-ID allocation across workstations

**Problem observed 2026-09-12.** `specs/001-module-mock-visual-fallback/tasks.md` is appended to by
`/speckit.implement` at the end of a session. Two workstations ran that flow the same day and independently
allocated the *same* identifiers:

- two `## Phase 24` headings (one for T144, one for the T147/T148(c) host run), and
- overlapping task IDs — `T150`/`T151` claimed twice (shape-4 cache work vs the host run's items).

Neither run was wrong in isolation; the collision only appears after a `git pull`.

## Rule for future appends

Before adding a section or a new task ID:

1. `git fetch --all` and check the working tree is at the tip you think it is.
2. Re-read the **end** of `tasks.md` for the highest `## Phase NN` **and** the highest `T1NN` actually
   defined — do not trust the last number you remember from the session start.
3. Grep for duplicate *definitions* (not mentions), which is the check that actually catches this:
   `Select-String -Path specs/001-module-mock-visual-fallback/tasks.md -Pattern '^\s*- \[[ xX]\] (T1[0-9][0-9])'`
   then group by ID and look for a count above 1.
4. Update the cross-references in `OPEN-TASKS.md` too — its box totals, its ID/phase pointers, and the
   §5 "Recommended order" list.

## Convention when a collision is found

Renumber the **later-arriving** section, keep the heading text but change the numbers, and leave an
explicit renumbering note in the section so the change is traceable (see `tasks.md` Phase 25). Do not
re-scope work when renumbering, and re-count the `OPEN-TASKS.md` box totals from the file afterwards —
they are exact counts of `- [ ]` / `- [x]` markers, not estimates.
