# Phase 1 Data Model — Waitlist Handler Fulfilment and Urgency Ordering

This feature adds **no** persisted entity and **no** schema artifact. Everything below either already
exists on the wire, or is in-memory presentation state derived from it. The sections are: §1 the stored
request as the actions read and write it, §2 the transient row state the card and detail page render,
§3 the gate rules that decide what a viewer is offered, §4 the urgency value the list is ordered by, §5
the audit view the history block renders, and §6 the invariants that hold across all of them.

---

## §1 Stored request

`WaitlistRequest` is read from `sp_waitlist_request_list` and written through
`sp_waitlist_request_status_update`. No stored column is added, renamed or re-typed — but the **read** grew one
projected field after the first build: `sp_waitlist_request_list` also returns `last_message_utc` now (T044),
so the list no longer has to infer "somebody spoke" from a timestamp that moves for every change.

| Field | Type | Role in this feature |
| --- | --- | --- |
| `Id` | `Guid` | Request identity; the DB `public_id`. The card's `SampleOrder.RequestId`. |
| `Building` | `string` | Scopes the list read. |
| `WorkCenter` / `WorkCenterName` | `string` | Destination press / work center shown to the handler. |
| `RequestType` / `Subtype` | `string` / `string?` | Selects the card template and the urgency max-allotted lookup. |
| `InputValue` | `string?` | The type-specific "request details" the handler reads. |
| `ActiveSetupJobId` | `string` | Work-order context. |
| `RequesterEmployeeNumber` | `string` | **Ownership key**: decides who may cancel, and which rows "My Requests" keeps. |
| `RequesterEmployeeName` | `string` | Shown as "Requested by". |
| `Status` | `string` | One of `Pending`, `Accepted`, `Completed`, `Canceled` — see §6 invariant 3. |
| `RequestedUtc` | `DateTimeOffset` | Waiting age, and the left end of the urgency window. |
| `TargetTimeUtc` | `DateTimeOffset?` | Due time; drives the card's "Remaining time" **and** the list order (§4). |
| `IsOverdue` | `bool` | Stored overdue flag, consistent with `TargetTimeUtc`. |
| `AssignedMaterialHandler` | `string?` | **Assignment key.** Set on accept, cleared on release; absent while available. |
| `CancellationReason` | `string?` | Set on cancel only; never touched by release. |
| `CanceledUtc`, `CanceledByEmployeeNumber` | `DateTimeOffset?`, `string?` | Set on cancel only; never touched by release. |
| `Note` | `string?` | The message the user sees on the request. |
| `LastMessageUtc` | `DateTimeOffset?` | **Projected, not a table column.** `MAX(occurred_utc)` over the request's `NoteUpdated` entries that carry an author, returned by `sp_waitlist_request_list`. Null when nobody has written on the request — which is what keeps a lifecycle change from looking like a message (§2, §5). |
| `AcceptedUtc`, `CompletedUtc`, `ReleasedUtc` | `DateTimeOffset?` | Stamped by the matching action. |

**Transitions this feature drives** (all pre-existing, all through
`sp_waitlist_request_status_update` with an audit row):

| Action | From | To | Also written |
| --- | --- | --- | --- |
| Accept | `Pending` | `Accepted` | `AssignedMaterialHandler` = the signed-in handler; `AcceptedUtc` |
| Complete | `Accepted` | `Completed` | `CompletedUtc` |
| Release | `Accepted` | `Pending` | `AssignedMaterialHandler` = `null`; `ReleasedUtc`; cancellation metadata untouched |
| Cancel (own) | `Pending` | `Canceled` | `CancellationReason`, `CanceledUtc`, `CanceledByEmployeeNumber` |
| Note | unchanged | unchanged | `Note` |

---

## §2 Row state rendered by the card (`SampleOrder`, in memory)

Added to the row model the view model already builds. Nothing here is persisted; it is recomputed every
load, so a stale row can never outlive the read that produced it.

| Member | Type | Meaning |
| --- | --- | --- |
| `Urgency` | `UrgencyState?` | The state the list was ordered by (§4). Null only when no due value could be derived. |
| `CanAccept` | `bool` | Reader may accept this row (§3). Drives the primary button's `Visibility` while the row is available — green, bare-check. |
| `CanCompleteOrRelease` | `bool` | Reader is the recorded assignee of a taken row (§3). Drives the **same** primary button once the row is claimed — accent colour, circled-check — so Accept and Complete are never both live and never look alike. |
| `CanCancelRequest` | `bool` | Reader raised this waiting row **or** is its assignee (§3). Drives Cancel, and is true whenever `CanCompleteOrRelease` is. |
| `AcceptCommand` / `CompleteCommand` / `CancelCommand` | `ICommand?` | The view model's commands, carried to the card through `Order`. Null when the flag is false. |
| `ReleaseCommand` | `ICommand?` | Present and tested, but deliberately **bound by nothing**: no card and no page draws a Release control (§C7). |
| `AcceptActionText`, `CompleteActionText`, `CancelActionText` | `string` | Localized automation names **and tooltips** for the icon buttons — accessibility without a literal in markup. |
| `ReleaseActionText` | `string` | Removed — no surface shows it. |
| `LastMessageUtc` | `DateTimeOffset?` | The queue row's `last_message_utc`, in UTC — the newest **human** entry on the request. The sign of its comparison with the viewer's seen marker is what drives `HasNewMessages` (§5). Replaced `updated_utc` after the first build: that value moves for every lifecycle change, so accepting or completing a job raised the new-message marker as though somebody had spoken (FR-028). |
| `ResolvedImagePath` | `string?` | The image the resolver returned, taken **only** when it is a real image — `RequestImagePathPolicy.IsUsableResolvedPath`. A placeholder returned because nothing is configured is refused and the row keeps its own `ImagePath` instead, so the tile cannot be replaced by the *no image available* card (FR-031). The refusal is logged. |
| `EffectiveImagePath` | `string` | What the tile actually renders: `ResolvedImagePath` when one was accepted, else the row's own `Assets/{ImagePath}`, else the placeholder. The placeholder is therefore the last resort rather than the first answer. |
| `HasNewMessages` | `bool` | True while the request changed after this viewer last read it. Drives the message marker's `Visibility`. |
| `NewMessageTooltip` | `string` | Localized tooltip for the marker, empty when there is nothing to say. |
| `Note`, `AssignedMaterialHandler` | `string?` | Carried from the store so the row reflects the request rather than a copy taken at submit time. |

**Why on the row, not on the card**: the card is instantiated inside the per-type line views, whose
`DataContext` is the row, so `Order` is the only channel that already crosses the template boundary
(research R1).

**Refusal feedback state** (list view model): `ActionRefusalMessage` (`string`, empty when there is
nothing to report). A refused action sets it; a successful one clears it. It is the surface FR-019
requires and is deliberately a message rather than a dialog, so a routine refusal never blocks the list
— **except for a lost claim**, which is also shown as an acknowledgement-required warning because the
handler has to be told plainly that another handler took the request and to choose a different one
(FR-024). The message stays set after the warning is dismissed, so the refusal is still visible on the
list.

---

## §3 Gate rules

Every flag is a *view* of a rule the service enforces independently (FR-018). The screen never widens
what the service would allow.

| Viewer | Request state | Offered |
| --- | --- | --- |
| Handler-or-above | Available (`Pending`) | **Accept** (+ **Cancel** when the viewer raised it) |
| Handler-or-above | Taken (`Accepted`) by this viewer | **Complete**, **Cancel** |
| Handler-or-above | Taken by someone else | nothing |
| Handler-or-above | `Completed` / `Canceled` | nothing |
| Not handler-or-above | any | nothing |
| The requester (any role) | own request, `Pending` | **Cancel** (**Accept** too when also a handler) |
| The requester (any role) | own request, `Accepted` by someone else | nothing — the service would refuse |
| Anyone | another person's request, `Pending` | nothing |

Two rules hold across the whole table and are asserted directly: **Accept and Complete are never both
offered** (they are one button in two states), and **Cancel is offered whenever Complete is** — so a handler
who has claimed a request can refuse it as readily as they can finish it.

**Handler-or-above** = the role is one of `Material Handler`, `Production`, `Production Lead`, `Setup`,
`Setup Lead`, `Plant Manager`, `Admin`, `Developer`, compared case-insensitively. This is the set the
Settings screens already gate on for the broadest management panel, so the two screens agree on who a
handler is.

**Action outcomes** — each command maps the service's answer to exactly one of:

| Outcome | Source | Effect |
| --- | --- | --- |
| Applied | service returned an updated request | clear the refusal message; the existing `RequestsChanged` refresh reloads the list |
| Refused | service returned `null` / a non-success cancel result | keep the row as it is; set the refusal message in plain language |
| Claim lost to a concurrent handler | accept returned `null` and the request is still on the list | set the refusal message **and** show an acknowledgement-required warning naming the request as already taken and directing the handler to choose another (FR-024) |
| Claim lost because the request left the list | accept returned `null` and the store no longer knows the request | same warning, with copy that says the request is gone rather than blaming another handler |
| Prompt dismissed | the prompt returned `false` / `null` | nothing happens; no service call, no message |

---

## §4 Urgency (derived, not stored)

`UrgencyState` — `CreatedUtc`, `DueUtc`, `Remaining`, `IsOverdue` — already exists in
`MTM_Waitlist.Module_Core.Models` and is produced by `UrgencyCalculator.Compute`.

**Derivation used here, in order:**

1. When the request carries `TargetTimeUtc`, the due value is that timestamp, so the order and the
   card's "Remaining time" come from the same number and cannot disagree.
2. Otherwise the due value is `RequestedUtc + max-allotted`, where max-allotted comes from
   `IUrgencyDeadlineService.GetMaxAllottedAsync(Subtype)` and defaults to 30 minutes — the service's own
   documented default — when the sub-type has no override or the service is absent.

**Ordering**: `UrgencyCalculator.OrderMostUrgentFirst(rows, row => row.Urgency)` — overdue rows first,
then least remaining time first. A row with no derivable urgency sorts last.

**Cost**: max-allotted is resolved once per distinct sub-type per load and memoised, so a list of N rows
costs at most one lookup per distinct sub-type.

---

## §5 Audit view (read-only)

`GetAuditTrail(requestId)` returns `WaitlistRequestAuditEntry` — `RequestId`, `FromStatus`, `ToStatus`,
`EventType`, `OccurredUtc`, `EmployeeNumber`, `EmployeeName`, `Details` — ordered by time.

**The history is read from the store, not from memory.** `LoadAuditTrailAsync(requestId, ct)` calls
`sp_waitlist_request_audit_list` and **merges** the rows it returns with whatever this session already
recorded, deduped by `occurredUtc | eventType | employeeNumber | details`. The merge is what makes calling it
on every refresh safe: the two sources overlap by construction, and an overlapping entry is added exactly
once. A **failed read returns the in-session trail unchanged** — a read failure must never be rendered as
"this request has no history". The synchronous `GetAuditTrail` remains as the in-session fast path and as the
fallback a failed read falls back to.

The detail page renders one history row per entry in three columns: local occurrence, **the sender's full name**
(`System` where the entry has no person), and the **message text** — the internal event type is not shown to the
user (FR-013, revised after the first build: the row used to render the event type, which told the user nothing
and printed `NoteUpdated` instead of the note). The block renders for an
empty history too, saying so — `IsHistoryEmpty` / `HistoryEmptyText` — because "nothing has happened yet" and
"the history could not be read" must not look the same.

**Merging is idempotent by construction.** The read returns the store's rows and the session's, and the same
entry is in both — so the merge key is content, not identity, and it compares `occurredUtc` **truncated to
seconds** because `occurred_utc` is a MySQL `datetime` (whole seconds) while the session stamps sub-second
precision. Comparing full precision made every entry appear once from the store and once from memory (T043).

**New-message marker.** `IWaitlistMessageSeenStore` records, per request, the newest entry the viewer has
seen. `HasNewMessages` is true while the request's `LastMessageUtc` is newer than that marker; a request with no
marker at all, or with no human message at all, is not flagged. Reading the request writes the marker as the
**newest entry's** `OccurredUtc`, not the current clock, so an entry that lands while the page is open re-flags
the card instead of being swallowed by a page that was already looking at it. The viewer's own action also marks
seen, so they are not pinged by their own change. Both timestamps are UTC and both reach the client from the
same store read, so they compare like with like.

Events the actions produce: `Created`, `Accepted`, `Completed`, `Released`, `Canceled`, `NoteUpdated`,
`StatusChanged`. `Released` is what distinguishes a release from a cancellation (FR-007).

---

## §6 Invariants

1. **Nothing is offered that cannot be done.** A button is drawn only while its gate flag is true, and
   that flag is computed from the policy the service also uses. No disabled action buttons.
2. **The screen is never the authority.** Every command calls the service and the service re-checks the
   role, the ownership and the state. A row built from stale data yields a refusal, never a widened
   right.
3. **Stored status vocabulary is fixed**: `Pending`, `Accepted`, `Completed`, `Canceled`. The friendly
   labels the card shows (`Waiting`, `In Progress`, `Done`, `Cancelled`) are display text and are never
   compared against, written, or sent to the database. "Released" is an audit event, not a status.
4. **The card anatomy does not move.** The new buttons occupy the existing vertically-centred action
   area above the compact status pill, at the 44×44 icon size the removed pair used. The 96×96 image,
   the title row, the four metadata rows, the per-type detail grid and the pill's height are unchanged.
5. **A cancelled request is never deleted and never resurrected.** Cancel writes `Canceled` with its
   reason and actor and removes the request from the active list; release writes `Pending` and clears
   the assignee, and neither touches the other's fields.
6. **No substitute value.** A row shows only what the request carries. Where a handler-needed value is
   absent the row omits it rather than inventing one (FR-014), keeping the existing no-fabrication
   guards green.
7. **The list order and the cards agree.** Both come from the same due value (§4), so a row's displayed
   urgency can never contradict its position.
