# Spec seed 05 — Request administration: cancellations monitor and retention

| Field | Value |
| --- | --- |
| **Suggested feature name** | `request-administration-retention` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §7 — from `PromptFiles/11-0%-Phase3-admin.md` (8 carry-forward after the obsolete boxes were removed) + `prompt.md` Tasks 22–23 |
| **Carry-forward boxes** | **8** |
| **Depends on** | template 01 (truthful surfaces). Independent of templates 02–04, but it shares the role vocabulary template 06 formalises. |
| **Defect files it closes** | none |
| **Validation** | role-gate tests, retention-window filtering tests, archival tests; build clean; suite `Failed: 0` |

---

## 1. Why this spec exists

The application keeps cancelled requests forever and never deletes one, and it has a configured
retention window for resolved requests — and nothing acts on either fact. A developer or administrator
cannot review who cancelled what and why, and the active waitlist keeps carrying requests that were
finished long enough ago to be archived. Both are small, independent, and self-contained.

## 2. Input to paste into `/speckit.specify`

```text
Add the administrative half of the request lifecycle to MTM Waitlist: a way to review cancellations,
and a retention window that keeps resolved requests out of the active list without ever deleting them.
Today a cancelled request is retained in full — who cancelled it, when, the request type and subtype,
and the reason — but nothing in the application lets an administrator see that record, and the
application already carries a configured resolved-retention window (default ninety days) that nothing
honours, so completed and cancelled requests stay mixed in with live work indefinitely. This feature
adds a role-gated monitoring view, for the developer and administrator level, listing cancelled
requests with who cancelled them, when, the request type and subtype, and the reason, read from the
retained records, which must never be purged. It also makes the retention window real: a request that
reached a resolved state — completed or cancelled — more than the configured number of days ago stops
appearing on the active waitlist, while remaining fully preserved for the monitoring view and for
analytics, and a housekeeping routine applies the window on a schedule. The change must add the
active-list filtering rather than deleting anything, must localise every new string, must gate the new
screen by role on both the screen and the data path, and must not alter how a request is created,
accepted, completed, released or cancelled.
```

## 3. Carried-forward requirements (from the source; do not weaken)

**Cancellations monitor (0.1)**

- A role-gated (developer/admin-level) view monitoring cancelled requests — **who** cancelled, **when**,
  the request type/subtype, and the **reason** — read from retained cancelled records that are never
  purged.
- Role-gate the monitor; register the page and view model through `PageService`/DI and add them to
  shell navigation.
- QA: tests that the monitor lists retained cancellations with the correct fields and enforces the role
  gate.

**Retention and archival (0.2)**

- Honour `waitlist.resolved_retention_days` (**default 90**): resolved (`Done`/`Cancelled`) requests
  older than the window are **hidden from the active Waitlist list but preserved** — never deleted —
  for analytics and administration.
- Add the active-list filtering so aged resolved requests do not appear, plus a housekeeping/archival
  routine; localise the UI strings.
- QA: tests for retention-window filtering and archival.

**Carried constraint — do not weaken.** "Never deleted" is the whole point: the filtered-out records
must remain readable by the monitor (0.1) and by the analytics workstream. Any archival routine must
move or mark, not remove.

## 4. Explicitly out of scope — do not resurrect

- **The mock-mode admin boxes** in the source — obsolete (`OPEN-WORK-NEXT-SPEC.md` §3).
- **Analytics on the retained data** — template 04 owns the reporting; this spec only preserves and
  filters.
- **User management** — template 06, even though both live under Settings → Administration.
- **Changing the lifecycle itself** — transitions, audit and cancellation rules are complete; template
  02 adds the handler's side of them.

## 5. Defects this spec closes

None. Its defect-adjacent duty is negative: it must not introduce a surface that promises an action it
cannot perform (template 01's rule), and it must leave the retained records untouched by any purge.

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. Monitor tests: a retained cancellation lists the who/when/type/subtype/reason fields; every role
   outside the allowed set is refused.
3. Retention tests: a resolved request inside the window stays on the active list; one outside the
   window leaves it; both remain retrievable by the monitor afterwards.
4. Confirm the retention value is read from configuration rather than hard-coded, and that the default
   is 90 days when unset.
5. Any `Database/**` change ships `create.sql` + `rollback.sql` together
   (`database-schema-rules.instructions.md`).

## 7. Open decisions to resolve before `/speckit.specify`

1. **Which roles** exactly may see the cancellations monitor, and whether it is a Settings category or
   its own navigation destination.
2. **"Archived" means what, mechanically** — a flag, a separate table, or only a filtered query? The
   retention requirement only demands that a record is hidden from the active list and preserved, so
   the cheapest honest option is likely a query filter plus the housekeeping routine.
3. **Housekeeping cadence** — when the routine runs (start-up, a timer, or alongside an existing
   scheduled path).
4. **Whether the window is per-building or plant-wide** — the source does not say.
