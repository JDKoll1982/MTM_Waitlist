# Medium — Waitlist — The request history shows only events from the current session

> **Naming note (2026-09-20).** The **Planned fix** row below names specs by their **template** number, from
> `WeekendProject/SpecTemplates/` -- the seeds written before the specs existed -- so `Spec 01-...` is *not*
> `specs/001-...`. Those templates have since shipped under different numbers: `01-truthful-data-and-controls`
> is now **`specs/002-truthful-data-and-controls`** (shipped), `02-handler-fulfilment-and-urgency` is
> **`specs/003-waitlist-handler-fulfilment`** (shipped), and `03-unified-card-and-taxonomy` is
> **`specs/004-unified-card-item-picker`** (in flight). Templates **04-08** have no spec yet. The single copy
> of this map is `WeekendProject/SpecTemplates/00-INDEX.md`. The **Status** row above already uses the real
> `specs/00N` names -- read that one for what actually happened.


| Field | Value |
| --- | --- |
| **Criticality** | Medium |
| **Feature area** | Waitlist → request detail page → Request history block (and the note it is meant to prove) |
| **Type** | Surface that renders absence where data exists; documented feature only half built |
| **Found** | 2026-09-12, working tree at `1b00f3e` + `specs/003-waitlist-handler-fulfilment` |
| **Status** | **FIXED — 2026-09-13.** **Found by:** using the build shipped by `specs/003-waitlist-handler-fulfilment`. **Fixed by:** `specs/003-waitlist-handler-fulfilment` task **T037**, which is where the live validation lives. |
| **Planned fix** | Add the read path: `sp_waitlist_request_audit_list` (paired `create.sql` / `rollback.sql`, `AllSPs.sql` synced, `update_table_descriptions.sql` if the table description changes), read it through `IWaitlistRequestService.GetAuditTrail`/a new async reader, and have the detail page load it. `sql_waitlist_request_audit` already stores every entry — `sp_waitlist_request_audit_insert` writes them and the writes are proven — so this is a read-only addition, not a schema change. |
| **Files** | `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` (`_auditTrail` is an in-memory `ConcurrentDictionary`, never loaded from the store), `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` (`LoadHistory`), `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` (history block), `Database/StoredProcedures/` (no `sp_waitlist_request_audit_list` exists) |

---

## 1. Summary

The request history block on the request detail page renders `IWaitlistRequestService.GetAuditTrail`, and
that method reads an in-memory dictionary:

```csharp
private readonly ConcurrentDictionary<Guid, List<WaitlistRequestAuditEntry>> _auditTrail = new();

public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId)
    => _auditTrail.TryGetValue(requestId, out var auditEntries)
        ? auditEntries.OrderBy(item => item.OccurredUtc).ToArray()
        : Array.Empty<WaitlistRequestAuditEntry>();
```

Entries are added when an action happens in this session — `Created` on submit, `Accepted`, `Completed`,
`Released`, `Canceled`, `NoteUpdated` — and each one is **also persisted** to the store through
`sp_waitlist_request_audit_insert`. Nothing ever reads them back. There is no
`sp_waitlist_request_audit_list`; the only audit procedure that exists is the insert.

So `RefreshFromDatabaseAsync` rebuilds the request set from `sp_waitlist_request_list` and leaves
`_auditTrail` untouched, which means:

- A request the user has not acted on **in this session** has no history to show, even though the store
  holds a full record of it.
- Restarting the app empties every history it was showing.

## 2. Impact

- The history block **hides itself when it has no rows** (`HasHistory`), so the page does not say
  "no history recorded" — it silently shows nothing. A reviewer reading the page cannot tell a request whose
  history was never loaded from one that genuinely has none. That is the same "absence rendered as nothing"
  failure the card and detail work in `specs/002-truthful-data-and-controls` set out to remove.
- Two shipped claims are currently only conditionally true: `FR-013` ("its addition MUST appear in the
  request's history") and `FR-017` ("MUST record who performed it and when in the request's audit history")
  hold for the session that performed the action and for nobody afterwards. The `[Unreleased]` changelog
  entry for this feature says the page "now shows its full history" — it does not; that wording has been
  corrected alongside this file.
- The note the note-editor writes is visible (it is stored on the request and read back by
  `sp_waitlist_request_list`), so the note itself is not lost — only its history entry is.

## 3. Evidence

Run against the built app on 2026-09-13:

1. Opened a request that had been accepted, completed and noted **in an earlier session**. The detail page
   rendered the request, its sections, its action bar and its note — and **no history block at all**.
2. Clicked `Complete request` on that request. The history block appeared immediately, reading
   `9/12/2026 11:04 PM | Completed | John Koll` — the entry the *current* session had just created.
3. The store holds the earlier entries: the same session's log shows
   `sp_waitlist_request_audit_insert` completing for each of them
   (`Audit entry persisted for request 'f0000000-0011-...'. EventType='Completed'`), for requests whose
   history the page could not show.

## 4. Required fix

1. **Database (Database Engineer).** Add `Database/StoredProcedures/sp_waitlist_request_audit_list/` with
   `create.sql` and `rollback.sql`, per `Database/Database-Ruleset.md` and the
   `database-schema-rules` instruction: read the entries for one request public id, ordered by occurrence.
   Add it to `Database/StoredProcedures/AllSPs.sql`. Validate against the live database.
2. **Backend (Backend Engineer).** Add an async read on `IWaitlistRequestService`
   (e.g. `LoadAuditTrailAsync(Guid requestId, CancellationToken)`), called by
   `RefreshFromDatabaseAsync` or by the detail view model on load, that fills `_auditTrail` from the
   procedure. Keep the synchronous `GetAuditTrail` for the in-session fast path. This is a read through the
   non-query seam's sibling — no inline statement text (constitution III).
3. **Frontend (Frontend Engineer).** Have the detail page load the history for the request on screen and
   render it; a successful empty read should say so rather than hiding the block, so "no history" is
   distinguishable from "not loaded".
4. **QA.** A test that a request whose history came from the store renders its rows, and a test that the
   empty case is distinguishable from the unloaded case. The live-DB test is opt-in and must be recorded as
   environment-gated if it cannot run here.

## 5. Related

- `specs/003-waitlist-handler-fulfilment/` — the feature that added the history block and this note.
- `Database/StoredProcedures/sp_waitlist_request_audit_insert/` — the write half, which already works.
- `specs/002-truthful-data-and-controls/` — the "no absence rendered as nothing" rule this file reinstates.

---

## 6. Fix — what shipped

The fix is the four parts section 4 asked for, and it is verified against the database and the running app.

| Part | What landed |
| --- | --- |
| Database | `Database/StoredProcedures/sp_waitlist_request_audit_list/{create,rollback}.sql`, block added to `AllSPs.sql`. The procedure takes one request public id and returns the entries **oldest first** (`ORDER BY occurred_utc ASC, id ASC`), so the page can render them in the order they happened without re-sorting. |
| Backend | `IWaitlistRequestService.LoadAuditTrailAsync(Guid, CancellationToken)` reads the procedure and **merges** its rows with whatever this session already recorded, deduped by `occurredUtc \| eventType \| employeeNumber \| details`. The merge is what makes it safe to call on every refresh: the two sources overlap, and an overlapping entry is added once. A **failed read keeps the in-session trail** instead of replacing it, so a read failure is not rendered as "this request has no history". |
| Frontend | The page reads the history on load and every 30 seconds while it is open, and the block now renders for a request with **no** history too, saying so — `IsHistoryEmpty` / `HistoryEmptyText` — so "nothing has happened yet" is distinguishable from "not loaded". |
| QA | `RequestPage_ReadsTheHistoryFromTheStore_NotJustThisSession` (a stub returns an entry this session never created, and the page must render it) and `RequestPage_WithNoHistory_SaysSoRatherThanHiding`. The live-database test is **not** in the suite — it is the manual evidence below. |

**Evidence (2026-09-13, local `mtm_waitlist` on 127.0.0.1:3306, MySQL 9.6.0).**

1. `create.sql` applied; `SHOW PROCEDURE STATUS` reports `sp_waitlist_request_audit_list`.
2. `CALL sp_waitlist_request_audit_list('f0000000-0010-4000-8000-000000000010')` returned the request's three
   entries oldest-first — `Created` (18:40:29), `Accepted` (18:45:29), `Completed` (03:54:58) — and an unknown
   id returned an empty set rather than an error.
3. The app writes and reads the same column names the procedure returns (`event_type`, `from_status`,
   `to_status`, `actor_employee_number`, `actor_employee_name`, `details`, `occurred_utc`), and the audit
   timestamps and the queue's `updated_utc` agree to the second on the rows the app wrote — so both are UTC and
   the new-message comparison compares like with like.
4. Opening a request in the built app logged `Audit trail loaded for request 'f0000000-0001-…'. Entries=1.`,
   and the page rendered that entry with its **detail text** (`Request submitted for press 100-03.`) — the defect
   this file reported alongside T034.

**One honest caveat, unchanged by this fix.** The history is read for the request the page is showing. There is
no cross-request or per-actor history view, and the "new message" marker compares the queue row's
`updated_utc` against the newest audit entry the viewer has seen — so a write that bumps `updated_utc` without
adding an audit entry would leave the marker up until the request is next read.
