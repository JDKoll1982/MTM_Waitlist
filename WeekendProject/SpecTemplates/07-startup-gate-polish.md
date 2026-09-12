# Spec seed 07 — Startup gate polish

| Field | Value |
| --- | --- |
| **Suggested feature name** | `startup-gate-polish` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §9 — the **only surviving items** of `PromptFiles/13-63%-Developer-UI.md` (the rest of that workstream is cancelled) |
| **Carry-forward boxes** | **2** (both are `+ [ ]` boxes — a `- [ ]`-only sweep reports this file as zero; count both markers) |
| **Depends on** | template 01 (which stops the app displaying what it cannot support) |
| **Defect files it closes** | none directly |
| **Validation** | the hard no-start-without-`mtm_waitlist` gate verified with the user-facing message; build clean; suite `Failed: 0` |

---

## 1. Why this spec exists

The application cannot start without its own database, and when it cannot reach it the user sees a
message that has grown by accretion: the splash text, the recovery text and the buttons are spread
across two near-duplicate splash views, and the wording has never been specified anywhere. `specs/001`
FR-021 covers the per-screen unavailable state for a store that fails mid-session; the *startup* gate
copy is specified nowhere.

It is 2 boxes and independent, which is exactly why it should not be bundled into a larger spec.

## 2. Input to paste into `/speckit.specify`

```text
Polish the startup gate: the experience when MTM Waitlist cannot reach its own database at launch.
The application deliberately refuses to start without that database, and today the user is shown a
launching message and then a recovery message assembled from more than one splash surface, with
wording that has never been reviewed and buttons whose behaviour is not specified. This feature makes
that experience a single, deliberate one: a non-technical message that says the application cannot
reach the MTM Waitlist server, that the user's work is not affected, and what happens if they try
again; a retry action that re-attempts the connection in place without restarting the application; a
close action that exits cleanly; and, when a saved connection setting is the reason the application
cannot connect, an action that restores the default connection so a user on the shop floor can recover
without help. The message must not expose server names, database names, ports, credentials, stack
traces or any other technical detail, must be fully localised, must be readable at a glance, and must
be the only startup surface the user sees when the gate closes, so the duplicate splash views are
reconciled into one. The gate itself must not weaken: the application still does not start without its
database, and the retry path must not leave two windows, a hidden window, or a process that outlives
the failed attempt.
```

## 3. Carried-forward requirements (from the source; do not weaken)

- **§9.1 Splash messaging** — confirm and polish the splash messaging: a clear, non-technical message
  ("Cannot connect to the MTM Waitlist server…") with a **Retry** option and a **Cancel** that exits.
  *`specs/001` FR-021 covers the per-screen internal-store unavailable state; the splash/startup gate
  copy is specified nowhere else.*
- **§9.2 Verification** — verify the hard no-start-without-`mtm_waitlist` gate with the user-facing
  message; full suite green.

**Carried constraint — do not weaken.** The gate is deliberate. Do not add a "continue anyway" path,
do not make the connection check optional, and do not turn the message into a dismissible banner: a
request that cannot reach its own store must not be accepted.

## 4. Explicitly out of scope — do not resurrect

- **The whole catalog-administration workstream** that the source file's other boxes belonged to — the
  Developer Settings page, the request-type master list, the type edit view, the 4-step guided wizard
  modal, the per-control card and the grid editor. **Cancelled by owner decision (2026-09-11)**:
  nothing is deferred or rescoped, and the backend has already been removed.
- **A generic error-handling framework** for every failure in the application — this is the startup
  gate only. The per-screen store-unavailable state already exists and is covered by `specs/001`.
- **Login or password-change flow changes** — `specs/001` T164 delivered the pre-sign-in password
  panel; leave it alone.
- **The connection-string fallback** (`MySqlHostFallback`) — delivered and proven; do not re-design it.

## 5. Defects this spec closes

None. Its obligation is negative: it must not introduce a startup surface that promises an action it
cannot perform, and it must not expose a technical detail in user-facing copy (template 01's rule).

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. **The gate, exercised for real**: point the application at an unreachable database, launch it, and
   confirm (a) the reviewed message appears and nothing else does, (b) Retry re-attempts in place,
   (c) Close exits with no orphan process, and (d) the message contains no server name, database name,
   port, credential, or stack trace.
3. Confirm a reachable database still starts the application normally, including the first-load
   computer gate and the pre-sign-in password panel.
4. UI automation per `.github/instructions/winui3-ui-automation.instructions.md`: the documented recipe
   for driving the splash and reading its text is exactly this case — use it, and remember the message
   text is the primary signal while the buttons need a separate `ControlType.Button` query.
5. Confirm one splash surface remains: no duplicate window, no hidden window, no second process.

## 7. Open decisions to resolve before `/speckit.specify`

1. **The final wording** — so it can be reviewed by someone who is not a developer, and localized once.
2. **"Restore Defaults"**: keep it (it exists in the current splash), rename it in plain language, or
   drop it? It resets a bad saved connection, which is genuinely useful on the floor.
3. **Whether the two splash views are merged or one is deleted** — and which one survives.
4. **What the user is told about their unsent work**, given the application has not started yet.
