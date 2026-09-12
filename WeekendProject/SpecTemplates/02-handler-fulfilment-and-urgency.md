# Spec seed 02 — Waitlist handler fulfilment and urgency ordering

| Field | Value |
| --- | --- |
| **Suggested feature name** | `waitlist-handler-fulfilment` |
| **Source** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §4 — from `PromptFiles/07-8%-Phase2-fulfill.md` (9 carry-forward) and `PromptFiles/08-70%-Phase2-urgency.md` (2 carry-forward), plus `prompt.md` Tasks 16–18 |
| **Carry-forward boxes** | **11** (9 + 2) |
| **Depends on** | template 01 (which removes the two inert buttons this spec then wires); **not** on template 03 |
| **Defect files it closes** | completes `defects/High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` |
| **Validation** | the source's own QA boxes, plus build clean and full suite `Failed: 0` |

---

## 1. Why this spec exists

The waitlist can show a request but nobody can *work* it. The service layer already supports every
transition, and a purpose-built action policy already decides who may do what — but no screen offers
the actions, and the two buttons that are drawn on every card do nothing. This is the smallest,
already-unblocked, highest-value workstream in the backlog: it depends only on Phase-1 work that is
complete.

## 2. Input to paste into `/speckit.specify`

```text
Make the waitlist workable by a handler. Today a request can be seen but not acted on: the service
layer already supports accepting, completing, releasing and cancelling a request and the app already
records status history, but no screen offers those actions, and the two buttons drawn on every card
do nothing at all. This feature adds the handler's side of the request lifecycle to the application.
A material handler, or anyone at that role or above, must be able to accept an unaccepted request,
which claims it for that person and moves it to In Progress while leaving it visible on the shared
list; the Accept affordance must then disappear for everyone else. Only the handler who accepted the
request may Complete it, which moves it to Done, or Release it, which returns it to the open list as
available rather than cancelling it. No handler actions are offered to a viewer who is not a handler,
and none are offered to a handler on a request that belongs to someone else. The requester keeps the
ability to cancel their own request while it is still waiting, with a reason and a confirmation, and
the cancellation is recorded rather than deleted. Any handler may add or edit a short note on a
request, visible on the request detail and in the request's history. The request card must surface the
data a handler needs to do the job — the coil or part and the quantity requested, where that stock
sits, the destination press or work center, who asked for it, and how urgent it is — without adding
new fields to the card's fixed layout. The available-request list must be ordered most-urgent-first,
using the due, remaining and overdue values the app already computes, so the oldest and most overdue
work is at the top. Every action must write through the existing stored procedures, must record who
did it and when in the request's audit history, must obey the same role and ownership rules the
service already enforces rather than trusting the screen, and must report a refusal in plain language
instead of failing silently. The accepted card anatomy is frozen and must not be regressed: the fixed
square request image and badge, the title on its own row, the four metadata rows, the centred action
buttons above a compact status badge, and the type-specific detail block all stay as they are.
```

## 3. Carried-forward requirements (from the source; do not weaken)

**Handler-facing data (the source's §4.2)**

- Surface the handler-needed data on the request card **and** the detail page: coil/part + requested
  quantity, the coil's location/stock, destination press/work center, requester, and
  urgency/remaining. *The source's "per mock toggle" parenthetical is stale — data is always live.*
- QA: tests for handler-data population and note add/persist, then the full suite.

**Accept / Complete / Release (the source's §4.3)**

- **Accept**: unaccepted request + viewer is Material Handler or above → show an **icon button** (not
  text); auto-assign to the signed-in user; status → `In Progress`; persisted.
- After accept the job **stays on the shared list**; Accept disappears for other handlers; only the
  **assigned** handler sees Complete and Release.
- **Complete** → `Done` (a single step now; leave TODO hooks for a future multi-step handoff).
- **Release** → returns the job to the open list (**available**, *not* a cancellation).
- **Edit** visible to the creator only; its behaviour is intentionally a TODO (no behaviour).
- Only the assigned handler may Complete/Release; non-handlers and non-owners see **no** actions.
- A handler can add a per-request **Note**, visible on the detail page and in the history.
- QA: tests covering accept assignment, others-see-taken, assignee-only complete/release, and
  release-versus-cancel.

**Urgency ordering (the source's §4.4)**

- Wire the existing `UrgencyCalculator.OrderMostUrgentFirst` helper into the Waitlist list view model
  so the available/handler list is ordered most-urgent-first (least remaining / overdue first). *The
  due/remaining/overdue maths and the per-subtype max-allotted settings editor are already done.*
- QA: tests for due/remaining/overdue maths **and** list ordering, then the full suite.

**Constraint to carry (do not regress)**

`PromptFiles/03-100%-Phase1-listdetail.md` holds a **"VERIFIED CARD ANATOMY … DO NOT REGRESS"** block.
It is still authoritative — reproduce it in this spec's constraints rather than restating the card
layout from scratch.

## 4. Explicitly out of scope — do not resurrect

- **Re-implementing the status model, the audit trail or the transitions** — all exist and are tested.
- **The demo/mock flow-coverage boxes** the source lists under "Mock-data flow coverage" — obsolete;
  the data is always live (see `OPEN-WORK-NEXT-SPEC.md` §3).
- **Multi-step handoff** after Complete — a future feature; leave the hooks only.
- **The Category/Item taxonomy and the 2-line card** — template 03. This spec adds no new card
  sections and removes none.
- **Analytics on handler performance** — template 04.

## 5. Defect this spec closes

`defects/High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` — template 01 removes or hides the
inert buttons; **this spec is where they come back working**. When this spec is implemented, update
that defect file's status line to `FIXED` with the commit and the tests that prove it.

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. Unit tests in the waitlist view-model test classes for: accept assigns the signed-in handler;
   another handler sees the request as taken with no actions; the assignee alone can complete and
   release; release puts the request back on the open list while cancel keeps it cancelled; the note
   round-trips and appears in history; ordering puts the most overdue request first.
3. Service-level tests already exist for the transitions
   (`MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs`) — extend rather than
   duplicate them.
4. UI automation pass per `.github/instructions/winui3-ui-automation.instructions.md`: accept a
   request as one user, confirm the actions disappear for a second user, complete it, and confirm the
   card leaves the open list. Note the documented trap — reaching the shell needs a signed-in session,
   and a shell that has launched the app must have its `MTM_*` variables cleared before running tests.

## 7. Open decisions to resolve before `/speckit.specify`

1. **What "Material Handler or above" resolves to** — the codebase has no shared rank helper yet
   (template 06 adds one). Decide whether this spec uses `RequestActionPolicy` plus a local role list,
   or waits for the shared helper.
2. **Auto-assignment storage** — which field records the assignee, and whether Release clears it.
3. **The creator's Edit affordance** — the source says it is visible with no behaviour. Confirm that a
   button with a TODO behind it is acceptable, or drop it (template 01's rule is "no control we cannot
   honour").
4. **Note editing by non-handlers** — the source grants it to handlers; confirm whether the requester
   may add a note too.
