# Phase 0 Research — Waitlist Handler Fulfilment and Urgency Ordering

Every unknown this plan left open is settled here. The service layer, the policy and the urgency math
already exist and are tested, so research is confined to how the screens reach them.

---

## R1 — How a card inside a `DataTemplate` reaches the view model's commands

**Decision**: carry the actions on the row. `SampleOrder` gains the action commands and the gate flags,
set by the view model as it builds each row; the card binds them through the `Order` property it already
receives (`Order="{Binding Mode=OneWay}"` in every per-type line view) with the `ElementName=Root`
pattern the card already uses for `RemainingTimeBrush` and `BadgeText`.

**Rationale**: the binding has to cross a `DataTemplate` boundary. The card is instantiated inside
`CoilWaitlistLineView` and its five siblings, whose `DataContext` is the `SampleOrder` row — not the
page. `SampleOrder` is already the row's presentation model (it owns `StatusBadgeText`,
`RemainingTimeText`, `EffectiveImagePath`, `Fields`, `RefreshTimeDerivedText`), so the commands belong
where the rest of the row's rendered state already lives. This route changes exactly one XAML file.

**Alternatives considered**:

- *`{Binding RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=Page}}`* — **rejected on
  evidence.** The WinUI XAML documentation retrieved through Context7 shows `RelativeSource` used only
  with `TemplatedParent`; no `FindAncestor` example appears, and a repository-wide search for
  `RelativeSource|AncestorType|FindAncestor` across every `.cs` and `.xaml` file returns **zero**
  matches. Adopting an undocumented-in-scope mode would be a guess about a platform API, which
  Principle IV forbids.
- *Command dependency properties on `WaitlistLineCardView`, forwarded by each per-type line view* —
  workable and recommended by the defect file, but it means threading three properties through the card
  plus six per-type views (seven XAML files) to deliver what one binding through `Order` already
  delivers. Rejected as the larger diff for the same behaviour.

**Note on grounding**: the Microsoft Learn MCP server was reported **disabled by the host** for this
run, so the WinUI platform question was answered from the Context7-indexed WinUI XAML documentation
plus the repository evidence above rather than from Learn directly. Recorded here because Principle IV
requires a deviation in source to be visible.

---

## R2 — How "Material Handler or above" is resolved

**Decision**: add `CanViewerHandleRequests(string? role)` to the existing `RequestActionPolicy`,
backed by the handler role set the floor already uses — `Material Handler`, `Production`,
`Production Lead`, `Setup`, `Setup Lead`, `Plant Manager`, `Admin`, `Developer` — and feed it the
signed-in role from `StartupState.CurrentRole`, which the list view model already receives and already
reads for the employee number.

**Rationale**: `RequestActionPolicy` is the class whose whole purpose is deciding what a viewer may do
to a request, and it is pure and unit-testable. The role vocabulary is not invented: `SettingsViewModel`
already carries `AllowedIgnoredLocationManageRoles` with exactly the roles above Material Handler, and
`ShellViewModel` already recognises `material handler` as a role. Putting the set in the policy keeps
the UI gate and the service gate provable against each other, which is what FR-018 asks for.

**Alternatives considered**: a new cross-module `IRoleRankService` — rejected as premature; the shared
rank helper is a separate workstream, and this feature needs one predicate, not a service. A literal
role check inline in the view model — rejected because it is not unit-testable in isolation and would
let the UI and service gates drift.

---

## R3 — Where the confirmation and the cancellation reason come from

**Decision**: add `IWaitlistRequestActionPrompt` to `MTM_Waitlist.Waitlist.View/Services/` with two
members — `ConfirmAsync(title, message, acceptText, cancelText)` and
`RequestCancellationReasonAsync(title, message, confirmText, cancelText)` returning the reason or
`null` when dismissed. The app-side implementation `Services/WaitlistRequestActionPrompt.cs` builds the
`ContentDialog` against `App.MainWindow.Content.XamlRoot`; `MTM_Waitlist.Tests` gets a
`NoOpWaitlistRequestActionPrompt`.

**Rationale**: the view model may not open a dialog directly — a headless test host has no XAML root,
and the repo already separates this concern for the Setup module (`ISetupDialogService` in the module,
`SetupDialogService` in the composition root, `NoOpSetupDialogService` in the tests). Mirroring that
pair means the prompt is injectable, the tests run with no UI, and the dialog text is localized the same
way the Setup dialog's is. Returning `null` for a dismissed prompt rather than a sentinel string keeps
"the user backed out" distinguishable from "the user gave an empty reason", which is the difference
between no cancellation and a cancellation with no reason.

**Alternatives considered**: `ContentDialog` constructed inside the view model — rejected: the library
is `UseWinUI` so it would compile, but a null `XamlRoot` in a headless host throws, and it puts UI
composition in a view model the repo deliberately keeps free of it. A message-only dialog with no
reason field — rejected: FR-010 requires a reason, and the service already stores one.

---

## R4 — What the list is ordered by

**Decision**: order in `WaitlistViewViewModel.LoadOrdersAsync` with
`UrgencyCalculator.OrderMostUrgentFirst`, using the due value the row already displays — the stored
target time when the request carries one — and falling back to `created + max-allotted` resolved
through `IUrgencyDeadlineService` (30-minute default, the service's documented default) when the target
is absent. The per-sub-type max-allotted lookup is memoised per load.

**Rationale**: `GetActiveRequests` currently orders by requested time **descending**, which is the
opposite of most-urgent-first and is what FR-015 replaces. Ordering by the same due value the card's
"Remaining time" is drawn from means the list order and the number on each card can never disagree —
ordering by a separately computed state would let a card say "overdue" while sitting under a card that
is not. `UrgencyCalculator.OrderMostUrgentFirst` is the existing home for the ordering rule
(overdue first, then least remaining) and is already unit-tested.

**Alternatives considered**: ordering inside `IWaitlistRequestService.GetActiveRequests` — rejected:
that method returns domain records that carry no urgency state, and the seeded requirement puts the
ordering in the list view model. Adding a new sort helper — rejected: the helper exists.

**Note**: the "My Requests" filter narrows the same ordered list rather than re-sorting it, so the
filtered view keeps the urgency order.

---

## R5 — What gates each action on the screen, and why the service still enforces it

**Decision**: each row carries `CanAccept`, `CanCompleteOrRelease` and `CanCancelRequest` flags, set
from `RequestActionPolicy` (accept needs `CanViewerAccept`; complete/release need
`CanViewerCompleteOrRelease`; cancel reuses the existing `CanRequesterCancel` plus an
`IsRequesterOrder` identity check). Buttons are hidden, never disabled, when the flag is false. The
service's own gate is left exactly as it is and is not relaxed.

**Rationale**: FR-004/FR-005/FR-008/FR-009 all reduce to "hide what this viewer may not do", and
FR-018 requires the screen's answer to be a *view* of the service's rule rather than the rule itself.
Hiding rather than disabling matches both the defect file's instruction and the FR-020 rule that no
control is drawn whose activation has no effect.

**Alternatives considered**: showing disabled buttons with a tooltip — rejected: it advertises an
action the viewer can never take, which is the failure mode this feature exists to remove.

---

## R6 — Where the note and the request history live

**Decision**: the note is a `TextBox` plus a send action on the request detail page, backed by
`IWaitlistRequestService.UpdateNoteAsync`. The request history is a new block on the detail page rendering
`GetAuditTrail`, showing each entry's local time, its sender and its message text.

**Revised after the first build — two parts of this decision were wrong.** *Visible only when the viewer may
handle requests* gated the box on the handler role, and the people who raise requests are the ones with
something to say, so the gate is gone (FR-012). *showing each entry's local time, its event, its actor and its
details* rendered the internal event type, so a note's row read `NoteUpdated` beside the name of whoever changed
it; the type is now hidden and the author is named in full (FR-013).

**Rationale**: FR-013 requires the note's addition to appear "in the request's history", and no screen
currently renders the audit trail at all — `GetAuditTrail` has no caller outside the tests. The detail
page is the only surface with room for it, and it is a page-level block, so the frozen card anatomy is
untouched. The history block follows the detail page's existing "add the section only when it has
rows" rule so a request with no readable history draws no empty shell.

**Alternatives considered**: putting the history on the card — rejected: it would change the frozen
card, and FR-021 forbids that. A new history page — rejected: a whole route for one list.

---

## R7 — How the list and the detail page pick up a change

**Decision**: rely on the existing `IWaitlistRequestService.RequestsChanged` event, which the list view
model already subscribes to and which every handler action and the cancel path already raise, plus an
explicit reload after a successful action on the detail page (the detail page does not subscribe).

**Rationale**: FR-023 asks for the new state to appear without a manual reload, and the machinery is
already there — `OnRequestsChanged` calls `LoadOrdersAsync`, and `PersistHandlerActionAsync` and
`TransitionStatusAsync` both raise the event. Adding a second refresh mechanism would be a second
source of truth for "the list is current".

**Note**: the read side of a transition is a fresh `sp_waitlist_request_list` read, so the list settles
on the stored result rather than on the local object graph — which is what the "two handlers race"
edge case needs.

**Alternatives considered**: reloading the list explicitly from every command — rejected as redundant
with the event and a way to get double loads.

**Revised after the first build.** The detail page no longer acts on a request at all (FR-025), so "an explicit
reload after a successful action on the detail page" describes nothing. In its place the page re-reads the
request and its history every 30 seconds while it is open (FR-026), because the activity a handler needs to see
is **another** handler's, and that raises no event on this client. The first attempt at the timer returned
silently when `DispatcherQueue.GetForCurrentThread()` was empty during navigation, leaving a page that looked
live and never refreshed; the current-thread fallback, and a log line saying whether the timer started, are the
fix (T041).

---

## R8 — Which stored statuses the new code compares against

**Decision**: keep comparing against the strings the model already stores —
`Pending`, `Accepted`, `Completed`, `Canceled` — and treat the friendly labels the card shows
(`Waiting`, `In Progress`, `Done`, `Cancelled`) as display text only, exactly as `StatusBadgeText`
already does. "Released" is an audit **event**, not a stored status: a release stores `Pending` with
`ReleasedUtc` stamped and the assignee cleared.

**Rationale**: `MapRowToRequest`, `TransitionStatusAsync`, `AcceptAsync`, `CompleteAsync` and
`ReleaseAsync` all already agree on this vocabulary, and `IsValidStatusTransition` validates against
it. Introducing a new status string would need a stored-procedure change (Principle III) for no
behaviour anyone asked for. The released identity is already observable through the audit trail, which
is what FR-007 and SC-004 actually require.

**Alternatives considered**: adding a stored `Released` status — rejected: it is a schema change for a
distinction the audit trail already carries, and it would need a paired `create.sql`/`rollback.sql`.

---

## R9 — What must not be touched

**Decision**: the card's layout is frozen exactly as the spec's Verbatim Constraints reproduce it. The
two action buttons go into the existing vertically-centred action area above the compact status pill,
at the same 44×44 icon size the removed pair used, and the detail page is the only place new blocks are
added.

**Rationale**: FR-021 and the user-approved "DO NOT REGRESS" block. The removed Cancel and Accept
buttons occupied that area at that size, so restoring actions there restores the approved anatomy
rather than altering it.

**Note**: `WaitlistLineCardMarkupTests` currently asserts the card offers **no** Cancel or Accept
control at all. That assertion was true while the buttons were inert and is false once they are wired,
so this feature revises it to the honest form — every button carries a `Command` and its `Visibility`
is bound to a per-row gate — rather than deleting the check.

**Revised after the first build.** Two things were added *inside* the frozen card after all — the new-message
marker on the request-type tile (FR-027) and a tooltip on each action button — and one thing changed on its
face: the status pill's colour now varies by state (FR-029). None of them moves a dimension. The marker
overlays the existing 96×96 tile in its corner, a tooltip is not drawn until it is hovered, and the pill keeps
its fixed ~36px height. The action area still carries at most the primary action and Cancel.

---

## R10 — What the new-message indicator is compared against

**Decision**: the queue read projects `last_message_utc` — `MAX(occurred_utc)` over the request's `NoteUpdated`
entries that carry an actor — and the card compares the viewer's seen marker against that, not against the row's
`updated_utc`.

**Rationale**: the first implementation used `updated_utc` because it was already on the row. It is the wrong
value: it moves for every lifecycle change, so accepting, completing or cancelling a job flagged the card as
though somebody had spoken. The marker became a "this row changed" light, which the status pill already is.
Requiring an actor also excludes the system's own entries, so only what a person wrote raises it (FR-028).

**Verified against the live database**: `last_message_utc` is `NULL` for a request accepted with no message and
set for one that was written on. That distinction only becomes observable once rows exist carrying both kinds of
change, which is why it could not be settled by a unit gate alone.

**Alternatives considered**: filtering client-side by re-reading each row's audit trail — rejected: N extra
reads per list load for a value the list query can produce once. Keeping `updated_utc` and suppressing the
marker for a recent status change — rejected: a heuristic where a predicate will do.

---

## R11 — What the card shows when the image resolver has nothing

**Decision**: a resolved image path is taken only when `RequestImagePathPolicy.IsUsableResolvedPath` accepts it —
non-blank, and not one of the `ImageLocationDefaults` placeholder paths, compared separator- and
case-insensitively. Otherwise the row keeps its own image and the refusal is logged.

**Rationale**: `ImageLocationService.ResolveRequestTypeImagePathAsync` answers "nothing is configured for this
type" with a *valid-looking* default — the *no image available* placeholder — and does not say which answer it
is giving. The card preferred any non-empty resolved path over the image it already had, so the placeholder
overwrote a working image. Nothing initializes the image service during start-up, which is what made the symptom
look like a navigation bug: the first list load skipped resolution and looked right, then the New Request
workflow initialized the singleton as a side effect and every later load resolved to the placeholder (FR-031).

**Why the store made it invisible**: every `waitlist_request_types.default_image_path` is `NULL` and
`config_images_locations` is empty, so on this database the resolver *can only* answer with the placeholder. The
real images come from each row's own legacy `ImagePath` (`pickup_ncm.png`).

**Alternatives considered**: returning `null` from the service — rejected: it changes a contract the Settings
image dialogs rely on, since they legitimately show the placeholder as the effective path. Initializing the
service during start-up — rejected: it moves the defect to the first load instead of fixing it. Populating the
catalog — rejected: a data change that would hide a code defect. Full account:
`defects/Closed-High-NewRequestVisitReplacesCardImagesWithThePlaceholder.md`.
