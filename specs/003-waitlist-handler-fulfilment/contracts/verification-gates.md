# Contract — Verification gates

Every gate below is a command or a test that must be run and its result recorded. A task is ticked only
against a gate that passed. Gates G3–G8 are unit-level and run in the normal suite; G9 is the only one
that needs the running application.

---

## G1 — Clean solution build

```
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
```

**Expected**: `0 Warning(s) 0 Error(s)`.

`WMC9999` is a **masked XAML error** on this toolchain, not an environment problem. If it appears, stop
any running `MTM_Waitlist.exe`, delete stale `*.pri` under `obj/`/`bin/`, rebuild, and if it persists
surface the real error by temporarily introducing a deliberate C# error (`.github/copilot-instructions.md`
→ Known Build Quirks). Note also that `[RelayCommand]` **strips a trailing `Async`** from the generated
command name — `ContinueToReviewAsync` binds as `ContinueToReviewCommand` — so a wrong bound name shows
up only as `WMC9999`.

## G2 — Green suite

```
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

**Expected**: `Failed: 0`.

Stop the running app and clear `MTM_*` environment overrides before running the suite
(`.github/instructions/winui3-ui-automation.instructions.md`).

## G3 — Card offers no control it cannot act on

`MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests`

- Every `Button` in the card carries a `Command` attribute.
- Every button in the action area carries a `Visibility` binding to a per-row gate property — the
  buttons are gated, not merely present.
- Every button in the action area carries a `ToolTipService.ToolTip` bound to the row's localized action
  text, so the action is named on hover as well as to a screen reader.
- **Accept and Complete differ in both colour and glyph** (`Card_AcceptAndCompleteAreUnmistakablyDifferent`):
  green `SystemFillColorSuccessBrush` with `E8FB` versus accent `AccentFillColorDefaultBrush` with `E930`.
  They share one slot in two states, so a viewer switching between them has to see that the button changed.
- The status pill still shows `StatusBadgeText` and is still gated on `HasStatusBadge`.
- **The status badge has one colour per lifecycle state** (`RequestStatusPaletteTests`): each stored status
  reaches its own role, no two roles share a colour, both spellings of cancelled agree, an unrecognised status
  claims no badge, and every colour clears the WCAG AA 4.5:1 contrast ratio against the badge text.
- The request detail page binds **none** of the four action commands (`RequestPage_DrawsNoActionButton`,
  in `WaitlistViewDetailActionTests`), so exactly one surface decides what a viewer may do.

The previous assertion that the card offers **no** Cancel or Accept control is revised, not deleted: it
was true while the buttons were inert and is false once they are wired (research R9). The other checks
stay in force.

## G4 — Gates match the policy for every combination

`MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests`

For each combination of viewer role (handler / non-handler) × request state (`Pending`, `Accepted` by
this viewer, `Accepted` by someone else, `Completed`, `Canceled`):

- `CanAccept` true **iff** `RequestActionPolicy.CanViewerAccept(...)` is true.
- `CanCompleteOrRelease` true **iff** `RequestActionPolicy.CanViewerCompleteOrRelease(...)` is true.
- `CanCancelRequest` true **iff** the viewer is the requester of a `Pending` row **or** its assignee.
- `CanAccept` and `CanCompleteOrRelease` are never both true — they are one button in two states
  (`Card_ShowsCancelWheneverCompleteIsOffered`).
- `CanCancelRequest` is true whenever `CanCompleteOrRelease` is.
- The count of non-null action commands equals the count of true flags — a flag with no command, or a
  command with no flag, fails.

## G5 — The commands call the service with the right identity

`MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests`

Against a recording fake of `IWaitlistRequestService`:

- Accept calls `AcceptAsync` with the row's `RequestId` and the **signed-in** employee number.
- Complete calls `CompleteAsync` with the same identity and **no** call reaches `ReleaseAsync` — the card
  draws no Release control, so nothing may take that path from the screen.
- Cancel calls `CancelOwnRequestAsync` with the **requester's** employee number and the reason the
  prompt returned when the viewer raised the row, and `TransitionStatusAsync(requestId, "Canceled", …)`
  when the viewer is the assignee.
- A service `null` / non-success answer sets `ActionRefusalMessage` to a non-empty string and does **not**
  report success.
- A dismissed prompt performs **no** service call.

**Concurrent claim (FR-024).** Two view models over one store that arbitrates first-claim-wins, both looking
at the same available request:

- The store's assignee after both attempts is the **first** handler's employee number.
- The request is still on the shared list.
- The winner's refusal message is empty and the winner was shown **no** warning.
- The loser's refusal message is the `Waitlist_Action.Refused.AcceptTaken` lookup and the loser was shown
  **exactly one** warning, titled with the `Waitlist_Action.AcceptRefusedTitle` lookup.
- A request the store no longer knows about reports the `Waitlist_Action.Refused.AcceptGone` lookup rather
  than blaming another handler.
- The pinned copy for `Waitlist_Action.Refused.AcceptTaken` says another handler took the request **and**
  directs the handler to a different one.

## G6 — Release and cancel are distinguishable

`MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests` (extend, do not duplicate)

- After Release: status `Pending`, assignee cleared, a `Released` audit entry, and **no** `Canceled`
  audit entry.
- After Cancel: status `Canceled`, reason and canceller recorded, and the assignee path untouched.
- The existing accept / complete / release / note tests stay green.

## G7 — The list is ordered most-urgent-first

`MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests`

- With a set of rows whose due values are known, the built `Source` order equals the expected
  `UrgencyCalculator.OrderMostUrgentFirst` order.
- Overdue rows precede non-overdue rows.
- Among non-overdue rows, the least remaining time is first.
- A row with no stored target time is ordered by its derived due value (`created + max-allotted`), and no
  ordering path throws.
- **Ordering agrees with the display**: the countdown text read back from each card is never more urgent on
  a row than on the row above it.

## G8 — Note round-trips and is localized

`MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailActionTests`

- Saving a note calls `UpdateNoteAsync` and a `NoteUpdated` audit entry exists afterwards, whose
  `Details` is the note text and whose employee number is the author.
- An empty or whitespace-only note is not stored as an empty note and claims no history entry.
- The detail view model exposes history rows after a load, one per audit entry.
- **The history is read from the store**: a stub whose `LoadAuditTrailAsync` returns an entry this session
  never created is rendered (`RequestPage_ReadsTheHistoryFromTheStore_NotJustThisSession`), and an empty read
  is reported as empty rather than hidden (`RequestPage_WithNoHistory_SaysSoRatherThanHiding`).
- Opening a request records its newest entry's time as seen (`RequestPage_OpeningIt_MarksTheRequestsActivitySeen`).
- The refresh cadence is the stated 30 seconds (`RequestPage_RefreshIntervalIsThirtySeconds`).
- Every new visible string resolves through the resource mechanism: a test asserts each new key added
  to `Strings/en-us/Resources.resw` is referenced from the code or markup that shows it, and no new
  literal user-visible string is added to the card or detail markup.

**Live-database evidence (manual, 2026-09-13).** `sp_waitlist_request_audit_list` created on
`mtm_waitlist`; `CALL sp_waitlist_request_audit_list('f0000000-0010-4000-8000-000000000010')` returned that
request's entries oldest-first and an unknown id returned an empty set rather than an error; opening the
request in the built app logged `Audit trail loaded for request 'f0000000-0001-…'. Entries=1.` and rendered
the entry with its detail text. Recorded in `defects/Closed-Medium-Waitlist-RequestHistoryShowsOnlyTheCurrentSession.md` §6.

## G9 — Running application pass (manual, documented)

Per `.github/instructions/winui3-ui-automation.instructions.md`, against the built
`bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`:

1. As **user A**, raise a request. Confirm the card offers Cancel to A and that a second viewer does
   not see it.
2. As **user B** (handler), accept A's request. Confirm the claim, that the card stays on the shared
   list, that Accept is gone for a third viewer, and that **the same card's Accept button has become
   Complete**, with Cancel beside it.
3. Complete the request as B. Confirm it leaves the open list and the badge reads `Done`.
4. Repeat with a second request, cancelling it as the assignee instead. Confirm it leaves the open list
   with a `Canceled` status and that the reason and canceller were recorded. (Release is not reachable
   from the screen — see C7 — so it is covered by the service tests alone.)
5. Confirm the request order leads with the most overdue row, and that a refusal (for example acting on
   a request another handler just took) is reported in plain language rather than doing nothing.
6. Add a note and confirm it appears on the detail page and in the request history — including for a
   request the current session never touched, which is the whole point of the store read.
7. Confirm the request detail page draws **no** action buttons, and that leaving it open for a full
   interval re-reads the request (the log carries `The request page will refresh every 30 seconds while
   it is open.` followed by repeated `Audit trail loaded for request …` lines one interval apart).
8. Confirm the new-message marker is present on a card whose request changed since it was last read, and
   that reading that request clears its marker while the others keep theirs.

**Verified on this build (2026-09-13, maximized app window, UIA dump + screenshots):** steps 2, 4, 6, 7
and 8 above. The card dump read `Pickup: WIP … req=John Koll pill=Waiting [Accept request, Cancel request]`
before the click and `pill=In Progress [Complete request, Cancel request]` after it, on the same row; a row
requested by someone else read `[Accept request]` alone; a row claimed by another handler read `[]`; the
detail page's button dump held only `Back`, `Close Navigation` and `Save note`; and the log showed the audit
read repeating at `04:32:07.286 → 04:32:38.296` and `04:37:02.244 → 04:37:33.262`.

**Known traps** (from the same instruction file, observed on this workstation): reaching the shell needs
a signed-in session, and the app needs **both** `MTM_WAITLIST_DB_CONNECTION_STRING` **and**
`MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` set to get past the startup gate; the window must never be
located by title; and a shell that has launched the app must have its `MTM_*` variables cleared before
running the test suite.

## G10 — Documentation reflects reality

- `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` moves to `FIXED` with the commit and the
  tests that prove it.
- `CHANGELOG.md` carries the two entries the defect file already claims exist, corrected to describe
  the surface as delivered.

## G11 — The post-completion passes hold

The feature shipped, and then four further passes changed behaviour (T031; T032–T036; T037–T041; T042–T049).
Each is a regression the gates above did not cover, so each has its own check. All are unit-level except the
last, which only the running application can prove.

**Message box** — FR-012 (`WaitlistViewDetailActionTests`)

- A click with text in the box stores it on the **first** click, without the box having to lose focus first.
- An unsent draft survives a refresh (`RequestPage_ARefresh_KeepsAnUnsavedMessageOnScreen`).
- A click with nothing to send is answered rather than silently swallowed.
- Anyone signed in can send, not only a handler (`RequestPage_AnyoneSignedInCanSendAMessage`).

**History** — FR-013, FR-017 (`WaitlistViewDetailActionTests`)

- Each row renders the **sender's full name**, not the internal event type
  (`RequestPage_HistoryNamesTheSenderAndNotTheMessageType`).
- An entry with no author is attributed to `System` (`HistoryEntry_WithNoAuthor_IsAttributedToTheSystem`).
- One note renders **once**, not twice, when the store's whole-second timestamp and the session's sub-second
  stamp describe the same entry (T043).

**New-message indicator** — FR-027, FR-028 (`WaitlistViewViewModelActionTests`)

- A lifecycle change with no message raises **no** marker (`Card_LifecycleChangeOnly_ShowsNoMessageMarker`).
- A message the viewer has not read raises it; once read it clears
  (`Card_MessageTheViewerHasNotRead_ShowsTheMarker`, `Card_MessageTheViewerHasAlreadyRead_ShowsNoMarker`).
- The compared value is `last_message_utc`, **verified against the live database**: `NULL` for a request
  accepted with no message, set for one that was written on.

**Card chrome** — FR-029, FR-030 (`WaitlistLineCardMarkupTests`, `RequestStatusPaletteTests`)

- Accept and Complete differ in both colour and glyph (`Card_AcceptAndCompleteAreUnmistakablyDifferent`),
  using `E8FB` and `E930`. The E0–E5 range is deprecated by the Segoe Fluent Icons list, so `E10B` is gone.
- Every action button carries a tooltip drawn from the row, not a literal in markup
  (`Card_ActionButtonsCarryATooltipFromTheRowNotFromALiteral`).
- Every status reaches its own colour, no two share one, both cancelled spellings agree, an unrecognised status
  claims no badge, and every colour clears WCAG AA 4.5:1 against the badge text (`RequestStatusPaletteTests`).

**Card image** — FR-031 (`RequestImagePathPolicyTests`)

- A real resolved path is taken; the request-type **and** sub-type placeholders are refused; the comparison
  tolerates separators, casing and surrounding whitespace — the resolver's `\` against the catalog's `/` is
  exactly the case that would let the defect back in; null and blank are refused as "nothing" rather than
  being reported as a placeholder.

**Running application (G9 repeated for these).** Verified on the built app, 2026-09-13:

- A message sent with `InvokePattern` **without ever focusing the send button** — the exact case that silently
  lost the text before — logged `Message sent.` and produced a single history row naming the sender.
- Accepting a request turned the same card from green `E8FB` to accent `E930`, beside Cancel.
- The status badge read grey `Waiting` then blue `In Progress`.
- The image round trip: open the list, enter the New Request workflow, return, read the tile back. Before the
  fix all three tiles had become the placeholder; after it all three keep their own image and the log carries
  `has no configured image; keeping the row's own image 'pickup_ncm.png' instead of the resolver's
  placeholder.` Only a navigation can prove this one, because that is the only way in.

## G12 — Documentation reflects reality after the fixes

- `defects/Closed-High-NewRequestVisitReplacesCardImagesWithThePlaceholder.md` records the image defect with its
  before/after evidence.
- `WeekendProject/ChangeLog.md` carries an entry per post-completion pass.
- `spec.md` FR-012, FR-013 and FR-028 … FR-031, `plan.md`'s revision section, `data-model.md` §1/§2/§5 and
  `contracts/action-contracts.md` C1/C7/C8 describe the code as it now stands rather than as first planned —
  in particular the two stored-procedure changes the original plan said would not happen.
- `.spec-context.json` carries this work **now**: the spec was reopened on 2026-09-13, the capture synced
  8 new task events (49/49 complete) and closed at `status: implemented`, and the coverage, decisions, verified
  evidence and concerns were recorded through the same writer. While the spec sat at `status: completed` the
  writer refused the task capture (`already at status=completed; not regressing to implement`), which is why the
  task boxes were briefly marked in `tasks.md` directly — see that file's header.
