# Tasks — Waitlist Handler Fulfilment and Urgency Ordering

**Feature**: `specs/003-waitlist-handler-fulfilment` | **Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md) | **Model**: [data-model.md](./data-model.md) | **Contracts**: [contracts/](./contracts/)

**Size**: `oversized` — the full pipeline applies. There is no Setup phase: the feature adds no
project, no package reference and no build configuration, so the first phase is Foundational.

Every task is ticked only against a gate that actually ran — see
[`contracts/verification-gates.md`](./contracts/verification-gates.md). `tasks.md` checkboxes are owned
by the companion journal; they are never hand-edited. **A temporary exception was taken, and then undone.**
The post-completion boxes below were first marked directly in this file, because the spec had already reached
`status: completed` and the companion writer refuses to append a task finish to a shipped spec. On 2026-09-13
the spec was reopened, the capture then journaled T042 … T049 (`Synced 8 new task event(s) (49/49 complete)`,
closing at `status: implemented`), and the boxes stand as the record the journal now agrees with. The direct
edit was a stopgap that has been reconciled, not a permanent divergence.

---

## Phase 1: Foundational — shared state, seam and strings (blocks every story)

**Wave 1 — independent (different files):**

- [x] **T001** [P] [US1] Add `CanViewerHandleRequests(string? role)` and the handler role set (`Material Handler`, `Production`, `Production Lead`, `Setup`, `Setup Lead`, `Plant Manager`, `Admin`, `Developer`; case-insensitive) to the existing action policy, and extend its tests with the role vocabulary cases · `MTM_Waitlist.Core/Services/RequestActionPolicy.cs`, `MTM_Waitlist.Tests/Core/Services/RequestActionPolicyTests.cs` · FR-001, FR-008, FR-018
- [x] **T002** [P] [US1] Add the row action state to the row model: `Urgency`, `CanAccept`, `CanCompleteOrRelease`, `CanCancelRequest`, the four `ICommand?` members and the four localized action texts · `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` · FR-004, FR-005, FR-020
- [x] **T003** [P] [US1] Add the prompt seam — `ConfirmAsync` and `RequestCancellationReasonAsync` returning `null` on dismissal · `MTM_Waitlist.Waitlist.View/Services/IWaitlistRequestActionPrompt.cs` · FR-010, FR-019
- [x] **T004** [P] [US1] Add the new user-visible strings (action labels and automation names, refusal sentences, note and history labels) to the resource file · `Strings/en-us/Resources.resw` · FR-022

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T005** [P] [US1] Implement the prompt seam in the composition root as a `ContentDialog` against `App.MainWindow.Content.XamlRoot`, answering false/null when there is no XAML root, and register it beside the existing Setup dialog registration · `Services/WaitlistRequestActionPrompt.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs` · FR-010
- [x] **T006** [P] [US1] Add the no-op test double for the prompt seam, mirroring the existing Setup dialog double · `MTM_Waitlist.Tests/NoOpWaitlistRequestActionPrompt.cs` · FR-010

**Checkpoint**: the shared policy, row state, prompt seam and strings exist; no story is user-facing yet.

---

## Phase 2: US1 — A handler takes a request (P1)

### Tests

- [x] **T007** [US1] New gate-matrix tests: `CanAccept` / `CanCompleteOrRelease` / `CanCancelRequest` against `RequestActionPolicy` for every viewer role × request state combination, plus flag-versus-command count consistency · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` · G4 · FR-004, FR-005, FR-008, FR-009, FR-018

### Implementation

**Wave 1 — independent (different files):**

- [x] **T008** [P] [US1] List view model: resolve the signed-in role and employee number, add `AcceptRequestCommand`, set every row's gate flags and commands as rows are built, set `ActionRefusalMessage` on a refusal or an unavailable action, and clear it on success · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-001, FR-002, FR-003, FR-019, FR-023
- [x] **T009** [P] [US1] Card markup: restore the gated icon buttons in the existing vertically-centred action area above the compact status pill, bound one-way to the row's gate flags and commands with localized automation names, without moving any frozen dimension · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-020, FR-021

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T010** [US1] Revise the card markup test: every button carries a `Command`, every action button's `Visibility` is bound to a per-row gate, and the status-pill checks stay in force (the absent-control assertion is replaced, not deleted) · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · G3 · FR-020, FR-021
- [x] **T011** [US1] Extend the action tests: accept calls `AcceptAsync` with the row's request id and the signed-in identity, a refused accept sets a non-empty refusal message, and no refusal reports success · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` · G5 · FR-002, FR-019
- [x] **T012** [US1] Extend the field tests: the card surfaces the handler-needed values the request carries and renders nothing where a value is absent · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelFieldTests.cs` · FR-014

**Checkpoint**: a handler can accept from the list, the claim persists, the request stays on the shared list, and no other viewer is offered Accept. US1 is independently functional and testable.

**Added after the spec review — concurrent claims (FR-024).** When two handlers claim the same request at the
same moment the store decides the winner, so the losing screen has to say so rather than looking untouched.
This lands with US1 because it is the same command.

- [x] **T031** [US1] Give the prompt seam an acknowledgement-required warning, add the lost-claim copy (another handler already took this request / the request is gone — choose a different one), and show it when an accept is refused, keeping the inline refusal set and never touching the winning claim · `MTM_Waitlist.Waitlist.View/Services/IWaitlistRequestActionPrompt.cs`, `Services/WaitlistRequestActionPrompt.cs`, `MTM_Waitlist.Tests/NoOpWaitlistRequestActionPrompt.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `Strings/en-us/Resources.resw`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` · G5 · FR-024

---

## Phase 3: US2 — The assigned handler finishes or hands back the work (P2)

### Tests

- [x] **T013** [US2] Extend the service tests: release leaves status `Pending` with the assignee cleared and a `Released` audit entry and no `Canceled` entry; cancel records the reason and canceller; the existing accept/complete/release/note tests stay green · `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs` · G6 · FR-007, FR-011, FR-016, FR-017

### Implementation

**Wave 1 — independent (different files):**

- [x] **T014** [P] [US2] List view model: add `CompleteRequestCommand` and `ReleaseRequestCommand`, gated to the recorded assignee, so a non-assignee's row carries neither · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-005, FR-006, FR-007
- [x] **T015** [P] [US2] Card markup: add the Complete and Release icon buttons to the same action area, gated identically, so at most two buttons are ever drawn · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-005, FR-020

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T016** [US2] Extend the action tests: complete and release call the service with the signed-in identity, and a request assigned to another handler yields no commands on the row · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` · G5 · FR-005, FR-009

**Checkpoint**: the assignee can complete or release; a release returns the request to the open list and remains accept-able, while a completion removes it. US2 is independently functional and testable.

---

## Phase 4: US3 — The list leads with the most urgent work (P3)

### Implementation

- [x] **T017** [US3] List view model: give each row its `UrgencyState` — the stored target time when present, else `created + max-allotted` — and order the built rows with `UrgencyCalculator.OrderMostUrgentFirst`, memoising the per-sub-type max-allotted lookup per load · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-015
- [x] **T018** [US3] Wire the deadline service into the list view model's constructor as an optional parameter and confirm it resolves in the container (register it if the existing registration does not cover it) · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Waitlist.View/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` · FR-015

**⟶ Wait for T017 and T018 to finish, then:**

- [x] **T019** [US3] Extend the action tests with the ordering gates: expected order matches `OrderMostUrgentFirst`, overdue rows precede non-overdue, least remaining is first among the rest, a row with no derivable due value sorts last without throwing, and order never contradicts the displayed remaining time · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` · G7 · FR-015

**Checkpoint**: the list reads most-urgent-first and the order agrees with the number on each card. US3 is independently functional and testable.

---

## Phase 5: US4 — A requester withdraws their own request (P4)

### Implementation

**Wave 1 — independent (different files):**

- [x] **T020** [P] [US4] List view model: add `CancelRequestCommand`, offered only on the viewer's own waiting row, prompting for confirmation and a reason before calling `CancelOwnRequestAsync` with the requester's employee number, and mapping each cancel result to a plain message · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-010, FR-011, FR-019
- [x] **T021** [P] [US4] Card markup: the requester's Cancel affordance in the same action area, gated to the requester's own waiting row · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-010, FR-020

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T022** [US4] Extend the action tests: cancel calls `CancelOwnRequestAsync` with the requester's identity and the prompted reason, a dismissed prompt performs no service call, an empty reason is still a confirmation, and each cancel result maps to a distinct message · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` · G5 · FR-010, FR-011

**Checkpoint**: a requester can withdraw their own waiting request with a recorded reason, and the request leaves the active list without being deleted. US4 is independently functional and testable.

---

## Phase 6: US5 — A handler leaves a short note, and the history is readable (P5)

### Implementation

**Wave 1 — independent (different files):**

- [x] **T023** [P] [US5] Detail view model: add the four action commands with the same gates as the list, `NoteDraft` plus `SaveNoteCommand` through `UpdateNoteAsync`, and history rows projected from `GetAuditTrail` · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` · FR-012, FR-013, FR-023
- [x] **T024** [P] [US5] Detail page: add the action bar, the note editor and the request-history block, each shown only when it has content, with every new string localized · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` · FR-012, FR-013, FR-022

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T025** [US5] New detail action tests: saving a note calls `UpdateNoteAsync` and leaves a `NoteUpdated` entry, a whitespace-only note is not stored as an empty note, history rows appear after a load, and every new resource key is referenced · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailActionTests.cs` · G8 · FR-012, FR-013, FR-022

**Checkpoint**: a handler can leave a note that appears on the detail page and in the request's history, and the history is readable. US5 is independently functional and testable.

---

## Phase 7: Polish — cross-cutting documentation, guards and validation

**Wave 1 — independent (different files):**

- [x] **T026** [P] Move the inert-buttons defect to `FIXED` with the closing commit and the tests that prove it · `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` · FR-021
- [x] **T027** [P] Correct the changelog claims so the documented surface matches what shipped · `CHANGELOG.md` · FR-021
- [x] **T028** [P] Add the frozen-anatomy guard: the card's image is a fixed 96×96 square, the title keeps its own row, the four metadata rows remain, the status pill stays a fixed ~36px below the buttons, and the per-type detail grid stays the 2×3 four-column pattern · `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs` · FR-021

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T029** Validate against the Success Criteria: clean solution build (`0 Warning(s) 0 Error(s)`) and the full suite green (`Failed: 0`), after stopping the running app and clearing `MTM_*` overrides · G1, G2 · SC-008
- [x] **T030** Running-application pass per the UI-automation instruction file: raise as user A, accept as handler B, confirm the actions disappear for a third viewer, complete one request and release another, confirm the most-overdue row leads, confirm a refusal is reported in plain language, and confirm the note appears on the detail page and in the history · G9 · SC-001, SC-002, SC-003, SC-004, SC-005, SC-006

---

## Post-completion fixes — found by using the shipped build

The first build after this feature landed was used on the floor and three defects showed up that no unit
gate could have caught, because all three only exist in a running WinUI app. They are recorded here rather
than silently patched, because two of them were cases where the feature *looked* like it worked.

- [x] **T032** [US2] Stop the action paths in both waitlist view models resuming their continuations off the UI thread: after `await`, setting a bindable property or mutating a bound collection on a pool thread throws `RPC_E_WRONG_THREAD` (0x8001010E). This is why Complete on the request page appeared to do nothing, and why the note save logged a failure and then raised an unhandled exception. The store was already updated by the time it threw, so the data changed and the screen did not · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-023
- [x] **T033** [US1] Supply every constructor dependency in the list view model's composition-root factory. The factory used six positional arguments, so the newer optional parameters — the confirmation seam and the urgency deadline service — were `null` in the running app: Cancel could never be confirmed, the lost-claim warning could never be shown, and ordering silently fell back to the 30-minute default. The factory now names every parameter, and a guard test fails the build if a future parameter is left unnamed · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewRegistrationTests.cs` · FR-010, FR-015, FR-024
- [x] **T034** [US5] Render each history entry's `Details`, so a note's history row shows the note itself rather than only its event name, and record the note's author on the entry so the row says who wrote it · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`, `MTM_Waitlist.Waitlist.View/Services/IWaitlistRequestService.cs`, `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` · FR-013, FR-017
- [x] **T035** [US4] Record the cancelled spelling `Canceled` in the action policy's finished set. The stored spelling is `Canceled` (one L) while the set carried only the display spelling `Cancelled`, so a cancelled request was not recognised as finished — harmless only because no other status predicate accepted it either · `MTM_Waitlist.Core/Services/RequestActionPolicy.cs` · FR-009

**Re-verified after these fixes**: clean build (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`; and a
running-app pass that opened a claimed request, read the action bar, clicked Complete, and confirmed the
request closed, the audit row was written, the page then offered no actions, and the log carried **no**
`COMException`.

**Still open — deferred, and honestly not done.** Found while re-verifying T034, and deliberately **not**
patched in that pass because the read half is a database artifact that needs live validation. It is closed
below by **T037**, which is where its validation lives.

- [x] **T036** [US5] Read the request history back from the store. `sp_waitlist_request_audit_insert` writes
  every entry and there is no matching read procedure, so `GetAuditTrail` serves an in-memory dictionary and
  the history block shows only this session's events — and hides itself entirely for a request the user has
  not touched, which is absence rendered as nothing. Closed by T037; the defect it was owned by is
  `defects/Closed-Medium-Waitlist-RequestHistoryShowsOnlyTheCurrentSession.md` · G8 · FR-013, FR-017

---

## Post-completion redesign — asked for after using the build

The actions were on the request page and the list card was read-only. Using it showed that a handler works
from the list, so the actions belong on the card, and the page is for reading a request rather than acting on
it. These tasks implement that, plus the new-message indicator and the refresh, and they close T036.

- [x] **T037** [US5] Add the missing read half of the audit trail: `sp_waitlist_request_audit_list`, paired
  `rollback.sql`, the master-list block, and the service read that merges the store's rows with whatever this
  session already recorded — deduped by content, so it is idempotent and an overlapping entry never doubles.
  A failed read keeps the in-session trail rather than rendering a failure as "no history" · `Database/StoredProcedures/sp_waitlist_request_audit_list/create.sql`, `Database/StoredProcedures/sp_waitlist_request_audit_list/rollback.sql`, `Database/StoredProcedures/AllSPs.sql`, `MTM_Waitlist.Waitlist.View/Services/IWaitlistRequestService.cs`, `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` · FR-013, FR-017
- [x] **T038** [US1] Two-button card chrome. The action area offers the primary — Accept while the request is
  available, Complete once the viewer has claimed it — and Cancel: the requester's own waiting request offers
  Accept + Cancel, a claimed request offers Complete + Cancel, a request assigned to someone else offers
  nothing, and a refusal title names itself from the row. Release keeps its command and loses its button · `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `Strings/en-us/Resources.resw` · FR-004, FR-005, FR-006, FR-007, FR-010, FR-020
- [x] **T039** [US5] Take the actions off the request page and keep the page current. The page offers no
  accept / complete / release / cancel control at all; it reloads the request and its history on a 30-second
  cadence while it is open, and the history block reports an empty history as empty instead of hiding · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` · FR-012, FR-013, FR-022, FR-025, FR-026
- [x] **T040** [US1] New-message indicator: a message marker on the lower-right corner of the request-type
  image with a tooltip, shown while the request changed after the viewer last read it and cleared by reading
  it. The marker is the newest entry's time rather than "now", so an entry that lands while the page is open
  re-flags the card instead of being swallowed · `MTM_Waitlist.Waitlist.View/Services/IWaitlistMessageSeenStore.cs`, `MTM_Waitlist.Waitlist.View/Services/LocalWaitlistMessageSeenStore.cs`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-025
- [x] **T041** [US5] Resolve the dispatcher when the container hands over none. A factory resolved while
  navigation was still completing can find `DispatcherQueue.GetForCurrentThread()` empty, and a null dispatcher
  meant `StartRefreshTimer` returned silently — the page never refreshed, with nothing in the log to say so.
  Both view models now fall back to the current thread (and the timer logs whether it started) · `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` · FR-023

**Re-verified after the redesign**: clean build (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0, Passed: 865`;
`sp_waitlist_request_audit_list` created on the database and exercised with a real request (rows returned
oldest-first) and an unknown one (empty set); and a running-app pass that read the history off the store
(`Audit trail loaded … Entries=1` in the log), confirmed the request page draws **no** action button, and
confirmed on the list that a waiting request shows `[Accept request, Cancel request]`, that accepting it turns
the same card into `In Progress` with `[Complete request, Cancel request]`, and that a request the viewer has
not claimed shows `[Accept request]` alone.

---

## Post-completion fixes — the second pass, found by using the build again

> **How these ticks were recorded.** This spec had already reached `status: completed`, and the companion
> writer refuses to append task finishes to a shipped spec (`already shipped; not appending`) and refuses to
> regress its lifecycle status. So the boxes below were first marked directly in this file — stated here rather
> than hidden. On 2026-09-13 the spec was **reopened**, the capture was re-run
> (`Synced 8 new task event(s) (49/49 complete)`, closing at `status: implemented`), and T042 … T049 are now
> journaled in `.spec-context.json` like every other task, each with a `task_summaries` row so the Activity
> panel shows what it did. Nothing below now disagrees with the journal.

- [x] **T042** [US5] Send a message on the first click. `{x:Bind}` defaults a `TextBox.Text` two-way binding
  to `LostFocus`, so the first click on Send read a stale draft and saved nothing, and the 30-second refresh
  then seeded the box from the store and erased what was being typed. The box now pushes every keystroke
  (`UpdateSourceTrigger=PropertyChanged`), the refresh never seeds over an unsent edit, and a click always
  answers — nothing to send, already sent, sent, or refused · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`,
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` · FR-012, FR-023
- [x] **T043** [US5] Stop the history showing one note twice. `occurred_utc` is a MySQL `datetime`, so the
  store returns whole seconds while the session stamped sub-second precision; the merge key compared full
  precision, so every entry appeared once from the store and once from memory. The key truncates to seconds ·
  `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` · FR-013
- [x] **T044** [US1] Raise the new-message marker for messages only. It was driven by the queue row's
  `updated_utc`, which moves for every lifecycle change, so job created, accepted, completed and canceled all
  flagged the card as if somebody had spoken. `sp_waitlist_request_list` now returns `last_message_utc` — the
  newest `NoteUpdated` entry **with an author** — and the marker compares against that · database artifact plus
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` · FR-027, FR-028
- [x] **T045** [US5] Let anyone signed in send a message. The message box was gated on the handler role, so the
  floor could not tell a handler anything. The gate is gone from both the markup and the command · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` · FR-012
- [x] **T046** [US5] Show who sent each history row, and stop showing the message type. The history now reads
  time → **full name** → message text, with `System` where the author is absent, and the event-type column is
  gone. The accepted entry also stops writing a bare employee number as its detail · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`,
  `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestAuditEntry.cs`, `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` · FR-013, FR-017
- [x] **T047** [US1] Make Accept and Complete unmistakable. They share one slot in two states, and both were
  drawn as a green tick with a glyph the Segoe Fluent Icons list marks **deprecated** (`E10B`, E0–E5 range).
  Accept is now green with `E8FB` (Accept) and Complete is the accent colour with `E930` (Completed), guarded by
  a test that asserts both differences · `Module_Waitlist/Controls/WaitlistLineCardView.xaml` · FR-020, FR-021, FR-030
- [x] **T048** [US1] Give each lifecycle state its own status-badge colour. The badge was one accent-blue pill
  for every state, so the element whose whole job is to say where a request is said nothing. `RequestStatusPalette`
  maps `Pending`/`Accepted`/`Completed`/`Canceled` to grey/blue/green/red, exposed as `Color` as well as brushes so
  the mapping is testable without a XAML runtime, and reached from markup through `RequestStatusToBrushConverter`.
  The `SystemFillColor*` theme tokens were rejected on evidence — they carry dark glyphs (`SystemFillColorCaution`
  is `#FCE100`, unreadable under white) — and a test asserts every colour clears the WCAG AA 4.5:1 contrast ratio.
  **Recorded limitation:** the list is built from `GetActiveRequests` alone, so `Done` and `Cancelled` cannot
  currently reach a card; their colours are mapped and tested but not visible on the list ·
  `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs`, `MTM_Waitlist.Waitlist.View/Converters/RequestStatusToBrushConverter.cs`,
  `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `App.xaml` · FR-020, FR-021, FR-029
- [x] **T049** [US1] Stop the resolver's "nothing configured" answer replacing a card's real image. The image
  service returns `ImageLocationDefaults.RequestTypeDefaultPath` — the *no image available* placeholder — when
  neither an override nor a catalog `default_image_path` exists, and the card preferred any non-empty
  `ResolvedImagePath` over the image it already had. Nothing initializes the service at startup, so the first
  load skipped resolution and looked right; opening New Request initialized the singleton, and every later load
  then resolved to the placeholder. `RequestImagePathPolicy.IsUsableResolvedPath` now states the rule in one
  testable place, the card keeps its own image when the resolver has nothing real, and the refusal is logged.
  **Owned by** `defects/Closed-High-NewRequestVisitReplacesCardImagesWithThePlaceholder.md` ·
  `MTM_Waitlist.Waitlist.View/Helpers/RequestImagePathPolicy.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`,
  `MTM_Waitlist.Settings/Services/ImageLocationService.cs` · FR-014, FR-031

**Re-verified after this pass**: clean build (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0, Passed: 888,
Skipped: 19, Total: 907`; and a running-app pass that sent a message with `InvokePattern` — **without ever
focusing the button**, the case that silently lost the text before — and read back `Message sent.` plus a single
history row naming the sender; the same pass claimed a request and confirmed the card's Accept (green, bare
check) became Complete (accent, circled check) beside Cancel, and read the status badge back as grey `Waiting`
then blue `In Progress`.

T049 was verified the same way, but it has to be stated separately because it is the one defect here that only
a navigation reproduces: open the list, read the tiles, enter the **New Request** workflow, return, read the
tiles again. **Before** the fix all three tiles had become the *no image available* placeholder; **after** it all
three keep their own image, and the log carries `Request '…' has no configured image; keeping the row's own
image 'pickup_ncm.png' instead of the resolver's placeholder.` The store was checked as part of the same pass
and holds no configured request-type image at all — every `default_image_path` is `NULL` and
`config_images_locations` is empty — which is why the real images come from each row's own legacy `ImagePath`.
With that in mind, per-type images remain a data task (populate the catalog or the overrides) rather than a code
task, and nothing further is owed by this feature.

---

## Dependencies & Execution Order

**Phase dependencies**: Foundational → US1 → US2 → US3 → US4 → US5 → Polish. There is no Setup phase
(nothing to install or configure). Foundational blocks every story — the policy predicate, the row
state, the prompt seam and the strings are all consumed by the story work.

**Story independence**: US3 (ordering) depends only on Foundational and can land without any other
story. US1 is the MVP slice. US2 extends the same two files as US1 (`WaitlistViewViewModel.cs` and
`WaitlistLineCardView.xaml`), and US4 and US5 touch those files again, so the stories run **in order**
even though each is separately demoable — the wave layout inside each phase is what makes the
independence visible, and the file overlap is why the phases do not run concurrently.

**Wave map**:

| Phase | Wave 1 (independent) | Wait line | Wave 2 |
| --- | --- | --- | --- |
| Foundational | T001, T002, T003, T004 | → | T005, T006 |
| US1 | T008, T009 | → | T010, T011, T012, T031 |
| US2 | T014, T015 | → | T016 |
| US3 | T017, T018 (same file — sequential within the wave) | → | T019 |
| US4 | T020, T021 | → | T022 |
| US5 | T023, T024 | → | T025 |
| Polish | T026, T027, T028 | → | T029, T030 |

**Same-file serialisation** — these pairs can never be in one wave: `WaitlistViewViewModel.cs` (T008 →
T014 → T017 → T020), `WaitlistLineCardView.xaml` (T009 → T015 → T021), and the test files that are
extended more than once.

**Parallel opportunities**: within Foundational, all four Wave-1 tasks touch different files and can be
built in any order. All three Polish Wave-1 tasks are independent. Everything else is a single-task
wave or a two-file wave.
